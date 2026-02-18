# Deployment

## Overview

CronBot is deployed as a single Docker Compose stack with Komodo as the management layer. This document covers the complete deployment process.

## Prerequisites

### System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 4 cores | 8 cores |
| RAM | 8 GB | 16 GB |
| Disk | 50 GB SSD | 200 GB SSD |
| OS | Linux (Ubuntu 22.04+) | Linux (Ubuntu 22.04+) |

### Software Requirements

- Docker 24.0+
- Docker Compose 2.20+
- Komodo (Docker Compose management)

### Install Komodo

```bash
# Quick install
curl -fsSL https://get.komodo.dev | bash

# Or manual install
docker pull ghcr.io/moghtech/komodo:latest
```

## Configuration

### Environment Variables

Create `.env` file:

```bash
# ═══════════════════════════════════════════════════════════════
# CRONBOT CONFIGURATION
# ═══════════════════════════════════════════════════════════════

# Traefik ID for traefik.me domain
# Get your ID from https://traefik.me
TRAEFIK_ID=your-unique-id

# ═══════════════════════════════════════════════════════════════
# DATABASE
# ═══════════════════════════════════════════════════════════════
POSTGRES_USER=cronbot
POSTGRES_PASSWORD=change-this-to-a-secure-password
POSTGRES_DB=cronbot

# ═══════════════════════════════════════════════════════════════
# AUTHENTIK
# ═══════════════════════════════════════════════════════════════
AUTHENTIK_SECRET_KEY=generate-with-openssl-rand-base64-60
AUTHENTIK_BOOTSTRAP_PASSWORD=admin-initial-password

# ═══════════════════════════════════════════════════════════════
# GITEA
# ═══════════════════════════════════════════════════════════════
GITEA__server__ROOT_URL=https://git.${TRAEFIK_ID}.traefik.me
GITEA__security__SECRET_KEY=generate-with-openssl-rand-hex-32

# ═══════════════════════════════════════════════════════════════
# MINIO
# ═══════════════════════════════════════════════════════════════
MINIO_ROOT_USER=cronbot
MINIO_ROOT_PASSWORD=change-this-to-a-secure-password

# ═══════════════════════════════════════════════════════════════
# RABBITMQ
# ═══════════════════════════════════════════════════════════════
RABBITMQ_USER=cronbot
RABBITMQ_PASSWORD=change-this-to-a-secure-password

# ═══════════════════════════════════════════════════════════════
# SEARXNG
# ═══════════════════════════════════════════════════════════════
SEARXNG_SECRET=generate-with-openssl-rand-hex-32

# ═══════════════════════════════════════════════════════════════
# CRONBOT
# ═══════════════════════════════════════════════════════════════
CRONBOT_JWT_SECRET=generate-with-openssl-rand-hex-32
CRONBOT_ENCRYPTION_KEY=generate-with-openssl-rand-hex-32

# Anthropic API
ANTHROPIC_API_KEY=your-anthropic-api-key

# Telegram Bot
TELEGRAM_BOT_TOKEN=your-telegram-bot-token

# ═══════════════════════════════════════════════════════════════
# KOMODO
# ═══════════════════════════════════════════════════════════════
KOMODO_DATABASE_URL=postgres://${POSTGRES_USER}:${POSTGRES_PASSWORD}@postgres:5432/komodo
```

## Docker Compose

### Main docker-compose.yml

