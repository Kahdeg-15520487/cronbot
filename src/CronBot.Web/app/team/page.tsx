'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { teamsApi, usersApi, Team, User } from '@/lib/api';
import { Sidebar } from '@/components/Sidebar';
import { Users, Plus, Settings, UserPlus, MoreVertical, Shield, User as UserIcon } from 'lucide-react';
import { useState } from 'react';
import Link from 'next/link';
import clsx from 'clsx';

export default function TeamPage() {
  const [showNewTeam, setShowNewTeam] = useState(false);

  const { data: teams, isLoading } = useQuery({
    queryKey: ['teams'],
    queryFn: async () => {
      const res = await teamsApi.getAll();
      return res.data;
    },
  });

  return (
    <div className="flex h-screen bg-gray-50">
      <Sidebar />

      <main className="flex-1 overflow-auto">
        <div className="p-8">
          <div className="flex items-center justify-between mb-8">
            <h1 className="text-3xl font-bold text-gray-900">Teams</h1>
            <button
              onClick={() => setShowNewTeam(true)}
              className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
            >
              <Plus className="w-4 h-4" />
              New Team
            </button>
          </div>

          {isLoading ? (
            <div className="text-center py-12 text-gray-500">Loading teams...</div>
          ) : teams && teams.length > 0 ? (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {teams.map((team) => (
                <TeamCard key={team.id} team={team} />
              ))}
            </div>
          ) : (
            <div className="text-center py-12 bg-white rounded-lg shadow">
              <Users className="w-12 h-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No teams yet</h3>
              <p className="text-gray-500 mb-4">Create your first team to collaborate with others</p>
              <button
                onClick={() => setShowNewTeam(true)}
                className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Create Team
              </button>
            </div>
          )}

          {showNewTeam && <NewTeamModal onClose={() => setShowNewTeam(false)} />}
        </div>
      </main>
    </div>
  );
}

function TeamCard({ team }: { team: Team }) {
  const [showMembers, setShowMembers] = useState(false);

  const { data: projects } = useQuery({
    queryKey: ['team-projects', team.id],
    queryFn: async () => {
      const res = await teamsApi.getProjects(team.id);
      return res.data;
    },
  });

  const roleColors: Record<string, string> = {
    owner: 'bg-purple-100 text-purple-700',
    admin: 'bg-red-100 text-red-700',
    member: 'bg-blue-100 text-blue-700',
    guest: 'bg-gray-100 text-gray-700',
  };

  return (
    <div className="bg-white rounded-lg shadow hover:shadow-md transition-shadow">
      <div className="p-6">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h3 className="text-xl font-semibold text-gray-900">{team.name}</h3>
            <p className="text-sm text-gray-500">{team.slug}</p>
          </div>
          <div className="flex items-center gap-2">
            {team.isPersonal && (
              <span className="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded">Personal</span>
            )}
            <button className="p-1 hover:bg-gray-100 rounded">
              <Settings className="w-4 h-4 text-gray-400" />
            </button>
          </div>
        </div>

        {team.description && <p className="text-gray-600 mb-4">{team.description}</p>}

        {/* Stats */}
        <div className="flex items-center gap-6 text-sm text-gray-500 mb-4">
          <div className="flex items-center gap-1">
            <Users className="w-4 h-4" />
            <span>0 members</span>
          </div>
          <div>
            <span>{projects?.length || 0} projects</span>
          </div>
        </div>

        {/* Projects */}
        {projects && projects.length > 0 && (
          <div className="border-t pt-4">
            <p className="text-sm font-medium text-gray-700 mb-2">Projects</p>
            <div className="flex flex-wrap gap-2">
              {projects.map((project) => (
                <Link
                  key={project.id}
                  href={`/projects/${project.id}`}
                  className="text-sm px-2 py-1 bg-gray-100 text-gray-700 rounded hover:bg-gray-200 transition-colors"
                >
                  {project.name}
                </Link>
              ))}
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center gap-2 mt-4 pt-4 border-t">
          <button
            onClick={() => setShowMembers(!showMembers)}
            className="flex items-center gap-1 px-3 py-1.5 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200 transition-colors"
          >
            <Users className="w-3 h-3" />
            View Members
          </button>
          <button className="flex items-center gap-1 px-3 py-1.5 text-sm bg-primary-100 text-primary-700 rounded hover:bg-primary-200 transition-colors">
            <UserPlus className="w-3 h-3" />
            Invite
          </button>
        </div>
      </div>
    </div>
  );
}

function NewTeamModal({ onClose }: { onClose: () => void }) {
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [description, setDescription] = useState('');

  const createMutation = useMutation({
    mutationFn: () => teamsApi.create({ name, slug, description }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['teams'] });
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
        <h2 className="text-xl font-semibold mb-4">Create New Team</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
            <input
              type="text"
              value={name}
              onChange={(e) => {
                setName(e.target.value);
                setSlug(e.target.value.toLowerCase().replace(/\s+/g, '-'));
              }}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Slug</label>
            <input
              type="text"
              value={slug}
              onChange={(e) => setSlug(e.target.value)}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              rows={3}
            />
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
