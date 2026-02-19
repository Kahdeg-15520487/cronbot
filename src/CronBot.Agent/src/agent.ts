import { query } from "@anthropic-ai/claude-agent-sdk";
import type { AgentConfig } from "./config.js";
import { StateManager } from "./state.js";

export interface AgentOptions {
  config: AgentConfig;
  prompt: string;
  targetDir?: string;
}

export class Agent {
  private config: AgentConfig;
  private state: StateManager;
  private agentId: string;

  constructor(options: AgentOptions) {
    this.config = options.config;
    this.agentId = crypto.randomUUID();
    this.state = new StateManager(options.config.statePath, this.agentId);
  }

  async initialize(): Promise<void> {
    await this.state.initialize();
    this.state.addEntry({
      type: "task_start",
      message: "Agent initialized",
    });
    await this.state.save();
  }

  async run(prompt: string, targetDir?: string): Promise<string> {
    const cwd = targetDir ?? this.config.workspacePath;

    this.state.addEntry({
      type: "task_start",
      message: `Starting task: ${prompt.slice(0, 100)}...`,
      metadata: { cwd },
    });
    await this.state.save();

    // Build allowed tools based on autonomy level
    const allowedTools = this.getAllowedTools();

    let result = "";

    try {
      for await (const message of query({
        prompt,
        options: {
          cwd,
          allowedTools,
          // MCP servers configured via MCP_SERVERS env var
          // SDK reads from mcpServers in options or config file
          mcpServers: this.config.mcpServers,
        },
      })) {
        if (message.type === "result") {
          if (message.subtype === "success") {
            result = message.result;
          } else {
            // Handle all error subtypes
            const errorSubtype = message.subtype;
            this.state.addEntry({
              type: "error",
              message: `Agent error: ${errorSubtype}`,
            });
          }
        }
      }

      this.state.addEntry({
        type: "task_complete",
        message: "Task completed successfully",
        metadata: { resultLength: result.length },
      });
      await this.state.save();

      return result;
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : String(error);
      this.state.addEntry({
        type: "error",
        message: errorMsg,
      });
      await this.state.save();
      throw error;
    }
  }

  private getAllowedTools(): string[] {
    // Base tools always available
    const baseTools = ["Read", "Glob", "Grep"];

    // File editing tools
    const editTools = ["Edit", "Write"];

    // Shell execution
    const shellTools = ["Bash"];

    // Based on autonomy level
    const level = this.config.autonomyLevel;

    switch (level) {
      case 0: // Read-only
        return [...baseTools];
      case 1: // Cautious - read + plan
        return [...baseTools];
      case 2: // Balanced - most operations
        return [...baseTools, ...editTools, ...shellTools];
      case 3: // Full autonomy
        return [...baseTools, ...editTools, ...shellTools];
      default:
        return [...baseTools, ...editTools, ...shellTools];
    }
  }

  getState(): ReturnType<StateManager["getState"]> {
    return this.state.getState();
  }
}
