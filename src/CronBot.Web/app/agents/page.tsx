'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { agentsApi, projectsApi, Agent } from '@/lib/api';
import { Sidebar } from '@/components/Sidebar';
import { Bot, Plus, Play, Square, RefreshCw, Activity, Cpu, HardDrive, Trash2, FileText, X } from 'lucide-react';
import { useState } from 'react';
import Link from 'next/link';
import clsx from 'clsx';

const statusFilters = [
  { value: '', label: 'All Status' },
  { value: 'idle', label: 'Idle' },
  { value: 'working', label: 'Working' },
  { value: 'paused', label: 'Paused' },
  { value: 'blocked', label: 'Blocked' },
  { value: 'error', label: 'Error' },
  { value: 'terminated', label: 'Terminated' },
];

export default function AgentsPage() {
  const [statusFilter, setStatusFilter] = useState('');
  const [showSpawnModal, setShowSpawnModal] = useState(false);

  const { data: agents, isLoading } = useQuery({
    queryKey: ['agents', statusFilter],
    queryFn: async () => {
      const res = await agentsApi.getAll(statusFilter ? { status: statusFilter } : undefined);
      return res.data;
    },
  });

  const { data: projects } = useQuery({
    queryKey: ['projects'],
    queryFn: async () => {
      const res = await projectsApi.getAll();
      return res.data;
    },
  });

  const stats = {
    total: agents?.length || 0,
    working: agents?.filter((a) => a.status === 'working').length || 0,
    idle: agents?.filter((a) => a.status === 'idle').length || 0,
    error: agents?.filter((a) => a.status === 'error').length || 0,
  };

  return (
    <div className="flex h-screen bg-gray-50">
      <Sidebar />

      <main className="flex-1 overflow-auto">
        <div className="p-8">
          <div className="flex items-center justify-between mb-8">
            <h1 className="text-3xl font-bold text-gray-900">Agents</h1>
            <button
              onClick={() => setShowSpawnModal(true)}
              className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
            >
              <Plus className="w-4 h-4" />
              Spawn Agent
            </button>
          </div>

          {/* Stats */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
            <StatCard label="Total Agents" value={stats.total} icon={<Bot className="w-5 h-5" />} />
            <StatCard
              label="Working"
              value={stats.working}
              icon={<Activity className="w-5 h-5" />}
              color="green"
            />
            <StatCard label="Idle" value={stats.idle} icon={<Play className="w-5 h-5" />} color="blue" />
            <StatCard
              label="Errors"
              value={stats.error}
              icon={<Square className="w-5 h-5" />}
              color="red"
            />
          </div>

          {/* Filter */}
          <div className="bg-white rounded-lg shadow p-4 mb-6">
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              {statusFilters.map((filter) => (
                <option key={filter.value} value={filter.value}>
                  {filter.label}
                </option>
              ))}
            </select>
          </div>

          {/* Agent Grid */}
          {isLoading ? (
            <div className="text-center py-12 text-gray-500">Loading agents...</div>
          ) : agents && agents.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {agents.map((agent) => (
                <AgentCard key={agent.id} agent={agent} projects={projects || []} />
              ))}
            </div>
          ) : (
            <div className="text-center py-12 bg-white rounded-lg shadow">
              <Bot className="w-12 h-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No agents running</h3>
              <p className="text-gray-500 mb-4">Spawn an agent to start working on tasks</p>
              <button
                onClick={() => setShowSpawnModal(true)}
                className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Spawn Agent
              </button>
            </div>
          )}

          {showSpawnModal && <SpawnAgentModal onClose={() => setShowSpawnModal(false)} />}
        </div>
      </main>
    </div>
  );
}

function StatCard({
  label,
  value,
  icon,
  color = 'gray',
}: {
  label: string;
  value: number;
  icon: React.ReactNode;
  color?: 'gray' | 'green' | 'blue' | 'red';
}) {
  const colors = {
    gray: 'bg-gray-100 text-gray-600',
    green: 'bg-green-100 text-green-600',
    blue: 'bg-blue-100 text-blue-600',
    red: 'bg-red-100 text-red-600',
  };

  return (
    <div className="bg-white rounded-lg shadow p-4">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-gray-500">{label}</p>
          <p className="text-2xl font-bold text-gray-900">{value}</p>
        </div>
        <div className={clsx('p-3 rounded-full', colors[color])}>{icon}</div>
      </div>
    </div>
  );
}

