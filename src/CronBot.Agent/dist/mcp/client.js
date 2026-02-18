"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.McpClient = void 0;
const index_js_1 = require("@modelcontextprotocol/sdk/client/index.js");
const stdio_js_1 = require("@modelcontextprotocol/sdk/client/stdio.js");
const sse_js_1 = require("@modelcontextprotocol/sdk/client/sse.js");
const logger_js_1 = require("../logger.js");
const logger = (0, logger_js_1.createLogger)('mcp-client');
/**
 * MCP Client wrapper for connecting to MCP servers.
 */
class McpClient {
    client = null;
    transport = null;
    config;
    connected = false;
    tools = [];
    resources = [];
    constructor(config) {
        this.config = config;
    }
    /**
     * Connect to the MCP server.
     */
    async connect() {
        try {
            if (this.config.transport === 'stdio') {
                await this.connectStdio();
            }
            else if (this.config.transport === 'sse') {
                await this.connectSse();
            }
            else {
                return {
                    success: false,
                    error: `Unsupported transport: ${this.config.transport}`,
                };
            }
            // Discover available tools and resources
            await this.discoverCapabilities();
            this.connected = true;
            logger.info({
                server: this.config.name,
                toolCount: this.tools.length,
                resourceCount: this.resources.length,
            }, 'Connected to MCP server');
            return { success: true };
        }
        catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            logger.error({ server: this.config.name, error: message }, 'Failed to connect to MCP server');
            return {
                success: false,
                error: message,
            };
        }
    }
    /**
     * Connect via stdio transport.
     */
    async connectStdio() {
        if (!this.config.command) {
            throw new Error('Command is required for stdio transport');
        }
        this.transport = new stdio_js_1.StdioClientTransport({
            command: this.config.command,
            args: this.config.args || [],
            env: {
                ...process.env,
                ...this.config.env,
            },
        });
        this.client = new index_js_1.Client({
            name: 'cronbot-agent',
            version: '1.0.0',
        }, {
            capabilities: {},
        });
        await this.client.connect(this.transport);
    }
    /**
     * Connect via SSE transport.
     */
    async connectSse() {
        if (!this.config.url) {
            throw new Error('URL is required for SSE transport');
        }
        this.transport = new sse_js_1.SSEClientTransport(new URL(this.config.url));
        this.client = new index_js_1.Client({
            name: 'cronbot-agent',
            version: '1.0.0',
        }, {
            capabilities: {},
        });
        await this.client.connect(this.transport);
    }
    /**
     * Discover available tools and resources.
     */
    async discoverCapabilities() {
        if (!this.client)
            return;
        // List tools
        try {
            const toolsResponse = await this.client.listTools();
            this.tools = (toolsResponse.tools || []).map(tool => ({
                name: tool.name,
                description: tool.description || '',
                inputSchema: tool.inputSchema,
            }));
        }
        catch (error) {
            logger.warn({ server: this.config.name }, 'Failed to list tools');
        }
        // List resources
        try {
            const resourcesResponse = await this.client.listResources();
            this.resources = (resourcesResponse.resources || []).map(resource => ({
                uri: resource.uri,
                name: resource.name,
                description: resource.description,
                mimeType: resource.mimeType,
            }));
        }
        catch (error) {
            logger.warn({ server: this.config.name }, 'Failed to list resources');
        }
    }
    /**
     * Get available tools.
     */
    getTools() {
        return this.tools;
    }
    /**
     * Get available resources.
     */
    getResources() {
        return this.resources;
    }
    /**
     * Check if a tool is available.
     */
    hasTool(name) {
        return this.tools.some(t => t.name === name);
    }
    /**
     * Call a tool.
     */
    async callTool(name, args) {
        if (!this.client || !this.connected) {
            return {
                success: false,
                error: 'MCP client not connected',
            };
        }
        try {
            const result = await this.client.callTool({
                name,
                arguments: args,
            });
            // Extract content from result
            const content = result.content;
            let data;
            if (Array.isArray(content)) {
                // Extract text from content blocks
                const textContent = content
                    .filter(block => block.type === 'text')
                    .map(block => block.text)
                    .join('\n');
                data = textContent || content;
            }
            else {
                data = content;
            }
            logger.debug({ tool: name, args }, 'Tool called successfully');
            return {
                success: !result.isError,
                data,
                error: result.isError ? String(data) : undefined,
            };
        }
        catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            logger.error({ tool: name, error: message }, 'Tool call failed');
            return {
                success: false,
                error: message,
            };
        }
    }
    /**
     * Read a resource.
     */
    async readResource(uri) {
        if (!this.client || !this.connected) {
            return {
                success: false,
                error: 'MCP client not connected',
            };
        }
        try {
            const result = await this.client.readResource({ uri });
            logger.debug({ uri }, 'Resource read successfully');
            return {
                success: true,
                data: result.contents,
            };
        }
        catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            logger.error({ uri, error: message }, 'Resource read failed');
            return {
                success: false,
                error: message,
            };
        }
    }
    /**
     * Disconnect from the MCP server.
     */
    async disconnect() {
        if (this.client && this.connected) {
            try {
                await this.client.close();
            }
            catch (error) {
                logger.warn({ server: this.config.name }, 'Error during disconnect');
            }
        }
        this.connected = false;
        this.client = null;
        this.transport = null;
        logger.info({ server: this.config.name }, 'Disconnected from MCP server');
    }
    /**
     * Check if connected.
     */
    isConnected() {
        return this.connected;
    }
}
exports.McpClient = McpClient;
//# sourceMappingURL=client.js.map