import Link from 'next/link';
import { LayoutDashboard, FolderKanban, Users, Settings, Bot } from 'lucide-react';
import { Sidebar } from '@/components/Sidebar';

export default function HomePage() {
  return (
    <div className="flex h-screen bg-gray-50">
      <Sidebar />

      <main className="flex-1 overflow-auto">
        <div className="p-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-8">
            Welcome to CronBot
          </h1>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
            <StatCard
              title="Active Projects"
              value="3"
              icon={<FolderKanban className="w-6 h-6" />}
              href="/projects"
            />
            <StatCard
              title="Running Agents"
              value="5"
              icon={<Bot className="w-6 h-6" />}
              href="/agents"
            />
            <StatCard
              title="Tasks Completed"
              value="127"
              icon={<LayoutDashboard className="w-6 h-6" />}
              href="/tasks"
            />
            <StatCard
              title="Team Members"
              value="8"
              icon={<Users className="w-6 h-6" />}
              href="/team"
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-xl font-semibold mb-4">Recent Activity</h2>
              <ActivityList />
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-xl font-semibold mb-4">Quick Actions</h2>
              <QuickActions />
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

function StatCard({
  title,
  value,
  icon,
  href,
}: {
  title: string;
  value: string;
  icon: React.ReactNode;
  href: string;
}) {
  return (
    <Link
      href={href}
      className="bg-white rounded-lg shadow p-6 hover:shadow-md transition-shadow"
    >
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-gray-500">{title}</p>
          <p className="text-2xl font-bold text-gray-900">{value}</p>
        </div>
        <div className="p-3 bg-primary-100 rounded-full text-primary-600">
          {icon}
        </div>
      </div>
    </Link>
  );
}

function ActivityList() {
  const activities = [
    { action: 'Task #42 completed', time: '2 minutes ago', type: 'success' },
    { action: 'Agent started on Task #45', time: '5 minutes ago', type: 'info' },
    { action: 'New project "API v2" created', time: '1 hour ago', type: 'info' },
    { action: 'Task #40 blocked', time: '2 hours ago', type: 'warning' },
  ];

  return (
    <div className="space-y-3">
      {activities.map((activity, i) => (
        <div
          key={i}
          className="flex items-center justify-between py-2 border-b last:border-0"
        >
          <span className="text-gray-700">{activity.action}</span>
          <span className="text-sm text-gray-400">{activity.time}</span>
        </div>
      ))}
    </div>
  );
}

function QuickActions() {
  const actions = [
    { label: 'Create New Project', href: '/projects/new' },
    { label: 'Add Task', href: '/tasks/new' },
    { label: 'Spawn Agent', href: '/agents/new' },
    { label: 'View Kanban Board', href: '/board' },
  ];

  return (
    <div className="grid grid-cols-2 gap-3">
      {actions.map((action, i) => (
        <Link
          key={i}
          href={action.href}
          className="flex items-center justify-center p-4 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors text-gray-700"
        >
          {action.label}
        </Link>
      ))}
    </div>
  );
}