function AgentCard({ agent, projects }: { agent: Agent; projects: { id: string; name: string }[] }) {
  const queryClient = useQueryClient();
  const [showLogs, setShowLogs] = useState(false);
  const project = projects.find((p) => p.id === agent.projectId);

  const statusColors: Record<string, { bg: string; dot: string }> = {
    idle: { bg: 'bg-gray-50 border-gray-200', dot: 'bg-gray-400' },
    working: { bg: 'bg-green-50 border-green-200', dot: 'bg-green-500' },
    paused: { bg: 'bg-yellow-50 border-yellow-200', dot: 'bg-yellow-500' },
    blocked: { bg: 'bg-orange-50 border-orange-200', dot: 'bg-orange-500' },
    error: { bg: 'bg-red-50 border-red-200', dot: 'bg-red-500' },
    terminated: { bg: 'bg-gray-100 border-gray-300', dot: 'bg-gray-500' },
  };

  const terminateMutation = useMutation({
    mutationFn: () => agentsApi.terminate(agent.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['agents'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => agentsApi.delete(agent.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['agents'] });
    },
  });

  const colors = statusColors[agent.status] || statusColors.idle;

  return (
    <div className={clsx('rounded-lg border-2 p-4', colors.bg)}>
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-2">
          <div className={clsx('w-2 h-2 rounded-full', colors.dot)} />
          <span className="text-sm font-medium text-gray-700 capitalize">{agent.status}</span>
        </div>
        <span className="text-xs text-gray-500 font-mono">
          {agent.containerName || agent.id.slice(0, 8)}
        </span>
      </div>

      <Link href={`/projects/${agent.projectId}`} className="block mb-3">
        <p className="text-sm text-gray-500">Project</p>
        <p className="font-medium text-gray-900 hover:text-primary-600">{project?.name || 'Unknown'}</p>
      </Link>

      {agent.currentTaskId && (
        <Link href={`/tasks/${agent.currentTaskId}`} className="block mb-3">
          <p className="text-sm text-gray-500">Current Task</p>
          <p className="font-medium text-primary-600 hover:underline">#{agent.currentTaskId.slice(0, 8)}</p>
        </Link>
      )}

      {agent.statusMessage && (
        <p className="text-sm text-gray-600 mb-3 line-clamp-2">{agent.statusMessage}</p>
      )}

      {/* Metrics */}
      <div className="grid grid-cols-2 gap-2 mb-4 text-xs">
        <div className="flex items-center gap-1 text-gray-500">
          <Cpu className="w-3 h-3" />
          <span>{agent.cpuUsagePercent?.toFixed(1) || 0}% CPU</span>
        </div>
        <div className="flex items-center gap-1 text-gray-500">
          <HardDrive className="w-3 h-3" />
          <span>{agent.memoryUsageMb || 0} MB</span>
        </div>
        <div className="flex items-center gap-1 text-gray-500">
          <CheckCircle className="w-3 h-3" />
          <span>{agent.tasksCompleted} tasks</span>
        </div>
        <div className="flex items-center gap-1 text-gray-500">
          <GitCommit className="w-3 h-3" />
          <span>{agent.commitsMade} commits</span>
        </div>
      </div>

      {/* Actions */}
      <div className="flex gap-2">
        <button
          onClick={() => setShowLogs(true)}
          className="flex-1 flex items-center justify-center gap-1 px-3 py-1.5 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200 transition-colors"
        >
          <FileText className="w-3 h-3" />
          Logs
        </button>
        {agent.status === 'working' && (
          <button
            onClick={() => terminateMutation.mutate()}
            disabled={terminateMutation.isPending}
            className="flex-1 flex items-center justify-center gap-1 px-3 py-1.5 text-sm bg-red-100 text-red-700 rounded hover:bg-red-200 transition-colors disabled:opacity-50"
          >
            <Square className="w-3 h-3" />
            Terminate
          </button>
        )}
        {(agent.status === 'paused' || agent.status === 'blocked') && (
          <button className="flex-1 flex items-center justify-center gap-1 px-3 py-1.5 text-sm bg-green-100 text-green-700 rounded hover:bg-green-200 transition-colors">
            <RefreshCw className="w-3 h-3" />
            Resume
          </button>
        )}
        <button
          onClick={() => {
            if (confirm('Are you sure you want to delete this agent?')) {
              deleteMutation.mutate();
            }
          }}
          disabled={deleteMutation.isPending}
          className="flex-1 flex items-center justify-center gap-1 px-3 py-1.5 text-sm bg-red-100 text-red-700 rounded hover:bg-red-200 transition-colors disabled:opacity-50"
        >
          <Trash2 className="w-3 h-3" />
          Delete
        </button>
      </div>

      <p className="text-xs text-gray-400 mt-3">
        Started {new Date(agent.startedAt).toLocaleString()}
      </p>

      {showLogs && <AgentLogsModal agentId={agent.id} onClose={() => setShowLogs(false)} />}
    </div>
  );
}

