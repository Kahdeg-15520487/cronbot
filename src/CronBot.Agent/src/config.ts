import { config } from "dotenv";
import { z } from "zod";

config();

const configSchema = z.object({
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

  // MCP servers
  mcpServers: z.record(z.string(), z.any()).optional(),
});

export type AgentConfig = z.infer<typeof configSchema>;

export function loadConfig(): AgentConfig {
  return configSchema.parse({
    anthropicApiKey: process.env.ANTHROPIC_API_KEY,
    anthropicBaseUrl: process.env.ANTHROPIC_BASE_URL,
    anthropicModel: process.env.ANTHROPIC_MODEL,
    anthropicMaxTokens: process.env.ANTHROPIC_MAX_TOKENS,

    workspacePath: process.env.WORKSPACE_PATH,
    targetDir: process.env.TARGET_DIR,

    statePath: process.env.STATE_PATH,

    defaultPrompt: process.env.DEFAULT_PROMPT,
    autonomyLevel: process.env.AUTONOMY_LEVEL,

    mcpServers: process.env.MCP_SERVERS
      ? JSON.parse(process.env.MCP_SERVERS)
      : undefined,
  });
}
