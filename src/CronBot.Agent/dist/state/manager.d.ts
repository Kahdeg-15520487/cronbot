import { AgentContext, AgentPhase, Checkpoint, BlockerDetection } from '../types.js';
/**
 * State manager for agent context persistence.
 */
export declare class StateManager {
    private statePath;
    private contextPath;
    private checkpointsPath;
    private context;
    private maxCheckpoints;
    private maxDecisions;
    private fileChangeHistory;
    private verificationHistory;
    private toolFailureCount;
    constructor(statePath: string);
    /**
     * Create default agent context.
     */
    private createDefaultContext;
    /**
     * Initialize state manager.
     */
    initialize(): Promise<void>;
    /**
     * Get current context.
     */
    getContext(): AgentContext;
    /**
     * Update context.
     */
    updateContext(updates: Partial<AgentContext>): Promise<void>;
    /**
     * Set current phase.
     */
    setPhase(phase: AgentPhase): Promise<void>;
    /**
     * Add active file.
     */
    addActiveFile(filePath: string): Promise<void>;
    /**
     * Remove active file.
     */
    removeActiveFile(filePath: string): Promise<void>;
    /**
     * Record a decision.
     */
    recordDecision(decision: string, reason?: string, importanceScore?: number): Promise<void>;
    /**
     * Update token count.
     */
    updateTokenCount(count: number): void;
    /**
     * Check if context compaction is needed.
     */
    needsCompaction(): boolean;
    /**
     * Compact context by summarizing old decisions.
     */
    compactContext(): Promise<void>;
    /**
     * Create a checkpoint.
     */
    createCheckpoint(lastOperation?: unknown): Promise<Checkpoint>;
    /**
     * Restore from checkpoint.
     */
    restoreCheckpoint(checkpointId: string): Promise<boolean>;
    /**
     * Get latest checkpoint.
     */
    getLatestCheckpoint(): Promise<Checkpoint | null>;
    /**
     * Track file change for loop detection.
     */
    trackFileChange(filePath: string, content: string): void;
    /**
     * Detect code loops (file changes back and forth).
     */
    detectCodeLoop(): BlockerDetection;
    /**
     * Track verification attempt.
     */
    trackVerification(success: boolean, message: string): void;
    /**
     * Detect verification loops.
     */
    detectVerificationLoop(): BlockerDetection;
    /**
     * Track tool failure.
     */
    trackToolFailure(toolName: string): void;
    /**
     * Detect tool failure pattern.
     */
    detectToolFailure(): BlockerDetection;
    /**
     * Run all blocker detections.
     */
    detectBlockers(): BlockerDetection | null;
    /**
     * Reset failure counters.
     */
    resetFailureCounters(): void;
    /**
     * Save context to file.
     */
    private saveContext;
    /**
     * Load context from file.
     */
    private loadContext;
    /**
     * Clean up old checkpoints.
     */
    private cleanupOldCheckpoints;
    /**
     * Simple content hash.
     */
    private hashContent;
}
//# sourceMappingURL=manager.d.ts.map