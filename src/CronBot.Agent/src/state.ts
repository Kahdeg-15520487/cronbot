import fs from "fs/promises";
import path from "path";

export interface AgentState {
  id: string;
  createdAt: string;
  updatedAt: string;
  lastCheckpoint?: string;
  history: HistoryEntry[];
  context: Record<string, unknown>;
}

export interface HistoryEntry {
  timestamp: string;
  type: "task_start" | "task_complete" | "error" | "checkpoint" | "decision";
  message: string;
  metadata?: Record<string, unknown>;
}

export class StateManager {
  private statePath: string;
  private state: AgentState;

  constructor(statePath: string, agentId: string) {
    this.statePath = statePath;
    this.state = {
      id: agentId,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      history: [],
      context: {},
    };
  }

  async initialize(): Promise<void> {
    await fs.mkdir(this.statePath, { recursive: true });
    await this.load();
  }

  private async load(): Promise<void> {
    const stateFile = path.join(this.statePath, "state.json");
    try {
      const data = await fs.readFile(stateFile, "utf-8");
      this.state = JSON.parse(data);
    } catch {
      // State doesn't exist, use defaults
    }
  }

  async save(): Promise<void> {
    this.state.updatedAt = new Date().toISOString();
    const stateFile = path.join(this.statePath, "state.json");
    await fs.writeFile(stateFile, JSON.stringify(this.state, null, 2));
  }

  addEntry(entry: Omit<HistoryEntry, "timestamp">): void {
    this.state.history.push({
      ...entry,
      timestamp: new Date().toISOString(),
    });
    // Keep last 100 entries
    if (this.state.history.length > 100) {
      this.state.history = this.state.history.slice(-100);
    }
  }

  setContext(key: string, value: unknown): void {
    this.state.context[key] = value;
  }

  getContext<T>(key: string): T | undefined {
    return this.state.context[key] as T | undefined;
  }

  getState(): Readonly<AgentState> {
    return this.state;
  }
}
