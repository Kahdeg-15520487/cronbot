import { query } from "@anthropic-ai/claude-agent-sdk";
import type { AgentConfig } from "./config.js";
import { StateManager } from "./state.js";

export interface AgentOptions {
  config: AgentConfig;
  prompt?: string;
  targetDir?: string;
}

/**
 * CronBot Agent - Autonomous AI development assistant
 *
 * Supports two modes:
 * - Single-task: Run once with a specific prompt
 * - Daemon: Continuously poll for tasks via MCP and execute them
 */
export class Agent {
  private config: AgentConfig;
  private state: StateManager;
  private agentId: string;
  private running: boolean = false;

  constructor(options: AgentOptions) {
    this.config = options.config;
    this.agentId = options.config.agentId ?? crypto.randomUUID();
    this.state = new StateManager(options.config.statePath, this.agentId);
  }

  async initialize(): Promise<void> {
    await this.state.initialize();
    this.state.addEntry({
      type: "task_start",
      message: "Agent initialized",
      metadata: {
        agentId: this.agentId,
        projectId: this.config.projectId,
        autonomyLevel: this.config.autonomyLevel,
      },
    });
    await this.state.save();
  }

  /**
   * Run agent in single-task mode with a specific prompt
   */
  async runOnce(prompt: string, targetDir?: string): Promise<string> {
    const cwd = targetDir ?? this.config.workspacePath;

    this.state.addEntry({
      type: "task_start",
      message: `Single task: ${prompt.slice(0, 100)}...`,
      metadata: { cwd },
    });
    await this.state.save();

    const result = await this.executeWithAgent(prompt, cwd);

    this.state.addEntry({
      type: "task_complete",
      message: "Task completed",
      metadata: { resultLength: result.length },
    });
    await this.state.save();

    return result;
  }

  /**
   * Run agent in daemon mode - continuously poll for tasks via MCP
   */
  async runDaemon(): Promise<void> {
    this.running = true;
    const pollInterval = this.config.taskPollIntervalMs;
    const projectId = this.config.projectId;

    console.log(`Agent ${this.agentId} starting in daemon mode`);
    console.log(`Project: ${projectId ?? "all projects"}`);
    console.log(`Poll interval: ${pollInterval}ms`);
    console.log("---");

    while (this.running) {
      try {
        await this.pollAndExecuteTask();
      } catch (error) {
        const timestamp = new Date().toISOString();
        const errorDetails = this.formatError(error);
        console.error(`[${timestamp}] Error in daemon loop:`);
        console.error(`  Type: ${errorDetails.errorType}`);
        console.error(`  Message: ${errorDetails.shortMessage}`);
        if (errorDetails.stack) {
          console.error(`  Stack:`);
          console.error(errorDetails.stack);
        }
        console.error(`  Raw: ${errorDetails.rawError}`);
        this.state.addEntry({
          type: "error",
          message: errorDetails.shortMessage,
          metadata: {
            timestamp,
            errorType: errorDetails.errorType,
            stack: errorDetails.stack,
            rawError: errorDetails.rawError,
          },
        });
        await this.state.save();
      }

      // Wait before next poll
      await this.sleep(pollInterval);
    }
  }

  /**
   * Stop the daemon loop
   */
  stop(): void {
    this.running = false;
  }

  /**
   * Poll for next task via MCP and execute it
   */
  private async pollAndExecuteTask(): Promise<void> {
    const cwd = this.config.workspacePath;
    const projectId = this.config.projectId;

    // Build prompt that tells agent to check for and work on tasks
    const prompt = this.buildTaskPollingPrompt(projectId);

    this.state.addEntry({
      type: "task_start",
      message: "Polling for tasks",
      metadata: { projectId },
    });
    await this.state.save();

    const result = await this.executeWithAgent(prompt, cwd);

    // Check if a task was actually worked on
    if (result.includes("No tasks ready for work")) {
      console.log("No tasks available, waiting...");
    } else {
      console.log("Task cycle completed");
      this.state.addEntry({
        type: "task_complete",
        message: "Task cycle completed",
        metadata: { result: result.slice(0, 500) },
      });
      await this.state.save();
    }
  }

