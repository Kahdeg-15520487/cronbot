import { Agent } from './agent.js';
import { loadConfig } from './config.js';
import { logger } from './logger.js';
/**
 * Main entry point for the CronBot Agent.
 */
async function main() {
    logger.info('CronBot Agent starting...');
    // Load configuration
    const config = loadConfig();
    logger.info({
        projectId: config.projectId,
        agentId: config.agentId,
        autonomyLevel: config.autonomyLevel,
    }, 'Configuration loaded');
    // Create agent instance
    const agent = new Agent(config);
    // Handle shutdown signals
    process.on('SIGINT', async () => {
        logger.info('Received SIGINT, shutting down...');
        await agent.stop();
        process.exit(0);
    });
    process.on('SIGTERM', async () => {
        logger.info('Received SIGTERM, shutting down...');
        await agent.stop();
        process.exit(0);
    });
    // Handle uncaught errors
    process.on('uncaughtException', (error) => {
        logger.error({ error: error.message, stack: error.stack }, 'Uncaught exception');
        process.exit(1);
    });
    process.on('unhandledRejection', (reason) => {
        logger.error({ reason }, 'Unhandled rejection');
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
        logger.error({ error: message }, 'Agent failed');
        process.exit(1);
    }
}
// Run main function
main();
//# sourceMappingURL=index.js.map