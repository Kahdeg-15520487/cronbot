# Technology Stack

## Overview

This document outlines all technology decisions for CronBot, including languages, frameworks, infrastructure, and third-party services.

## Languages

### C# / .NET 8

**Use Cases:**
- All system services (Orchestrator, Kanban, Permission, Runner, etc.)
- API Gateway
- MCP tools (system MCPs)
- Background workers

**Libraries:**
| Library | Purpose |
|---------|---------|
| ASP.NET Core | Web API, gRPC, SignalR |
| Entity Framework Core | PostgreSQL ORM |
| MassTransit | RabbitMQ message bus |
| ModelContextProtocol | Official C# MCP SDK |
| Yarp | Reverse Proxy |
| FluentValidation | Input validation |
| Serilog | Structured logging |
| dotenv.net | Configuration |

### TypeScript / Node.js

**Use Cases:**
- Agent container (Claude Agent SDK)
- Web UI frontend (Next.js)
- Custom MCP servers (optional, user preference)

**Libraries:**
| Library | Purpose |
|---------|---------|
| @anthropic-ai/claude-code | Official Claude Agent SDK |
| @modelcontextprotocol/sdk | Official MCP SDK |
| React / Next.js | Web UI |
| TailwindCSS | Styling |
| tRPC | Type-safe API |
| Zod | Validation |
| Puppeteer | Screenshots, verification |

### Python

**Use Cases:**
- Skills (utility scripts)
- Code analysis tools
- Data processing

**Libraries:**
| Library | Purpose |
|---------|---------|
| Standard library | Core functionality (os, sys, json, argparse) |
| ast | Code analysis |
| pyyaml | YAML parsing |
| requests | HTTP requests |

## Infrastructure

### Container Orchestration

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Container Runtime** | Docker Engine | Container execution |
| **Compose Management** | Komodo | Stack lifecycle management |
| **Stack Definition** | Docker Compose | Service orchestration |

### Networking

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Reverse Proxy** | Traefik v3 | SSL, routing, load balancing |
| **DNS** | traefik.me | Free dynamic DNS |
| **Internal DNS** | Docker | Service discovery |

### Data Storage

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Primary Database** | PostgreSQL 16 | All persistent data |
| **Cache/Sessions** | Redis 7 | Caching, sessions, pub/sub |
| **Object Storage** | MinIO | Artifacts, screenshots, files |

### Message Queue

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Message Broker** | RabbitMQ 3 | Service communication, events |

### Authentication

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Identity Provider** | Authentik | OIDC, OAuth2, user management |
| **Token Format** | JWT | API authentication |

### Version Control

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Internal Git** | Gitea | Repository hosting |
| **Mirror Targets** | GitHub, Gitea | External repository sync |

### Search

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Search Engine** | SearXNG | Self-hosted metasearch |

### Container Registry

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Internal Registry** | Docker Distribution | Custom MCP images |
| **External** | Docker Hub / GHCR | Base images |

## Service Dependencies

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         SERVICE DEPENDENCIES                                  │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  TRAEFIK (Entry Point)                                                      │
│  │                                                                          │
│  ├── AUTHENTIK ──────────► PostgreSQL, Redis                               │
│  │                                                                          │
│  ├── GITEA ──────────────► PostgreSQL                                      │
│  │                                                                          │
│  ├── KOMODO ─────────────► PostgreSQL, Docker Socket                       │
│  │                                                                          │
│  ├── SEARXNG ────────────► (standalone)                                    │
│  │                                                                          │
│  ├── MINIO ──────────────► (standalone)                                    │
│  │                                                                          │
│  └── CRONBOT API ────────► PostgreSQL, Redis, RabbitMQ,                   │
│  │                        Authentik, Komodo, Gitea, MinIO                  │
│  │                                                                          │
│  └── CRONBOT WEB UI ─────► CRONBOT API                                     │
│                                                                             │
│  PROJECT PODS ───────────► CRONBOT API, Gitea, MinIO                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Third-Party Services

### AI / LLM

