'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { projectsApi, teamsApi, tasksApi, Project } from '@/lib/api';
import { Sidebar } from '@/components/Sidebar';
import { AuthGuard } from '@/components/AuthGuard';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Plus, FolderKanban, MoreVertical, ArrowRight, Bot, Sparkles, CheckCircle } from 'lucide-react';
import { useState } from 'react';

export default function ProjectsPage() {
  const queryClient = useQueryClient();
  const [showNewProject, setShowNewProject] = useState(false);

  const { data: projects, isLoading } = useQuery({
    queryKey: ['projects'],
    queryFn: async () => {
      const res = await projectsApi.getAll();
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
            <h1 className="text-3xl font-bold text-gray-900">Projects</h1>
            <button
              onClick={() => setShowNewProject(true)}
              className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
            >
              <Plus className="w-4 h-4" />
              New Project
            </button>
          </div>

          {isLoading ? (
            <div className="text-center py-12 text-gray-500">Loading projects...</div>
          ) : projects && projects.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {projects.map((project) => (
                <ProjectCard key={project.id} project={project} />
              ))}
            </div>
          ) : (
            <div className="text-center py-12">
              <FolderKanban className="w-12 h-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No projects yet</h3>
              <p className="text-gray-500 mb-4">Create your first project to get started</p>
              <button
                onClick={() => setShowNewProject(true)}
                className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Create Project
              </button>
            </div>
          )}

          {showNewProject && (
            <NewProjectModal onClose={() => setShowNewProject(false)} />
          )}
        </div>
      </main>
    </div>
    </AuthGuard>
  );
}

function ProjectCard({ project }: { project: Project }) {
  return (
    <Link
      href={`/projects/${project.id}`}
      className="block bg-white rounded-lg shadow hover:shadow-md transition-shadow p-6"
    >
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
            <FolderKanban className="w-5 h-5 text-primary-600" />
          </div>
          <div>
            <h3 className="font-semibold text-gray-900">{project.name}</h3>
            <p className="text-sm text-gray-500">{project.slug}</p>
          </div>
        </div>
        <button
          onClick={(e) => {
            e.preventDefault();
          }}
          className="p-1 hover:bg-gray-100 rounded"
        >
          <MoreVertical className="w-4 h-4 text-gray-400" />
        </button>
      </div>

      {project.description && (
        <p className="text-sm text-gray-600 mb-4 line-clamp-2">{project.description}</p>
      )}

      <div className="flex items-center gap-4 text-sm text-gray-500">
        <span className={`px-2 py-1 rounded ${project.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'}`}>
          {project.isActive ? 'Active' : 'Inactive'}
        </span>
        <span>Autonomy: {project.autonomyLevel}</span>
        <span>Max Agents: {project.maxAgents}</span>
      </div>
    </Link>
  );
}

function NewProjectModal({ onClose }: { onClose: () => void }) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [step, setStep] = useState(1);
  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [description, setDescription] = useState('');
  const [goals, setGoals] = useState('');
  const [initialTasks, setInitialTasks] = useState<string[]>(['']);
  const [createdProject, setCreatedProject] = useState<Project | null>(null);
  const [createdTasks, setCreatedTasks] = useState<number>(0);

  const { data: teams } = useQuery({
    queryKey: ['teams'],
    queryFn: async () => {
      const res = await teamsApi.getAll();
      return res.data;
    },
  });

  const createProjectMutation = useMutation({
    mutationFn: async () => {
      // Find or create a team
      let teamId = teams?.[0]?.id;

      if (!teamId) {
        // Create a personal team
        const teamRes = await teamsApi.create({
          name: 'My Team',
          slug: 'my-team',
          description: 'Personal team',
          isPersonal: true,
        });
        teamId = teamRes.data.id;
        await queryClient.invalidateQueries({ queryKey: ['teams'] });
      }

      const res = await projectsApi.create({ name, slug, description, teamId });
      return res.data;
    },
    onSuccess: (project) => {
      setCreatedProject(project);
      setStep(2);
    },
  });

  const createTasksMutation = useMutation({
    mutationFn: async () => {
      if (!createdProject) return;

      const validTasks = initialTasks.filter(t => t.trim());
      let count = 0;

      for (const taskTitle of validTasks) {
        await tasksApi.createInProject(createdProject.id, {
          title: taskTitle,
          description: '',
          type: 'task',
        });
        count++;
      }

      setCreatedTasks(count);
    },
    onSuccess: () => {
      setStep(3);
    },
  });

  const handleAddTask = () => {
    setInitialTasks([...initialTasks, '']);
  };

  const handleUpdateTask = (index: number, value: string) => {
    const updated = [...initialTasks];
    updated[index] = value;
    setInitialTasks(updated);
  };

  const handleRemoveTask = (index: number) => {
    setInitialTasks(initialTasks.filter((_, i) => i !== index));
  };

  const handleGoToProject = () => {
    queryClient.invalidateQueries({ queryKey: ['projects'] });
    if (createdProject) {
      router.push(`/projects/${createdProject.id}`);
    }
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg p-6">
        {/* Progress Steps */}
        <div className="flex items-center justify-center mb-6">
          {[1, 2, 3].map((s) => (
            <div key={s} className="flex items-center">
              <div
                className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium ${
                  s <= step
                    ? 'bg-primary-600 text-white'
                    : 'bg-gray-200 text-gray-500'
                }`}
              >
                {s < step ? <CheckCircle className="w-4 h-4" /> : s}
              </div>
              {s < 3 && (
                <div className={`w-12 h-1 ${s < step ? 'bg-primary-600' : 'bg-gray-200'}`} />
              )}
            </div>
          ))}
        </div>

        {/* Step 1: Project Details */}
        {step === 1 && (
          <>
            <h2 className="text-xl font-semibold mb-4">Create New Project</h2>

            <form onSubmit={(e) => {
              e.preventDefault();
              createProjectMutation.mutate();
            }} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Project Name
                </label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value);
                    setSlug(e.target.value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''));
                  }}
                  className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
                  placeholder="My Awesome Project"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Slug
                </label>
                <input
                  type="text"
                  value={slug}
                  onChange={(e) => setSlug(e.target.value)}
                  className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
                  placeholder="my-awesome-project"
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
                  placeholder="A brief description of your project..."
                  rows={2}
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
                  disabled={createProjectMutation.isPending || !name || !slug}
                  className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
                >
                  Next: Add Tasks
                  <ArrowRight className="w-4 h-4" />
                </button>
              </div>
            </form>
          </>
        )}

        {/* Step 2: Initial Tasks */}
        {step === 2 && (
          <>
            <h2 className="text-xl font-semibold mb-2">Add Initial Tasks</h2>
            <p className="text-gray-500 text-sm mb-4">
              What would you like to build? Add some tasks to get started.
            </p>

            <div className="space-y-3 mb-4">
              {initialTasks.map((task, index) => (
                <div key={index} className="flex gap-2">
                  <input
                    type="text"
                    value={task}
                    onChange={(e) => handleUpdateTask(index, e.target.value)}
                    className="flex-1 px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
                    placeholder={`Task ${index + 1}: e.g., "Set up authentication"`}
                  />
                  {initialTasks.length > 1 && (
                    <button
                      type="button"
                      onClick={() => handleRemoveTask(index)}
                      className="px-3 py-2 text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                    >
                      Remove
                    </button>
                  )}
                </div>
              ))}

              <button
                type="button"
                onClick={handleAddTask}
                className="w-full px-3 py-2 border-2 border-dashed border-gray-300 rounded-lg text-gray-500 hover:border-primary-500 hover:text-primary-600 transition-colors"
              >
                + Add another task
              </button>
            </div>

            <div className="bg-blue-50 rounded-lg p-4 mb-4">
              <div className="flex items-start gap-2">
                <Sparkles className="w-5 h-5 text-blue-600 mt-0.5" />
                <div>
                  <p className="text-sm font-medium text-blue-900">Pro tip</p>
                  <p className="text-sm text-blue-700">
                    You can spawn an agent later to help break down complex tasks and implement features automatically.
                  </p>
                </div>
              </div>
            </div>

            <div className="flex justify-between gap-3 pt-4">
              <button
                type="button"
                onClick={() => setStep(1)}
                className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
              >
                Back
              </button>
              <button
                type="button"
                onClick={() => createTasksMutation.mutate()}
                disabled={createTasksMutation.isPending}
                className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
              >
                {createTasksMutation.isPending ? 'Creating...' : 'Create Tasks'}
              </button>
            </div>
          </>
        )}

        {/* Step 3: Complete */}
        {step === 3 && (
          <div className="text-center py-4">
            <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <CheckCircle className="w-8 h-8 text-green-600" />
            </div>

            <h2 className="text-xl font-semibold text-gray-900 mb-2">
              Project Created!
            </h2>

            <p className="text-gray-500 mb-6">
              {createdTasks > 0
                ? `Created ${createdTasks} task${createdTasks > 1 ? 's' : ''} for ${createdProject?.name}`
                : `${createdProject?.name} is ready to go!`}
            </p>

            <div className="flex flex-col gap-3">
              <button
                onClick={handleGoToProject}
                className="w-full flex items-center justify-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                <FolderKanban className="w-4 h-4" />
                Go to Project
              </button>

              <button
                onClick={handleGoToProject}
                className="w-full flex items-center justify-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
              >
                <Bot className="w-4 h-4" />
                Spawn Agent to Start Working
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
