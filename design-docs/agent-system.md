# Agent System

## Overview

Agents are the autonomous workers in CronBot, powered by the Claude Agent SDK. Each agent runs in its own container and can work on tasks independently based on the project's autonomy level.

## Agent Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         AGENT CONTAINER                                       │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  BASE IMAGE: node:20-bookworm-slim                                          │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  /agent/                                                             │   │
│  │  ├── dist/                                                           │   │
│  │  │   └── index.js            # Main agent entry point               │   │
│  │  ├── package.json                                                   │   │
│  │  └── node_modules/                                                  │   │
│  │      ├── @anthropic-ai/claude-code   # Claude Agent SDK             │   │
│  │      ├── @modelcontextprotocol/sdk   # MCP client SDK               │   │
│  │      └── ...                                                        │   │
│  │                                                                     │   │
│  │  Python Runtime (for skills)                                        │   │
│  │  - python3                                                          │   │
│  │  - pyyaml, requests, python-dateutil                               │   │
│  │                                                                     │   │
│  │  /workspace/                # Project files (mounted volume)        │   │
│  │  /skills/                   # Skills directory (mounted volume)     │   │
│  │  /mcp-config/               # MCP configuration                      │   │
│  │  /agent-state/              # State persistence                      │   │
│  │                                                                     │   │
│  │  Environment:                                                       │   │
│  │  - PROJECT_ID                                                       │   │
│  │  - MCP_REGISTRY_URL                                                 │   │
│  │  - KANBAN_URL                                                       │   │
│  │  - GIT_URL                                                          │   │
│  │  - AUTONOMY_LEVEL                                                   │   │
│  │                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Agent Dockerfile

```dockerfile
FROM node:20-bookworm-slim

# Install Python for skills
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    python3-venv \
    git \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Install common Python packages for skills
RUN pip3 install --no-cache-dir \
    pyyaml \
    requests \
    python-dateutil

# Create directories
RUN mkdir -p /workspace /skills /mcp-config /agent-state

WORKDIR /workspace

# Copy agent code
COPY agent/ /agent/
COPY package*.json /agent/

# Install Node dependencies
WORKDIR /agent
RUN npm ci --only=production

# Set up environment
ENV MCP_CONFIG_PATH=/mcp-config
ENV SKILLS_PATH=/skills
ENV WORKSPACE_PATH=/workspace
ENV AGENT_STATE_PATH=/agent-state

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:3000/health || exit 1

CMD ["node", "dist/index.js"]
```

## Agent Startup Flow

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         AGENT STARTUP                                         │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ Container   │     │ MCP         │     │ Task        │     │ Ready to    │
│ Starts      │────►│ Registry    │────►│ Assignment  │────►│ Work        │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
       │                   │                   │
       │                   │                   │
       ▼                   ▼                   ▼

1. Read environment     2. Fetch MCP         3. Get assigned
   variables               config              task from Kanban

   PROJECT_ID            GET /mcp/           GET /tasks/
   AUTONOMY_LEVEL        registry/           assigned
   MCP_REGISTRY_URL      project/{id}

                        4. Initialize
                           MCP clients
                           for each MCP

                           filesystem, git,
                           kanban, runner,
                           search, etc.

5. Load task state from Memory Service

