#!/bin/bash
# Test script for CronBot Agent

set -e

echo "=== CronBot Agent Test Setup ==="

# Check Node.js
if ! command -v node &> /dev/null; then
    echo "Error: Node.js is required"
    exit 1
fi

echo "Node.js version: $(node --version)"

# Check Python
if ! command -v python3 &> /dev/null; then
    echo "Warning: Python3 not found, skills will not work"
fi

# Navigate to agent directory
cd "$(dirname "$0")"

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    npm install
fi

# Create test .env if not exists
if [ ! -f ".env" ]; then
    echo "Creating test .env file..."
    cat > .env << EOF
# Test Configuration
PROJECT_ID=test-project-001
AGENT_ID=test-agent-001
AUTONOMY_LEVEL=2

# API Configuration (set your API key)
ANTHROPIC_API_KEY=${ANTHROPIC_API_KEY:-your-api-key-here}
# ANTHROPIC_BASE_URL=https://api.anthropic.com

# Service URLs (mock for testing)
MCP_REGISTRY_URL=http://localhost:5000/api/mcp/registry
KANBAN_URL=http://localhost:5000/api
GIT_URL=http://localhost:3000

# Paths
WORKSPACE_PATH=$(pwd)/test-workspace
SKILLS_PATH=$(pwd)/skills
AGENT_STATE_PATH=$(pwd)/test-state

# Logging
LOG_LEVEL=debug
MAX_TOKENS=200000
EOF
    echo "Created .env file - edit ANTHROPIC_API_KEY before running"
fi

# Create test directories
mkdir -p test-workspace test-state

# Build TypeScript
echo "Building TypeScript..."
npm run build

echo ""
echo "=== Setup Complete ==="
echo ""
echo "To run the agent:"
echo "  1. Edit .env and set your ANTHROPIC_API_KEY"
echo "  2. Run: npm run dev"
echo ""
echo "Or run directly:"
echo "  node dist/index.js"
