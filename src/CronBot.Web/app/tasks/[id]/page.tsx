'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tasksApi, projectsApi, Task, TaskLog, GitDiffSummary } from '@/lib/api';
import { Sidebar } from '@/components/Sidebar';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowLeft,
  Edit2,
  Trash2,
  MessageSquare,
  GitBranch,
  GitCommit,
  GitPullRequest,
  Clock,
  User,
  Bot,
  AlertCircle,
  CheckCircle,
  Play,
  Calendar,
  FileText,
  FilePlus,
  FileMinus,
  Terminal,
  Activity,
  ChevronDown,
  ChevronUp,
  ExternalLink,
  Merge,
} from 'lucide-react';
import { useState } from 'react';
import clsx from 'clsx';

const statusConfig: Record<string, { color: string; icon: typeof CheckCircle }> = {
  backlog: { color: 'bg-gray-100 text-gray-700', icon: Clock },
  sprint: { color: 'bg-blue-100 text-blue-700', icon: Calendar },
  in_progress: { color: 'bg-yellow-100 text-yellow-700', icon: Play },
  review: { color: 'bg-purple-100 text-purple-700', icon: CheckCircle },
  blocked: { color: 'bg-red-100 text-red-700', icon: AlertCircle },
  done: { color: 'bg-green-100 text-green-700', icon: CheckCircle },
};

const logTypeConfig: Record<string, { icon: typeof GitCommit; color: string; label: string }> = {
  StatusChange: { icon: Play, color: 'text-blue-500', label: 'Status changed' },
  BranchCreated: { icon: GitBranch, color: 'text-purple-500', label: 'Branch created' },
  Commit: { icon: GitCommit, color: 'text-green-500', label: 'Commit' },
  Push: { icon: GitBranch, color: 'text-indigo-500', label: 'Pushed' },
  PullRequestCreated: { icon: GitPullRequest, color: 'text-purple-500', label: 'PR created' },
  PullRequestMerged: { icon: Merge, color: 'text-green-500', label: 'PR merged' },
  FilesCreated: { icon: FilePlus, color: 'text-green-500', label: 'Files created' },
  FilesModified: { icon: FileText, color: 'text-yellow-500', label: 'Files modified' },
  FilesDeleted: { icon: FileMinus, color: 'text-red-500', label: 'Files deleted' },
  AgentMessage: { icon: Bot, color: 'text-blue-500', label: 'Agent' },
  AgentError: { icon: AlertCircle, color: 'text-red-500', label: 'Error' },
  Command: { icon: Terminal, color: 'text-gray-500', label: 'Command' },
  CommandOutput: { icon: Terminal, color: 'text-gray-400', label: 'Output' },
};

