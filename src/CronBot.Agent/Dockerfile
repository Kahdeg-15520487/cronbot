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

# Runtime stage
FROM node:20-alpine

WORKDIR /app

# Install runtime dependencies only
COPY package.json ./
RUN npm install --omit=dev

# Copy built files
COPY --from=builder /app/dist ./dist

# Create directories for workspace and state
RUN mkdir -p /workspace /agent-state

# Set defaults
ENV WORKSPACE_PATH=/workspace
ENV STATE_PATH=/agent-state
ENV NODE_ENV=production

# Run the agent in daemon mode
ENTRYPOINT ["node", "dist/index.js"]
CMD ["daemon"]
