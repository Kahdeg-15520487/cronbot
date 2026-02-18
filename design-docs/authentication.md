# Authentication

## Overview

CronBot uses **Authentik** as its identity provider, supporting OAuth2, OIDC, and integration with Telegram for chat-based authentication.

## Authentik Configuration

### Applications

Authentik manages authentication for all CronBot components:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         AUTHENTIK APPLICATIONS                               │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐              │
│  │ CronBot Web UI  │ │     Gitea       │ │    Komodo       │              │
│  │                 │ │                 │ │                 │              │
│  │ Type: OIDC      │ │ Type: OIDC      │ │ Type: OIDC      │              │
│  │ Provider:       │ │ Provider:       │ │ Provider:       │              │
│  │ OAuth2/OIDC     │ │ OAuth2/OIDC     │ │ OAuth2/OIDC     │              │
│  │                 │ │                 │ │                 │              │
│  │ Redirect:       │ │ Redirect:       │ │ Redirect:       │              │
│  │ /auth/callback  │ │ /user/oauth2/   │ │ /auth/callback  │              │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘              │
│                                                                             │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐              │
│  │ CronBot API     │ │    Telegram     │ │   Dashboard     │              │
│  │                 │ │                 │ │                 │              │
│  │ Type: API Token │ │ Type: Custom    │ │ Type: OIDC      │              │
│  │                 │ │ (link flow)     │ │                 │              │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Authentik Setup

```yaml
# docker-compose.core.yaml (excerpt)

authentik-server:
  image: ghcr.io/goauthentik/server:2024.10
  environment:
    AUTHENTIK_SECRET_KEY: ${AUTHENTIK_SECRET_KEY}
    AUTHENTIK_REDIS__HOST: redis
    AUTHENTIK_POSTGRESQL__HOST: postgres
    AUTHENTIK_POSTGRESQL__USER: ${POSTGRES_USER}
    AUTHENTIK_POSTGRESQL__PASSWORD: ${POSTGRES_PASSWORD}
    AUTHENTIK_POSTGRESQL__NAME: authentik
  ports:
    - "9000:9000"
    - "9443:9443"
  networks:
    - cronbot-core
  labels:
    - "traefik.enable=true"
    - "traefik.http.routers.authentik.rule=Host(`auth.${TRAEFIK_ID}.traefik.me`)"
    - "traefik.http.routers.authentik.tls=true"

authentik-worker:
  image: ghcr.io/goauthentik/server:2024.10
  command: worker
  environment:
    # Same as server
```

## Authentication Flows

### Web UI Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         WEB UI AUTHENTICATION                                 │
└─────────────────────────────────────────────────────────────────────────────┘

User                Web UI              Authentik            CronBot API
  │                    │                     │                     │
  │  1. Visit URL      │                     │                     │
  │───────────────────►│                     │                     │
  │                    │                     │                     │
  │                    │  2. Check session   │                     │
  │                    │  (no valid session) │                     │
  │                    │                     │                     │
  │  3. Redirect to    │                     │                     │
  │     Authentik      │                     │                     │
  │◄───────────────────│                     │                     │
  │                    │                     │                     │
  │  4. Login page     │                     │                     │
  │────────────────────────────────────────►│                     │
  │                    │                     │                     │
  │  5. Credentials    │                     │                     │
  │────────────────────────────────────────►│                     │
  │                    │                     │                     │
  │                    │                     │  6. Verify          │
  │                    │                     │     credentials     │
  │                    │                     │                     │
  │  7. MFA (if        │                     │                     │
  │     configured)    │                     │                     │
  │◄───────────────────────────────────────│                     │
  │                    │                     │                     │
  │  8. MFA code       │                     │                     │
  │────────────────────────────────────────►│                     │
  │                    │                     │                     │
  │  9. Authorization  │                     │                     │
  │     code           │                     │                     │
  │◄───────────────────────────────────────│                     │
  │                    │                     │                     │
  │  10. Callback with │                     │                     │
  │      auth code     │                     │                     │
  │───────────────────►│                     │                     │
  │                    │                     │                     │
  │                    │  11. Exchange code  │                     │
  │                    │      for tokens     │                     │
  │                    │────────────────────►│                     │
  │                    │                     │                     │
  │                    │  12. Access token   │                     │
  │                    │      Refresh token  │                     │
  │                    │◄────────────────────│                     │
  │                    │                     │                     │
  │                    │  13. Get user info  │                     │
  │                    │────────────────────►│                     │
  │                    │                     │                     │
  │                    │  14. User profile   │                     │
  │                    │◄────────────────────│                     │
  │                    │                     │                     │
  │                    │  15. Sync user to   │                     │
  │                    │      CronBot DB     │                     │
  │                    │─────────────────────────────────────────►│
  │                    │                     │                     │
  │                    │  16. Session        │                     │
  │                    │      created        │                     │
  │                    │◄────────────────────────────────────────│
  │                    │                     │                     │
  │  17. Dashboard     │                     │                     │
  │◄───────────────────│                     │                     │
  │                    │                     │                     │
