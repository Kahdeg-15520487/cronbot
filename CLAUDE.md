# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is a **design documentation repository** for CronBot, an autonomous AI-powered development assistant. It contains comprehensive architectural specifications, technical designs, and deployment guidance, but **no implementation code**.

All documentation is in the `design-docs/` directory. When working with this repository, you'll be modifying or creating documentation, not implementing features.

## Documentation Structure

```
design-docs/
├── README.md              # Overview and quick start
├── architecture.md        # System architecture and component breakdown
├── tech-stack.md          # Technology decisions with rationale
├── database-schema.md     # Complete PostgreSQL schema (25+ tables)
├── api-contracts.md       # gRPC and REST API specifications
├── mcp-skills.md          # MCP and Skills architecture
├── agent-system.md        # Agent lifecycle, autonomy levels, state persistence
├── authentication.md      # Authentik OIDC integration
├── deployment.md          # Docker Compose setup, traefik.me configuration
└── sprint-workflow.md     # SCRUM ceremonies, blocker detection
```

## Key Architecture Concepts

### Technology Stack
- **Services**: C# / .NET 8 (ASP.NET Core, gRPC, Entity Framework Core)
- **Agents**: TypeScript / Node.js (Claude Agent SDK, MCP SDK)
- **Skills**: Python scripts executed directly by agents
- **Infrastructure**: Docker, Komodo, Traefik, PostgreSQL 16, Redis 7, RabbitMQ 3
- **Authentication**: Authentik (OIDC/OAuth2)

### Core Services (10 C# services)
1. **Orchestrator** - Project pod lifecycle via Komodo API
2. **Kanban** - Task/project management, SCRUM ceremonies
3. **Permission** - RBAC with preset roles (reader, contributor, planner, executor, admin)
4. **Runner** - Build/test/preview execution, Puppeteer screenshots
5. **Notification** - Telegram push, WebSocket, email
6. **Memory** - Context storage, compaction, archival
7. **Git** - Internal Gitea management, loop detection
8. **Watchdog** - Container monitoring, syscall tracking (Falco), audit logs
9. **Auth** - Authentik integration, JWT management
10. **Template** - Project scaffolding from templates

### Project Pod Architecture
Each project runs in an isolated Docker Compose stack managed by Komodo:
- **Agent Container** - Claude SDK, MCP client, Python runtime for skills
- **Execution Container** - Project files, runtime environment, tools/compilers
- **Watchdog Sidecar** - Hidden from agent, syscall monitoring, behavior analysis
- **Preview Container** (on-demand) - Running application with Puppeteer screenshots

### Autonomy Levels (0-3)
- **Level 0: Reactive** - Read-only, responds when spoken to
- **Level 1: Cautious** - Read/analyze, plans before executing, approval on writes
- **Level 2: Balanced** (default) - Autonomous on tasks, approval on major changes
- **Level 3: Full Autonomy** - Full sandbox autonomy, only security-critical ops need approval

### MCP (Model Context Protocol)
Four MCP groups:
1. **System** - Core tools: filesystem, git, kanban, runner, search, permission, memory, notification
2. **Project** - Auto-provisioned per project
3. **Custom** - User/agent-created MCPs (e.g., Jira, Slack integrations)
4. **External** - Third-party MCPs (GitHub, filesystem, etc.)

### Skills
Python scripts executed directly in agent context with workspace access. Scopes: system, team, project. Lower overhead than MCPs for quick operations.

### Blocker Detection
- Code loop detection (file changes back and forth)
- Verification loop detection (failed tests)
- Tool failure detection (MCP retry limits)
- Agent stuck detection (activity monitoring)
- Dependency and scope creep detection

## URL Strategy (traefik.me)

All services use `https://{service}.{TRAEFIK_ID}.traefik.me`:
- CronBot Web UI: `{TRAEFIK_ID}.traefik.me`
- CronBot API: `api.{TRAEFIK_ID}.traefik.me`
- Authentik: `auth.{TRAEFIK_ID}.traefik.me`
- Gitea: `git.{TRAEFIK_ID}.traefik.me`
- Komodo: `komodo.{TRAEFIK_ID}.traefik.me`
- Previews: `project-{id}-task-{id}.{TRAEFIK_ID}.traefik.me`

## Security Model (Defense in Depth)

1. **Network Isolation** - Each project in isolated Docker network
2. **Authentication** - Authentik OIDC, JWT tokens, Telegram ID binding
3. **Authorization** - RBAC with per-project/team permissions
4. **Container Isolation** - Docker namespaces, Seccomp profiles, resource limits
5. **Watchdog Monitoring** - Falco syscall tracking, file access logging
6. **Audit Logging** - Immutable logs of all agent actions

## Common Commands Reference

### Deployment
```bash
# Install Komodo
curl -fsSL https://get.komodo.dev | bash

# Deploy
cp .env.example .env
nano .env  # Configure secrets
docker compose up -d
```

### Health Checks
```bash
curl https://api.{TRAEFIK_ID}.traefik.me/health
curl https://auth.{TRAEFIK_ID}.traefik.me/health
curl https://git.{TRAEFIK_ID}.traefik.me/healthcheck
```

### Backup
```bash
docker compose exec -T postgres pg_dumpall -U cronbot > backup.sql
```

## Documentation Guidelines

When modifying or creating documentation:

1. **Keep cross-references updated** - If you add a new service, update `architecture.md`
2. **API changes** - Update `api-contracts.md` for any gRPC/REST changes
3. **Schema changes** - Modify `database-schema.md` for database changes
4. **New technology** - Add rationale to `tech-stack.md`
5. **Deployment changes** - Update `deployment.md` with new services or configuration

## Key Files to Understand

- `architecture.md` - Read first for system overview
- `agent-system.md` - Critical for understanding agent autonomy
- `mcp-skills.md` - Understand extensibility model
- `api-contracts.md` - Reference for all API definitions
- `deployment.md` - Complete Docker Compose setup
