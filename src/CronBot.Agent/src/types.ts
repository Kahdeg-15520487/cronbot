/**
 * Autonomy levels for agent behavior control.
 * - Level 0: Reactive - Read-only, responds when spoken to
 * - Level 1: Cautious - Read/analyze, plans before executing, approval on writes
 * - Level 2: Balanced - Autonomous on tasks, approval on major changes
 * - Level 3: Full Autonomy - Full sandbox autonomy, only security-critical ops need approval
 */
export enum AutonomyLevel {
  Reactive = 0,
  Cautious = 1,
  Balanced = 2,
  FullAutonomy = 3,
}

/**
 * Agent status enum matching the database.
 */
export enum AgentStatus {
  Idle = 'idle',
  Working = 'working',
  Paused = 'paused',
  Blocked = 'blocked',
  Error = 'error',
  Terminated = 'terminated',
}

/**
 * Task status enum matching the database.
 */
export enum TaskStatus {
  Backlog = 'backlog',
  Sprint = 'sprint',
  InProgress = 'in_progress',
  Review = 'review',
  Blocked = 'blocked',
  Done = 'done',
  Cancelled = 'cancelled',
}

/**
 * Task type enum.
 */
export enum TaskType {
  Task = 'task',
  Bug = 'bug',
  Blocker = 'blocker',
  Idea = 'idea',
  Epic = 'epic',
}

/**
 * Represents a task from the Kanban system.
 */
export interface Task {
  id: string;
  projectId: string;
  number: number;
  title: string;
  description?: string;
  type: TaskType;
  status: TaskStatus;
  sprintId?: string;
  storyPoints?: number;
  assigneeType?: string;
  assigneeId?: string;
  gitBranch?: string;
  gitPrUrl?: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
}

/**
 * Agent context for maintaining state.
 */
export interface AgentContext {
  phase: AgentPhase;
  lastAction?: string;
  nextPlannedAction?: string;
  activeFiles: string[];
  recentDecisions: Decision[];
  tokenCount: number;
  maxTokens: number;
}

/**
 * Phases of agent operation.
 */
export enum AgentPhase {
  Initializing = 'initializing',
  ReadingContext = 'reading_context',
  Planning = 'planning',
  Executing = 'executing',
  Verifying = 'verifying',
  Committing = 'committing',
  WaitingApproval = 'waiting_approval',
  Blocked = 'blocked',
  Completed = 'completed',
  Error = 'error',
}

/**
 * A decision made by the agent.
 */
export interface Decision {
  decision: string;
  reason?: string;
  importanceScore: number;
  timestamp: string;
}

/**
 * Configuration for the agent.
 */
export interface AgentConfig {
  projectId: string;
  agentId: string;
  autonomyLevel: AutonomyLevel;
  anthropicApiKey?: string;
  anthropicBaseUrl?: string;
  mcpRegistryUrl: string;
  kanbanUrl: string;
  gitUrl: string;
  workspacePath: string;
  skillsPath: string;
  statePath: string;
  maxTokens: number;
  retryConfig: RetryConfig;
}

/**
 * Retry configuration.
 */
export interface RetryConfig {
  maxAttempts: number;
  initialDelayMs: number;
  maxDelayMs: number;
  backoffMultiplier: number;
}

/**
 * MCP tool definition.
 */
export interface McpTool {
  name: string;
  description: string;
  inputSchema: Record<string, unknown>;
}

/**
 * MCP resource definition.
 */
export interface McpResource {
  uri: string;
  name: string;
  description?: string;
  mimeType?: string;
}

/**
 * MCP server configuration.
 */
export interface McpServerConfig {
  name: string;
  transport: 'stdio' | 'sse' | 'http';
  url?: string;
  command?: string;
  args?: string[];
  env?: Record<string, string>;
  tools?: string[];
  resources?: string[];
}

/**
 * Skill metadata.
 */
export interface SkillMeta {
  name: string;
  version: string;
  description: string;
  author: string;
  tags: string[];
  inputs: Record<string, SkillInput>;
  outputs: Record<string, string>;
}

/**
 * Skill input definition.
 */
export interface SkillInput {
  type: string;
  required: boolean;
  description: string;
  default?: unknown;
  enum?: string[];
}

/**
 * Operation result.
 */
export interface OperationResult<T = unknown> {
  success: boolean;
  data?: T;
  error?: string;
  requiresApproval?: boolean;
  approvalReason?: string;
}

/**
 * Blocker detection result.
 */
export interface BlockerDetection {
  detected: boolean;
  type?: BlockerType;
  severity?: 'low' | 'medium' | 'high';
  description?: string;
  suggestedAction?: string;
}

/**
 * Types of blockers that can be detected.
 */
export enum BlockerType {
  CodeLoop = 'code_loop',
  VerificationLoop = 'verification_loop',
  ToolFailure = 'tool_failure',
  AgentStuck = 'agent_stuck',
  DependencyIssue = 'dependency_issue',
  ScopeCreep = 'scope_creep',
}

/**
 * Checkpoint for state recovery.
 */
export interface Checkpoint {
  id: string;
  timestamp: string;
  phase: AgentPhase;
  context: AgentContext;
  lastOperation?: OperationResult;
}
