# Database Schema

## Overview

Complete PostgreSQL schema for CronBot. Uses PostgreSQL 16+ features including JSONB, arrays, and full-text search.

## Extensions

```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";  -- For text search
```

## Custom Types

```sql
CREATE TYPE team_role AS ENUM ('owner', 'admin', 'member', 'guest');
CREATE TYPE project_role AS ENUM ('admin', 'planner', 'contributor', 'reader');
CREATE TYPE git_mode AS ENUM ('internal', 'external', 'import');
CREATE TYPE sandbox_mode AS ENUM ('standard', 'hardened', 'maximum');
CREATE TYPE task_type AS ENUM ('task', 'bug', 'blocker', 'idea', 'epic');
CREATE TYPE task_status AS ENUM ('backlog', 'sprint', 'in_progress', 'review', 'blocked', 'done', 'cancelled');
CREATE TYPE agent_status AS ENUM ('idle', 'working', 'paused', 'blocked', 'error', 'terminated');
CREATE TYPE mcp_group AS ENUM ('system', 'project', 'custom', 'external');
CREATE TYPE mcp_transport AS ENUM ('stdio', 'sse', 'http');
CREATE TYPE mcp_status AS ENUM ('registered', 'deploying', 'running', 'stopped', 'failed', 'deprecated');
CREATE TYPE skill_scope AS ENUM ('system', 'team', 'project');
CREATE TYPE notification_channel AS ENUM ('telegram', 'web', 'email');
CREATE TYPE notification_priority AS ENUM ('low', 'normal', 'high', 'urgent');
```

## Tables

### Users & Teams

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(255) NOT NULL UNIQUE,
    email VARCHAR(255),
    telegram_id BIGINT UNIQUE,
    authentik_id VARCHAR(255),
    display_name VARCHAR(255),
    avatar_url TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_telegram_id ON users(telegram_id);
CREATE INDEX idx_users_username ON users(username);

CREATE TABLE teams (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL UNIQUE,
    description TEXT,
    is_personal BOOLEAN NOT NULL DEFAULT false,
    owner_id UUID NOT NULL REFERENCES users(id),
    settings JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_teams_slug ON teams(slug);
CREATE INDEX idx_teams_owner ON teams(owner_id);

CREATE TABLE team_members (
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL DEFAULT 'member',
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (team_id, user_id)
);
```

### Projects

```sql
CREATE TABLE projects (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL,
    description TEXT,

    -- Git configuration
    git_mode git_mode NOT NULL DEFAULT 'internal',
    external_git_url TEXT,
    external_git_token_encrypted TEXT,

    -- Internal repo info
    internal_repo_id INTEGER,
    internal_repo_url TEXT,

    -- Settings
    autonomy_level SMALLINT NOT NULL DEFAULT 1 CHECK (autonomy_level BETWEEN 0 AND 3),
    sandbox_mode sandbox_mode NOT NULL DEFAULT 'standard',

    -- Template info
    template_name VARCHAR(255),

    -- Komodo stack
    komodo_stack_id VARCHAR(255),

    -- Resource limits
    max_agents SMALLINT NOT NULL DEFAULT 3,

    -- Configuration JSONs
    settings JSONB NOT NULL DEFAULT '{}',
    scaling_config JSONB NOT NULL DEFAULT '{
        "maxAgents": 3,
        "agentsPerTask": 1,
        "taskQueueSize": 10,
        "resourceLimits": {
            "cpuLimit": 0.5,
            "memoryLimitMb": 512,
            "diskLimitGb": 5
        },
        "executionLimits": {
            "maxExecutionTimeSeconds": 3600,
            "maxPreviewTimeSeconds": 7200,
            "idleTimeoutSeconds": 1800
        },
        "concurrency": {
            "parallelTasks": false,
            "parallelFiles": true
        }
    }',
    error_handling_config JSONB NOT NULL DEFAULT '{
        "retry": {
            "maxAttempts": 3,
            "initialDelayMs": 1000,
            "maxDelayMs": 30000,
            "backoffMultiplier": 2
        },
        "contextRollback": {
            "enabled": true,
            "preserveSteps": 5
        },
        "agentRecovery": {
            "restartOnCrash": true,
            "maxRestarts": 3,
            "restartWindowMinutes": 60
        },
        "blockerDetection": {
            "codeLoopThreshold": 2,
            "verificationRetryLimit": 3,
            "toolRetryLimit": 3,
            "stuckTimeoutMinutes": 30,
            "autoPivot": true
        }
    }',

    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(team_id, slug)
);