export default function TaskDetailPage() {
  const params = useParams();
  const router = useRouter();
  const taskId = params.id as string;
  const [showEditModal, setShowEditModal] = useState(false);
  const [newComment, setNewComment] = useState('');
  const [activeTab, setActiveTab] = useState<'description' | 'activity'>('description');

  const { data: task, isLoading: taskLoading } = useQuery({
    queryKey: ['task', taskId],
    queryFn: async () => {
      const res = await tasksApi.get(taskId);
      return res.data;
    },
  });

  const { data: comments } = useQuery({
    queryKey: ['task-comments', taskId],
    queryFn: async () => {
      const res = await tasksApi.getComments(taskId);
      return res.data;
    },
  });

  const { data: history } = useQuery({
    queryKey: ['task-history', taskId],
    queryFn: async () => {
      const res = await tasksApi.getHistory(taskId);
      return res.data;
    },
  });

  const { data: project } = useQuery({
    queryKey: ['project', task?.projectId],
    enabled: !!task?.projectId,
    queryFn: async () => {
      const res = await projectsApi.get(task!.projectId);
      return res.data;
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => tasksApi.delete(taskId),
    onSuccess: () => {
      router.push('/tasks');
    },
  });

  const commentMutation = useMutation({
    mutationFn: () => tasksApi.addComment(taskId, newComment),
    onSuccess: () => {
      setNewComment('');
      queryClient.invalidateQueries({ queryKey: ['task-comments', taskId] });
    },
  });

  const prMutation = useMutation({
    mutationFn: () => tasksApi.createPullRequest(taskId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['task', taskId] });
      queryClient.invalidateQueries({ queryKey: ['task-history', taskId] });
    },
  });

  const mergeMutation = useMutation({
    mutationFn: () => tasksApi.mergePullRequest(taskId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['task', taskId] });
      queryClient.invalidateQueries({ queryKey: ['task-history', taskId] });
    },
  });

  const queryClient = useQueryClient();

  if (taskLoading) {
    return (
      <div className="flex h-screen bg-gray-50">
        <Sidebar />
        <main className="flex-1 flex items-center justify-center">
          <div className="text-gray-500">Loading task...</div>
        </main>
      </div>
    );
  }

  if (!task) {
    return (
      <div className="flex h-screen bg-gray-50">
        <Sidebar />
        <main className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <h2 className="text-xl font-semibold text-gray-900">Task not found</h2>
            <Link href="/tasks" className="text-primary-600 hover:underline mt-2 block">
              Back to tasks
            </Link>
          </div>
        </main>
      </div>
    );
  }

  const StatusIcon = statusConfig[task.status]?.icon || Clock;
  const statusColor = statusConfig[task.status]?.color || 'bg-gray-100 text-gray-700';

  return (
    <div className="flex h-screen bg-gray-50">
      <Sidebar />

      <main className="flex-1 overflow-auto">
        <div className="p-8">
          {/* Header */}
          <div className="flex items-center gap-4 mb-6">
            <Link
              href="/tasks"
              className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
            >
              <ArrowLeft className="w-5 h-5 text-gray-600" />
            </Link>
            <div className="flex-1">
              <div className="flex items-center gap-3 mb-1">
                <span className="text-sm text-gray-500">#{task.number}</span>
                <span
                  className={clsx(
                    'inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded',
                    statusColor
                  )}
                >
                  <StatusIcon className="w-3 h-3" />
                  {task.status.replace('_', ' ')}
                </span>
              </div>
              <h1 className="text-2xl font-bold text-gray-900">{task.title}</h1>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setShowEditModal(true)}
                className="flex items-center gap-2 px-3 py-2 text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
              >
                <Edit2 className="w-4 h-4" />
                Edit
              </button>
              <button
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
                className="flex items-center gap-2 px-3 py-2 text-red-600 hover:bg-red-50 rounded-lg transition-colors disabled:opacity-50"
              >
                <Trash2 className="w-4 h-4" />
                Delete
              </button>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Main Content */}
            <div className="lg:col-span-2 space-y-6">
              {/* Tabs */}
              <div className="flex gap-1 bg-gray-100 p-1 rounded-lg">
                <button
                  onClick={() => setActiveTab('description')}
                  className={clsx(
                    'flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors',
                    activeTab === 'description'
                      ? 'bg-white text-gray-900 shadow'
                      : 'text-gray-600 hover:text-gray-900'
                  )}
                >
                  Description
                </button>
                <button
                  onClick={() => setActiveTab('activity')}
                  className={clsx(
                    'flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors flex items-center justify-center gap-2',
                    activeTab === 'activity'
                      ? 'bg-white text-gray-900 shadow'
                      : 'text-gray-600 hover:text-gray-900'
                  )}
                >
                  <Activity className="w-4 h-4" />
                  Activity
                  {history?.logs && history.logs.length > 0 && (
                    <span className="bg-primary-100 text-primary-700 text-xs px-1.5 py-0.5 rounded-full">
                      {history.logs.length}
                    </span>
                  )}
                </button>
              </div>

              {activeTab === 'description' ? (
                <>
                  {/* Description */}
                  <div className="bg-white rounded-lg shadow p-6">
                    <h2 className="text-lg font-semibold text-gray-900 mb-4">Description</h2>
                    {task.description ? (
                      <div className="prose prose-sm max-w-none text-gray-600">
                        {task.description}
                      </div>
                    ) : (
                      <p className="text-gray-400 italic">No description provided</p>
                    )}
                  </div>

                  {/* Comments */}
                  <div className="bg-white rounded-lg shadow p-6">
                    <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
                      <MessageSquare className="w-5 h-5" />
                      Comments
                    </h2>

                    {/* Comment List */}
                    <div className="space-y-4 mb-4">
                      {comments && comments.length > 0 ? (
                        comments.map((comment: { id: string; authorType: string; authorId: string; content: string; createdAt: string }) => (
                          <div key={comment.id} className="flex gap-3">
                            <div className="w-8 h-8 bg-gray-200 rounded-full flex items-center justify-center flex-shrink-0">
                              {comment.authorType === 'agent' ? (
                                <Bot className="w-4 h-4 text-gray-600" />
                              ) : (
                                <User className="w-4 h-4 text-gray-600" />
                              )}
                            </div>
                            <div className="flex-1">
                              <div className="flex items-center gap-2 mb-1">
                                <span className="font-medium text-gray-900">
                                  {comment.authorType === 'agent' ? 'Agent' : 'User'}
                                </span>
                                <span className="text-xs text-gray-400">
                                  {new Date(comment.createdAt).toLocaleString()}
                                </span>
                              </div>
                              <p className="text-gray-600">{comment.content}</p>
                            </div>
                          </div>
                        ))
                      ) : (
                        <p className="text-gray-400 text-center py-4">No comments yet</p>
                      )}
                    </div>

                    {/* Add Comment */}
                    <div className="flex gap-3">
                      <div className="w-8 h-8 bg-primary-100 rounded-full flex items-center justify-center flex-shrink-0">
                        <User className="w-4 h-4 text-primary-600" />
                      </div>
                      <div className="flex-1">
                        <textarea
                          value={newComment}
                          onChange={(e) => setNewComment(e.target.value)}
                          placeholder="Add a comment..."
                          className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 resize-none"
                          rows={2}
                        />
                        <div className="flex justify-end mt-2">
                          <button
                            onClick={() => commentMutation.mutate()}
                            disabled={!newComment.trim() || commentMutation.isPending}
                            className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
                          >
                            {commentMutation.isPending ? 'Posting...' : 'Post Comment'}
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                </>
              ) : (
                /* Activity Tab */
                <div className="space-y-6">
                  {/* Git Diff Summary */}
                  {history?.diffSummary && (
                    <div className="bg-white rounded-lg shadow p-6">
                      <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
                        <GitBranch className="w-5 h-5" />
                        Changes Summary
                      </h2>
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                        <div className="bg-gray-50 rounded-lg p-3">
                          <div className="text-2xl font-bold text-gray-900">
                            {history.diffSummary.commitCount}
                          </div>
                          <div className="text-sm text-gray-500">Commits</div>
                        </div>
                        <div className="bg-green-50 rounded-lg p-3">
                          <div className="text-2xl font-bold text-green-600">
                            {history.diffSummary.filesAdded.length}
                          </div>
                          <div className="text-sm text-green-600">Files Added</div>
                        </div>
                        <div className="bg-yellow-50 rounded-lg p-3">
                          <div className="text-2xl font-bold text-yellow-600">
                            {history.diffSummary.filesModified.length}
                          </div>
                          <div className="text-sm text-yellow-600">Files Modified</div>
                        </div>
                        <div className="bg-red-50 rounded-lg p-3">
                          <div className="text-2xl font-bold text-red-600">
                            {history.diffSummary.filesDeleted.length}
                          </div>
                          <div className="text-sm text-red-600">Files Deleted</div>
                        </div>
                      </div>

                      {history.diffSummary.latestCommit && (
                        <div className="mt-4 p-3 bg-gray-50 rounded-lg">
                          <div className="text-sm text-gray-500">Latest Commit</div>
                          <div className="flex items-center gap-2 mt-1">
                            <code className="text-sm bg-gray-200 px-2 py-0.5 rounded font-mono">
                              {history.diffSummary.latestCommit.slice(0, 7)}
                            </code>
                            <span className="text-gray-700">{history.diffSummary.latestCommitMessage}</span>
                          </div>
                        </div>
                      )}
                    </div>
                  )}

                  {/* Activity Timeline */}
                  <div className="bg-white rounded-lg shadow p-6">
                    <h2 className="text-lg font-semibold text-gray-900 mb-4">Activity Timeline</h2>
                    {history?.logs && history.logs.length > 0 ? (
                      <div className="space-y-4">
                        {history.logs.map((log: TaskLog) => {
                          const config = logTypeConfig[log.type] || { icon: Activity, color: 'text-gray-500', label: log.type };
                          const Icon = config.icon;
                          return (
                            <ActivityLogItem key={log.id} log={log} icon={Icon} config={config} />
                          );
                        })}
                      </div>
                    ) : (
                      <p className="text-gray-400 text-center py-8">No activity yet</p>
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* Sidebar */}
            <div className="space-y-6">
              {/* Details */}
              <div className="bg-white rounded-lg shadow p-6">
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Details</h2>
                <dl className="space-y-4">
                  <div>
                    <dt className="text-sm text-gray-500">Project</dt>
                    <dd>
                      <Link
                        href={`/projects/${task.projectId}`}
                        className="text-primary-600 hover:underline"
                      >
                        {project?.name || 'Unknown'}
                      </Link>
                    </dd>
                  </div>

                  <div>
                    <dt className="text-sm text-gray-500">Type</dt>
                    <dd className="capitalize">{task.type}</dd>
                  </div>

                  <div>
                    <dt className="text-sm text-gray-500">Status</dt>
                    <dd>
                      <span className={clsx('px-2 py-1 rounded text-sm', statusColor)}>
                        {task.status.replace('_', ' ')}
                      </span>
                    </dd>
                  </div>

                  {task.storyPoints && (
                    <div>
                      <dt className="text-sm text-gray-500">Story Points</dt>
                      <dd>{task.storyPoints}</dd>
                    </div>
                  )}

                  {task.assigneeType && (
                    <div>
                      <dt className="text-sm text-gray-500">Assignee</dt>
                      <dd className="flex items-center gap-2">
                        <div className="w-6 h-6 bg-primary-100 rounded-full flex items-center justify-center">
                          {task.assigneeType === 'agent' ? (
                            <Bot className="w-3 h-3 text-primary-600" />
                          ) : (
                            <User className="w-3 h-3 text-primary-600" />
                          )}
                        </div>
                        <span className="capitalize">{task.assigneeType}</span>
                      </dd>
                    </div>
                  )}

                  {task.gitBranch && (
                    <div>
                      <dt className="text-sm text-gray-500">Branch</dt>
                      <dd className="flex items-center gap-2">
                        <GitBranch className="w-4 h-4 text-gray-400" />
                        <code className="text-sm bg-gray-100 px-2 py-0.5 rounded">
                          {task.gitBranch}
                        </code>
                      </dd>
                    </div>
                  )}

                  {task.gitPrUrl && (
                    <div>
                      <dt className="text-sm text-gray-500">Pull Request</dt>
                      <dd>
                        <a
                          href={task.gitPrUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-primary-600 hover:underline flex items-center gap-1"
                        >
                          View PR
                          <ExternalLink className="w-3 h-3" />
                        </a>
                      </dd>
                    </div>
                  )}
                </dl>
              </div>

              {/* PR Actions */}
              {task.gitBranch && !task.gitPrUrl && (
                <div className="bg-white rounded-lg shadow p-6">
                  <h2 className="text-lg font-semibold text-gray-900 mb-4">Pull Request</h2>
                  <button
                    onClick={() => prMutation.mutate()}
                    disabled={prMutation.isPending}
                    className="w-full flex items-center justify-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
                  >
                    <GitPullRequest className="w-4 h-4" />
                    {prMutation.isPending ? 'Creating...' : 'Create Pull Request'}
                  </button>
                </div>
              )}

              {task.gitPrUrl && task.status !== 'done' && (
                <div className="bg-white rounded-lg shadow p-6">
                  <h2 className="text-lg font-semibold text-gray-900 mb-4">Merge PR</h2>
                  <button
                    onClick={() => mergeMutation.mutate()}
                    disabled={mergeMutation.isPending}
                    className="w-full flex items-center justify-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50"
                  >
                    <Merge className="w-4 h-4" />
                    {mergeMutation.isPending ? 'Merging...' : 'Merge Pull Request'}
                  </button>
                </div>
              )}

              {/* Timeline */}
              <div className="bg-white rounded-lg shadow p-6">
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Timeline</h2>
                <dl className="space-y-3">
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-500">Created</span>
                    <span className="text-gray-900">
                      {new Date(task.createdAt).toLocaleString()}
                    </span>
                  </div>
                  {task.startedAt && (
                    <div className="flex justify-between text-sm">
                      <span className="text-gray-500">Started</span>
                      <span className="text-gray-900">
                        {new Date(task.startedAt).toLocaleString()}
                      </span>
                    </div>
                  )}
                  {task.completedAt && (
                    <div className="flex justify-between text-sm">
                      <span className="text-gray-500">Completed</span>
                      <span className="text-gray-900">
                        {new Date(task.completedAt).toLocaleString()}
                      </span>
                    </div>
                  )}
                </dl>
              </div>
            </div>
          </div>

          {showEditModal && (
            <EditTaskModal task={task} onClose={() => setShowEditModal(false)} />
          )}
        </div>
      </main>
    </div>
  );
}

function ActivityLogItem({ log, icon: Icon, config }: { log: TaskLog; icon: typeof GitCommit; config: { icon: typeof GitCommit; color: string; label: string } }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div className="flex gap-3">
      <div className={clsx('w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 bg-gray-100', config.color)}>
        <Icon className="w-4 h-4" />
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 mb-1">
          <span className="font-medium text-gray-900">{config.label}</span>
          <span className="text-xs text-gray-400">
            {new Date(log.createdAt).toLocaleString()}
          </span>
        </div>
        <p className="text-gray-600">{log.message}</p>

        {log.gitCommit && (
          <code className="text-xs bg-gray-100 px-2 py-0.5 rounded mt-1 inline-block">
            {log.gitCommit.slice(0, 7)}
          </code>
        )}

        {log.filesAffected && log.filesAffected.length > 0 && (
          <div className="mt-2">
            <button
              onClick={() => setExpanded(!expanded)}
              className="text-sm text-primary-600 hover:underline flex items-center gap-1"
            >
              {expanded ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />}
              {log.filesAffected.length} file{log.filesAffected.length !== 1 ? 's' : ''}
            </button>
            {expanded && (
              <ul className="mt-2 space-y-1 text-sm text-gray-500">
                {log.filesAffected.map((file, i) => (
                  <li key={i} className="font-mono">{file}</li>
                ))}
              </ul>
            )}
          </div>
        )}

        {log.details && (
          <div className="mt-2">
            <button
              onClick={() => setExpanded(!expanded)}
              className="text-sm text-primary-600 hover:underline flex items-center gap-1"
            >
              {expanded ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />}
              View details
            </button>
            {expanded && (
              <pre className="mt-2 p-3 bg-gray-900 text-gray-100 rounded-lg text-xs overflow-x-auto">
                {log.details}
              </pre>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function EditTaskModal({ task, onClose }: { task: Task; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState(task.title);
  const [description, setDescription] = useState(task.description || '');
  const [status, setStatus] = useState(task.status);
  const [type, setType] = useState(task.type);

  const updateMutation = useMutation({
    mutationFn: () => tasksApi.update(task.id, { title, description, status, type }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['task', task.id] });
      onClose();
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMutation.mutate();
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg p-6">
        <h2 className="text-xl font-semibold mb-4">Edit Task</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Title</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
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
              rows={4}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
              <select
                value={status}
                onChange={(e) => setStatus(e.target.value)}
                className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                <option value="backlog">Backlog</option>
                <option value="sprint">Sprint</option>
                <option value="in_progress">In Progress</option>
                <option value="review">Review</option>
                <option value="blocked">Blocked</option>
                <option value="done">Done</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Type</label>
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
