# CronBot

Autonomous AI-powered development assistant.

## Quick Start

```bash
# 1. Clone and configure
cp .env.example .env
nano .env  # Set ANTHROPIC_API_KEY

# 2. Start all services
docker compose up -d

# 3. Access the application
open http://localhost
```

## Services

| Service | URL | Description |
|---------|-----|-------------|
| Web UI | http://localhost | Next.js dashboard and Kanban |
| API | http://localhost/api | REST API endpoints |
| Traefik | http://localhost:8080 | Reverse proxy dashboard |
| RabbitMQ | http://localhost:15672 | Message queue management |
| MinIO | http://localhost:9001 | Object storage console |

Default credentials:
- RabbitMQ: `cronbot` / `cronbot`
- MinIO: `cronbot` / `cronbot123`

## Running with Agent

To start the agent service:

```bash
docker compose --profile agent up -d
```

## Development

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- Docker & Docker Compose

### API (.NET)

```bash
cd src/CronBot.Api
dotnet run
```

### Web UI (Next.js)

```bash
cd src/CronBot.Web
npm install
npm run dev
```

### Agent (Node.js)

```bash
cd src/CronBot.Agent
npm install
npm run dev
```

## Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Web UI    │────▶│    API      │────▶│  PostgreSQL │
│  (Next.js)  │     │  (.NET 8)   │     │             │
└─────────────┘     └──────┬──────┘     └─────────────┘
                          │
                    ┌─────┴─────┐
                    │           │
              ┌─────▼─────┐ ┌───▼────┐
              │   Agent   │ │ Redis  │
              │ (Node.js) │ │        │
              └───────────┘ └────────┘
```

## Documentation

See `design-docs/` for detailed architecture documentation.

## Progress

See `PROGRESS.md` for implementation status.
