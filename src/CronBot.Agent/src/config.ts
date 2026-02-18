import dotenv from 'dotenv';
import { AgentConfig, AutonomyLevel, RetryConfig } from './types.js';

dotenv.config();

/**
 * Load agent configuration from environment variables.
 */
export function loadConfig(): AgentConfig {
  return {
    projectId: getRequiredEnv('PROJECT_ID'),
    agentId: getRequiredEnv('AGENT_ID'),
    autonomyLevel: parseAutonomyLevel(process.env.AUTONOMY_LEVEL),
    mcpRegistryUrl: process.env.MCP_REGISTRY_URL || 'http://localhost:5000/api/mcp/registry',
    kanbanUrl: process.env.KANBAN_URL || 'http://cronbot-api:8080/api',
    gitUrl: process.env.GIT_URL || 'http://gitea:3000',
    workspacePath: process.env.WORKSPACE_PATH || '/workspace',
    skillsPath: process.env.SKILLS_PATH || '/skills',
    statePath: process.env.AGENT_STATE_PATH || '/agent-state',
    maxTokens: parseInt(process.env.MAX_TOKENS || '200000', 10),
    retryConfig: loadRetryConfig(),
  };
}

/**
 * Load retry configuration.
 */
function loadRetryConfig(): RetryConfig {
  return {
    maxAttempts: parseInt(process.env.RETRY_MAX_ATTEMPTS || '3', 10),
    initialDelayMs: parseInt(process.env.RETRY_INITIAL_DELAY_MS || '1000', 10),
    maxDelayMs: parseInt(process.env.RETRY_MAX_DELAY_MS || '30000', 10),
    backoffMultiplier: parseInt(process.env.RETRY_BACKOFF_MULTIPLIER || '2', 10),
  };
}

/**
 * Parse autonomy level from string or number.
 */
function parseAutonomyLevel(value?: string): AutonomyLevel {
  if (value === undefined) {
    return AutonomyLevel.Balanced; // Default
  }

  const num = parseInt(value, 10);
  if (!isNaN(num) && num >= 0 && num <= 3) {
    return num as AutonomyLevel;
  }

  switch (value.toLowerCase()) {
    case 'reactive':
    case 'readonly':
      return AutonomyLevel.Reactive;
    case 'cautious':
      return AutonomyLevel.Cautious;
    case 'balanced':
    case 'default':
      return AutonomyLevel.Balanced;
    case 'full':
    case 'autonomous':
      return AutonomyLevel.FullAutonomy;
    default:
      return AutonomyLevel.Balanced;
  }
}

/**
 * Get required environment variable or throw error.
 */
function getRequiredEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Required environment variable ${name} is not set`);
  }
  return value;
}

/**
 * Default agent configuration for development.
 */
export const defaultConfig: AgentConfig = {
  projectId: '00000000-0000-0000-0000-000000000001',
  agentId: '00000000-0000-0000-0000-000000000001',
  autonomyLevel: AutonomyLevel.Balanced,
  mcpRegistryUrl: 'http://localhost:5000/api/mcp/registry',
  kanbanUrl: 'http://localhost:5000/api',
  gitUrl: 'http://localhost:3000',
  workspacePath: '/workspace',
  skillsPath: '/skills',
  statePath: '/agent-state',
  maxTokens: 200000,
  retryConfig: {
    maxAttempts: 3,
    initialDelayMs: 1000,
    maxDelayMs: 30000,
    backoffMultiplier: 2,
  },
};
