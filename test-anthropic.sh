#!/bin/bash
# Test Anthropic API connection
# Usage: ./test-anthropic.sh [api_key] [base_url]

set -e

# Load .env if exists
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
fi

API_KEY="${1:-${ANTHROPIC_API_KEY:-}}"
BASE_URL="${2:-${ANTHROPIC_BASE_URL:-https://api.anthropic.com}}"

if [ -z "$API_KEY" ]; then
    echo "Error: ANTHROPIC_API_KEY not set"
    echo "Usage: $0 [api_key] [base_url]"
    exit 1
fi

echo "Testing Anthropic API connection..."
echo "Base URL: ${BASE_URL}"
echo "API Key: ${API_KEY:0:10}..."
echo ""

# Test with curl
RESPONSE=$(curl -s -w "\n%{http_code}" "${BASE_URL}/v1/messages" \
  -H "Content-Type: application/json" \
  -H "x-api-key: ${API_KEY}" \
  -H "anthropic-version: 2023-06-01" \
  -d '{
    "model": "claude-sonnet-4-20250514",
    "max_tokens": 10,
    "messages": [{"role": "user", "content": "Say hi"}]
  }')

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n -1)

echo "HTTP Status: $HTTP_CODE"
echo ""

if [ "$HTTP_CODE" = "200" ]; then
    echo "✓ Connection successful!"
    echo "Response:"
    echo "$BODY" | head -c 500
else
    echo "✗ Connection failed!"
    echo "Response:"
    echo "$BODY"
    exit 1
fi
