import axios from 'axios';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001/api';

export const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Enum mappings (API uses integers, frontend uses strings)
export const TaskStatus = {
  0: 'backlog',
  1: 'sprint',
  2: 'in_progress',
  3: 'review',
  4: 'blocked',
  5: 'done',
  6: 'cancelled',
} as const;

export const TaskType = {
  0: 'task',
  1: 'bug',
  2: 'blocker',
  3: 'idea',
  4: 'epic',
} as const;

export const AgentStatus = {
  0: 'idle',
  1: 'working',
  2: 'paused',
  3: 'blocked',
  4: 'error',
  5: 'terminated',
} as const;

// Helper functions to convert API response
export function mapTaskStatus(status: number | string): string {
  if (typeof status === 'string') return status;
  return TaskStatus[status as keyof typeof TaskStatus] || 'backlog';
}

export function mapTaskType(type: number | string): string {
  if (typeof type === 'string') return type;
  return TaskType[type as keyof typeof TaskType] || 'task';
}

export function mapAgentStatus(status: number | string): string {
  if (typeof status === 'string') return status;
  return AgentStatus[status as keyof typeof AgentStatus] || 'idle';
}

// Reverse mappings for sending to API
export const TaskStatusToNumber: Record<string, number> = {
  backlog: 0,
  sprint: 1,
  in_progress: 2,
  review: 3,
  blocked: 4,
  done: 5,
  cancelled: 6,
};

export const TaskTypeToNumber: Record<string, number> = {
  task: 0,
  bug: 1,
  blocker: 2,
  idea: 3,
  epic: 4,
};

// Types
export interface User {
  id: string;
  username: string;
  email?: string;
  displayName?: string;
  avatarUrl?: string;
  isActive: boolean;
  createdAt: string;
}

export interface Team {
  id: string;
  name: string;
  slug: string;
  description?: string;
  isPersonal: boolean;
  ownerId: string;
  createdAt: string;
}

export interface Project {
  id: string;
  teamId: string;
  name: string;
  slug: string;
  description?: string;
  gitMode: number;
  internalRepoUrl?: string;
  autonomyLevel: number;
  sandboxMode: number;
  maxAgents: number;
  isActive: boolean;
  createdAt: string;
}

export interface Task {
  id: string;
  projectId: string;
  number: number;
  title: string;
  description?: string;
  type: string;
  status: string;
  sprintId?: string;
  boardId?: string;
  storyPoints?: number;
  assigneeType?: string;
  assigneeId?: string;
  gitBranch?: string;
  gitPrUrl?: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
}

export interface Agent {
  id: string;
  projectId: string;
  currentTaskId?: string;
  containerId?: string;
  containerName?: string;
  status: string;
  statusMessage?: string;
  cpuUsagePercent?: number;
  memoryUsageMb?: number;
  startedAt: string;
  lastActivityAt?: string;
  tasksCompleted: number;
  commitsMade: number;
}

// Raw API response types (with number enums)
interface RawTask {
  id: string;
  projectId: string;
  number: number;
  title: string;
  description?: string;
  type: number | string;
  status: number | string;
  sprintId?: string;
  boardId?: string;
  storyPoints?: number;
  assigneeType?: string;
  assigneeId?: string;
  gitBranch?: string;
  gitPrUrl?: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
}

interface RawAgent {
  id: string;
  projectId: string;
  currentTaskId?: string;
  containerId?: string;
  containerName?: string;
  status: number | string;
  statusMessage?: string;
  cpuUsagePercent?: number;
  memoryUsageMb?: number;
  startedAt: string;
  lastActivityAt?: string;
  tasksCompleted: number;
  commitsMade: number;
}

// Transform functions
function transformTask(raw: RawTask): Task {
  return {
    ...raw,
    type: mapTaskType(raw.type),
    status: mapTaskStatus(raw.status),
  };
}

function transformAgent(raw: RawAgent): Agent {
  return {
    ...raw,
    status: mapAgentStatus(raw.status),
  };
}