// Import missing icons
function CheckCircle({ className }: { className?: string }) {
  return (
    <svg className={className} fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
  );
}

function GitCommit({ className }: { className?: string }) {
  return (
    <svg className={className} fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <circle cx="12" cy="12" r="3" strokeWidth={2} />
      <path strokeLinecap="round" strokeWidth={2} d="M12 3v6M12 15v6" />
    </svg>
  );
}

function SpawnAgentModal({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient();
  const [projectId, setProjectId] = useState('');

  const { data: projects } = useQuery({
    queryKey: ['projects'],
    queryFn: async () => {
      const res = await projectsApi.getAll();
      return res.data;
    },
  });

  const spawnMutation = useMutation({
    mutationFn: () => projectsApi.createAgent(projectId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['agents'] });
      onClose();
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    spawnMutation.mutate();
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-xl font-semibold mb-4">Spawn New Agent</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Project</label>
            <select
              value={projectId}
              onChange={(e) => setProjectId(e.target.value)}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              required
            >
              <option value="">Select a project</option>
              {projects?.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>

          <div className="bg-gray-50 rounded-lg p-4 text-sm text-gray-600">
            <p className="font-medium mb-2">Agent will:</p>
            <ul className="list-disc list-inside space-y-1">
              <li>Pick up available tasks from the backlog</li>
              <li>Work autonomously based on project autonomy level</li>
              <li>Report progress and blockers automatically</li>
            </ul>
          </div>

          <div className="flex justify-end gap-3 pt-4">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={spawnMutation.isPending}
              className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
            >
              {spawnMutation.isPending ? 'Spawning...' : 'Spawn Agent'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function AgentLogsModal({ agentId, onClose }: { agentId: string; onClose: () => void }) {
  const [tail, setTail] = useState(100);

  const { data: logs, isLoading, refetch } = useQuery({
    queryKey: ['agent-logs', agentId, tail],
    queryFn: async () => {
      const res = await agentsApi.getLogs(agentId, tail);
      return res.data;
    },
  });

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[80vh] flex flex-col">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="text-xl font-semibold">Agent Logs</h2>
          <div className="flex items-center gap-2">
            <select
              value={tail}
              onChange={(e) => setTail(Number(e.target.value))}
              className="px-2 py-1 border rounded text-sm"
            >
              <option value={50}>Last 50</option>
              <option value={100}>Last 100</option>
              <option value={500}>Last 500</option>
              <option value={1000}>Last 1000</option>
            </select>
            <button
              onClick={() => refetch()}
              className="p-1 hover:bg-gray-100 rounded"
              title="Refresh"
            >
              <RefreshCw className="w-4 h-4" />
            </button>
            <button
              onClick={onClose}
              className="p-1 hover:bg-gray-100 rounded"
              title="Close"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>
        <div className="flex-1 overflow-auto p-4 bg-gray-900">
          {isLoading ? (
            <div className="text-gray-400 text-center py-8">Loading logs...</div>
          ) : (
            <pre className="text-xs text-gray-100 font-mono whitespace-pre-wrap">
              {logs || 'No logs available'}
            </pre>
          )}
        </div>
      </div>
    </div>
  );
}
