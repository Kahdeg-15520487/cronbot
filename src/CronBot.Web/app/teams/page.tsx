'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { teamsApi, Team, projectsApi } from '@/lib/api';
import { Sidebar } from '@/components/Sidebar';
import { AuthGuard } from '@/components/AuthGuard';
import { Users, Plus, Edit, Trash2, FolderKanban, X, Settings } from 'lucide-react';
import { useState } from 'react';
import Link from 'next/link';

export default function TeamsPage() {
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingTeam, setEditingTeam] = useState<Team | null>(null);

  const { data: teams, isLoading } = useQuery({
    queryKey: ['teams'],
    queryFn: async () => {
      const res = await teamsApi.getAll();
      return res.data;
    },
  });

  return (
    <AuthGuard>
      <div className="flex h-screen bg-gray-50">
        <Sidebar />

      <main className="flex-1 overflow-auto">
        <div className="p-8">
          <div className="flex items-center justify-between mb-8">
            <h1 className="text-3xl font-bold text-gray-900">Teams</h1>
            <button
              onClick={() => setShowCreateModal(true)}
              className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
            >
              <Plus className="w-4 h-4" />
              Create Team
            </button>
          </div>

          {isLoading ? (
            <div className="text-center py-12 text-gray-500">Loading teams...</div>
          ) : teams && teams.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {teams.map((team) => (
                <TeamCard
                  key={team.id}
                  team={team}
                  onEdit={() => setEditingTeam(team)}
                />
              ))}
            </div>
          ) : (
            <div className="text-center py-12 bg-white rounded-lg shadow">
              <Users className="w-12 h-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No teams yet</h3>
              <p className="text-gray-500 mb-4">Create your first team to get started</p>
              <button
                onClick={() => setShowCreateModal(true)}
                className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Create Team
              </button>
            </div>
          )}

          {showCreateModal && <CreateTeamModal onClose={() => setShowCreateModal(false)} />}
          {editingTeam && (
            <EditTeamModal team={editingTeam} onClose={() => setEditingTeam(null)} />
          )}
        </div>
      </main>
    </div>
    </AuthGuard>
  );
}

function TeamCard({ team, onEdit }: { team: Team; onEdit: () => void }) {
  const queryClient = useQueryClient();

  const { data: projects } = useQuery({
    queryKey: ['team-projects', team.id],
    queryFn: async () => {
      const res = await teamsApi.getProjects(team.id);
      return res.data;
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => teamsApi.delete(team.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
    },
  });

  const handleDelete = () => {
    if (confirm('Are you sure you want to delete this team? This will also delete all projects in this team.')) {
      deleteMutation.mutate();
    }
  };

  return (
    <div className="bg-white rounded-lg shadow p-6">
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-primary-100 rounded-lg">
            <Users className="w-5 h-5 text-primary-600" />
          </div>
          <div>
            <h3 className="font-semibold text-gray-900">{team.name}</h3>
            <p className="text-sm text-gray-500">@{team.slug}</p>
          </div>
        </div>
        <div className="flex items-center gap-1">
          <button
            onClick={onEdit}
            className="p-1 hover:bg-gray-100 rounded"
            title="Edit"
          >
            <Edit className="w-4 h-4 text-gray-400" />
          </button>
          <button
            onClick={handleDelete}
            disabled={deleteMutation.isPending}
            className="p-1 hover:bg-red-50 rounded"
            title="Delete"
          >
            <Trash2 className="w-4 h-4 text-red-400" />
          </button>
        </div>
      </div>

      {team.description && (
        <p className="text-sm text-gray-600 mb-4 line-clamp-2">{team.description}</p>
      )}

      <div className="flex items-center justify-between pt-4 border-t">
        <div className="flex items-center gap-1 text-sm text-gray-500">
          <FolderKanban className="w-4 h-4" />
          <span>{projects?.length || 0} projects</span>
        </div>
        {team.isPersonal && (
          <span className="px-2 py-0.5 text-xs bg-blue-100 text-blue-700 rounded">
            Personal
          </span>
        )}
      </div>
    </div>
  );
}

function CreateTeamModal({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState({
    name: '',
    slug: '',
    description: '',
    isPersonal: false,
  });

  const createMutation = useMutation({
    mutationFn: () => teamsApi.create(form),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      onClose();
    },
  });

  const handleNameChange = (name: string) => {
    setForm({
      ...form,
      name,
      slug: name
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-|-$/g, ''),
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate();
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold">Create New Team</h2>
          <button onClick={onClose} className="p-1 hover:bg-gray-100 rounded">
            <X className="w-4 h-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Name <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={form.name}
              onChange={(e) => handleNameChange(e.target.value)}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Slug <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={form.slug}
              onChange={(e) => setForm({ ...form, slug: e.target.value })}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 font-mono"
              required
            />
            <p className="text-xs text-gray-500 mt-1">Used in URLs and references</p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              rows={3}
            />
          </div>

          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isPersonal"
              checked={form.isPersonal}
              onChange={(e) => setForm({ ...form, isPersonal: e.target.checked })}
              className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
            />
            <label htmlFor="isPersonal" className="text-sm text-gray-700">
              Personal team (only you can access)
            </label>
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
              {createMutation.isPending ? 'Creating...' : 'Create Team'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function EditTeamModal({ team, onClose }: { team: Team; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState({
    name: team.name,
    slug: team.slug,
    description: team.description || '',
    isPersonal: team.isPersonal,
  });

  const updateMutation = useMutation({
    mutationFn: () => teamsApi.update(team.id, form),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      onClose();
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMutation.mutate();
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold">Edit Team</h2>
          <button onClick={onClose} className="p-1 hover:bg-gray-100 rounded">
            <X className="w-4 h-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Name <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Slug <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={form.slug}
              onChange={(e) => setForm({ ...form, slug: e.target.value })}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 font-mono"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              rows={3}
            />
          </div>

          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="editIsPersonal"
              checked={form.isPersonal}
              onChange={(e) => setForm({ ...form, isPersonal: e.target.checked })}
              className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
            />
            <label htmlFor="editIsPersonal" className="text-sm text-gray-700">
              Personal team (only you can access)
            </label>
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
              disabled={updateMutation.isPending}
              className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
            >
              {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
