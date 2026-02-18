import axios from 'axios';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

export const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

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
  getTasks: (id: string) => api.get<Task[]>(`/projects/${id}/tasks`),
  getAgents: (id: string) => api.get<Agent[]>(`/projects/${id}/agents`),
  createAgent: (id: string) => api.post<Agent>(`/projects/${id}/agents`),
};

export const tasksApi = {
  getAll: (params?: { projectId?: string; sprintId?: string; status?: string }) =>
    api.get<Task[]>('/tasks', { params }),
  get: (id: string) => api.get<Task>(`/tasks/${id}`),
  create: (data: Partial<Task>) => api.post<Task>('/tasks', data),
  createInProject: (projectId: string, data: Partial<Task>) =>
    api.post<Task>(`/projects/${projectId}/tasks`, data),
  update: (id: string, data: Partial<Task>) => api.patch<Task>(`/tasks/${id}`, data),
  delete: (id: string) => api.delete(`/tasks/${id}`),
  getComments: (id: string) => api.get(`/tasks/${id}/comments`),
  addComment: (id: string, content: string) => api.post(`/tasks/${id}/comments`, { content }),
};

export const agentsApi = {
  getAll: (params?: { projectId?: string; status?: string }) =>
    api.get<Agent[]>('/agents', { params }),
  get: (id: string) => api.get<Agent>(`/agents/${id}`),
  terminate: (id: string) => api.post<Agent>(`/agents/${id}/terminate`),
};
