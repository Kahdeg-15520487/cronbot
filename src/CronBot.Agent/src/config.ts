import { config } from "dotenv";
import { z } from "zod";

config();

const configSchema = z.object({
  // Agent Identity (optional)
  agentId: z.string().uuid().optional(),
  projectId: z.string().uuid().optional(),

  // Anthropic API
  anthropicApiKey: z.string().min(1, "ANTHROPIC_API_KEY is required"),
  anthropicBaseUrl: z.string().url().optional(),
  anthropicModel: z.string().default("claude-sonnet-4-20250514"),
  anthropicMaxTokens: z.coerce.number().default(4096),

  // Workspace
  workspacePath: z.string().default(process.cwd()),
  targetDir: z.string().optional(),

  // State
  statePath: z.string().default("./state"),

  // Agent behavior
  defaultPrompt: z.string().optional(),
  autonomyLevel: z.coerce.number().min(0).max(3).default(2),

  // Task polling
  taskPollIntervalMs: z.coerce.number().default(30000),
  retryDelayMs: z.coerce.number().default(10000),
  blockerWaitMs: z.coerce.number().default(30000),

  // Git Configuration (for task branching)
  giteaUrl: z.string().optional(),
  giteaUsername: z.string().optional(),
  giteaPassword: z.string().optional(),
  giteaToken: z.string().optional(),
  repoUrl: z.string().optional(),

  // MCP Configuration
  kanbanUrl: z.string().url().optional(),
  mcpServers: z.record(z.string(), z.any()).optional(),
});

export type AgentConfig = z.infer<typeof configSchema>;

export function loadConfig(): AgentConfig {
  // Build MCP servers config from KANBAN_URL if not explicitly set
  let mcpServers: Record<string, unknown> | undefined;
  if (process.env.MCP_SERVERS) {
    mcpServers = JSON.parse(process.env.MCP_SERVERS);
  } else if (process.env.KANBAN_URL) {
    // Convert KANBAN_URL to MCP server config
    // KANBAN_URL is the base API URL (e.g., http://localhost:5001/api)
    // MCP endpoint is at the root (e.g., http://localhost:5001/)
    const mcpUrl = process.env.KANBAN_URL.replace(/\/api$/, "");
    mcpServers = { kanban: { url: mcpUrl } };
  }

  // Helper to convert empty strings to undefined
  const optionalString = (val: string | undefined): string | undefined =>
    val && val.length > 0 ? val : undefined;

  return configSchema.parse({
    agentId: optionalString(process.env.AGENT_ID),
    projectId: optionalString(process.env.PROJECT_ID),

    anthropicApiKey: process.env.ANTHROPIC_API_KEY,
    anthropicBaseUrl: optionalString(process.env.ANTHROPIC_BASE_URL),
    anthropicModel: process.env.ANTHROPIC_MODEL,
    anthropicMaxTokens: process.env.ANTHROPIC_MAX_TOKENS,

    workspacePath: process.env.WORKSPACE_PATH,
    targetDir: process.env.TARGET_DIR,

    statePath: process.env.STATE_PATH ?? process.env.AGENT_STATE_PATH,

    defaultPrompt: optionalString(process.env.DEFAULT_PROMPT),
    autonomyLevel: process.env.AUTONOMY_LEVEL,

    taskPollIntervalMs: process.env.TASK_POLL_INTERVAL_MS,
    retryDelayMs: process.env.RETRY_DELAY_MS,
    blockerWaitMs: process.env.BLOCKER_WAIT_MS,

    kanbanUrl: optionalString(process.env.KANBAN_URL),
    mcpServers,

    // Git Configuration
    giteaUrl: optionalString(process.env.GITEA_URL),
    giteaUsername: optionalString(process.env.GITEA_USERNAME),
    giteaPassword: optionalString(process.env.GITEA_PASSWORD),
    giteaToken: optionalString(process.env.GITEA_TOKEN),
    repoUrl: optionalString(process.env.REPO_URL),
  });
}
