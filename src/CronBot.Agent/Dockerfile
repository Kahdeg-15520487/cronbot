# CronBot Agent Container
# Node.js runtime with Python support for skills

FROM node:20-bookworm

# Install Python and build tools
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    python3-venv \
    git \
    curl \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Create symbolic links for python
RUN ln -sf /usr/bin/python3 /usr/bin/python

# Set working directory
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install Node.js dependencies
RUN npm install --only=production

# Copy TypeScript source
COPY tsconfig.json ./
COPY src ./src

# Build TypeScript
RUN npm run build

# Copy skills
COPY skills ./skills

# Create necessary directories
RUN mkdir -p /workspace /agent-state /skills

# Set environment variables
ENV NODE_ENV=production
ENV WORKSPACE_PATH=/workspace
ENV AGENT_STATE_PATH=/agent-state
ENV SKILLS_PATH=/app/skills

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:3000/health || exit 1

# Run the agent
CMD ["node", "dist/index.js"]