// API functions
export const usersApi = {
  getAll: () => api.get<User[]>('/users'),
  get: (id: string) => api.get<User>(`/users/${id}`),
  create: (data: Partial<User>) => api.post<User>('/users', data),
  update: (id: string, data: Partial<User>) => api.patch<User>(`/users/${id}`, data),
  delete: (id: string) => api.delete(`/users/${id}`),
};

export const teamsApi = {
  getAll: () => api.get<Team[]>('/teams'),
  get: (id: string) => api.get<Team>(`/teams/${id}`),
  create: (data: Partial<Team>) => api.post<Team>('/teams', data),
  update: (id: string, data: Partial<Team>) => api.patch<Team>(`/teams/${id}`, data),
  delete: (id: string) => api.delete(`/teams/${id}`),
  getProjects: (id: string) => api.get<Project[]>(`/teams/${id}/projects`),
};

export const projectsApi = {
  getAll: (teamId?: string) => api.get<Project[]>('/projects', { params: { teamId } }),
  get: (id: string) => api.get<Project>(`/projects/${id}`),
  create: (data: Partial<Project>) => api.post<Project>('/projects', data),
  update: (id: string, data: Partial<Project>) => api.patch<Project>(`/projects/${id}`, data),
  delete: (id: string) => api.delete(`/projects/${id}`),
  getTasks: (id: string) => api.get<RawTask[]>(`/projects/${id}/tasks`).then(res => ({
    ...res,
    data: res.data.map(transformTask)
  })),
  getAgents: (id: string) => api.get<RawAgent[]>(`/projects/${id}/agents`).then(res => ({
    ...res,
    data: res.data.map(transformAgent)
  })),
  createAgent: (id: string) => api.post<RawAgent>(`/projects/${id}/agents`).then(res => ({
    ...res,
    data: transformAgent(res.data)
  })),
};

export const tasksApi = {
  getAll: (params?: { projectId?: string; sprintId?: string; status?: string }) =>
    api.get<RawTask[]>('/tasks', { params }).then(res => ({
      ...res,
      data: res.data.map(transformTask)
    })),
  get: (id: string) => api.get<RawTask>(`/tasks/${id}`).then(res => ({
    ...res,
    data: transformTask(res.data)
  })),
  create: (data: Partial<Task>) => {
    const payload = {
      ...data,
      type: data.type ? TaskTypeToNumber[data.type] : 0,
      status: data.status ? TaskStatusToNumber[data.status] : 0,
    };
    return api.post<RawTask>('/tasks', payload).then(res => ({
      ...res,
      data: transformTask(res.data)
    }));
  },
  createInProject: (projectId: string, data: Partial<Task>) => {
    const payload = {
      ...data,
      type: data.type ? TaskTypeToNumber[data.type] : 0,
      status: data.status ? TaskStatusToNumber[data.status] : 0,
    };
    return api.post<RawTask>(`/projects/${projectId}/tasks`, payload).then(res => ({
      ...res,
      data: transformTask(res.data)
    }));
  },
  update: (id: string, data: Partial<Task>) => {
    const payload: Record<string, unknown> = { ...data };
    if (data.type) payload.type = TaskTypeToNumber[data.type];
    if (data.status) payload.status = TaskStatusToNumber[data.status];
    return api.patch<RawTask>(`/tasks/${id}`, payload).then(res => ({
      ...res,
      data: transformTask(res.data)
    }));
  },
  delete: (id: string) => api.delete(`/tasks/${id}`),
  getComments: (id: string) => api.get(`/tasks/${id}/comments`),
  addComment: (id: string, content: string) => api.post(`/tasks/${id}/comments`, { content }),
};

export const agentsApi = {
  getAll: (params?: { projectId?: string; status?: string }) =>
    api.get<RawAgent[]>('/agents', { params }).then(res => ({
      ...res,
      data: res.data.map(transformAgent)
    })),
  get: (id: string) => api.get<RawAgent>(`/agents/${id}`).then(res => ({
    ...res,
    data: transformAgent(res.data)
  })),
  terminate: (id: string) => api.post<RawAgent>(`/agents/${id}/terminate`).then(res => ({
    ...res,
    data: transformAgent(res.data)
  })),
};
