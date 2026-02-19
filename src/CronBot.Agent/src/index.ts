#!/usr/bin/env node
import { loadConfig } from "./config.js";
import { Agent } from "./agent.js";

const MODE_SINGLE = "single";
const MODE_DAEMON = "daemon";

function printUsage(): void {
  console.log(`
CronBot Agent - Autonomous AI Development Assistant

Usage:
  node dist/index.js                    Run in daemon mode (polls for tasks via MCP)
  node dist/index.js daemon             Run in daemon mode
  node dist/index.js single "<prompt>"  Run single task with prompt
  node dist/index.js "<prompt>"         Run single task with prompt (shorthand)

Environment Variables:
  ANTHROPIC_API_KEY     Required: Your Anthropic API key
  ANTHROPIC_BASE_URL    Optional: Custom API endpoint
  ANTHROPIC_MODEL       Optional: Model to use (default: claude-sonnet-4-20250514)

  PROJECT_ID            Optional: Project UUID to work on
  AGENT_ID              Optional: Agent UUID (auto-generated if not set)

  WORKSPACE_PATH        Working directory (default: current directory)
  STATE_PATH            State storage directory (default: ./state)

  AUTONOMY_LEVEL        0-3 (default: 2)
                        0 = Read-only
                        1 = Cautious (read + plan)
                        2 = Balanced (read + write + bash)
                        3 = Full autonomy

  KANBAN_URL            URL to CronBot API (e.g., http://api:8080/api)
                        MCP endpoint is derived from this URL

  MCP_SERVERS           JSON config for MCP servers (alternative to KANBAN_URL)
                        Example: {"kanban":{"url":"http://api:8080/mcp"}}

  TASK_POLL_INTERVAL_MS Poll interval in daemon mode (default: 30000)
`);
}

async function main(): Promise<void> {
  const config = loadConfig();
  const args = process.argv.slice(2);

  // Determine mode
  let mode: string;
  let prompt: string | undefined;

  if (args.length === 0) {
    // No args = daemon mode
    mode = MODE_DAEMON;
  } else if (args[0] === "--help" || args[0] === "-h") {
    printUsage();
    process.exit(0);
  } else if (args[0] === MODE_DAEMON) {
    mode = MODE_DAEMON;
  } else if (args[0] === MODE_SINGLE) {
    mode = MODE_SINGLE;
    prompt = args.slice(1).join(" ");
  } else {
    // Shorthand: treat all args as prompt for single mode
    mode = MODE_SINGLE;
    prompt = args.join(" ");
  }

  // Validate
  if (mode === MODE_SINGLE && !prompt) {
    console.error("Error: Single mode requires a prompt");
    printUsage();
    process.exit(1);
  }

  if (mode === MODE_DAEMON && !config.mcpServers) {
    console.error("Error: Daemon mode requires MCP server configuration");
    console.error("Set KANBAN_URL or MCP_SERVERS environment variable");
    process.exit(1);
  }

  console.log("CronBot Agent starting...");
  console.log(`Mode: ${mode}`);
  console.log(`Model: ${config.anthropicModel}`);
  console.log(`Autonomy Level: ${config.autonomyLevel}`);
  if (config.projectId) {
    console.log(`Project: ${config.projectId}`);
  }
  if (config.mcpServers) {
    console.log(`MCP Servers: ${Object.keys(config.mcpServers).join(", ")}`);
  }
  console.log("---");

  const agent = new Agent({
    config,
    prompt,
  });

  try {
    await agent.initialize();

    if (mode === MODE_DAEMON) {
      // Run forever, polling for tasks
      await agent.runDaemon();
    } else {
      // Single task mode
      const result = await agent.runOnce(prompt!, config.workspacePath);
      console.log("\n--- Result ---\n");
      console.log(result);
    }
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    console.error(`Error: ${message}`);
    process.exit(1);
  }
}

main();
