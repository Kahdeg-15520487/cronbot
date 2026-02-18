import fs from 'fs/promises';
import path from 'path';
import { v4 as uuidv4 } from 'uuid';
import { AgentContext, AgentPhase, Checkpoint, Decision, BlockerDetection, BlockerType } from '../types.js';
import { createLogger } from '../logger.js';

const logger = createLogger('state');

/**
 * State manager for agent context persistence.
 */
export class StateManager {
  private statePath: string;
  private contextPath: string;
  private checkpointsPath: string;
  private context: AgentContext;
  private maxCheckpoints: number = 10;
  private maxDecisions: number = 50;
  private fileChangeHistory: Map<string, string[]> = new Map();
  private verificationHistory: Array<{ timestamp: string; success: boolean; message: string }> = [];
  private toolFailureCount: Map<string, number> = new Map();

  constructor(statePath: string) {
    this.statePath = statePath;
    this.contextPath = path.join(statePath, 'context.json');
    this.checkpointsPath = path.join(statePath, 'checkpoints');
    this.context = this.createDefaultContext();
  }

  /**
   * Create default agent context.
   */
  private createDefaultContext(): AgentContext {
    return {
      phase: AgentPhase.Initializing,
      activeFiles: [],
      recentDecisions: [],
      tokenCount: 0,
      maxTokens: 200000,
    };
  }

  /**
   * Initialize state manager.
   */
  async initialize(): Promise<void> {
    await fs.mkdir(this.statePath, { recursive: true });
    await fs.mkdir(this.checkpointsPath, { recursive: true });

    try {
      const savedContext = await this.loadContext();
      if (savedContext) {
        this.context = savedContext;
        logger.info({ phase: this.context.phase }, 'Loaded saved context');
      }
    } catch (error) {
      logger.info('No saved context found, starting fresh');
    }
  }

  /**
   * Get current context.
   */
  getContext(): AgentContext {
    return { ...this.context };
  }

  /**
   * Update context.
   */
  async updateContext(updates: Partial<AgentContext>): Promise<void> {
    this.context = { ...this.context, ...updates };
    await this.saveContext();
  }

  /**
   * Set current phase.
   */
  async setPhase(phase: AgentPhase): Promise<void> {
    const previousPhase = this.context.phase;
    this.context.phase = phase;
    logger.info({ previousPhase, newPhase: phase }, 'Phase changed');
    await this.saveContext();
  }

  /**
   * Add active file.
   */
  async addActiveFile(filePath: string): Promise<void> {
    if (!this.context.activeFiles.includes(filePath)) {
      this.context.activeFiles.push(filePath);
      await this.saveContext();
    }
  }

  /**
   * Remove active file.
   */
  async removeActiveFile(filePath: string): Promise<void> {
    this.context.activeFiles = this.context.activeFiles.filter(f => f !== filePath);
    await this.saveContext();
  }

  /**
   * Record a decision.
   */
  async recordDecision(decision: string, reason?: string, importanceScore: number = 5): Promise<void> {
    const decisionRecord: Decision = {
      decision,
      reason,
      importanceScore,
      timestamp: new Date().toISOString(),
    };

    this.context.recentDecisions.push(decisionRecord);

    // Keep only the most recent decisions
    if (this.context.recentDecisions.length > this.maxDecisions) {
      this.context.recentDecisions = this.context.recentDecisions.slice(-this.maxDecisions);
    }

    await this.saveContext();
    logger.debug({ decision }, 'Recorded decision');
  }

  /**
   * Update token count.
   */
  updateTokenCount(count: number): void {
    this.context.tokenCount = count;
  }

  /**
   * Check if context compaction is needed.
   */
  needsCompaction(): boolean {
    return this.context.tokenCount > this.context.maxTokens * 0.8;
  }

  /**
   * Compact context by summarizing old decisions.
   */
  async compactContext(): Promise<void> {
    logger.info('Starting context compaction');

    // Keep only high-importance recent decisions
    const importantDecisions = this.context.recentDecisions
      .filter(d => d.importanceScore >= 7)
      .slice(-10);

    this.context.recentDecisions = importantDecisions;
    this.context.tokenCount = 0; // Reset after compaction

    await this.saveContext();
    logger.info({ decisionCount: importantDecisions.length }, 'Context compacted');
  }

  /**
   * Create a checkpoint.
   */
  async createCheckpoint(lastOperation?: unknown): Promise<Checkpoint> {
    const checkpoint: Checkpoint = {
      id: uuidv4(),
      timestamp: new Date().toISOString(),
      phase: this.context.phase,
      context: { ...this.context },
      lastOperation: lastOperation as undefined,
    };

    const checkpointPath = path.join(this.checkpointsPath, `${checkpoint.id}.json`);
    await fs.writeFile(checkpointPath, JSON.stringify(checkpoint, null, 2));

    // Clean up old checkpoints
    await this.cleanupOldCheckpoints();

    logger.info({ checkpointId: checkpoint.id, phase: checkpoint.phase }, 'Checkpoint created');
    return checkpoint;
  }

  /**
   * Restore from checkpoint.
   */
  async restoreCheckpoint(checkpointId: string): Promise<boolean> {
    try {
      const checkpointPath = path.join(this.checkpointsPath, `${checkpointId}.json`);
      const data = await fs.readFile(checkpointPath, 'utf-8');
      const checkpoint: Checkpoint = JSON.parse(data);

      this.context = checkpoint.context;
      await this.saveContext();

      logger.info({ checkpointId, phase: checkpoint.phase }, 'Restored from checkpoint');
      return true;
    } catch (error) {
      logger.error({ checkpointId, error }, 'Failed to restore checkpoint');
      return false;
    }
  }