  /**
   * Build the prompt for task polling
   */
  private buildTaskPollingPrompt(projectId?: string): string {
    const projectFilter = projectId
      ? `for project ${projectId}`
      : "from any project";

    // Build git config section
    const gitConfig = this.buildGitConfigSection();

    return `You are CronBot Agent, an autonomous AI development assistant.

Your task is to check for available work and execute it using the Kanban tools available via MCP.

## Instructions:

1. **Check for tasks**: Use the \`get_next_task\` MCP tool ${projectFilter} to find the next task ready to be worked on.

2. **If no tasks**: If the response indicates no tasks are available, respond with "No tasks ready for work" and stop.

3. **If task found**:
   - Move the task to "InProgress" status using \`move_task\`
   - Read and understand the task description
${gitConfig}
   - Execute the work requested in the task
   - When complete:
     - Commit your changes with a descriptive message
     - Push the branch to remote
     - Update the task with the branch name and any PR URL
     - Move the task to "Done" status
   - If blocked, move to "Blocked" status and add a comment explaining why

4. **Important**:
   - Work autonomously within your autonomy level (${this.config.autonomyLevel})
   - Use file tools (Read, Edit, Write) and Bash as needed
   - Be thorough but efficient
   - Add comments to the task for important updates

## Current Context:
- Agent ID: ${this.agentId}
- Project ID: ${projectId ?? "not specified"}
- Autonomy Level: ${this.config.autonomyLevel} (0=read-only, 1=cautious, 2=balanced, 3=full)
- Workspace: ${this.config.workspacePath}

Start by checking for the next available task.`;
  }

  /**
   * Build git configuration instructions
   */
  private buildGitConfigSection(): string {
    const hasGitConfig = this.config.giteaUrl || this.config.repoUrl;

    if (!hasGitConfig) {
      return `   - Work on the files in the workspace`;
    }

    const repoUrl = this.config.repoUrl
      ? this.config.repoUrl
      : this.config.giteaUsername && this.config.giteaPassword && this.config.giteaUrl
        ? `${this.config.giteaUrl.replace("://", `://${this.config.giteaUsername}:${this.config.giteaPassword}@`)}/PROJECT_SLUG.git`
        : null;

    if (!repoUrl) {
      return `   - Work on the files in the workspace`;
    }

    return `   - **Git Workflow (MANDATORY)**:
     a. Initialize git if not already done: \`git init\`
     b. Configure git identity: \`git config user.email "agent@cronbot.local"\` and \`git config user.name "CronBot Agent"\`
     c. Clone the repository if needed (or ensure you're in the repo directory)
     d. Fetch latest: \`git fetch origin\`
     e. Create a feature branch for this task: \`git checkout -b task/TASK-NUMBER-TASK-SLUG\`
        - Branch naming: use task number from the task data (e.g., task/42-add-login-button)
        - If task type is "bug", use "fix/" prefix instead (e.g., fix/42-fix-login-crash)
     f. Make sure you're on the new branch before making changes: \`git branch\`
     g. Stage changes as you work: \`git add .\`
     h. Commit frequently with meaningful messages: \`git commit -m "descriptive message"\``;
  }