```

### Telegram Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         TELEGRAM AUTHENTICATION                               │
└─────────────────────────────────────────────────────────────────────────────┘

User                Telegram Bot        Authentik            CronBot DB
  │                    │                     │                     │
  │  1. /start         │                     │                     │
  │───────────────────►│                     │                     │
  │                    │                     │                     │
  │                    │  2. Check if        │                     │
  │                    │     Telegram ID     │                     │
  │                    │     is linked       │                     │
  │                    │─────────────────────────────────────────►│
  │                    │                     │                     │
  │                    │  3. Not linked      │                     │
  │                    │◄────────────────────────────────────────│
  │                    │                     │                     │
  │  4. "Please link   │                     │                     │
  │     your account"  │                     │                     │
  │     + URL          │                     │                     │
  │◄───────────────────│                     │                     │
  │                    │                     │                     │
  │  5. Click link     │                     │                     │
  │────────────────────────────────────────►│                     │
  │                    │                     │                     │
  │  6. Login to       │                     │                     │
  │     Authentik      │                     │                     │
  │◄──────────────────►│                     │                     │
  │                    │                     │                     │
  │  7. Authorize      │                     │                     │
  │     Telegram link  │                     │                     │
  │────────────────────────────────────────►│                     │
  │                    │                     │                     │
  │                    │                     │  8. Store link      │
  │                    │                     │     in DB           │
  │                    │                     │────────────────────►│
  │                    │                     │                     │
  │                    │  9. Notify success  │                     │
  │                    │◄────────────────────│                     │
  │                    │                     │                     │
  │  10. "Account      │                     │                     │
  │      linked!"      │                     │                     │
  │◄───────────────────│                     │                     │
  │                    │                     │                     │
  │  11. /start again  │                     │                     │
  │───────────────────►│                     │                     │
  │                    │                     │                     │
  │                    │  12. Telegram ID    │                     │
  │                    │      found          │                     │
  │                    │─────────────────────────────────────────►│
  │                    │                     │                     │
  │                    │  13. User data      │                     │
  │                    │◄────────────────────────────────────────│
  │                    │                     │                     │
  │  14. Session       │                     │                     │
  │      created,      │                     │                     │
  │      welcome msg   │                     │                     │
  │◄───────────────────│                     │                     │
  │                    │                     │                     │
```

### API Token Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         API TOKEN AUTHENTICATION                              │
└─────────────────────────────────────────────────────────────────────────────┘

User                Authentik UI        CronBot API
  │                    │                     │
  │  1. Create token   │                     │
  │     in Authentik   │                     │
  │───────────────────►│                     │
  │                    │                     │
  │  2. Token generated│                     │
  │◄───────────────────│                     │
  │                    │                     │
  │  3. API request    │                     │
  │     with token     │                     │
  │─────────────────────────────────────────►│
  │                    │                     │
  │                    │  4. Validate token  │
  │                    │◄────────────────────│
  │                    │                     │
  │                    │  5. Token valid     │
  │                    │────────────────────►│
  │                    │                     │
  │  6. Response       │                     │
  │◄─────────────────────────────────────────│
  │                    │                     │
