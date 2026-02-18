import pino from 'pino';

/**
 * Logger instance for the agent.
 */
export const logger = pino.default({
  level: process.env.LOG_LEVEL || 'info',
  transport: {
    target: 'pino-pretty',
    options: {
      colorize: true,
      translateTime: 'SYS:standard',
      ignore: 'pid,hostname',
    },
  },
});

/**
 * Create a child logger with context.
 */
export function createLogger(context: string): pino.Logger {
  return logger.child({ context });
}