  /**
   * Execute a prompt using the Claude Agent SDK
   */
  private async executeWithAgent(prompt: string, cwd: string): Promise<string> {
    const allowedTools = this.getAllowedTools();
    const mcpServers = this.buildMcpServersConfig();

    let result = "";

    try {
      for await (const message of query({
        prompt,
        options: {
          model: this.config.anthropicModel,
          cwd,
          allowedTools,
          mcpServers,
          // Bypass permissions for autonomous agent operation (requires non-root user)
          permissionMode: "bypassPermissions",
          // API key is read from ANTHROPIC_API_KEY env var
          // Base URL is read from ANTHROPIC_BASE_URL env var
        },
      })) {
        if (message.type === "result") {
          if (message.subtype === "success") {
            result = message.result;
          } else {
            const timestamp = new Date().toISOString();
            const errorSubtype = message.subtype;
            const errorDetails = message.errors?.join("; ") ?? "Unknown error";
            console.error(`[${timestamp}] Agent returned error result:`);
            console.error(`  Subtype: ${errorSubtype}`);
            console.error(`  Errors: ${errorDetails}`);
            this.state.addEntry({
              type: "error",
              message: `Agent error: ${errorSubtype} - ${errorDetails}`,
              metadata: {
                timestamp,
                subtype: errorSubtype,
                errors: message.errors,
                cwd,
              },
            });
            result = `Error: ${errorSubtype} - ${errorDetails}`;
          }
        }
      }
    } catch (error) {
      const timestamp = new Date().toISOString();
      const errorDetails = this.formatError(error);
      console.error(`[${timestamp}] Error in executeWithAgent:`);
      console.error(`  Type: ${errorDetails.errorType}`);
      console.error(`  Message: ${errorDetails.shortMessage}`);
      if (errorDetails.stack) {
        console.error(`  Stack:\n${errorDetails.stack}`);
      }
      this.state.addEntry({
        type: "error",
        message: `executeWithAgent failed: ${errorDetails.shortMessage}`,
        metadata: {
          timestamp,
          errorType: errorDetails.errorType,
          stack: errorDetails.stack,
          cwd,
          allowedTools,
        },
      });
      await this.state.save();
      throw error;
    }

    return result;
  }

  /**
   * Build MCP servers config in the format expected by the SDK
   */
  private buildMcpServersConfig(): Record<string, { type: "http"; url: string }> | undefined {
    if (!this.config.mcpServers) {
      return undefined;
    }

    // Convert config format to SDK format (use HTTP transport)
    const servers: Record<string, { type: "http"; url: string }> = {};
    for (const [name, config] of Object.entries(this.config.mcpServers)) {
      if (typeof config === "object" && config !== null && "url" in config) {
        servers[name] = {
          type: "http",
          url: (config as { url: string }).url,
        };
      }
    }
    return Object.keys(servers).length > 0 ? servers : undefined;
  }

  /**
   * Get allowed tools based on autonomy level
   */
  private getAllowedTools(): string[] {
    const baseTools = ["Read", "Glob", "Grep"];
    const editTools = ["Edit", "Write"];
    const shellTools = ["Bash"];

    switch (this.config.autonomyLevel) {
      case 0: // Read-only
        return [...baseTools];
      case 1: // Cautious - read only, plans before executing
        return [...baseTools];
      case 2: // Balanced - most operations
        return [...baseTools, ...editTools, ...shellTools];
      case 3: // Full autonomy
        return [...baseTools, ...editTools, ...shellTools];
      default:
        return [...baseTools, ...editTools, ...shellTools];
    }
  }

  private sleep(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  /**
   * Format error for detailed logging
   */
  private formatError(error: unknown): {
    shortMessage: string;
    errorType: string;
    stack?: string;
    rawError: string;
  } {
    if (error instanceof Error) {
      return {
        shortMessage: error.message,
        errorType: error.constructor.name,
        stack: error.stack,
        rawError: JSON.stringify({
          name: error.name,
          message: error.message,
          stack: error.stack,
          cause: error.cause,
        }, null, 2),
      };
    }

    const strError = String(error);
    return {
      shortMessage: strError,
      errorType: typeof error,
      rawError: strError,
    };
  }

  getState(): ReturnType<StateManager["getState"]> {
    return this.state.getState();
  }

  getAgentId(): string {
    return this.agentId;
  }
}