  /**
   * Get latest checkpoint.
   */
  async getLatestCheckpoint(): Promise<Checkpoint | null> {
    try {
      const files = await fs.readdir(this.checkpointsPath);
      const checkpoints = files
        .filter(f => f.endsWith('.json'))
        .map(f => f.replace('.json', ''))
        .sort()
        .reverse();

      if (checkpoints.length === 0) {
        return null;
      }

      const latestPath = path.join(this.checkpointsPath, `${checkpoints[0]}.json`);
      const data = await fs.readFile(latestPath, 'utf-8');
      return JSON.parse(data);
    } catch (error) {
      return null;
    }
  }

  /**
   * Track file change for loop detection.
   */
  trackFileChange(filePath: string, content: string): void {
    const hash = this.hashContent(content);
    const history = this.fileChangeHistory.get(filePath) || [];
    history.push(hash);

    // Keep last 10 changes
    if (history.length > 10) {
      history.shift();
    }

    this.fileChangeHistory.set(filePath, history);
  }

  /**
   * Detect code loops (file changes back and forth).
   */
  detectCodeLoop(): BlockerDetection {
    for (const [filePath, history] of this.fileChangeHistory) {
      if (history.length >= 4) {
        // Check for alternating pattern
        const last4 = history.slice(-4);
        if (last4[0] === last4[2] && last4[1] === last4[3] && last4[0] !== last4[1]) {
          return {
            detected: true,
            type: BlockerType.CodeLoop,
            severity: 'high',
            description: `File ${filePath} is being changed back and forth between two states`,
            suggestedAction: 'Review the conflicting requirements and make a definitive decision',
          };
        }
      }
    }

    return { detected: false };
  }

  /**
   * Track verification attempt.
   */
  trackVerification(success: boolean, message: string): void {
    this.verificationHistory.push({
      timestamp: new Date().toISOString(),
      success,
      message,
    });

    // Keep last 20 verifications
    if (this.verificationHistory.length > 20) {
      this.verificationHistory.shift();
    }
  }

  /**
   * Detect verification loops.
   */
  detectVerificationLoop(): BlockerDetection {
    const recentVerifications = this.verificationHistory.slice(-5);

    if (recentVerifications.length >= 5) {
      const allFailed = recentVerifications.every(v => !v.success);
      const sameError = new Set(recentVerifications.map(v => v.message)).size === 1;

      if (allFailed && sameError) {
        return {
          detected: true,
          type: BlockerType.VerificationLoop,
          severity: 'high',
          description: `Verification repeatedly failing with same error: ${recentVerifications[0].message}`,
          suggestedAction: 'Try a different approach or escalate for human assistance',
        };
      }
    }

    return { detected: false };
  }

  /**
   * Track tool failure.
   */
  trackToolFailure(toolName: string): void {
    const count = this.toolFailureCount.get(toolName) || 0;
    this.toolFailureCount.set(toolName, count + 1);
  }

  /**
   * Detect tool failure pattern.
   */
  detectToolFailure(): BlockerDetection {
    for (const [toolName, count] of this.toolFailureCount) {
      if (count >= 3) {
        return {
          detected: true,
          type: BlockerType.ToolFailure,
          severity: 'medium',
          description: `Tool ${toolName} has failed ${count} times`,
          suggestedAction: 'Check tool configuration or try alternative approach',
        };
      }
    }

    return { detected: false };
  }

  /**
   * Run all blocker detections.
   */
  detectBlockers(): BlockerDetection | null {
    const codeLoop = this.detectCodeLoop();
    if (codeLoop.detected) return codeLoop;

    const verificationLoop = this.detectVerificationLoop();
    if (verificationLoop.detected) return verificationLoop;

    const toolFailure = this.detectToolFailure();
    if (toolFailure.detected) return toolFailure;

    return null;
  }

  /**
   * Reset failure counters.
   */
  resetFailureCounters(): void {
    this.toolFailureCount.clear();
    this.verificationHistory = this.verificationHistory.filter(v => v.success);
  }

  /**
   * Save context to file.
   */
  private async saveContext(): Promise<void> {
    await fs.writeFile(this.contextPath, JSON.stringify(this.context, null, 2));
  }

  /**
   * Load context from file.
   */
  private async loadContext(): Promise<AgentContext | null> {
    try {
      const data = await fs.readFile(this.contextPath, 'utf-8');
      return JSON.parse(data);
    } catch {
      return null;
    }
  }

  /**
   * Clean up old checkpoints.
   */
  private async cleanupOldCheckpoints(): Promise<void> {
    const files = await fs.readdir(this.checkpointsPath);
    const checkpoints = files
      .filter(f => f.endsWith('.json'))
      .sort();

    while (checkpoints.length > this.maxCheckpoints) {
      const oldest = checkpoints.shift();
      if (oldest) {
        await fs.unlink(path.join(this.checkpointsPath, oldest));
      }
    }
  }

  /**
   * Simple content hash.
   */
  private hashContent(content: string): string {
    let hash = 0;
    for (let i = 0; i < content.length; i++) {
      const char = content.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash;
    }
    return hash.toString(16);
  }
}