```yaml
version: "3.8"

x-common-env: &common-env
  TRAEFIK_ID: ${TRAEFIK_ID}

x-healthcheck: &default-healthcheck
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 10s

services:
  # ═══════════════════════════════════════════════════════════════
  # REVERSE PROXY
  # ═══════════════════════════════════════════════════════════════
  traefik:
    image: traefik:v3.0
    command:
      - "--api.insecure=true"
      - "--providers.docker=true"
      - "--providers.docker.exposedbydefault=false"
      - "--providers.docker.network=cronbot-network"
      - "--entrypoints.web.address=:80"
      - "--entrypoints.websecure.address=:443"
      - "--entrypoints.web.http.redirections.entrypoint.to=websecure"
      - "--entrypoints.web.http.redirections.entrypoint.scheme=https"
      # traefik.me wildcard certificate
      - "--certificatesresolvers.traefikme.acme.tlschallenge=true"
      - "--certificatesresolvers.traefikme.acme.email=admin@${TRAEFIK_ID}.traefik.me"
      - "--certificatesresolvers.traefikme.acme.storage=/letsencrypt/acme.json"
      - "--certificatesresolvers.traefikme.acme.caserver=https://acme-v02.api.letsencrypt.org/directory"
    ports:
      - "80:80"
      - "443:443"
      - "8080:8080"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - traefik-letsencrypt:/letsencrypt
    networks:
      - cronbot-network
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.traefik.rule=Host(`traefik.${TRAEFIK_ID}.traefik.me`)"
      - "traefik.http.routers.traefik.tls=true"
      - "traefik.http.routers.traefik.tls.certresolver=traefikme"
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "traefik", "healthcheck"]

  # ═══════════════════════════════════════════════════════════════
  # AUTHENTICATION
  # ═══════════════════════════════════════════════════════════════
  authentik-server:
    image: ghcr.io/goauthentik/server:2024.10
    environment:
      AUTHENTIK_SECRET_KEY: ${AUTHENTIK_SECRET_KEY}
      AUTHENTIK_REDIS__HOST: redis
      AUTHENTIK_REDIS__PASSWORD: ""
      AUTHENTIK_POSTGRESQL__HOST: postgres
      AUTHENTIK_POSTGRESQL__USER: ${POSTGRES_USER}
      AUTHENTIK_POSTGRESQL__PASSWORD: ${POSTGRES_PASSWORD}
      AUTHENTIK_POSTGRESQL__NAME: authentik
      AUTHENTIK_BOOTSTRAP_PASSWORD: ${AUTHENTIK_BOOTSTRAP_PASSWORD}
    networks:
      - cronbot-network
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.authentik.rule=Host(`auth.${TRAEFIK_ID}.traefik.me`)"
      - "traefik.http.routers.authentik.tls=true"
      - "traefik.http.routers.authentik.tls.certresolver=traefikme"
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:9000/health"]

  authentik-worker:
    image: ghcr.io/goauthentik/server:2024.10
    command: worker
    environment:
      AUTHENTIK_SECRET_KEY: ${AUTHENTIK_SECRET_KEY}
      AUTHENTIK_REDIS__HOST: redis
      AUTHENTIK_POSTGRESQL__HOST: postgres
      AUTHENTIK_POSTGRESQL__USER: ${POSTGRES_USER}
      AUTHENTIK_POSTGRESQL__PASSWORD: ${POSTGRES_PASSWORD}
      AUTHENTIK_POSTGRESQL__NAME: authentik
    networks:
      - cronbot-network

  # ═══════════════════════════════════════════════════════════════
  # CONTAINER MANAGEMENT
  # ═══════════════════════════════════════════════════════════════
  komodo:
    image: ghcr.io/moghtech/komodo:latest
    environment:
      KOMODO_DATABASE_URL: postgres://${POSTGRES_USER}:${POSTGRES_PASSWORD}@postgres:5432/komodo
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    networks:
      - cronbot-network
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.komodo.rule=Host(`komodo.${TRAEFIK_ID}.traefik.me`)"
      - "traefik.http.routers.komodo.tls=true"
      - "traefik.http.routers.komodo.tls.certresolver=traefikme"
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]

  # ═══════════════════════════════════════════════════════════════
  # GIT
  # ═══════════════════════════════════════════════════════════════
  gitea:
    image: gitea/gitea:latest
    environment:
      GITEA__database__DB_TYPE: postgres
      GITEA__database__HOST: postgres:5432
      GITEA__database__NAME: gitea
      GITEA__database__USER: ${POSTGRES_USER}
      GITEA__database__PASSWD: ${POSTGRES_PASSWORD}
      GITEA__server__ROOT_URL: https://git.${TRAEFIK_ID}.traefik.me
      GITEA__server__DOMAIN: git.${TRAEFIK_ID}.traefik.me
      GITEA__security__SECRET_KEY: ${GITEA__security__SECRET_KEY}
      GITEA__openid__ENABLE_OPENID_SIGNIN: "true"
      GITEA__openid__ENABLE_OPENID_SIGNUP: "false"
    volumes:
      - gitea-data:/var/lib/gitea
      - gitea-config:/etc/gitea
    networks:
      - cronbot-network
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.gitea.rule=Host(`git.${TRAEFIK_ID}.traefik.me`)"
      - "traefik.http.routers.gitea.tls=true"
      - "traefik.http.routers.gitea.tls.certresolver=traefikme"
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:3000/healthcheck"]

  # ═══════════════════════════════════════════════════════════════
  # SEARCH
  # ═══════════════════════════════════════════════════════════════
  searxng:
    image: searxng/searxng:latest
    environment:
      SEARXNG_BASE_URL: https://search.${TRAEFIK_ID}.traefik.me/
      SEARXNG_SECRET: ${SEARXNG_SECRET}
    volumes:
      - searxng-config:/etc/searxng:rw
    networks:
      - cronbot-network
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.searxng.rule=Host(`search.${TRAEFIK_ID}.traefik.me`)"
      - "traefik.http.routers.searxng.tls=true"
      - "traefik.http.routers.searxng.tls.certresolver=traefikme"
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:8080/healthz"]

  # ═══════════════════════════════════════════════════════════════
  # DATA STORES
  # ═══════════════════════════════════════════════════════════════
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: cronbot
    volumes:
      - postgres-data:/var/lib/postgresql/data
    networks:
      - cronbot-network
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
    command: >
      postgres
      -c max_connections=200
      -c shared_buffers=256MB

  redis:
    image: redis:7-alpine
    volumes:
      - redis-data:/data
    networks:
      - cronbot-network
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "redis-cli", "ping"]

  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: ${MINIO_ROOT_USER}
      MINIO_ROOT_PASSWORD: ${MINIO_ROOT_PASSWORD}
    volumes:
      - minio-data:/data
    networks:
      - cronbot-network
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.minio.rule=Host(`storage.${TRAEFIK_ID}.traefik.me`)"
      - "traefik.http.routers.minio.tls=true"
      - "traefik.http.routers.minio.tls.certresolver=traefikme"
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]

  rabbitmq:
    image: rabbitmq:3-management-alpine
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
    networks:
      - cronbot-network
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.rabbitmq.rule=Host(`mq.${TRAEFIK_ID}.traefik.me`)"
      - "traefik.http.routers.rabbitmq.tls=true"
      - "traefik.http.routers.rabbitmq.tls.certresolver=traefikme"
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "rabbitmqctl", "status"]

  # ═══════════════════════════════════════════════════════════════
  # CRONBOT SERVICES
  # ═══════════════════════════════════════════════════════════════
  cronbot-api:
    build:
      context: ./src/CronBot.Api
      dockerfile: Dockerfile
    environment:
      <<: *common-env
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: Host=postgres;Database=cronbot;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      Redis__Connection: redis:6379
      RabbitMQ__Host: rabbitmq
      RabbitMQ__Username: ${RABBITMQ_USER}
      RabbitMQ__Password: ${RABBITMQ_PASSWORD}
      Authentik__Authority: https://auth.${TRAEFIK_ID}.traefik.me
      Komodo__Url: http://komodo:8080
      Gitea__Url: http://gitea:3000
      MinIO__Endpoint: minio:9000
      MinIO__AccessKey: ${MINIO_ROOT_USER}
      MinIO__SecretKey: ${MINIO_ROOT_PASSWORD}
      SearXNG__Url: http://searxng:8080
      Jwt__Secret: ${CRONBOT_JWT_SECRET}
      Encryption__Key: ${CRONBOT_ENCRYPTION_KEY}
      Anthropic__ApiKey: ${ANTHROPIC_API_KEY}
      Telegram__BotToken: ${TELEGRAM_BOT_TOKEN}
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    networks:
      - cronbot-network
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.cronbot-api.rule=Host(`api.${TRAEFIK_ID}.traefik.me`)"
      - "traefik.http.routers.cronbot-api.tls=true"
      - "traefik.http.routers.cronbot-api.tls.certresolver=traefikme"
      - "traefik.http.services.cronbot-api.loadbalancer.server.port=8080"
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]

  cronbot-web:
    build:
      context: ./src/CronBot.Web
      dockerfile: Dockerfile
    environment:
      <<: *common-env
      NEXT_PUBLIC_API_URL: https://api.${TRAEFIK_ID}.traefik.me
      NEXT_PUBLIC_AUTH_URL: https://auth.${TRAEFIK_ID}.traefik.me
    depends_on:
      - cronbot-api
      - authentik-server
    networks:
      - cronbot-network
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.cronbot-web.rule=Host(`${TRAEFIK_ID}.traefik.me`,`www.${TRAEFIK_ID}.traefik.me`)"
      - "traefik.http.routers.cronbot-web.tls=true"
      - "traefik.http.routers.cronbot-web.tls.certresolver=traefikme"
      - "traefik.http.services.cronbot-web.loadbalancer.server.port=3000"
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:3000/health"]

  cronbot-telegram:
    build:
      context: ./src/CronBot.Telegram
      dockerfile: Dockerfile
    environment:
      <<: *common-env
      ConnectionStrings__DefaultConnection: Host=postgres;Database=cronbot;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      Telegram__BotToken: ${TELEGRAM_BOT_TOKEN}
      Api__Url: http://cronbot-api:8080
    depends_on:
      - cronbot-api
    networks:
      - cronbot-network

  # ═══════════════════════════════════════════════════════════════
  # MCP SERVICES
  # ═══════════════════════════════════════════════════════════════
  mcp-filesystem:
    build:
      context: ./src/McpTools/McpFilesystem
      dockerfile: Dockerfile
    environment:
      WORKSPACE_BASE: /workspaces
    volumes:
      - project-workspaces:/workspaces
    networks:
      - cronbot-network
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]

  mcp-git:
    build:
      context: ./src/McpTools/McpGit
      dockerfile: Dockerfile
    environment:
      GITEA_URL: http://gitea:3000
    volumes:
      - project-workspaces:/workspaces
    networks:
      - cronbot-network
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]

  mcp-kanban:
    build:
      context: ./src/McpTools/McpKanban
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: Host=postgres;Database=cronbot;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
    networks:
      - cronbot-network
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]

  mcp-runner:
    build:
      context: ./src/McpTools/McpRunner
      dockerfile: Dockerfile
    environment:
      DOCKER_HOST: unix:///var/run/docker.sock
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - project-workspaces:/workspaces
    networks:
      - cronbot-network
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]

  mcp-search:
    build:
      context: ./src/McpTools/McpSearch
      dockerfile: Dockerfile
    environment:
      SEARXNG_URL: http://searxng:8080
    networks:
      - cronbot-network
    healthcheck:
      <<: *default-healthcheck
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]

networks:
  cronbot-network:
    driver: bridge

volumes:
  traefik-letsencrypt:
  postgres-data:
  redis-data:
  minio-data:
  rabbitmq-data:
  gitea-data:
  gitea-config:
  searxng-config:
  project-workspaces:
```

