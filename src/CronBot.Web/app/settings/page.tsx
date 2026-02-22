'use client';

import { useState } from 'react';
import { Sidebar } from '@/components/Sidebar';
import { AuthGuard } from '@/components/AuthGuard';
import { Settings, User, Bell, Shield, Palette, Globe, Key, Save } from 'lucide-react';
import clsx from 'clsx';

const tabs = [
  { id: 'profile', label: 'Profile', icon: User },
  { id: 'notifications', label: 'Notifications', icon: Bell },
  { id: 'security', label: 'Security', icon: Shield },
  { id: 'appearance', label: 'Appearance', icon: Palette },
  { id: 'integrations', label: 'Integrations', icon: Globe },
  { id: 'api', label: 'API Keys', icon: Key },
];

export default function SettingsPage() {
  const [activeTab, setActiveTab] = useState('profile');
  const [saving, setSaving] = useState(false);

  const handleSave = async () => {
    setSaving(true);
    // Simulate save
    await new Promise((resolve) => setTimeout(resolve, 1000));
    setSaving(false);
  };

  return (
    <AuthGuard>
      <div className="flex h-screen bg-gray-50">
        <Sidebar />

      <main className="flex-1 overflow-auto">
        <div className="p-8">
          <div className="flex items-center justify-between mb-8">
            <h1 className="text-3xl font-bold text-gray-900">Settings</h1>
            <button
              onClick={handleSave}
              disabled={saving}
              className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
            >
              <Save className="w-4 h-4" />
              {saving ? 'Saving...' : 'Save Changes'}
            </button>
          </div>

          <div className="flex gap-8">
            {/* Sidebar Navigation */}
            <div className="w-64 flex-shrink-0">
              <nav className="space-y-1">
                {tabs.map((tab) => (
                  <button
                    key={tab.id}
                    onClick={() => setActiveTab(tab.id)}
                    className={clsx(
                      'w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors text-left',
                      activeTab === tab.id
                        ? 'bg-primary-50 text-primary-700'
                        : 'text-gray-600 hover:bg-gray-100'
                    )}
                  >
                    <tab.icon className="w-5 h-5" />
                    {tab.label}
                  </button>
                ))}
              </nav>
            </div>

            {/* Content */}
            <div className="flex-1">
              <div className="bg-white rounded-lg shadow p-6">
                {activeTab === 'profile' && <ProfileSettings />}
                {activeTab === 'notifications' && <NotificationSettings />}
                {activeTab === 'security' && <SecuritySettings />}
                {activeTab === 'appearance' && <AppearanceSettings />}
                {activeTab === 'integrations' && <IntegrationSettings />}
                {activeTab === 'api' && <ApiSettings />}
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
    </AuthGuard>
  );
}

function ProfileSettings() {
  const [displayName, setDisplayName] = useState('User');
  const [email, setEmail] = useState('user@example.com');
  const [bio, setBio] = useState('');

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Profile Settings</h2>
        <p className="text-sm text-gray-500 mb-6">
          Manage your public profile information
        </p>
      </div>

      {/* Avatar */}
      <div className="flex items-center gap-4">
        <div className="w-20 h-20 bg-gray-200 rounded-full flex items-center justify-center">
          <User className="w-8 h-8 text-gray-400" />
        </div>
        <button className="px-4 py-2 border rounded-lg text-gray-700 hover:bg-gray-50 transition-colors">
          Change Avatar
        </button>
      </div>

      {/* Form */}
      <div className="space-y-4 max-w-lg">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Display Name</label>
          <input
            type="text"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Bio</label>
          <textarea
            value={bio}
            onChange={(e) => setBio(e.target.value)}
            rows={3}
            className="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
            placeholder="Tell us about yourself..."
          />
        </div>
      </div>
    </div>
  );
}

function NotificationSettings() {
  const [emailNotifications, setEmailNotifications] = useState(true);
  const [taskCompleted, setTaskCompleted] = useState(true);
  const [agentBlocked, setAgentBlocked] = useState(true);
  const [mentions, setMentions] = useState(true);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Notification Preferences</h2>
        <p className="text-sm text-gray-500 mb-6">
          Choose how you want to be notified
        </p>
      </div>

      <div className="space-y-4">
        <ToggleSwitch
          label="Email Notifications"
          description="Receive notifications via email"
          checked={emailNotifications}
          onChange={setEmailNotifications}
        />

        <div className="border-t pt-4">
          <h3 className="font-medium text-gray-900 mb-3">Notify me when:</h3>

          <div className="space-y-3">
            <ToggleSwitch
              label="Task Completed"
              description="When an agent completes a task"
              checked={taskCompleted}
              onChange={setTaskCompleted}
            />

            <ToggleSwitch
              label="Agent Blocked"
              description="When an agent encounters a blocker"
              checked={agentBlocked}
              onChange={setAgentBlocked}
            />

            <ToggleSwitch
              label="Mentions"
              description="When someone mentions you in a comment"
              checked={mentions}
              onChange={setMentions}
            />
          </div>
        </div>
      </div>
    </div>
  );
}

