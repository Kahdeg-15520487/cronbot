# System Architecture

## High-Level Overview

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                              USER INTERFACES                                  │
├──────────────────┬───────────────────┬────────────────────────────────────────┤
│  Telegram Bot    │     Web UI        │           MCP Server                   │
│  (Chat + Slash)  │  (Chat + Kanban   │      (External Integration)            │
│   + Settings)    │   + Settings)     │                                        │
└────────┬─────────┴─────────┬─────────┴───────────────────┬────────────────────┘
         │                   │                             │
         └───────────────────┼─────────────────────────────┘
                             │
                 ┌───────────▼───────────┐
                 │   API Gateway (Yarp)  │
                 │      + JWT Auth       │
                 └───────────┬───────────┘
                             │
         ┌───────────────────┼───────────────────────────────────────────────┐
         │                   │                                               │
         │   ┌───────────────▼───────────────────────────────────────────────┤
         │   │                    CORE SERVICES (C#)                          │
         │   │                                                               │
         │   │  ┌─────────────────────────────────────────────────────────┐ │
         │   │  │                 MCP LAYER (Official C# SDK)              │ │
         │   │  │                                                         │ │
         │   │  │  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐  │ │
         │   │  │  │ MCP Registry  │ │ MCP Router    │ │ MCP Executor  │  │ │
         │   │  │  └───────────────┘ └───────────────┘ └───────────────┘  │ │
         │   │  │                                                         │ │
         │   │  │  Groups: System | Project | Custom | External           │ │
         │   │  └─────────────────────────────────────────────────────────┘ │
         │   │                                                               │
         │   │  ┌─────────────────────────────────────────────────────────┐ │
         │   │  │                SKILLS LAYER                             │ │
         │   │  │                                                         │ │
         │   │  │  Python scripts/modules executed in agent context       │ │
         │   │  │  Scopes: System | Team | Project                        │ │
         │   │  │                                                         │ │
         │   │  └─────────────────────────────────────────────────────────┘ │
         │   │                                                               │
         │   │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
         │   │  │ Orchestrator│ │   Kanban    │ │ Permission  │            │
         │   │  │ (Komodo API)│ │   Service   │ │   Service   │            │
         │   │  └─────────────┘ └─────────────┘ └─────────────┘            │
         │   │                                                               │
         │   │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
         │   │  │   Runner    │ │ Notification│ │   Memory    │            │
         │   │  │   Service   │ │   Service   │ │   Service   │            │
         │   │  └─────────────┘ └─────────────┘ └─────────────┘            │
         │   │                                                               │
         │   │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
         │   │  │    Git      │ │  Watchdog   │ │   Auth      │            │
         │   │  │   Service   │ │   Service   │ │   Service   │            │
         │   │  └─────────────┘ └─────────────┘ └─────────────┘            │
         │   │                                                               │
         │   │  ┌─────────────────────────────────────────────────────────┐ │
         │   │  │         MONITORING DASHBOARD (WebUI)                    │ │
         │   │  │                                                         │ │
         │   │  │  - Kanban board      - MCP/Skill config                │ │
         │   │  │  - Project status    - Preview gallery                  │ │
         │   │  │  - Agent activity    - Settings                         │ │
         │   │  │  - Sandbox metrics   - User/Team management             │ │
         │   │  └─────────────────────────────────────────────────────────┘ │
         │   │                                                               │
         │   │  ┌─────────────────────────────────────────────────────────┐ │
         │   │  │              TEMPLATE SERVICE                            │ │
         │   │  │                                                         │ │
         │   │  │  dotnet-api | nextjs | python-fastapi | mcp-server      │ │
         │   │  │  skill | ... (extensible)                               │ │
         │   │  └─────────────────────────────────────────────────────────┘ │
         │   │                                                               │
         │   └───────────────────────────────────────────────────────────────┤
         │                                                                       │
         │   ┌───────────────────────────────────────────────────────────────┐ │
         │   │                    PROJECT PODS                                │ │
         │   │                  (Docker Compose via Komodo)                    │ │
         │   │                                                               │ │
         │   │  Each pod: Agent | Execution | Watchdog | Preview (optional) │ │
         │   │                                                               │ │
         │   └───────────────────────────────────────────────────────────────┘ │
         │                                                                       │
         └───────────────────────────────────────────────────────────────────────┘
                             │
                 ┌───────────▼───────────┐
                 │   Persistent Storage  │
                 │  ┌─────┐ ┌─────┐ ┌────┴─────┐
                 │  │ PSQL │ │Redis│ │ S3/MinIO │
                 │  └─────┘ └─────┘ └──────────┘
                 └───────────────────────┘
```

## Service Breakdown

### Gateway Layer

| Service | Technology | Description |
|---------|------------|-------------|
| **Telegram Gateway** | C# / ASP.NET Core | Telegram Bot API integration, message routing |
| **Web Gateway** | C# / ASP.NET Core + React | Web UI with Kanban board, chat, settings |
| **MCP Gateway** | C# / ASP.NET Core | MCP protocol server for external integrations |
| **API Gateway** | Yarp (Reverse Proxy) | Request routing, rate limiting, auth |

### Core Services (C#)

| Service | Description |
|---------|-------------|
| **Orchestrator** | Project pod lifecycle via Komodo API, agent scaling |
| **Kanban** | Task/project management, sprint ceremonies |
| **Permission** | Role-based access control, permission checks |
| **Runner** | Build/test/preview execution, verification |
| **Notification** | Telegram push, WebSocket, email notifications |
| **Memory** | Context storage, compaction, conversation archival |
| **Git** | Internal Gitea management, mirroring, loop detection |
| **Watchdog** | Container monitoring, behavior analysis, audit logging |
| **Auth** | Authentik integration, JWT management |
| **Template** | Project scaffolding from templates |

### Infrastructure Services

| Service | Description |
|---------|-------------|
| **Authentik** | OIDC/OAuth2 authentication, user management |
| **Komodo** | Docker Compose stack management |
| **Traefik** | Reverse proxy, SSL, routing |
| **Gitea** | Internal git hosting |
| **SearXNG** | Self-hosted search engine |
| **PostgreSQL** | Primary database |
| **Redis** | Cache, sessions, pub/sub |
| **RabbitMQ** | Message queue |
| **MinIO** | Object storage |

## Project Pod Architecture

Each project runs in its own isolated Docker Compose stack managed by Komodo:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  PROJECT POD (Komodo Stack)                                                  │
│                                                                             │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────────────┐   │
│  │ Agent Container  │ │ Execution Cont.  │ │ Watchdog Sidecar         │   │
│  │                  │ │                  │ │ (Hidden from agent)      │   │
│  │ - Claude SDK     │ │ - Project files  │ │ - Syscall monitoring     │   │
│  │ - MCP Client     │ │ - Runtime env    │ │ - File access logging    │   │
│  │ - Python runtime │ │ - Tools/compilers│ │ - Behavior analysis      │   │
│  │ - Skills access  │ │                  │ │ - Audit log generation   │   │
│  └────────┬─────────┘ └────────┬─────────┘ └──────────────────────────┘   │
│           │                    │                                           │
│           └────────────────────┘                                           │
│                      │                                                      │
│              Shared Volume                                                   │
│              /workspace                                                      │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ Preview Container (on-demand)                                         │  │
│  │ - Running application                                                 │  │
│  │ - URL: project-id-task-id.<traefik-id>.traefik.me                    │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  Network: project-{id}-network (isolated)                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Data Flow

### Task Execution Flow

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         TASK EXECUTION FLOW                                   │
└──────────────────────────────────────────────────────────────────────────────┘

1. Task Assignment
   ┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
   │  Kanban  │────►│ Orchestr │────►│  Agent   │────►│  Task    │
   │  Service │     │ ator     │     │ Container│     │  Started │
   └──────────┘     └──────────┘     └──────────┘     └──────────┘

2. Agent Initialization
   ┌──────────────────────────────────────────────────────────────────────┐
   │  Agent Container                                                      │
   │                                                                       │
   │  a. Fetch MCP registry for project                                   │
   │  b. Initialize MCP clients (filesystem, git, kanban, runner, etc.)   │
   │  c. Load task context from Memory Service                            │
   │  d. Begin task execution                                             │
   │                                                                       │
   └──────────────────────────────────────────────────────────────────────┘

3. Task Execution Loop
   ┌──────────────────────────────────────────────────────────────────────┐
   │                                                                       │
   │  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐            │
   │  │ Read files  │────►│ Plan action │────►│ Execute     │            │
   │  │ (MCP)       │     │ (Claude)    │     │ (MCP/Skill) │            │
   │  └─────────────┘     └─────────────┘     └──────┬──────┘            │
   │                                                 │                    │
   │                                                 ▼                    │
   │                                          ┌─────────────┐            │
   │                                          │ Verify      │            │
   │                                          │ (Runner)    │            │
   │                                          └──────┬──────┘            │
   │                                                 │                    │
   │                                    ┌────────────┴────────────┐      │
   │                                    │                         │      │
   │                                 Pass                      Fail      │
   │                                    │                         │      │
   │                                    ▼                         ▼      │
   │                            ┌─────────────┐          ┌─────────────┐ │
   │                            │ Commit      │          │ Iterate     │ │
   │                            │ (Git MCP)   │          │ & Fix       │ │
   │                            └──────┬──────┘          └──────┬──────┘ │
   │                                   │                        │        │
   │                                   │    ┌───────────────────┘        │
   │                                   │    │                            │
   │                                   ▼    ▼                            │
   │                            ┌─────────────────┐                      │
   │                            │ Blocker         │                      │
   │                            │ Detection       │                      │
   │                            └────────┬────────┘                      │
   │                                     │                               │
   │                        ┌────────────┴────────────┐                  │
   │                        │                         │                  │
   │                    No blocker               Blocker found           │
   │                        │                         │                  │
   │                        ▼                         ▼                  │
   │                 ┌─────────────┐          ┌─────────────┐            │
   │                 │ Continue    │          │ Create      │            │
   │                 │ or Complete │          │ Blocker     │            │
   │                 └─────────────┘          │ Pivot       │            │
   │                                          └─────────────┘            │
   │                                                                     │
   └──────────────────────────────────────────────────────────────────────┘

4. Task Completion
   ┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
   │  Commit  │────►│  Create  │────►│ Update   │────►│ Notify   │
   │  & Push  │     │  PR      │     │  Kanban  │     │  User    │
   └──────────┘     └──────────┘     └──────────┘     └──────────┘
```

## Security Model

### Defense in Depth

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         SECURITY LAYERS                                       │
└──────────────────────────────────────────────────────────────────────────────┘

Layer 1: Network Isolation
├── Each project in isolated Docker network
├── Services communicate via internal DNS
└── External access only through Traefik

Layer 2: Authentication
├── Authentik OIDC for Web UI
├── JWT tokens for API access
├── Telegram ID binding for chat
└── API tokens for MCP access

Layer 3: Authorization
├── Role-based access control (RBAC)
├── Per-project permissions
├── Team-level permissions
└── Permission presets (reader, contributor, planner, admin)

Layer 4: Container Isolation
├── Docker namespace isolation
├── Seccomp profiles
├── Read-only filesystems where possible
└── Resource limits (CPU, memory)

Layer 5: Watchdog Monitoring
├── Syscall monitoring (Falco)
├── File access logging
├── Network activity monitoring
└── Behavior anomaly detection

Layer 6: Audit Logging
├── Immutable audit logs
├── All agent actions recorded
├── Command execution history
└── Access patterns tracked
```

### Permission Presets

| Preset | Permissions |
|--------|-------------|
| **Reader** | `read:files`, `read:tasks` |
| **Contributor** | Reader + `write:files`, `write:tasks` (own) |
| **Planner** | Contributor + `read:*`, `write:tasks`, `manage:sprint` |
| **Executor** | `read:*`, `write:files`, `execute:*` |
| **Admin** | All permissions |

## Scalability

### Horizontal Scaling

- Multiple agents per project (configurable, default: 3)
- Agent pool management per team
- Concurrent project execution

### Resource Management

```yaml
# Per-project resource limits (configurable)
scaling:
  max_agents: 3
  resource_limits:
    cpu_per_agent: 0.5      # CPU cores
    memory_per_agent: 512   # MB
    disk_per_project: 5     # GB

# System-wide limits
system:
  max_projects_per_team: 10
  max_concurrent_previews: 50
  max_tasks_per_sprint: 20
```

## Failure Recovery

### Agent Crash Recovery

1. Komodo detects container failure
2. Container automatically restarted
3. Agent reads last state from `/agent-state/`
4. Resume from last checkpoint
5. Report recovery to Orchestrator

### Context Rollback

1. On persistent errors, agent context is rolled back
2. Preserves last N successful steps (default: 5)
3. Continues from safe state
4. Logs rollback event

### Blocker Pivot

1. Blocker detected by Watchdog
2. Agent receives pivot signal
3. Current task state saved
4. Blocker created on Kanban
5. User notified
6. Agent moves to next available task