## Deployment Steps

### 1. Prepare Environment

```bash
# Clone repository
git clone https://github.com/yourorg/cronbot.git
cd cronbot

# Copy and configure environment
cp .env.example .env
nano .env  # Edit with your values

# Generate secrets
openssl rand -base64 60  # AUTHENTIK_SECRET_KEY
openssl rand -hex 32     # Various secrets
```

### 2. Deploy with Komodo

```bash
# Initialize databases
docker compose up -d postgres redis

# Wait for databases to be ready
sleep 10

# Create additional databases
docker compose exec postgres psql -U cronbot -c "CREATE DATABASE authentik;"
docker compose exec postgres psql -U cronbot -c "CREATE DATABASE gitea;"
docker compose exec postgres psql -U cronbot -c "CREATE DATABASE komodo;"

# Deploy all services
docker compose up -d

# Check status
docker compose ps
```

### 3. Initial Configuration

```bash
# Access Authentik
# https://auth.{TRAEFIK_ID}.traefik.me
# Login with bootstrap credentials from .env

# Create OIDC provider for CronBot
# In Authentik admin:
# 1. Applications → Providers → Create → OAuth2/OpenID Provider
# 2. Applications → Applications → Create
# 3. Bind provider to application

# Access CronBot
# https://{TRAEFIK_ID}.traefik.me
# Should redirect to Authentik for login
```

