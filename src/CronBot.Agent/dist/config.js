"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.defaultConfig = void 0;
exports.loadConfig = loadConfig;
const dotenv_1 = __importDefault(require("dotenv"));
const types_js_1 = require("./types.js");
dotenv_1.default.config();
/**
 * Load agent configuration from environment variables.
 */
function loadConfig() {
    return {
        projectId: getRequiredEnv('PROJECT_ID'),
        agentId: getRequiredEnv('AGENT_ID'),
        autonomyLevel: parseAutonomyLevel(process.env.AUTONOMY_LEVEL),
        anthropicApiKey: process.env.ANTHROPIC_API_KEY,
        anthropicBaseUrl: process.env.ANTHROPIC_BASE_URL,
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
function loadRetryConfig() {
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
function parseAutonomyLevel(value) {
    if (value === undefined) {
        return types_js_1.AutonomyLevel.Balanced; // Default
    }
    const num = parseInt(value, 10);
    if (!isNaN(num) && num >= 0 && num <= 3) {
        return num;
    }
    switch (value.toLowerCase()) {
        case 'reactive':
        case 'readonly':
            return types_js_1.AutonomyLevel.Reactive;
        case 'cautious':
            return types_js_1.AutonomyLevel.Cautious;
        case 'balanced':
        case 'default':
            return types_js_1.AutonomyLevel.Balanced;
        case 'full':
        case 'autonomous':
            return types_js_1.AutonomyLevel.FullAutonomy;
        default:
            return types_js_1.AutonomyLevel.Balanced;
    }
}
/**
 * Get required environment variable or throw error.
 */
function getRequiredEnv(name) {
    const value = process.env[name];
    if (!value) {
        throw new Error(`Required environment variable ${name} is not set`);
    }
    return value;
}
/**
 * Default agent configuration for development.
 */
exports.defaultConfig = {
    projectId: '00000000-0000-0000-0000-000000000001',
    agentId: '00000000-0000-0000-0000-000000000001',
    autonomyLevel: types_js_1.AutonomyLevel.Balanced,
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
//# sourceMappingURL=config.js.map