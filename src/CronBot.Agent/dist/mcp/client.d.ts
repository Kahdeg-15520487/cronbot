import { McpServerConfig, McpTool, McpResource, OperationResult } from '../types.js';
/**
 * MCP Client wrapper for connecting to MCP servers.
 */
export declare class McpClient {
    private client;
    private transport;
    private config;
    private connected;
    private tools;
    private resources;
    constructor(config: McpServerConfig);
    /**
     * Connect to the MCP server.
     */
    connect(): Promise<OperationResult>;
    /**
     * Connect via stdio transport.
     */
    private connectStdio;
    /**
     * Connect via SSE transport.
     */
    private connectSse;
    /**
     * Discover available tools and resources.
     */
    private discoverCapabilities;
    /**
     * Get available tools.
     */
    getTools(): McpTool[];
    /**
     * Get available resources.
     */
    getResources(): McpResource[];
    /**
     * Check if a tool is available.
     */
    hasTool(name: string): boolean;
    /**
     * Call a tool.
     */
    callTool(name: string, args: Record<string, unknown>): Promise<OperationResult>;
    /**
     * Read a resource.
     */
    readResource(uri: string): Promise<OperationResult>;
    /**
     * Disconnect from the MCP server.
     */
    disconnect(): Promise<void>;
    /**
     * Check if connected.
     */
    isConnected(): boolean;
}
//# sourceMappingURL=client.d.ts.map