CREATE INDEX idx_projects_team ON projects(team_id);
CREATE INDEX idx_projects_slug ON projects(slug);

CREATE TABLE project_members (
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL DEFAULT 'contributor',
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (project_id, user_id)
);
```

### Kanban / Tasks

```sql
CREATE TABLE boards (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    columns JSONB NOT NULL DEFAULT '["Backlog", "Sprint", "In Progress", "Review", "Blocked", "Done"]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE sprints (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    number INTEGER NOT NULL,
    goal TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'planning',
    start_date DATE,
    end_date DATE,
    velocity_points INTEGER,
    retrospective_notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(project_id, number)
);

CREATE INDEX idx_sprints_project ON sprints(project_id);
CREATE INDEX idx_sprints_status ON sprints(status);

CREATE TABLE tasks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    sprint_id UUID REFERENCES sprints(id) ON DELETE SET NULL,
    board_id UUID NOT NULL REFERENCES boards(id) ON DELETE CASCADE,

    -- Task info
    number BIGINT NOT NULL,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    type task_type NOT NULL DEFAULT 'task',
    status task_status NOT NULL DEFAULT 'backlog',

    -- Estimation
    story_points SMALLINT,

    -- Assignment
    assignee_type VARCHAR(50),
    assignee_id UUID,

    -- Parent/child for epics
    parent_task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,

    -- Git integration
    git_branch VARCHAR(255),
    git_pr_id INTEGER,
    git_pr_url TEXT,
    git_merged_commit TEXT,

    -- Blocking
    blocked_by_task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,
    blocked_reason TEXT,

    -- Order in column
    column_order INTEGER NOT NULL DEFAULT 0,

    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,

    UNIQUE(project_id, number)
);

CREATE INDEX idx_tasks_project ON tasks(project_id);
CREATE INDEX idx_tasks_sprint ON tasks(sprint_id);
CREATE INDEX idx_tasks_status ON tasks(status);
CREATE INDEX idx_tasks_assignee ON tasks(assignee_type, assignee_id);
CREATE INDEX idx_tasks_parent ON tasks(parent_task_id);

CREATE TABLE task_comments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    task_id UUID NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    author_type VARCHAR(50) NOT NULL,
    author_id UUID NOT NULL,
    content TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_task_comments_task ON task_comments(task_id);
```

### Agents

```sql
CREATE TABLE agents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    current_task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,

    -- Container info
    container_id VARCHAR(255),
    container_name VARCHAR(255),

    -- Status
    status agent_status NOT NULL DEFAULT 'idle',
    status_message TEXT,

    -- Resource usage
    cpu_usage_percent DECIMAL(5,2),
    memory_usage_mb INTEGER,

    -- Timing
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_activity_at TIMESTAMPTZ,
    terminated_at TIMESTAMPTZ,

    -- Stats
    tasks_completed INTEGER NOT NULL DEFAULT 0,
    commits_made INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX idx_agents_project ON agents(project_id);