### 4. Configure Telegram Bot

```bash
# Create bot with @BotFather on Telegram
# Get token and add to .env

# Set webhook
curl -X POST "https://api.telegram.org/bot{TOKEN}/setWebhook" \
  -d "url=https://api.{TRAEFIK_ID}.traefik.me/telegram/webhook"

# Restart telegram service
docker compose restart cronbot-telegram
```

## Post-Deployment

### Create Admin User

```bash
# Access Authentik
# Create user with admin group membership
# Or promote existing user
```

### Configure Gitea

```bash
# Access Gitea
# https://git.{TRAEFIK_ID}.traefik.me
# Complete initial setup
# Configure OIDC authentication
```

### Test Deployment

```bash
# Health check all services
curl https://api.{TRAEFIK_ID}.traefik.me/health
curl https://auth.{TRAEFIK_ID}.traefik.me/health
curl https://git.{TRAEFIK_ID}.traefik.me/healthcheck
curl https://search.{TRAEFIK_ID}.traefik.me/healthz

# Test Telegram
# Send /start to your bot
```

## URLs Reference

After deployment, services are available at:

| Service | URL |
|---------|-----|
| CronBot Web UI | `https://{TRAEFIK_ID}.traefik.me` |
| CronBot API | `https://api.{TRAEFIK_ID}.traefik.me` |
| Authentik | `https://auth.{TRAEFIK_ID}.traefik.me` |
| Gitea | `https://git.{TRAEFIK_ID}.traefik.me` |
| Komodo | `https://komodo.{TRAEFIK_ID}.traefik.me` |
| SearXNG | `https://search.{TRAEFIK_ID}.traefik.me` |
| MinIO | `https://storage.{TRAEFIK_ID}.traefik.me` |
| RabbitMQ | `https://mq.{TRAEFIK_ID}.traefik.me` |
| Traefik Dashboard | `https://traefik.{TRAEFIK_ID}.traefik.me` |
| Previews | `https://project-{id}-task-{id}.{TRAEFIK_ID}.traefik.me` |

## Backup & Recovery

### Backup Script

```bash
#!/bin/bash
# backup.sh

BACKUP_DIR="/backups/$(date +%Y%m%d_%H%M%S)"
mkdir -p $BACKUP_DIR

# Database backup
docker compose exec -T postgres pg_dumpall -U cronbot > $BACKUP_DIR/database.sql

# Volume backup
docker run --rm -v cronbot_gitea-data:/data -v $BACKUP_DIR:/backup alpine tar czf /backup/gitea-data.tar.gz /data
docker run --rm -v cronbot_minio-data:/data -v $BACKUP_DIR:/backup alpine tar czf /backup/minio-data.tar.gz /data

# Config backup
cp .env $BACKUP_DIR/
cp docker-compose.yml $BACKUP_DIR/

echo "Backup completed: $BACKUP_DIR"
```

### Recovery

```bash
# Restore database
cat /backups/.../database.sql | docker compose exec -T postgres psql -U cronbot

# Restore volumes
docker run --rm -v cronbot_gitea-data:/data -v /backups/...:/backup alpine tar xzf /backup/gitea-data.tar.gz -C /
```