6. Begin task execution loop
```

## Task Execution Loop

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         TASK EXECUTION                                        │
└──────────────────────────────────────────────────────────────────────────────┘

        ┌─────────────────────────────────────────────────────────────┐
        │                                                             │
        │  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐  │
        │  │ Read Task   │     │ Understand  │     │ Plan        │  │
        │  │ Context     │────►│ Requirement │────►│ Approach    │  │
        │  └─────────────┘     └─────────────┘     └──────┬──────┘  │
        │                                                 │        │
        │                                                 ▼        │
        │                                          ┌─────────────┐  │
        │                                          │ Execute     │  │
        │                                          │ Actions     │  │
        │                                          └──────┬──────┘  │
        │                                                 │        │
        │                                    ┌────────────┴─────┐  │
        │                                    │                  │  │
        │                                 Read/Write        Execute  │
        │                                    │                  │  │
        │                                    │                  │  │
        │                                    ▼                  ▼  │
        │                            ┌─────────────┐    ┌─────────┐ │
        │                            │ Filesystem  │    │ Runner  │ │
        │                            │ MCP         │    │ MCP     │ │
        │                            └─────────────┘    └─────────┘ │
        │                                                 │        │
        │                                                 ▼        │
        │                                          ┌─────────────┐  │
        │                                          │ Verify      │  │
        │                                          │ Results     │  │
        │                                          └──────┬──────┘  │
        │                                                 │        │
        │                                    ┌────────────┴─────┐  │
        │                                    │                  │  │
        │                                 Pass               Fail  │
        │                                    │                  │  │
        │                                    ▼                  ▼  │
        │                            ┌─────────────┐    ┌─────────┐ │
        │                            │ Commit      │    │ Iterate │ │
        │                            │ (Git MCP)   │    │ & Fix   │ │
        │                            └──────┬──────┘    └────┬────┘ │
        │                                   │                │      │
        │                                   │                │      │
        │                                   ▼                │      │
        │                            ┌─────────────┐        │      │
        │                            │ Blocker     │        │      │
        │                            │ Detection   │◄───────┘      │
        │                            └──────┬──────┘               │
        │                                   │                       │
        │                      ┌────────────┴────────────┐         │
        │                      │                         │         │
        │                   No blocker              Blocker found  │
        │                      │                         │         │
        │                      ▼                         ▼         │
        │               ┌─────────────┐          ┌─────────────┐   │
        │               │ Continue    │          │ Create      │   │
        │               │ or Complete │          │ Blocker     │   │
        │               └─────────────┘          │ & Pivot     │   │
        │                                        └─────────────┘   │
        │                                                          │
        └──────────────────────────────────────────────────────────┘
```

