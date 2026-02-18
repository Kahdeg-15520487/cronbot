import { McpTool, OperationResult } from '../types.js';
import { createLogger } from '../logger.js';

const logger = createLogger('kanban-tools');

/**
 * API client for Kanban operations.
 */
class KanbanApiClient {
  private baseUrl: string;
  private projectId: string;

  constructor(baseUrl: string, projectId: string) {
    this.baseUrl = baseUrl;
    this.projectId = projectId;
  }

  async get<T>(path: string): Promise<OperationResult<T>> {
    try {
      const response = await fetch(`${this.baseUrl}${path}`);
      if (!response.ok) {
        return { success: false, error: `HTTP ${response.status}: ${response.statusText}` };
      }
      const data = await response.json() as T;
      return { success: true, data };
    } catch (error) {
      return { success: false, error: String(error) };
    }
  }

  async post<T>(path: string, body: unknown): Promise<OperationResult<T>> {
    try {
      const response = await fetch(`${this.baseUrl}${path}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (!response.ok) {
        const errorText = await response.text();
        return { success: false, error: `HTTP ${response.status}: ${errorText}` };
      }
      const data = await response.json() as T;
      return { success: true, data };
    } catch (error) {
      return { success: false, error: String(error) };
    }
  }

  async patch<T>(path: string, body: unknown): Promise<OperationResult<T>> {
    try {
      const response = await fetch(`${this.baseUrl}${path}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (!response.ok) {
        const errorText = await response.text();
        return { success: false, error: `HTTP ${response.status}: ${errorText}` };
      }
      const data = await response.json() as T;
      return { success: true, data };
    } catch (error) {
      return { success: false, error: String(error) };
    }
  }

  async delete<T>(path: string): Promise<OperationResult<T>> {
    try {
      const response = await fetch(`${this.baseUrl}${path}`, {
        method: 'DELETE',
      });
      if (!response.ok) {
        return { success: false, error: `HTTP ${response.status}: ${response.statusText}` };
      }
      const data = await response.json() as T;
      return { success: true, data };
    } catch (error) {
      return { success: false, error: String(error) };
    }
  }
}

// Task status mapping
const STATUS_MAP: Record<string, number> = {
  backlog: 0,
  sprint: 1,
  in_progress: 2,
  review: 3,
  blocked: 4,
  done: 5,
  cancelled: 6,
};

// Task type mapping
const TYPE_MAP: Record<string, number> = {
  task: 0,
  bug: 1,
  blocker: 2,
  idea: 3,
  epic: 4,
};

/**
 * Kanban tools for task management.
 */
export class KanbanTools {
  private api: KanbanApiClient;
  private tools: Map<string, { tool: McpTool; handler: (args: Record<string, unknown>) => Promise<OperationResult> }> = new Map();

  constructor(apiUrl: string, projectId: string) {
    this.api = new KanbanApiClient(apiUrl, projectId);
    this.registerTools();
  }

