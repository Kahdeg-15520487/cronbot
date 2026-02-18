"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const agent_js_1 = require("./agent.js");
const config_js_1 = require("./config.js");
const logger_js_1 = require("./logger.js");
/**
 * Main entry point for the CronBot Agent.
 */
async function main() {
    logger_js_1.logger.info('CronBot Agent starting...');
    // Load configuration
    const config = (0, config_js_1.loadConfig)();
    logger_js_1.logger.info({
        projectId: config.projectId,
        agentId: config.agentId,
        autonomyLevel: config.autonomyLevel,
    }, 'Configuration loaded');
    // Create agent instance
    const agent = new agent_js_1.Agent(config);
    // Handle shutdown signals
    process.on('SIGINT', async () => {
        logger_js_1.logger.info('Received SIGINT, shutting down...');
        await agent.stop();
        process.exit(0);
    });
    process.on('SIGTERM', async () => {
        logger_js_1.logger.info('Received SIGTERM, shutting down...');
        await agent.stop();
        process.exit(0);
    });
    // Handle uncaught errors
    process.on('uncaughtException', (error) => {
        logger_js_1.logger.error({ error: error.message, stack: error.stack }, 'Uncaught exception');
        process.exit(1);
    });
    process.on('unhandledRejection', (reason) => {
        logger_js_1.logger.error({ reason }, 'Unhandled rejection');
        process.exit(1);
    });
    try {
        // Initialize agent
        await agent.initialize();
        // Start main loop
        await agent.start();
    }
    catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        logger_js_1.logger.error({ error: message }, 'Agent failed');
        process.exit(1);
    }
}
// Run main function
main();
//# sourceMappingURL=index.js.map