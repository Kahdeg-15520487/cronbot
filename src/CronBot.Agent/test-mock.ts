/**
 * Mock API server for testing the agent without real services.
 * Run with: npx ts-node test-mock.ts
 */

import http from 'http';
import { randomUUID } from 'crypto';

const PORT = 5555;

// Mock data
const mockProject = {
  id: 'test-project-001',
  name: 'Test Project',
  slug: 'test-project',
};

const mockTasks = [
  {
    id: randomUUID(),
    projectId: mockProject.id,
    number: 1,
    title: 'Create a hello world program',
    description: 'Write a simple hello world program in Python',
    type: 'task',
    status: 'sprint',
    createdAt: new Date().toISOString(),
  },
];

let currentTaskIndex = 0;

const server = http.createServer((req, res) => {
  console.log(`[${new Date().toISOString()}] ${req.method} ${req.url}`);

  // CORS headers
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PATCH, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    res.writeHead(200);
    res.end();
    return;
  }

  // Route handling
  if (req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ status: 'healthy' }));
    return;
  }

  // Get next task
  if (req.url?.match(/\/projects\/.*\/tasks\/next/)) {
    res.writeHead(200, { 'Content-Type': 'application/json' });

    if (currentTaskIndex < mockTasks.length) {
      const task = mockTasks[currentTaskIndex];
      currentTaskIndex++;
      res.end(JSON.stringify(task));
    } else {
      res.end(JSON.stringify(null));
    }
    return;
  }

  // Update task status
  if (req.url?.match(/\/tasks\/.*/) && req.method === 'PATCH') {
    let body = '';
    req.on('data', chunk => body += chunk);
    req.on('end', () => {
      console.log('  Task update:', body);
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ success: true }));
    });
    return;
  }

  // Report blocker
  if (req.url?.match(/\/agents\/.*\/blockers/) && req.method === 'POST') {
    let body = '';
    req.on('data', chunk => body += chunk);
    req.on('end', () => {
      console.log('  Blocker reported:', body);
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ success: true }));
    });
    return;
  }

  // MCP Registry (empty for now)
  if (req.url?.includes('/mcp/registry')) {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ mcps: [] }));
    return;
  }

  // Default 404
  res.writeHead(404, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({ error: 'Not found' }));
});

server.listen(PORT, () => {
  console.log(`Mock API server running on http://localhost:${PORT}`);
  console.log('');
  console.log('Available endpoints:');
  console.log('  GET  /health');
  console.log('  GET  /projects/:id/tasks/next');
  console.log('  PATCH /tasks/:id');
  console.log('  POST /agents/:id/blockers');
  console.log('');
  console.log('Update your .env to use:');
  console.log(`  KANBAN_URL=http://localhost:${PORT}`);
  console.log('');
  console.log('Press Ctrl+C to stop');
});
