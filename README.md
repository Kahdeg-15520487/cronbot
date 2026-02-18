# CronBot

Autonomous AI-powered development assistant.

## Quick Start

```bash
# 1. Clone and configure
cp .env.example .env
nano .env  # Set ANTHROPIC_API_KEY

# 2. Start all services
docker compose up -d

# 3. Run database migrations
cd src/CronBot.Api
dotnet ef database update --project ../CronBot.Infrastructure \
  --connection "Host=localhost;Port=5433;Database=cronbot;Username=cronbot;Password=cronbot"

# 4. Access the application
open http://localhost:3000
```

## Services

| Service | URL | Description |
|---------|-----|-------------|
| Web UI | http://localhost:3000 | Next.js dashboard and Kanban |
| API | http://localhost:5001/api | REST API endpoints |
| PostgreSQL | localhost:5433 | Database (user: cronbot, db: cronbot) |
| Redis | localhost:6380 | Cache and session storage |
| RabbitMQ | http://localhost:15674 | Message queue management |
| MinIO | http://localhost:9003 | Object storage console |

Default credentials:
- PostgreSQL: `cronbot` / `cronbot`
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