| Service | Purpose |
|---------|---------|
| Anthropic Claude API | Primary LLM via Claude Agent SDK |
| Custom endpoint support | Self-hosted or alternative providers |

### Communication

| Service | Purpose |
|---------|---------|
| Telegram Bot API | Chat interface |

### External MCPs (Optional)

| MCP | Purpose |
|-----|---------|
| GitHub MCP | GitHub integration |
| Brave Search MCP | Alternative search (replaced by SearXNG) |
| Filesystem MCP | File operations |
| Any user-configured MCP | Extensibility |

## URL Strategy

### Development / Self-Hosted

Using **traefik.me** for free dynamic DNS:

```
https://{service}.{traefik-id}.traefik.me

Examples:
- https://cronbot.abc123.traefik.me      # Main app
- https://auth.abc123.traefik.me         # Authentik
- https://git.abc123.traefik.me          # Gitea
- https://komodo.abc123.traefik.me       # Komodo
- https://search.abc123.traefik.me       # SearXNG
- https://project-123.abc123.traefik.me  # Preview
```

### SSL Certificates

| Environment | Method |
|-------------|--------|
| Development | mkcert (local CA) |
| Production | Let's Encrypt via Traefik |

## Resource Requirements

### Minimum (Development)

| Component | CPU | RAM | Disk |
|-----------|-----|-----|------|
| Host | 4 cores | 8 GB | 50 GB |
| PostgreSQL | 0.5 | 1 GB | 10 GB |
| Redis | 0.25 | 256 MB | 1 GB |
| RabbitMQ | 0.25 | 512 MB | 1 GB |
| Authentik | 0.5 | 512 MB | 1 GB |
| Gitea | 0.25 | 256 MB | 5 GB |
| Komodo | 0.25 | 256 MB | 1 GB |
| SearXNG | 0.25 | 256 MB | 1 GB |
| MinIO | 0.25 | 512 MB | 10 GB |
| CronBot Services | 1 | 2 GB | 5 GB |
| Project Pods | 1 | 2 GB | 10 GB |

### Recommended (Production)

| Component | CPU | RAM | Disk |
|-----------|-----|-----|------|
| Host | 8 cores | 16 GB | 200 GB |
| All services | Scaled accordingly |

## Technology Decision Log

### Why C# for Services?

- Strong typing and compile-time safety
- Excellent async/await support
- First-class gRPC support
- Entity Framework Core for clean data access
- Cross-platform with .NET 8
- Good performance characteristics

### Why TypeScript for Agent?

- Official Claude Agent SDK is TypeScript-first
- MCP SDK has best TypeScript support
- Easy JSON manipulation for context
- Good tooling for the AI/LLM ecosystem

### Why Komodo over plain Docker?

- Web UI for stack management
- REST API for programmatic control
- Real-time logs and monitoring
- GitOps support
- Multi-server ready (future scaling)

### Why Authentik over Keycloak?

- Lower resource usage (~500MB vs ~1GB)
- Simpler configuration
- Modern UI
- Docker-native
- OIDC + Proxy provider support

### Why SearXNG over Brave Search?

- Self-hosted, no API keys
- No rate limits
- Privacy-focused
- Aggregates multiple engines
- No external dependencies

### Why traefik.me?

- Free dynamic DNS
- No domain registration needed
- Automatic SSL support
- Simple setup
- Works with Traefik out of the box

### Why PostgreSQL over MySQL?

- Better JSONB support
- Superior full-text search
- Better concurrency
- More predictable performance
- Strong ecosystem

## Version Requirements

| Component | Minimum Version | Recommended |
|-----------|-----------------|-------------|
| Docker | 24.0 | 25.0+ |
| Docker Compose | 2.20 | 2.24+ |
| .NET | 8.0 | 8.0 |
| Node.js | 20 LTS | 20 LTS |
| Python | 3.11 | 3.12 |
| PostgreSQL | 15 | 16 |
| Redis | 7.0 | 7.2 |
| RabbitMQ | 3.12 | 3.13 |
