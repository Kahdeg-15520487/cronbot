import Anthropic from '@anthropic-ai/sdk';
import {
  AgentConfig,
  AgentPhase,
  AgentStatus,
  AutonomyLevel,
  Task,
  OperationResult,
  BlockerDetection,
} from './types.js';
import { StateManager } from './state/manager.js';
import { McpRegistry } from './mcp/registry.js';
import { KanbanTools } from './mcp/kanban-tools.js';
import { SkillExecutor } from './skills/executor.js';
import { createLogger } from './logger.js';

const logger = createLogger('agent');

/**
 * Main agent class that orchestrates task execution.
 */
export class Agent {
  private config: AgentConfig;
  private stateManager: StateManager;
  private mcpRegistry: McpRegistry;
  private kanbanTools: KanbanTools;
  private skillExecutor: SkillExecutor;
  private anthropic: Anthropic;
  private status: AgentStatus = AgentStatus.Idle;
  private currentTask: Task | null = null;
  private shouldStop: boolean = false;
  private apiClient: ApiClient;

  constructor(config: AgentConfig) {
    this.config = config;
    this.stateManager = new StateManager(config.statePath);
    this.mcpRegistry = new McpRegistry(config.autonomyLevel);
    this.kanbanTools = new KanbanTools(config.kanbanUrl, config.projectId);
    this.skillExecutor = new SkillExecutor(config.skillsPath, config.workspacePath);
    this.anthropic = new Anthropic({
      apiKey: config.anthropicApiKey,
      baseURL: config.anthropicBaseUrl,
    });
    this.apiClient = new ApiClient(config.kanbanUrl);
  }

  /**
   * Initialize the agent.
   */
  async initialize(): Promise<void> {
    logger.info({ agentId: this.config.agentId }, 'Initializing agent');

    // Initialize state manager
    await this.stateManager.initialize();

    // Initialize skills
    await this.skillExecutor.initialize();

    // Connect to system MCPs
    await this.connectSystemMcps();

    // Restore from checkpoint if available
    const checkpoint = await this.stateManager.getLatestCheckpoint();
    if (checkpoint) {
      logger.info({ checkpointId: checkpoint.id }, 'Restoring from checkpoint');
    }

    await this.stateManager.setPhase(AgentPhase.ReadingContext);

    logger.info('Agent initialized successfully');
  }

  /**
   * Connect to system MCP servers.
   */
  private async connectSystemMcps(): Promise<void> {
    // TODO: Load MCP configurations from registry service
    // For now, we'll skip MCP connections until the services are implemented

    logger.info('System MCP connections skipped (not yet implemented)');
  }

  /**
   * Start the agent's main loop.
   */
  async start(): Promise<void> {
    logger.info('Starting agent main loop');
    this.shouldStop = false;
    this.status = AgentStatus.Working;

    while (!this.shouldStop) {
      try {
        // Check for blockers
        const blocker = this.stateManager.detectBlockers();
        if (blocker) {
          await this.handleBlocker(blocker);
          continue;
        }

        // Get next task
        const task = await this.getNextTask();

        if (!task) {
          // No task available, wait and retry
          this.status = AgentStatus.Idle;
          await this.stateManager.setPhase(AgentPhase.Idle);
          logger.debug('No tasks available, waiting...');
          await this.sleep(30000); // Wait 30 seconds before checking again
          continue;
        }

        // Execute task
        this.status = AgentStatus.Working;
        await this.executeTask(task);

      } catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        logger.error({ error: message }, 'Error in main loop');
        this.status = AgentStatus.Error;

        // Create checkpoint for recovery
        await this.stateManager.createCheckpoint();

        // Wait before retrying
        await this.sleep(10000);
        this.status = AgentStatus.Working;
      }
    }