CREATE INDEX idx_agents_status ON agents(status);
CREATE INDEX idx_agents_task ON agents(current_task_id);
```

### Task State & Memory

```sql
CREATE TABLE task_states (
    task_id UUID PRIMARY KEY REFERENCES tasks(id) ON DELETE CASCADE,

    -- Current context (compacted)
    context JSONB NOT NULL DEFAULT '{}',
    context_tokens INTEGER,

    -- Conversation archive reference
    archive_id UUID,

    -- Compaction metadata
    last_compacted_at TIMESTAMPTZ,
    compaction_count INTEGER NOT NULL DEFAULT 0,

    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE conversation_archives (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    task_id UUID NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,

    -- Full conversation
    messages JSONB NOT NULL DEFAULT '[]',
    total_tokens INTEGER,

    -- Search
    search_vector TSVECTOR,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_conversation_archives_task ON conversation_archives(task_id);
CREATE INDEX idx_conversation_archives_search ON conversation_archives USING GIN(search_vector);

CREATE TABLE task_decisions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    task_id UUID NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,

    decision TEXT NOT NULL,
    reason TEXT,
    importance_score DECIMAL(3,2) DEFAULT 0.5,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_task_decisions_task ON task_decisions(task_id);
```

### MCP Configuration

```sql
CREATE TABLE mcps (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    version VARCHAR(50) NOT NULL DEFAULT '1.0.0',
    description TEXT,

    -- Grouping
    group_type mcp_group NOT NULL,
    team_id UUID REFERENCES teams(id) ON DELETE CASCADE,
    project_id UUID REFERENCES projects(id) ON DELETE CASCADE,

    -- Container
    container_image VARCHAR(500),
    komodo_stack_id VARCHAR(255),

    -- Connection
    transport mcp_transport NOT NULL DEFAULT 'stdio',
    command VARCHAR(500),
    url VARCHAR(500),

    -- Config
    environment_vars JSONB NOT NULL DEFAULT '{}',
    required_config JSONB NOT NULL DEFAULT '[]',

    -- Discovered capabilities
    tools JSONB NOT NULL DEFAULT '[]',
    resources JSONB NOT NULL DEFAULT '[]',
    prompts JSONB NOT NULL DEFAULT '[]',

    -- Status
    status mcp_status NOT NULL DEFAULT 'registered',
    status_message TEXT,
    last_health_check TIMESTAMPTZ,

    -- If custom, which project created it
    created_by_project_id UUID REFERENCES projects(id) ON DELETE SET NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(name, group_type, COALESCE(team_id, '00000000-0000-0000-0000-000000000000'))
);

CREATE INDEX idx_mcps_group ON mcps(group_type);
CREATE INDEX idx_mcps_team ON mcps(team_id);
CREATE INDEX idx_mcps_project ON mcps(project_id);

CREATE TABLE project_mcps (
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    mcp_id UUID NOT NULL REFERENCES mcps(id) ON DELETE CASCADE,
    enabled_tools JSONB,
    enabled_resources JSONB,
    config_override JSONB NOT NULL DEFAULT '{}',
    enabled_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    PRIMARY KEY (project_id, mcp_id)
);
```

### Skills

```sql
CREATE TABLE skills (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    version VARCHAR(50) NOT NULL DEFAULT '1.0.0',
    description TEXT,

    -- Scope
    scope skill_scope NOT NULL,
    team_id UUID REFERENCES teams(id) ON DELETE CASCADE,
    project_id UUID REFERENCES projects(id) ON DELETE CASCADE,

    -- File
    file_path TEXT NOT NULL,

    -- Metadata
    meta JSONB NOT NULL DEFAULT '{}',

    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(name, scope, COALESCE(team_id, '00000000-0000-0000-0000-000000000000'), COALESCE(project_id, '00000000-0000-0000-0000-000000000000'))
);

CREATE INDEX idx_skills_scope ON skills(scope);
CREATE INDEX idx_skills_team ON skills(team_id);
CREATE INDEX idx_skills_project ON skills(project_id);

CREATE TABLE project_skills (
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    skill_id UUID NOT NULL REFERENCES skills(id) ON DELETE CASCADE,
    enabled BOOLEAN NOT NULL DEFAULT true,
    enabled_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    PRIMARY KEY (project_id, skill_id)
);
```

### Git & Loop Detection

```sql
CREATE TABLE git_commits (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,
    agent_id UUID REFERENCES agents(id) ON DELETE SET NULL,

    commit_hash VARCHAR(40) NOT NULL,
    branch VARCHAR(255) NOT NULL,
    message TEXT,
    author_email VARCHAR(255),

    -- Stats
    additions INTEGER,
    deletions INTEGER,
    files_changed INTEGER,

    committed_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_git_commits_project ON git_commits(project_id);
CREATE INDEX idx_git_commits_task ON git_commits(task_id);
CREATE INDEX idx_git_commits_hash ON git_commits(commit_hash);

CREATE TABLE loop_detections (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    task_id UUID NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,

    file_path TEXT NOT NULL,
    line_number INTEGER,
    pattern VARCHAR(50),
    commits_involved TEXT[] NOT NULL,
    transition_count INTEGER NOT NULL,

    resolved BOOLEAN NOT NULL DEFAULT false,
    resolved_at TIMESTAMPTZ,
    resolution_notes TEXT,

    detected_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_loop_detections_project ON loop_detections(project_id);
CREATE INDEX idx_loop_detections_task ON loop_detections(task_id);
CREATE INDEX idx_loop_detections_resolved ON loop_detections(resolved);
```

### Blockers

```sql
CREATE TABLE blocker_detection_config (
    project_id UUID PRIMARY KEY REFERENCES projects(id) ON DELETE CASCADE,

    -- Code loop detection
    code_loop_threshold INTEGER NOT NULL DEFAULT 2,

    -- Verification loop detection
    verification_retry_limit INTEGER NOT NULL DEFAULT 3,

    -- Tool failure detection
    tool_retry_limit INTEGER NOT NULL DEFAULT 3,

    -- Stuck agent detection
    stuck_timeout_minutes INTEGER NOT NULL DEFAULT 30,

    -- Scope creep detection
    scope_file_limit INTEGER NOT NULL DEFAULT 10,
    scope_requirement_limit INTEGER NOT NULL DEFAULT 5,

    -- Auto-pivot on blocker
    auto_pivot BOOLEAN NOT NULL DEFAULT true,

    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE blockers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    task_id UUID NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    agent_id UUID REFERENCES agents(id) ON DELETE SET NULL,

    -- Blocker info
    type VARCHAR(100) NOT NULL,
    severity VARCHAR(50) NOT NULL DEFAULT 'medium',
    title VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,

    -- Detection details
    detection_data JSONB NOT NULL DEFAULT '{}',

    -- Resolution
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    resolved_by UUID REFERENCES users(id),
    resolved_at TIMESTAMPTZ,
    resolution_notes TEXT,

    detected_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_blockers_project ON blockers(project_id);
CREATE INDEX idx_blockers_task ON blockers(task_id);
CREATE INDEX idx_blockers_status ON blockers(status);
```

### Notifications

```sql
CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,

    -- Source
    project_id UUID REFERENCES projects(id) ON DELETE CASCADE,
    task_id UUID REFERENCES tasks(id) ON DELETE CASCADE,
    agent_id UUID REFERENCES agents(id) ON DELETE SET NULL,

    -- Content
    type VARCHAR(100) NOT NULL,
    title VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    data JSONB NOT NULL DEFAULT '{}',

    -- Delivery
    channel notification_channel NOT NULL DEFAULT 'web',
    priority notification_priority NOT NULL DEFAULT 'normal',
    sent_at TIMESTAMPTZ,
    read_at TIMESTAMPTZ,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notifications_user ON notifications(user_id);
CREATE INDEX idx_notifications_unread ON notifications(user_id) WHERE read_at IS NULL;
CREATE INDEX idx_notifications_created ON notifications(created_at DESC);
```

### Previews

```sql
CREATE TABLE previews (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,

    preview_id VARCHAR(100) NOT NULL UNIQUE,
    url TEXT NOT NULL,

    -- Container
    container_id VARCHAR(255),

    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'starting',
    status_message TEXT,

    -- Screenshot
    screenshot_path TEXT,
    screenshot_taken_at TIMESTAMPTZ,

    -- Lifecycle
    expires_at TIMESTAMPTZ NOT NULL,
    last_accessed_at TIMESTAMPTZ,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_previews_project ON previews(project_id);
CREATE INDEX idx_previews_task ON previews(task_id);
CREATE INDEX idx_previews_expires ON previews(expires_at) WHERE status = 'running';
```

### Audit Logs

```sql
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    agent_id UUID REFERENCES agents(id) ON DELETE SET NULL,

    -- Event
    event_type VARCHAR(100) NOT NULL,
    event_data JSONB NOT NULL DEFAULT '{}',

    -- Context
    container_id VARCHAR(255),
    session_id UUID,

    -- Classification
    severity VARCHAR(50) NOT NULL DEFAULT 'info',
    is_anomaly BOOLEAN NOT NULL DEFAULT false,

    occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_logs_project ON audit_logs(project_id);
CREATE INDEX idx_audit_logs_agent ON audit_logs(agent_id);
CREATE INDEX idx_audit_logs_time ON audit_logs(occurred_at DESC);
CREATE INDEX idx_audit_logs_anomaly ON audit_logs(project_id) WHERE is_anomaly = true;
```

### User Sessions

```sql
CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,

    -- Current context
    context_level VARCHAR(50) NOT NULL DEFAULT 'general',
    context_team_id UUID REFERENCES teams(id) ON DELETE SET NULL,
    context_project_id UUID REFERENCES projects(id) ON DELETE SET NULL,
    context_task_id UUID REFERENCES tasks(id) ON DELETE SET NULL,

    -- Session
    telegram_chat_id BIGINT,
    web_socket_id VARCHAR(255),

    -- Metadata
    ip_address INET,
    user_agent TEXT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_activity_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_user_sessions_user ON user_sessions(user_id);
CREATE INDEX idx_user_sessions_telegram ON user_sessions(telegram_chat_id);
CREATE INDEX idx_user_sessions_expires ON user_sessions(expires_at);
```

### Templates

```sql
CREATE TABLE templates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL UNIQUE,
    version VARCHAR(50) NOT NULL DEFAULT '1.0.0',
    description TEXT,
    author VARCHAR(255),

    -- Content
    template_data JSONB NOT NULL,
    file_hash VARCHAR(64),

    -- Metadata
    tags TEXT[] NOT NULL DEFAULT '{}',
    is_builtin BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### System Config

```sql
CREATE TABLE system_config (
    key VARCHAR(255) PRIMARY KEY,
    value JSONB NOT NULL,
    description TEXT,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## Functions & Triggers

```sql
-- Update timestamp trigger
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply to tables with updated_at
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER update_teams_updated_at BEFORE UPDATE ON teams
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER update_projects_updated_at BEFORE UPDATE ON projects
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER update_sprints_updated_at BEFORE UPDATE ON sprints
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER update_tasks_updated_at BEFORE UPDATE ON tasks
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER update_task_comments_updated_at BEFORE UPDATE ON task_comments
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER update_task_states_updated_at BEFORE UPDATE ON task_states
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER update_mcps_updated_at BEFORE UPDATE ON mcps
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER update_skills_updated_at BEFORE UPDATE ON skills
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER update_templates_updated_at BEFORE UPDATE ON templates
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

-- Task number sequence per project
CREATE OR REPLACE FUNCTION generate_task_number()
RETURNS TRIGGER AS $$
DECLARE
    next_number BIGINT;
BEGIN
    SELECT COALESCE(MAX(number), 0) + 1 INTO next_number
    FROM tasks WHERE project_id = NEW.project_id;

    NEW.number := next_number;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER generate_task_number_trigger BEFORE INSERT ON tasks
    FOR EACH ROW EXECUTE FUNCTION generate_task_number();

-- Search vector for conversation archives
CREATE OR REPLACE FUNCTION update_conversation_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector := to_tsvector('english', NEW.messages::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_conversation_search_vector_trigger
    BEFORE INSERT OR UPDATE ON conversation_archives
    FOR EACH ROW EXECUTE FUNCTION update_conversation_search_vector();
```

## Default Data

```sql
-- Default system config
INSERT INTO system_config (key, value, description) VALUES
('limits', '{
  "maxProjectsPerTeam": 10,
  "maxTeamsPerUser": 5,
  "maxTasksPerSprint": 20,
  "maxConcurrentPreviews": 50
}', 'System-wide resource limits'),
('defaults', '{
  "projectScaling": {
    "maxAgents": 3,
    "resourceLimits": {
      "cpuLimit": 0.5,
      "memoryLimitMb": 512
    }
  },
  "errorHandling": {
    "retry": {
      "maxAttempts": 3
    }
  },
  "blockerDetection": {
    "codeLoopThreshold": 2
  }
}', 'Default values for new projects');
```
