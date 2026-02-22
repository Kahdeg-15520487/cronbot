'use client';

import { useQuery } from '@tanstack/react-query';
import { projectsApi, tasksApi, agentsApi, teamsApi } from '@/lib/api';
import { Sidebar } from '@/components/Sidebar';
import { AuthGuard } from '@/components/AuthGuard';
import Link from 'next/link';
import { LayoutDashboard, FolderKanban, Users, Bot, CheckCircle, Clock, AlertCircle, Play } from 'lucide-react';

export default function HomePage() {
  const { data: projects } = useQuery({
    queryKey: ['projects'],
    queryFn: async () => {
      const res = await projectsApi.getAll();
      return res.data;
    },
  });

  const { data: tasks } = useQuery({
    queryKey: ['tasks'],
    queryFn: async () => {
      const res = await tasksApi.getAll();
      return res.data;
    },
  });

  const { data: agents } = useQuery({
    queryKey: ['agents'],
    queryFn: async () => {
      const res = await agentsApi.getAll();
      return res.data;
    },
  });

  const { data: teams } = useQuery({
    queryKey: ['teams'],
    queryFn: async () => {
      const res = await teamsApi.getAll();
      return res.data;
    },
  });

  // Calculate stats
  const stats = {
    activeProjects: projects?.filter((p) => p.isActive).length || 0,
    runningAgents: agents?.filter((a) => a.status === 'working').length || 0,
    tasksCompleted: tasks?.filter((t) => t.status === 'done').length || 0,
    teamMembers: teams?.length || 0,
    tasksInProgress: tasks?.filter((t) => t.status === 'in_progress').length || 0,
    tasksBlocked: tasks?.filter((t) => t.status === 'blocked').length || 0,
  };

  // Get recent tasks (last 10)
  const recentTasks = tasks
    ?.slice()
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5);

  // Get working agents
  const workingAgents = agents?.filter((a) => a.status === 'working').slice(0, 5);

  return (
    <AuthGuard>
      <div className="flex h-screen bg-gray-50">
        <Sidebar />

        <main className="flex-1 overflow-auto">
          <div className="p-8">
            <h1 className="text-3xl font-bold text-gray-900 mb-8">
              Welcome to CronBot
            </h1>

            {/* Stats Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
              <StatCard
                title="Active Projects"
                value={stats.activeProjects}
                icon={<FolderKanban className="w-6 h-6" />}
                href="/projects"
                color="blue"
              />
              <StatCard
                title="Running Agents"
                value={stats.runningAgents}
                icon={<Bot className="w-6 h-6" />}
                href="/agents"
                color="green"
              />
              <StatCard
                title="Tasks Completed"
                value={stats.tasksCompleted}
                icon={<CheckCircle className="w-6 h-6" />}
                href="/tasks?status=done"
                color="purple"
              />
              <StatCard
                title="Teams"
                value={stats.teamMembers}
                icon={<Users className="w-6 h-6" />}
                href="/team"
                color="gray"
              />
            </div>

            {/* Secondary Stats */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
              <MiniStatCard
                title="In Progress"
                value={stats.tasksInProgress}
                icon={<Play className="w-4 h-4" />}
                color="yellow"
              />
              <MiniStatCard
                title="Blocked"
                value={stats.tasksBlocked}
                icon={<AlertCircle className="w-4 h-4" />}
                color="red"
              />
              <MiniStatCard
                title="Total Tasks"
                value={tasks?.length || 0}
                icon={<Clock className="w-4 h-4" />}
                color="gray"
              />
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Recent Tasks */}
              <div className="bg-white rounded-lg shadow p-6">
                <div className="flex items-center justify-between mb-4">
                  <h2 className="text-xl font-semibold">Recent Tasks</h2>
                  <Link href="/tasks" className="text-primary-600 hover:underline text-sm">
                    View all
                  </Link>
                </div>
                {recentTasks && recentTasks.length > 0 ? (
                  <div className="space-y-3">
                    {recentTasks.map((task) => (
                      <TaskRow key={task.id} task={task} />
                    ))}
                  </div>
                ) : (
                  <p className="text-gray-500 text-center py-8">No tasks yet</p>
                )}
              </div>

              {/* Working Agents */}
              <div className="bg-white rounded-lg shadow p-6">
                <div className="flex items-center justify-between mb-4">
                  <h2 className="text-xl font-semibold">Active Agents</h2>
                  <Link href="/agents" className="text-primary-600 hover:underline text-sm">
                    View all
                  </Link>
                </div>
                {workingAgents && workingAgents.length > 0 ? (
                  <div className="space-y-3">
                    {workingAgents.map((agent) => (
                      <AgentRow key={agent.id} agent={agent} projects={projects || []} />
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-8">
                    <Bot className="w-12 h-12 text-gray-300 mx-auto mb-2" />
                    <p className="text-gray-500">No active agents</p>
                    <Link
                      href="/agents"
                      className="inline-block mt-2 text-primary-600 hover:underline text-sm"
                    >
                      Spawn an agent
                    </Link>
                  </div>
                )}
              </div>
            </div>

            {/* Quick Actions */}
            <div className="mt-8 bg-white rounded-lg shadow p-6">
              <h2 className="text-xl font-semibold mb-4">Quick Actions</h2>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                <QuickActionButton label="Create Project" href="/projects" />
                <QuickActionButton label="Add Task" href="/tasks" />
                <QuickActionButton label="Spawn Agent" href="/agents" />
                <QuickActionButton label="Create Team" href="/team" />
              </div>
            </div>
          </div>
        </main>
      </div>
    </AuthGuard>
  );
}

function StatCard({
  title,
  value,
  icon,
  href,
  color,
}: {
  title: string;
  value: number;
  icon: React.ReactNode;
  href: string;
  color: 'blue' | 'green' | 'purple' | 'gray';
}) {
  const colors = {
    blue: 'bg-blue-100 text-blue-600',
    green: 'bg-green-100 text-green-600',
    purple: 'bg-purple-100 text-purple-600',
    gray: 'bg-gray-100 text-gray-600',
  };

  return (
    <Link
      href={href}
      className="bg-white rounded-lg shadow p-6 hover:shadow-md transition-shadow"
    >
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-gray-500">{title}</p>
          <p className="text-3xl font-bold text-gray-900">{value}</p>
        </div>
        <div className={`p-3 rounded-full ${colors[color]}`}>{icon}</div>
      </div>
    </Link>
  );
}

function MiniStatCard({
  title,
  value,
  icon,
  color,
}: {
  title: string;
  value: number;
  icon: React.ReactNode;
  color: 'yellow' | 'red' | 'gray';
}) {
  const colors = {
    yellow: 'bg-yellow-50 border-yellow-200 text-yellow-700',
    red: 'bg-red-50 border-red-200 text-red-700',
    gray: 'bg-gray-50 border-gray-200 text-gray-700',
  };

  return (
    <div className={`flex items-center gap-3 p-4 rounded-lg border ${colors[color]}`}>
      {icon}
      <div>
        <p className="text-2xl font-bold">{value}</p>
        <p className="text-sm">{title}</p>
      </div>
    </div>
  );
}

function TaskRow({ task }: { task: { id: string; number: number; title: string; status: string; type: string } }) {
  const statusColors: Record<string, string> = {
    backlog: 'bg-gray-100 text-gray-600',
    sprint: 'bg-blue-100 text-blue-600',
    in_progress: 'bg-yellow-100 text-yellow-600',
    review: 'bg-purple-100 text-purple-600',
    blocked: 'bg-red-100 text-red-600',
    done: 'bg-green-100 text-green-600',
  };

  return (
    <Link
      href={`/tasks/${task.id}`}
      className="flex items-center justify-between py-2 px-3 -mx-3 rounded-lg hover:bg-gray-50 transition-colors"
    >
      <div className="flex items-center gap-3">
        <span className="text-sm text-gray-500">#{task.number}</span>
        <span className="text-gray-700 truncate max-w-[200px]">{task.title}</span>
      </div>
      <span className={`text-xs px-2 py-1 rounded ${statusColors[task.status] || statusColors.backlog}`}>
        {task.status.replace('_', ' ')}
      </span>
    </Link>
  );
}

function AgentRow({
  agent,
  projects,
}: {
  agent: { id: string; projectId: string; status: string; startedAt: string };
  projects: { id: string; name: string }[];
}) {
  const project = projects.find((p) => p.id === agent.projectId);

  return (
    <div className="flex items-center justify-between py-2 px-3 -mx-3 rounded-lg hover:bg-gray-50 transition-colors">
      <div className="flex items-center gap-3">
        <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
        <span className="text-gray-700">{project?.name || 'Unknown Project'}</span>
      </div>
      <span className="text-xs text-gray-500">
        {new Date(agent.startedAt).toLocaleTimeString()}
      </span>
    </div>
  );
}

function QuickActionButton({ label, href }: { label: string; href: string }) {
  return (
    <Link
      href={href}
      className="flex items-center justify-center p-4 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors text-gray-700 font-medium"
    >
      {label}
    </Link>
  );
}