    this.status = AgentStatus.Terminated;
    logger.info('Agent stopped');
  }

  /**
   * Stop the agent.
   */
  async stop(): Promise<void> {
    logger.info('Stopping agent');
    this.shouldStop = true;

    // Create final checkpoint
    await this.stateManager.createCheckpoint();

    // Disconnect MCPs
    await this.mcpRegistry.disconnectAll();
  }

  /**
   * Get next task from Kanban service.
   */
  private async getNextTask(): Promise<Task | null> {
    try {
      const response = await this.apiClient.get<Task | null>(
        `/projects/${this.config.projectId}/tasks/next`
      );

      if (response.success && response.data) {
        this.currentTask = response.data;
        return response.data;
      }

      return null;
    } catch (error) {
      logger.warn({ error }, 'Failed to get next task');
      return null;
    }
  }

  /**
   * Execute a task using Claude.
   */
  private async executeTask(task: Task): Promise<void> {
    logger.info({ taskId: task.id, title: task.title }, 'Executing task');

    await this.stateManager.setPhase(AgentPhase.Planning);
    await this.stateManager.recordDecision(`Started task: ${task.title}`, undefined, 5);

    // Update task status to in_progress
    await this.apiClient.patch(`/tasks/${task.id}`, { status: 'in_progress' });

    // Create checkpoint before starting work
    await this.stateManager.createCheckpoint();

    // Build context for Claude
    const context = this.buildTaskContext(task);

    // Get available tools
    const tools = this.buildClaudeTools();

    await this.stateManager.setPhase(AgentPhase.Executing);

    try {
      // Stream response from Claude
      const stream = this.anthropic.messages.stream({
        model: 'claude-opus-4-20250514',
        max_tokens: 4096,
        system: this.buildSystemPrompt(),
        messages: [
          {
            role: 'user',
            content: context,
          },
        ],
        tools,
      });

      // Wait for final message
      const message = await stream.finalMessage();

      // Process tool calls
      await this.processToolCalls(message);

      // Mark task as complete
      await this.stateManager.setPhase(AgentPhase.Verifying);
      await this.stateManager.recordDecision(`Completed task: ${task.title}`, undefined, 7);

      // Update task status
      await this.apiClient.patch(`/tasks/${task.id}`, { status: 'done' });

      logger.info({ taskId: task.id }, 'Task completed successfully');

    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      logger.error({ taskId: task.id, error: message }, 'Task execution failed');

      await this.stateManager.recordDecision(`Task failed: ${message}`, undefined, 3);

      // Update task status to blocked
      await this.apiClient.patch(`/tasks/${task.id}`, {
        status: 'blocked',
        blockedReason: message,
      });

      throw error;
    }
  }

  /**
   * Build task context for Claude.
   */
  private buildTaskContext(task: Task): string {
    const context = this.stateManager.getContext();
    const recentDecisions = context.recentDecisions
      .slice(-5)
      .map(d => `- ${d.decision}`)
      .join('\n');

    const mcpTools = this.mcpRegistry.getAllTools()
      .map(t => `- ${t.tool.name}: ${t.tool.description}`)
      .join('\n') || 'No MCP tools available';

    const kanbanTools = this.kanbanTools.getTools()
      .map(t => `- ${t.name}: ${t.description}`)
      .join('\n');

    const skills = this.skillExecutor.getAvailableSkills()
      .map(s => `- ${s.meta.name}: ${s.meta.description}`)
      .join('\n') || 'No skills available';

    return `
## Task Information
- **ID**: ${task.id}
- **Number**: #${task.number}
- **Title**: ${task.title}
- **Description**: ${task.description || 'No description provided'}
- **Type**: ${task.type}

## Recent Decisions
${recentDecisions || 'No recent decisions'}

## Available MCP Tools
${mcpTools}

## Available Kanban Tools (for task management)
${kanbanTools}

## Available Skills
${skills}

Please complete this task. Start by creating a plan, then execute it step by step.
`;
  }

  /**
   * Build system prompt for Claude.
   */
  private buildSystemPrompt(): string {
    const autonomyDescription = this.getAutonomyDescription();

    return `You are CronBot, an autonomous AI development assistant. You work on software development tasks autonomously.

## Autonomy Level: ${this.config.autonomyLevel} (${autonomyDescription})

## Guidelines
1. Always read and understand existing code before making changes
2. Make small, focused changes and verify after each change
3. Write clear commit messages describing what and why
4. If you encounter blockers, report them clearly
5. Document important decisions you make

## Available Actions
- Use MCP tools to interact with the filesystem, git, and other systems
- Execute skills for specialized operations
- Request approval for sensitive operations based on autonomy level

## Error Handling
- If an operation fails, try alternative approaches
- After 3 failures, report as blocked
- Create checkpoints before significant changes
`;
  }

  /**
   * Get description of current autonomy level.
   */
  private getAutonomyDescription(): string {
    switch (this.config.autonomyLevel) {
      case AutonomyLevel.Reactive:
        return 'Read-only, responds when spoken to';
      case AutonomyLevel.Cautious:
        return 'Read/analyze, plans before executing, approval on writes';
      case AutonomyLevel.Balanced:
        return 'Autonomous on tasks, approval on major changes';
      case AutonomyLevel.FullAutonomy:
        return 'Full sandbox autonomy, only security-critical ops need approval';
      default:
        return 'Unknown';
    }
  }

  /**
   * Build Claude tools from MCP tools and built-in tools.
   */
  private buildClaudeTools(): Anthropic.Tool[] {
    const tools: Anthropic.Tool[] = [];

    // Add MCP tools
    for (const { tool } of this.mcpRegistry.getAllTools()) {
      tools.push({
        name: tool.name,
        description: tool.description,
        input_schema: tool.inputSchema as Anthropic.Tool['input_schema'],
      });
    }

    // Add Kanban tools
    for (const tool of this.kanbanTools.getTools()) {
      tools.push({
        name: tool.name,
        description: tool.description,
        input_schema: tool.inputSchema as Anthropic.Tool['input_schema'],
      });
    }

    // Add skill execution tool
    tools.push({
      name: 'execute_skill',
      description: 'Execute a Python skill script',
      input_schema: {
        type: 'object',
        properties: {
          skill_name: {
            type: 'string',
            description: 'Name of the skill to execute',
          },
          arguments: {
            type: 'object',
            description: 'Arguments to pass to the skill',
          },
        },
        required: ['skill_name'],
      },
    });

    return tools;
  }

  /**
   * Process tool calls from Claude's response.
   */
  private async processToolCalls(message: Anthropic.Message): Promise<void> {
    for (const block of message.content) {
      if (block.type === 'tool_use') {
        logger.info({ tool: block.name }, 'Processing tool call');

        let result: OperationResult;

        if (block.name === 'execute_skill') {
          const args = block.input as { skill_name: string; arguments?: Record<string, unknown> };
          result = await this.skillExecutor.execute(args.skill_name, args.arguments || {});
        } else if (this.kanbanTools.hasTool(block.name)) {
          result = await this.kanbanTools.callTool(block.name, block.input as Record<string, unknown>);
        } else {
          result = await this.mcpRegistry.callTool(block.name, block.input as Record<string, unknown>);
        }

        // Check if approval is needed
        if (result.requiresApproval) {
          logger.info({ tool: block.name, reason: result.approvalReason }, 'Tool call requires approval');
          // In a real implementation, this would wait for user approval
          continue;
        }

        // Track failures for blocker detection
        if (!result.success) {
          this.stateManager.trackToolFailure(block.name);
        }

        logger.debug({ tool: block.name, success: result.success }, 'Tool call completed');
      }
    }
  }

  /**
   * Handle a detected blocker.
   */
  private async handleBlocker(blocker: BlockerDetection): Promise<void> {
    logger.warn({ blocker }, 'Blocker detected');

    this.status = AgentStatus.Blocked;
    await this.stateManager.setPhase(AgentPhase.Blocked);

    // Report blocker to API
    await this.apiClient.post(`/agents/${this.config.agentId}/blockers`, {
      type: blocker.type,
      severity: blocker.severity,
      description: blocker.description,
      suggestedAction: blocker.suggestedAction,
    });

    // Wait for resolution
    await this.sleep(30000);

    // Reset failure counters and retry
    this.stateManager.resetFailureCounters();
    this.status = AgentStatus.Working;
  }

  /**
   * Get current agent status.
   */
  getStatus(): AgentStatus {
    return this.status;
  }

  /**
   * Get current task.
   */
  getCurrentTask(): Task | null {
    return this.currentTask;
  }

  /**
   * Sleep for a specified duration.
   */
  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

/**
 * Simple API client for communicating with CronBot services.
 */
class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  async get<T>(path: string): Promise<OperationResult<T>> {
    try {
      const response = await fetch(`${this.baseUrl}${path}`);
      const data = await response.json() as T;
      return { success: response.ok, data };
    } catch (error) {
      return { success: false, error: String(error) };
    }
  }

  async post<T>(path: string, body: unknown): Promise<OperationResult<T>> {
    try {
      const response = await fetch(`${this.baseUrl}${path}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      const data = await response.json() as T;
      return { success: response.ok, data };
    } catch (error) {
      return { success: false, error: String(error) };
    }
  }

  async patch<T>(path: string, body: unknown): Promise<OperationResult<T>> {
    try {
      const response = await fetch(`${this.baseUrl}${path}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      const data = await response.json() as T;
      return { success: response.ok, data };
    } catch (error) {
      return { success: false, error: String(error) };
    }
  }
}
