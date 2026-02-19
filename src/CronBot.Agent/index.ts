import { config } from "dotenv";
import { query } from "@anthropic-ai/claude-agent-sdk";

config();

const targetDir = process.argv[2] || process.env.TARGET_DIR || process.cwd();
const cmd = process.argv[3] || process.env.DEFAULT_PROMPT || "Find all TODO comments and create a summary";



for await (const message of query({
  prompt: cmd,
  options: {
    cwd: targetDir,
    allowedTools: ["Read", "Glob", "Grep", "Edit", "Write"]
  }
})) {
  if (message.type === "result" && message.subtype === "success") {
    console.log(message.result);
  }
}