function SecuritySettings() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Security Settings</h2>
        <p className="text-sm text-gray-500 mb-6">
          Manage your account security
        </p>
      </div>

      <div className="space-y-6">
        <div className="p-4 bg-gray-50 rounded-lg">
          <h3 className="font-medium text-gray-900 mb-2">Change Password</h3>
          <p className="text-sm text-gray-500 mb-4">
            Update your password to keep your account secure
          </p>
          <button className="px-4 py-2 bg-gray-900 text-white rounded-lg hover:bg-gray-800 transition-colors">
            Change Password
          </button>
        </div>

        <div className="p-4 bg-gray-50 rounded-lg">
          <h3 className="font-medium text-gray-900 mb-2">Two-Factor Authentication</h3>
          <p className="text-sm text-gray-500 mb-4">
            Add an extra layer of security to your account
          </p>
          <button className="px-4 py-2 border rounded-lg text-gray-700 hover:bg-gray-100 transition-colors">
            Enable 2FA
          </button>
        </div>

        <div className="p-4 bg-gray-50 rounded-lg">
          <h3 className="font-medium text-gray-900 mb-2">Active Sessions</h3>
          <p className="text-sm text-gray-500 mb-4">
            Manage your active login sessions
          </p>
          <button className="px-4 py-2 text-red-600 hover:bg-red-50 rounded-lg transition-colors">
            Sign Out All Devices
          </button>
        </div>
      </div>
    </div>
  );
}

function AppearanceSettings() {
  const [theme, setTheme] = useState('system');
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Appearance</h2>
        <p className="text-sm text-gray-500 mb-6">
          Customize how CronBot looks
        </p>
      </div>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Theme</label>
          <div className="flex gap-3">
            {['light', 'dark', 'system'].map((t) => (
              <button
                key={t}
                onClick={() => setTheme(t)}
                className={clsx(
                  'px-4 py-2 rounded-lg border capitalize transition-colors',
                  theme === t
                    ? 'border-primary-500 bg-primary-50 text-primary-700'
                    : 'border-gray-200 text-gray-600 hover:bg-gray-50'
                )}
              >
                {t}
              </button>
            ))}
          </div>
        </div>

        <ToggleSwitch
          label="Collapse Sidebar"
          description="Show a compact sidebar by default"
          checked={sidebarCollapsed}
          onChange={setSidebarCollapsed}
        />
      </div>
    </div>
  );
}

function IntegrationSettings() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Integrations</h2>
        <p className="text-sm text-gray-500 mb-6">
          Connect CronBot with your favorite tools
        </p>
      </div>

      <div className="space-y-4">
        <IntegrationItem
          name="Telegram"
          description="Get notifications via Telegram bot"
          connected={false}
        />
        <IntegrationItem
          name="GitHub"
          description="Sync repositories with your projects"
          connected={false}
        />
        <IntegrationItem
          name="Slack"
          description="Post updates to Slack channels"
          connected={false}
        />
        <IntegrationItem
          name="Discord"
          description="Receive notifications in Discord"
          connected={false}
        />
      </div>
    </div>
  );
}

function IntegrationItem({
  name,
  description,
  connected,
}: {
  name: string;
  description: string;
  connected: boolean;
}) {
  return (
    <div className="flex items-center justify-between p-4 border rounded-lg">
      <div>
        <h3 className="font-medium text-gray-900">{name}</h3>
        <p className="text-sm text-gray-500">{description}</p>
      </div>
      <button
        className={clsx(
          'px-4 py-2 rounded-lg transition-colors',
          connected
            ? 'bg-green-100 text-green-700'
            : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
        )}
      >
        {connected ? 'Connected' : 'Connect'}
      </button>
    </div>
  );
}

function ApiSettings() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold text-gray-900 mb-4">API Keys</h2>
        <p className="text-sm text-gray-500 mb-6">
          Manage API keys for programmatic access
        </p>
      </div>

      <div className="p-4 bg-yellow-50 border border-yellow-200 rounded-lg mb-6">
        <p className="text-sm text-yellow-700">
          API keys are sensitive. Never share them or commit them to version control.
        </p>
      </div>

      <div className="space-y-4">
        <div className="p-4 border rounded-lg">
          <div className="flex items-center justify-between mb-2">
            <h3 className="font-medium text-gray-900">Default API Key</h3>
            <span className="text-xs text-gray-500">Created Jan 1, 2026</span>
          </div>
          <div className="flex items-center gap-2">
            <code className="flex-1 px-3 py-2 bg-gray-100 rounded text-sm font-mono">
              cb_live_****************************
            </code>
            <button className="px-3 py-2 text-gray-600 hover:bg-gray-100 rounded transition-colors">
              Copy
            </button>
          </div>
        </div>
      </div>

      <button className="flex items-center gap-2 px-4 py-2 border rounded-lg text-gray-700 hover:bg-gray-50 transition-colors">
        <Key className="w-4 h-4" />
        Generate New Key
      </button>
    </div>
  );
}

function ToggleSwitch({
  label,
  description,
  checked,
  onChange,
}: {
  label: string;
  description: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
}) {
  return (
    <div className="flex items-center justify-between">
      <div>
        <p className="font-medium text-gray-900">{label}</p>
        <p className="text-sm text-gray-500">{description}</p>
      </div>
      <button
        onClick={() => onChange(!checked)}
        className={clsx(
          'relative w-11 h-6 rounded-full transition-colors',
          checked ? 'bg-primary-600' : 'bg-gray-200'
        )}
      >
        <span
          className={clsx(
            'absolute top-1 left-1 w-4 h-4 bg-white rounded-full transition-transform',
            checked && 'translate-x-5'
          )}
        />
      </button>
    </div>
  );
}
