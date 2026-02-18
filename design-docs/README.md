# CronBot Design Documentation

> An autonomous AI-powered development assistant that operates through Telegram, Web UI, and MCP Server interfaces.

## Overview

CronBot is a self-contained, deployable system that uses Claude Agent SDK to autonomously work on software projects. It features:

- **Multiple Interfaces**: Telegram chatbot, Web UI (Kanban board + chat), MCP Server
- **Autonomous Agents**: Configurable autonomy levels (0-3) for self-directed work
- **Sprint-Based Workflow**: SCRUM-inspired ceremonies with planning, review, and retrospectives
- **Sandboxed Execution**: Docker-based isolation with watchdog monitoring
- **Extensible via MCP**: Custom MCPs and Python skills can be created and deployed
- **Git Integration**: Internal Gitea with optional mirroring to external providers

## Documentation Index

### Core Architecture

| Document | Description |
|----------|-------------|
| [Architecture](./architecture.md) | System architecture, service breakdown, and data flow |
| [Tech Stack](./tech-stack.md) | Technology decisions and justifications |
| [Database Schema](./database-schema.md) | Complete PostgreSQL schema with all tables |
| [API Contracts](./api-contracts.md) | gRPC service definitions and REST API endpoints |

### Component Documentation

| Document | Description |
|----------|-------------|
| [MCP & Skills](./mcp-skills.md) | MCP architecture, discovery, and Python skills system |
| [Agent System](./agent-system.md) | Agent container, Claude SDK integration, task execution |
| [Authentication](./authentication.md) | Authentik integration, OAuth/OIDC flows |
| [Deployment](./deployment.md) | Docker Compose deployment, traefik.me configuration |
| [Sprint Workflow](./sprint-workflow.md) | SCRUM ceremonies, blocker detection, task management |

## Quick Start

### Prerequisites

1. **Komodo** - Docker Compose management system
2. **Docker** - Container runtime
3. **Domain** - traefik.me for automatic DNS (or custom domain)

### Deployment

```bash
# Clone repository
git clone https://github.com/yourorg/cronbot.git
cd cronbot

# Copy environment file
cp .env.example .env

# Edit environment variables
nano .env

# Deploy with Komodo
komodo deploy -f docker-compose.yml
```

### First Run

1. Access Authentik at `https://auth.<your-id>.traefik.me`
2. Create admin account
3. Access CronBot at `https://cronbot.<your-id>.traefik.me`
4. Connect Telegram via `/start` command

## System Requirements

### Minimum

- CPU: 4 cores
- RAM: 8 GB
- Disk: 50 GB SSD
- OS: Linux (Ubuntu 22.04+ recommended)

### Recommended (Production)

- CPU: 8 cores
- RAM: 16 GB
- Disk: 200 GB SSD
- OS: Linux (Ubuntu 22.04+)

## License

MIT License - See [LICENSE](../LICENSE) for details.

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for development guidelines.
