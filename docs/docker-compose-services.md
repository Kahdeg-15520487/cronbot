# Docker Compose Services & Ports

## Infrastructure

| Service | Image | External Port | Internal Port | Purpose |
|---------|-------|---------------|---------------|---------|
| **postgres** | postgres:16-alpine | `5433` | 5432 | Primary database |
| **redis** | redis:7-alpine | `6380` | 6379 | Cache, sessions, pub/sub |
| **rabbitmq** | rabbitmq:3-management-alpine | `5673` (AMQP), `15674` (Management UI) | 5672, 15672 | Message queue |
| **minio** | minio/minio:latest | `9002` (API), `9003` (Console) | 9000, 9001 | Object storage (S3-compatible) |

## CronBot Services

| Service | External Port | Internal Port | Purpose |
|---------|---------------|---------------|---------|
| **api** | `5001` | 8080 | REST API (`/api`) + MCP endpoint (`/`) |
| **web** | `3000` | 3000 | Next.js Web UI |
| **agent** | - | - | Claude Agent (profile: `agent`) |

## Quick Reference

```
Web UI:              http://localhost:3000
API:                 http://localhost:5001/api
API Health:          http://localhost:5001/health
MCP Endpoint:        http://localhost:5001/ (Streamable HTTP transport)
PostgreSQL:          localhost:5433 (user: cronbot, db: cronbot)
Redis:               localhost:6380
RabbitMQ Management: http://localhost:15674 (user: cronbot)
MinIO Console:       http://localhost:9003 (user: cronbot)
```

## MCP Tools Available

The MCP endpoint provides these Kanban tools for agents:

| Tool | Description |
|------|-------------|
| `list_projects` | List all available projects |
| `get_project` | Get details of a specific project |
| `list_tasks` | List tasks with optional filters |
| `get_task` | Get details of a specific task |
| `create_task` | Create a new task |
| `update_task` | Update task fields |
| `move_task` | Move task to different status |
| `get_next_task` | Get next task ready for work (Sprint status) |
| `add_task_comment` | Add a comment to a task |
| `get_task_comments` | Get all comments for a task |
| `list_sprints` | List all sprints for a project |
| `get_active_sprint` | Get the currently active sprint |
| `get_board` | Get the Kanban board with tasks by status |

## Start Commands

```bash
# Start core services (api + web + infrastructure)
docker compose up api web

# Start with agent
docker compose --profile agent up

# Start everything
docker compose up

# Rebuild after code changes
docker compose up --build api web

# Force rebuild with no cache
docker compose build --no-cache web
docker compose up web
```

## Service Dependencies

```
web ──────▶ api ──────▶ postgres
                     └──▶ redis
                     └──▶ rabbitmq

agent ─────▶ api (optional, profile: agent)
```

## Environment Variables

Required in `.env`:

```bash
ANTHROPIC_API_KEY=your-key-here
ANTHROPIC_BASE_URL=https://api.anthropic.com  # or custom endpoint
```

Optional:

```bash
POSTGRES_PASSWORD=cronbot
RABBITMQ_PASSWORD=cronbot
MINIO_PASSWORD=cronbot123
```

## Volumes

| Volume | Purpose |
|--------|---------|
| `postgres-data` | PostgreSQL data persistence |
| `redis-data` | Redis data persistence |
| `rabbitmq-data` | RabbitMQ data persistence |
| `minio-data` | MinIO object storage |
| `agent-workspace` | Agent working directory |
| `agent-state` | Agent state/checkpoints |

## Network

All services run on `cronbot_default` bridge network. Services communicate internally using service names as hostnames (e.g., `postgres:5432`, `api:8080`).