## Autonomy Levels

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         AUTONOMY LEVELS                                       │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  LEVEL 0: Reactive                                                          │
│  ══════════════════                                                         │
│                                                                             │
│  Behavior:                                                                  │
│  - Only responds when spoken to                                            │
│  - No proactive actions                                                     │
│  - Reports status when asked                                               │
│                                                                             │
│  Allowed Actions:                                                           │
│  - Read files                                                               │
│  - Answer questions                                                         │
│  - Report status                                                            │
│                                                                             │
│  Requires Approval:                                                         │
│  - All write operations                                                    │
│  - All executions                                                           │
│  - All task management                                                      │
│                                                                             │
│  Use Case: Observation, debugging, Q&A                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  LEVEL 1: Cautious                                                          │
│  ══════════════════                                                         │
│                                                                             │
│  Behavior:                                                                  │
│  - Can read and analyze freely                                             │
│  - Plans before executing                                                  │
│  - Asks for approval on significant changes                                │
│                                                                             │
│  Allowed Actions:                                                           │
│  - Read files                                                               │
│  - Search web (SearXNG)                                                    │
│  - Run tests (read-only results)                                           │
│  - Create task comments                                                    │
│                                                                             │
│  Requires Approval:                                                         │
│  - Write files                                                              │
│  - Execute commands                                                         │
│  - Commit changes                                                           │
│  - Create/modify tasks                                                      │
│                                                                             │
│  Use Case: Research, code review, planning                                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  LEVEL 2: Balanced (Default)                                                │
│  ═════════════════════════                                                  │
│                                                                             │
│  Behavior:                                                                  │
│  - Works autonomously on assigned tasks                                    │
│  - Makes decisions within task scope                                       │
│  - Asks for approval on major changes                                      │
│                                                                             │
│  Allowed Actions:                                                           │
│  - All Level 1 actions                                                     │
│  - Write files                                                              │
│  - Run commands (safe list)                                                │
│  - Execute tests                                                            │
│  - Commit changes                                                           │
│  - Create pull requests                                                    │
│  - Update task status                                                      │
│                                                                             │
│  Requires Approval:                                                         │
│  - Merge to main                                                            │
│  - Delete files                                                             │
│  - Change project settings                                                 │
│  - Execute privileged commands                                             │
│                                                                             │
│  Use Case: Standard development work                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  LEVEL 3: Full Autonomy                                                     │
│  ═════════════════════                                                      │
│                                                                             │
│  Behavior:                                                                  │
│  - Full autonomy within sandbox                                            │
│  - Can make all decisions                                                  │
│  - Only blocked on security-critical operations                            │
│                                                                             │
│  Allowed Actions:                                                           │
│  - All Level 2 actions                                                     │
│  - Merge to main (after tests pass)                                        │
│  - Delete files                                                             │
│  - Execute any command                                                     │
│  - Manage sprint tasks                                                     │
│  - Create/delete branches                                                  │
│                                                                             │
│  Requires Approval:                                                         │
│  - Push to external git                                                    │
│  - Deploy to production                                                    │
│  - Modify project settings                                                 │
│  - Access secrets/credentials                                              │
│                                                                             │
│  Use Case: Trusted projects, rapid iteration                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## State Persistence

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         STATE PERSISTENCE                                     │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  /agent-state/                                                              │
│  ├── current-task.json        # Current task assignment                    │
│  ├── context.json             # Agent's working context                     │
│  ├── last-operation.json      # Last operation attempted                    │
│  └── checkpoints/             # Periodic state snapshots                    │
│      ├── checkpoint-001.json                                               │
│      ├── checkpoint-002.json                                               │
│      └── ...                                                               │
│                                                                             │
│  current-task.json:                                                         │
│  {                                                                          │
│    "taskId": "uuid",                                                        │
│    "assignedAt": "2024-01-15T10:30:00Z",                                   │
│    "status": "in_progress"                                                  │
│  }                                                                          │
│                                                                             │
│  context.json:                                                              │
│  {                                                                          │
│    "phase": "implementation",                                               │
│    "lastAction": "Fixed auth middleware",                                  │
│    "nextPlannedAction": "Run verification",                                │
│    "activeFiles": ["src/auth/middleware.ts"],                              │
│    "recentDecisions": [                                                    │
│      {                                                                      │
│        "decision": "Use JWT library",                                      │
│        "reason": "Reduces complexity",                                     │
│        "timestamp": "2024-01-15T10:35:00Z"                                 │
│      }                                                                      │
│    ]                                                                        │
│  }                                                                          │
│                                                                             │
│  last-operation.json:                                                       │
│  {                                                                          │
│    "type": "commit",                                                        │
│    "params": {                                                              │
│      "message": "Add expiry check",                                        │
│      "files": ["src/auth/middleware.ts"]                                   │
│    },                                                                       │
│    "status": "success",                                                     │
│    "timestamp": "2024-01-15T10:40:00Z",                                    │
│    "idempotent": true                                                       │
│  }                                                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Error Recovery

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         ERROR RECOVERY                                        │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  ERROR TYPES                                                                │
│  ═══════════                                                                │
│                                                                             │
│  1. Transient Errors (Retry)                                               │
│     - Network timeouts                                                      │
│     - Service unavailable                                                  │
│     - Rate limits                                                           │
│                                                                             │
│  2. Recoverable Errors (Rollback + Retry)                                  │
│     - Context overflow                                                      │
│     - State corruption                                                      │
│     - Verification loops                                                    │
│                                                                             │
│  3. Fatal Errors (Report + Pivot)                                          │
│     - Permission denied                                                     │
│     - Resource exhausted                                                   │
│     - Unresolvable blocker                                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  RETRY STRATEGY                                                             │
│  ══════════════                                                             │
│                                                                             │
│  Configuration:                                                             │
│  {                                                                          │
│    "maxAttempts": 3,                                                        │
│    "initialDelayMs": 1000,                                                 │
│    "maxDelayMs": 30000,                                                    │
│    "backoffMultiplier": 2                                                  │
│  }                                                                          │
│                                                                             │
│  Example:                                                                   │
│  Attempt 1: Delay 1s                                                        │
│  Attempt 2: Delay 2s                                                        │
│  Attempt 3: Delay 4s                                                        │
│  After 3 failures: Rollback or Report                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  CONTEXT ROLLBACK                                                           │
│  ═════════════════                                                          │
│                                                                             │
│  When triggered:                                                            │
│  - Context token count exceeds limit                                       │
│  - State corruption detected                                               │
│  - Persistent verification failures                                        │
│                                                                             │
│  Process:                                                                   │
│  1. Save current state to checkpoint                                       │
│  2. Load previous successful checkpoint                                    │
│  3. Compact and summarize lost context                                     │
│  4. Continue from known-good state                                         │
│                                                                             │
│  Rollback preserves:                                                        │
│  - Last 5 successful operation states                                      │
│  - Key decisions made                                                      │
│  - Active file list                                                        │
│  - Current phase                                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Agent Communication

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         AGENT COMMUNICATION                                   │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  TO AGENT (Inbound)                                                         │
│  ═══════════════════                                                        │
│                                                                             │
│  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐       │
│  │ Task Assignment │     │ Pivot Signal    │     │ User Message    │       │
│  │                 │     │                 │     │                 │       │
│  │ From: Kanban    │     │ From: Watchdog  │     │ From: Telegram  │       │
│  │                 │     │                 │     │                 │       │
│  │ taskId          │     │ blockerId       │     │ message         │       │
│  │ priority        │     │ nextTaskId      │     │ context         │       │
│  └─────────────────┘     └─────────────────┘     └─────────────────┘       │
│                                                                             │
│  FROM AGENT (Outbound)                                                      │
│  ═══════════════════                                                        │
│                                                                             │
│  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐       │
│  │ Task Update     │     │ Status Update   │     │ Notification    │       │
│  │                 │     │                 │     │                 │       │
│  │ To: Kanban      │     │ To: Orchestrator│     │ To: Notification│       │
│  │                 │     │                 │     │                 │       │
│  │ status          │     │ status          │     │ type            │       │
│  │ progress        │     │ cpuUsage        │     │ message         │       │
│  │ blockers        │     │ memoryUsage     │     │ priority        │       │
│  └─────────────────┘     └─────────────────┘     └─────────────────┘       │
│                                                                             │
│  Message Queue (RabbitMQ)                                                   │
│  ════════════════════════                                                   │
│                                                                             │
│  Queues:                                                                    │
│  - agent.{agentId}.inbox     # Incoming messages                           │
│  - agent.{agentId}.outbox    # Outgoing messages                           │
│  - agent.broadcast           # Broadcast to all agents                     │
│                                                                             │
│  Exchange:                                                                  │
│  - agent.direct             # Direct routing                               │
│  - agent.topic              # Topic-based routing                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Agent Metrics

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         AGENT METRICS                                         │
└──────────────────────────────────────────────────────────────────────────────┘

Collected metrics (reported to Orchestrator):

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  Resource Usage:                                                            │
│  - cpu_usage_percent        # Current CPU usage                            │
│  - memory_usage_mb          # Current memory usage                         │
│  - disk_usage_mb            # Current disk usage                           │
│                                                                             │
│  Performance:                                                               │
│  - tasks_completed          # Total tasks completed                        │
│  - tasks_failed             # Total tasks failed                           │
│  - commits_made             # Total commits                                │
│  - tests_run                # Total tests executed                         │
│  - avg_task_duration_ms     # Average task duration                        │
│                                                                             │
│  Health:                                                                    │
│  - status                   # idle, working, paused, blocked, error        │
│  - last_activity_at         # Timestamp of last activity                   │
│  - uptime_seconds           # Time since agent started                     │
│  - restart_count            # Number of restarts                           │
│                                                                             │
│  Context:                                                                   │
│  - context_tokens           # Current context size                         │
│  - compaction_count         # Number of compactions                        │
│  - checkpoint_count         # Number of checkpoints saved                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```
