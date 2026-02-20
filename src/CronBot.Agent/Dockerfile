# Build stage
FROM node:20-alpine AS builder

WORKDIR /app

# Copy package files
COPY package.json ./
RUN npm install

# Copy source and build
COPY tsconfig.json ./
COPY src ./src
RUN npm run build

# Runtime stage - use Debian-based image for Claude CLI compatibility
FROM node:20-slim

WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    git \
    && rm -rf /var/lib/apt/lists/*

# Install Claude CLI globally
RUN npm install -g @anthropic-ai/claude-code

# Create symlink for claude-code binary (SDK may look for this)
RUN ln -s /usr/local/bin/claude /usr/local/bin/claude-code

# Create non-root user for security (required for bypassPermissions)
# The node image already has a 'node' user, use that
RUN chown -R node:node /app

# Install runtime dependencies only
COPY --chown=node:node package.json ./
RUN npm install --omit=dev

# Copy built files
COPY --from=builder --chown=node:node /app/dist ./dist

# Create directories for workspace and state
RUN mkdir -p /workspace /agent-state && \
    chown -R node:node /workspace /agent-state

# Set defaults
ENV WORKSPACE_PATH=/workspace
ENV STATE_PATH=/agent-state
ENV NODE_ENV=production

# Switch to non-root user
USER node

# Run the agent in daemon mode
ENTRYPOINT ["node", "dist/index.js"]
CMD ["daemon"]