  private registerTools(): void {
    // List tasks
    this.tools.set('kanban_list_tasks', {
      tool: {
        name: 'kanban_list_tasks',
        description: 'List all tasks for the project, optionally filtered by status',
        inputSchema: {
          type: 'object',
          properties: {
            status: {
              type: 'string',
              description: 'Filter by status (backlog, sprint, in_progress, review, blocked, done)',
              enum: ['backlog', 'sprint', 'in_progress', 'review', 'blocked', 'done'],
            },
          },
        },
      },
      handler: async (args) => {
        const status = args.status as string | undefined;
        const params = status ? `?status=${STATUS_MAP[status]}` : '';
        const result = await this.api.get<{ id: string; number: number; title: string; status: number; type: number }[]>(
          `/projects/${this.api['projectId']}/tasks${params}`
        );

        if (result.success && result.data) {
          // Transform status/type back to strings
          const tasks = result.data.map(t => ({
            ...t,
            status: Object.keys(STATUS_MAP).find(k => STATUS_MAP[k] === t.status) || 'backlog',
            type: Object.keys(TYPE_MAP).find(k => TYPE_MAP[k] === t.type) || 'task',
          }));
          return { success: true, data: tasks };
        }
        return result;
      },
    });

    // Create task
    this.tools.set('kanban_create_task', {
      tool: {
        name: 'kanban_create_task',
        description: 'Create a new task in the project backlog',
        inputSchema: {
          type: 'object',
          properties: {
            title: {
              type: 'string',
              description: 'Task title',
            },
            description: {
              type: 'string',
              description: 'Task description',
            },
            type: {
              type: 'string',
              description: 'Task type (task, bug, feature, idea, epic)',
              enum: ['task', 'bug', 'feature', 'idea', 'epic'],
            },
            story_points: {
              type: 'number',
              description: 'Estimated story points',
            },
          },
          required: ['title'],
        },
      },
      handler: async (args) => {
        const body: Record<string, unknown> = {
          title: args.title,
          description: args.description || '',
          type: TYPE_MAP[args.type as string] ?? 0,
          projectId: this.api['projectId'],
        };

        if (args.story_points) {
          body.storyPoints = args.story_points;
        }

        logger.info({ title: args.title }, 'Creating task');
        return this.api.post<{ id: string; number: number }>(`/projects/${this.api['projectId']}/tasks`, body);
      },
    });

    // Update task
    this.tools.set('kanban_update_task', {
      tool: {
        name: 'kanban_update_task',
        description: 'Update an existing task',
        inputSchema: {
          type: 'object',
          properties: {
            task_id: {
              type: 'string',
              description: 'Task ID to update',
            },
            title: {
              type: 'string',
              description: 'New task title',
            },
            description: {
              type: 'string',
              description: 'New task description',
            },
            status: {
              type: 'string',
              description: 'New status',
              enum: ['backlog', 'sprint', 'in_progress', 'review', 'blocked', 'done'],
            },
            type: {
              type: 'string',
              description: 'New type',
              enum: ['task', 'bug', 'feature', 'idea', 'epic'],
            },
            story_points: {
              type: 'number',
              description: 'New story points estimate',
            },
          },
          required: ['task_id'],
        },
      },
      handler: async (args) => {
        const body: Record<string, unknown> = {};

        if (args.title) body.title = args.title;
        if (args.description !== undefined) body.description = args.description;
        if (args.status) body.status = STATUS_MAP[args.status as string];
        if (args.type) body.type = TYPE_MAP[args.type as string];
        if (args.story_points !== undefined) body.storyPoints = args.story_points;

        logger.info({ taskId: args.task_id }, 'Updating task');
        return this.api.patch(`/tasks/${args.task_id}`, body);
      },
    });

    // Get task details
    this.tools.set('kanban_get_task', {
      tool: {
        name: 'kanban_get_task',
        description: 'Get detailed information about a specific task',
        inputSchema: {
          type: 'object',
          properties: {
            task_id: {
              type: 'string',
              description: 'Task ID',
            },
          },
          required: ['task_id'],
        },
      },
      handler: async (args) => {
        return this.api.get(`/tasks/${args.task_id}`);
      },
    });

    // Move task to status
    this.tools.set('kanban_move_task', {
      tool: {
        name: 'kanban_move_task',
        description: 'Move a task to a different status column',
        inputSchema: {
          type: 'object',
          properties: {
            task_id: {
              type: 'string',
              description: 'Task ID',
            },
            status: {
              type: 'string',
              description: 'Target status',
              enum: ['backlog', 'sprint', 'in_progress', 'review', 'blocked', 'done'],
            },
          },
          required: ['task_id', 'status'],
        },
      },
      handler: async (args) => {
        logger.info({ taskId: args.task_id, status: args.status }, 'Moving task');
        return this.api.patch(`/tasks/${args.task_id}`, {
          status: STATUS_MAP[args.status as string],
        });
      },
    });

    // Add comment to task
    this.tools.set('kanban_add_comment', {
      tool: {
        name: 'kanban_add_comment',
        description: 'Add a comment to a task',
        inputSchema: {
          type: 'object',
          properties: {
            task_id: {
              type: 'string',
              description: 'Task ID',
            },
            content: {
              type: 'string',
              description: 'Comment content',
            },
          },
          required: ['task_id', 'content'],
        },
      },
      handler: async (args) => {
        logger.info({ taskId: args.task_id }, 'Adding comment to task');
        return this.api.post(`/tasks/${args.task_id}/comments`, {
          content: args.content,
          authorType: 'agent',
        });
      },
    });

    // Create sprint
    this.tools.set('kanban_create_sprint', {
      tool: {
        name: 'kanban_create_sprint',
        description: 'Create a new sprint for the project',
        inputSchema: {
          type: 'object',
          properties: {
            name: {
              type: 'string',
              description: 'Sprint name',
            },
            goal: {
              type: 'string',
              description: 'Sprint goal',
            },
            duration_days: {
              type: 'number',
              description: 'Sprint duration in days (default: 14)',
            },
          },
          required: ['name'],
        },
      },
      handler: async (args) => {
        const startDate = new Date();
        const endDate = new Date();
        endDate.setDate(endDate.getDate() + (args.duration_days as number || 14));

        logger.info({ name: args.name }, 'Creating sprint');
        return this.api.post(`/projects/${this.api['projectId']}/sprints`, {
          name: args.name,
          goal: args.goal || '',
          startDate: startDate.toISOString().split('T')[0],
          endDate: endDate.toISOString().split('T')[0],
        });
      },
    });

    // List sprints
    this.tools.set('kanban_list_sprints', {
      tool: {
        name: 'kanban_list_sprints',
        description: 'List all sprints for the project',
        inputSchema: {
          type: 'object',
          properties: {},
        },
      },
      handler: async () => {
        return this.api.get(`/projects/${this.api['projectId']}/sprints`);
      },
    });
  }

  /**
   * Get all available Kanban tools.
   */
  getTools(): McpTool[] {
    return Array.from(this.tools.values()).map(t => t.tool);
  }

  /**
   * Check if a tool exists.
   */
  hasTool(name: string): boolean {
    return this.tools.has(name);
  }

  /**
   * Call a Kanban tool.
   */
  async callTool(name: string, args: Record<string, unknown>): Promise<OperationResult> {
    const tool = this.tools.get(name);
    if (!tool) {
      return { success: false, error: `Kanban tool '${name}' not found` };
    }
    return tool.handler(args);
  }
}