```

## JWT Token Structure

```json
{
  "sub": "user-uuid",
  "iss": "https://auth.{traefik-id}.traefik.me",
  "aud": "cronbot",
  "exp": 1705312800,
  "iat": 1705309200,
  "preferred_username": "johndoe",
  "email": "john@example.com",
  "telegram_id": 123456789,
  "groups": ["team-uuid-1", "team-uuid-2"],
  "roles": ["admin"],
  "permissions": [
    "read:projects",
    "write:projects",
    "manage:teams"
  ]
}
```

## Session Management

### Session Storage

Sessions are stored in PostgreSQL and cached in Redis:

```sql
-- user_sessions table (see database-schema.md)
```

### Session Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SESSION LIFECYCLE                                     │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  Creation:                                                                  │
│  ──────────                                                                 │
│  - Web: After successful OIDC flow                                         │
│  - Telegram: After account link verification                               │
│  - API: Token validation creates session context                           │
│                                                                             │
│  Refresh:                                                                   │
│  ─────────                                                                  │
│  - Web: Refresh token flow with Authentik                                  │
│  - Telegram: Activity on any message                                       │
│  - API: Each validated request                                             │
│                                                                             │
│  Expiration:                                                                │
│  ───────────                                                                │
│  - Web: 7 days (configurable)                                              │
│  - Telegram: 30 days (configurable)                                        │
│  - API: Token expiration                                                   │
│                                                                             │
│  Termination:                                                               │
│  ───────────                                                                │
│  - User logout                                                              │
│  - Token revocation in Authentik                                           │
│  - Admin-initiated termination                                             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Permission System

### Permission Structure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PERMISSIONS                                           │
└─────────────────────────────────────────────────────────────────────────────┘

Permission format: {action}:{resource}

Actions:
- read    - View/access resource
- write   - Modify resource
- execute - Run operations
- manage  - Full control

Resources:
- files       - Project files
- tasks       - Kanban tasks
- sprints     - Sprint management
- agents      - Agent control
- mcps        - MCP configuration
- skills      - Skill management
- settings    - Project settings
- team        - Team management

Examples:
- read:files        - Read project files
- write:files       - Modify project files
- execute:commands  - Run commands in sandbox
- manage:agents     - Control agent lifecycle
```

### Role Presets

| Preset | Permissions |
|--------|-------------|
| **Reader** | `read:files`, `read:tasks`, `read:sprints` |
| **Contributor** | Reader + `write:files`, `write:tasks`, `execute:runner` |
| **Planner** | Contributor + `manage:sprints`, `read:agents` |
| **Executor** | Contributor + `execute:commands`, `read:mcps` |
| **Admin** | All permissions |

### Permission Resolution

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PERMISSION RESOLUTION                                 │
└─────────────────────────────────────────────────────────────────────────────┘

Permission check order:

1. Direct project permission
   └─► If found: Use this

2. Team role → permissions
   └─► If found: Use team role permissions

3. Default role
   └─► Use default (reader)

Example:
User is member of Team A
Team A has Project X
Project X has no direct member entry for User
User's team role: admin
Admin role has all permissions
→ User has all permissions for Project X
```

## Security Considerations

### Token Security

- JWTs signed with RS256
- Short-lived access tokens (15 min)
- Long-lived refresh tokens (7 days) in secure storage
- Token rotation on refresh

### Session Security

- HTTPS only (Traefik enforces)
- HttpOnly cookies for web sessions
- SameSite=Strict for CSRF protection
- Session ID rotation on privilege change

### Telegram Security

- Telegram ID binding requires Authentik login
- One Telegram ID per account
- Rate limiting on commands
- Suspicious activity detection

### Audit Logging

All authentication events are logged:

```sql
-- Audit events
INSERT INTO audit_logs (event_type, event_data, severity)
VALUES
('auth.login_success', '{"user_id": "...", "method": "oidc"}', 'info'),
('auth.login_failure', '{"username": "...", "reason": "invalid_password"}', 'warning'),
('auth.telegram_link', '{"user_id": "...", "telegram_id": 123}', 'info'),
('auth.session_created', '{"user_id": "...", "session_id": "..."}', 'info'),
('auth.session_terminated', '{"user_id": "...", "reason": "logout"}', 'info');
```
