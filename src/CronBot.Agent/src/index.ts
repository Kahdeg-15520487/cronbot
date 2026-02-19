#!/usr/bin/env node
import { loadConfig } from "./config.js";
import { Agent } from "./agent.js";

async function main(): Promise<void> {
  const config = loadConfig();

  // Get prompt from CLI args or use default
  const prompt = process.argv[2] ?? config.defaultPrompt;
  if (!prompt) {
    console.error("Usage: node dist/index.js <prompt>");
    console.error("       Or set DEFAULT_PROMPT environment variable");
    process.exit(1);
  }

  // Get target directory
  const targetDir = process.argv[3] ?? config.targetDir ?? config.workspacePath;

  console.log(`CronBot Agent starting...`);
  console.log(`Target: ${targetDir}`);
  console.log(`Model: ${config.anthropicModel}`);
  console.log(`Autonomy Level: ${config.autonomyLevel}`);
  console.log(`---`);

  const agent = new Agent({
    config,
    prompt,
    targetDir,
  });

  try {
    await agent.initialize();
    const result = await agent.run(prompt, targetDir);
    console.log("\n--- Result ---\n");
    console.log(result);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    console.error(`Error: ${message}`);
    process.exit(1);
  }
}

main();
