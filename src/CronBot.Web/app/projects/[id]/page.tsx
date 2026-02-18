'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { projectsApi, tasksApi, Task } from '@/lib/api';
import { Sidebar } from '@/components/Sidebar';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowLeft, Plus, Bot, Settings, ExternalLink } from 'lucide-react';
import { useState } from 'react';
import { KanbanBoard } from '@/components/KanbanBoard';

const statusColumns = [
  { id: 'backlog', name: 'Backlog', color: 'bg-gray-100' },
  { id: 'sprint', name: 'Sprint', color: 'bg-blue-100' },
  { id: 'in_progress', name: 'In Progress', color: 'bg-yellow-100' },
  { id: 'review', name: 'Review', color: 'bg-purple-100' },
  { id: 'blocked', name: 'Blocked', color: 'bg-red-100' },
  { id: 'done', name: 'Done', color: 'bg-green-100' },
];

export default function ProjectDetailPage() {
  const params = useParams();
  const projectId = params.id as string;
  const [showNewTask, setShowNewTask] = useState(false);

  const { data: project, isLoading: projectLoading } = useQuery({
    queryKey: ['project', projectId],
    queryFn: async () => {
      const res = await projectsApi.get(projectId);
      return res.data;
    },
  });

  const { data: tasks, isLoading: tasksLoading } = useQuery({
    queryKey: ['tasks', projectId],
    queryFn: async () => {
      const res = await tasksApi.getAll({ projectId });
      return res.data;
    },
  });

  const { data: agents } = useQuery({
    queryKey: ['agents', projectId],
    queryFn: async () => {
      const res = await projectsApi.getAgents(projectId);
      return res.data;
    },
  });

  if (projectLoading) {
    return (
      <div className="flex h-screen bg-gray-50">
        <Sidebar />
        <main className="flex-1 flex items-center justify-center">
          <div className="text-gray-500">Loading project...</div>
        </main>
      </div>
    );
  }

  if (!project) {
    return (
      <div className="flex h-screen bg-gray-50">
        <Sidebar />
        <main className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <h2 className="text-xl font-semibold text-gray-900">Project not found</h2>
            <Link href="/projects" className="text-primary-600 hover:underline mt-2 block">
              Back to projects
            </Link>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="flex h-screen bg-gray-50">
      <Sidebar />

      <main className="flex-1 overflow-hidden flex flex-col">
        {/* Header */}
        <header className="bg-white border-b px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Link
                href="/projects"
                className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
              >
                <ArrowLeft className="w-5 h-5 text-gray-600" />
              </Link>
              <div>
                <h1 className="text-2xl font-bold text-gray-900">{project.name}</h1>
                <p className="text-sm text-gray-500">{project.description || 'No description'}</p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <button
                onClick={() => setShowNewTask(true)}
                className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                <Plus className="w-4 h-4" />
                Add Task
              </button>
              <button className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors">
                <Bot className="w-4 h-4" />
                Spawn Agent
              </button>
              <button className="p-2 hover:bg-gray-100 rounded-lg transition-colors">
                <Settings className="w-5 h-5 text-gray-600" />
              </button>
            </div>
          </div>
        </header>

        {/* Stats Bar */}
        <div className="bg-white border-b px-6 py-3">
          <div className="flex items-center gap-8 text-sm">
            <StatItem
              label="Active Agents"
              value={agents?.filter((a) => a.status === 'working').length || 0}
            />
            <StatItem
              label="Tasks in Progress"
              value={tasks?.filter((t) => t.status === 'in_progress').length || 0}
            />
            <StatItem
              label="Tasks Done"
              value={tasks?.filter((t) => t.status === 'done').length || 0}
            />
            <StatItem
              label="Autonomy Level"
              value={project.autonomyLevel}
            />
          </div>
        </div>

        {/* Kanban Board */}
        <div className="flex-1 overflow-auto p-6">
          {tasksLoading ? (
            <div className="text-center py-12 text-gray-500">Loading tasks...</div>
          ) : (
            <KanbanBoard
              projectId={projectId}
              tasks={tasks || []}
              columns={statusColumns}
            />
          )}
        </div>

        {showNewTask && (
          <NewTaskModal
            projectId={projectId}
            onClose={() => setShowNewTask(false)}
          />
        )}
      </main>
    </div>
  );
}

function StatItem({ label, value }: { label: string; value: number | string }) {
  return (
    <div>
      <span className="text-gray-500">{label}:</span>
      <span className="ml-2 font-medium text-gray-900">{value}</span>
    </div>
  );
}

function NewTaskModal({
  projectId,
  onClose,
}: {
  projectId: string;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [type, setType] = useState('task');

  const createMutation = useMutation({
    mutationFn: () => tasksApi.createInProject(projectId, { title, description, type }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', projectId] });
      onClose();
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate();
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-xl font-semibold mb-4">Create New Task</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Title
            </label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Description
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              rows={3}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Type
            </label>
            <select
              value={type}
              onChange={(e) => setType(e.target.value)}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value="task">Task</option>
              <option value="bug">Bug</option>
              <option value="feature">Feature</option>
              <option value="idea">Idea</option>
              <option value="epic">Epic</option>
            </select>
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
              disabled={createMutation.isPending}
              className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
            >
              {createMutation.isPending ? 'Creating...' : 'Create Task'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
