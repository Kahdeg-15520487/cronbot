# MCP & Skills Architecture

## Overview

CronBot uses a two-tier extensibility system:
- **MCP (Model Context Protocol)**: External services providing tools, resources, and prompts
- **Skills**: Python scripts executed directly by agents

## MCP Architecture

### MCP Groups

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           MCP GROUPS                                         │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  SYSTEM MCP GROUP (Core)                                                    │
│  ════════════════════════                                                   │
│  Always available, managed by system                                        │
│                                                                             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐          │
│  │ filesystem  │ │    git      │ │   kanban    │ │  runner     │          │
│  │             │ │             │ │             │ │             │          │
│  │ read/write  │ │ branch/     │ │ create/     │ │ execute/    │          │
│  │ search      │ │ commit/pr   │ │ update/     │ │ test/       │          │
│  │             │ │             │ │ query       │ │ preview     │          │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘          │
│                                                                             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐          │
│  │ permission  │ │  memory     │ │ notification│ │  search     │          │
│  │             │ │             │ │             │ │             │          │
│  │ check/      │ │ store/      │ │ send/       │ │ web_search  │          │
│  │ grant       │ │ recall      │ │ subscribe   │ │ (SearXNG)   │          │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘          │
│                                                                             │
│  Container: mcp-system (always running)                                    │
│  Access: All agents                                                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  PROJECT MCP GROUP                                                          │
│  ═══════════════════                                                        │
│  Auto-provisioned per project                                               │
│                                                                             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                           │
│  │ project-    │ │  project-   │ │  project-   │                           │
│  │ workspace   │ │  database   │ │  cache      │                           │
│  │             │ │             │ │             │                           │
│  │ Project-    │ │ Project-    │ │ Project-    │                           │
│  │ specific    │ │ specific    │ │ specific    │                           │
│  │ files       │ │ queries     │ │ operations  │                           │
│  └─────────────┘ └─────────────┘ └─────────────┘                           │
│                                                                             │
│  Container: mcp-project-{id}                                               │
│  Access: Agents assigned to this project                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  CUSTOM MCP GROUP (User-Created)                                            │
│  ══════════════════════════════                                             │
│  Created by agents or users                                                 │
│                                                                             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                           │
│  │ custom-jira │ │ custom-     │ │ custom-     │                           │
│  │             │ │ slack       │ │ analytics   │                           │
│  │             │ │             │ │             │                           │
│  │ Created by  │ │ Created by  │ │ Created by  │                           │
│  │ agent for   │ │ agent for   │ │ agent for   │                           │
│  │ team A      │ │ team B      │ │ team A      │                           │
│  └─────────────┘ └─────────────┘ └─────────────┘                           │
│                                                                             │
│  Container: mcp-custom-{team-id}                                           │
│  Access: Team members (configurable)                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│  EXTERNAL MCP GROUP (Third-Party)                                           │
│  ═══════════════════════════════                                            │
│  User-configured external MCPs                                              │
│                                                                             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                           │
│  │ github      │ │ filesystem  │ │ custom-     │                           │
│  │ (official)  │ │ (official)  │ │ external    │                           │
│  │             │ │             │ │             │                           │
│  │ GitHub API  │ │ Local files │ │ Any MCP     │                           │
│  │ integration │ │             │ │ server      │                           │
│  └─────────────┘ └─────────────┘ └─────────────┘                           │
│                                                                             │
│  Container: External or mcp-external                                       │
│  Access: Configured per-project                                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### MCP Registry Service

The MCP Registry provides discovery and configuration for agents:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         MCP REGISTRY                                         │
└─────────────────────────────────────────────────────────────────────────────┘

Agent requests MCP config:
──────────────────────────

GET /api/v1/mcp/registry/project/{project-id}

Response:
{
  "mcps": [
    {
      "name": "filesystem",
      "transport": "http",
      "url": "http://mcp-filesystem:8080",
      "tools": ["read_file", "write_file", "list_dir", "search"],
      "resources": ["workspace://"]
    },
    {
      "name": "git",
      "transport": "http",
      "url": "http://mcp-git:8080",
      "tools": ["clone", "branch", "commit", "push", "pr"],
      "resources": ["git://repo"]
    },
    {
      "name": "kanban",
      "transport": "http",
      "url": "http://mcp-kanban:8080",
      "tools": ["create_task", "update_task", "list_tasks", "move_task"],
      "resources": ["kanban://board", "kanban://sprint"]
    },
    {
      "name": "runner",
      "transport": "http",
      "url": "http://mcp-runner:8080",
      "tools": ["execute", "test", "preview", "screenshot"]
    },
    {
      "name": "search",
      "transport": "http",
      "url": "http://mcp-search:8080",
      "tools": ["web_search", "code_search"]
    }
  ]
}
```

### System MCP Tools

#### Filesystem MCP

```csharp
[McpServerToolType]
public static class FileSystemTool
{
    [McpServerTool, Description("Read a file from the project workspace")]
    public static async Task<string> ReadFile(
        [Description("Path to the file relative to workspace")] string path,
        CancellationToken ct)
    {
        // Implementation
    }

    [McpServerTool, Description("Write content to a file in the workspace")]
    public static async Task<string> WriteFile(
        [Description("Path to the file relative to workspace")] string path,
        [Description("Content to write")] string content,
        CancellationToken ct)
    {
        // Implementation
    }

    [McpServerTool, Description("List files in a directory")]
    public static async Task<FileInfo[]> ListDir(
        [Description("Directory path relative to workspace")] string path,
        [Description("Include hidden files")] bool includeHidden = false,
        CancellationToken ct = default)
    {
        // Implementation
    }

    [McpServerTool, Description("Search for text in files")]
    public static async Task<SearchResult[]> Search(
        [Description("Search pattern (regex supported)")] string pattern,
        [Description("File glob pattern")] string glob = "**/*",
        [Description("Case insensitive")] bool ignoreCase = true,
        CancellationToken ct = default)
    {
        // Implementation
    }
}
```

#### Git MCP

```csharp
[McpServerToolType]
public static class GitTool
{
    [McpServerTool, Description("Create a new branch")]
    public static async Task<string> CreateBranch(
        [Description("Branch name")] string name,
        [Description("Base branch")] string from = "main",
        CancellationToken ct = default)
    {
        // Implementation
    }

    [McpServerTool, Description("Commit changes")]
    public static async Task<CommitResult> Commit(
        [Description("Commit message")] string message,
        [Description("Files to commit")] string[] files,
        CancellationToken ct = default)
    {
        // Implementation - triggers loop detection
    }

    [McpServerTool, Description("Push branch to remote")]
    public static async Task<string> Push(
        [Description("Branch name")] string branch,
        CancellationToken ct = default)
    {
        // Implementation
    }

    [McpServerTool, Description("Create pull request")]
    public static async Task<PrResult> CreatePr(
        [Description("PR title")] string title,
        [Description("PR description")] string description,
        [Description("Source branch")] string source,
        [Description("Target branch")] string target = "main",
        CancellationToken ct = default)
    {
        // Implementation
    }
}
```

#### Search MCP (SearXNG)

```csharp
[McpServerToolType]
public static class SearchTool
{
    [McpServerTool, Description("Search the web using SearXNG")]
    public static async Task<SearchResult> WebSearch(
        [Description("Search query")] string query,
        [Description("Number of results (default 10)")] int count = 10,
        [Description("Search engines to use")] string[]? engines = null,
        CancellationToken ct = default)
    {
        var baseUrl = Environment.GetEnvironmentVariable("SEARXNG_URL")
            ?? "http://searxng:8888";

        var url = $"{baseUrl}/search?" +
            $"q={Uri.EscapeDataString(query)}" +
            $"&format=json" +
            $"&pageno=1";

        if (engines != null && engines.Length > 0)
        {
            url += $"&engines={string.Join(",", engines)}";
        }

        // Implementation
    }

    [McpServerTool, Description("Search for code examples")]
    public static async Task<SearchResult> CodeSearch(
        [Description("Search query")] string query,
        [Description("Programming language")] string? language = null,
        CancellationToken ct = default)
    {
        // Implementation with code-specific filtering
    }
}
```

#### Runner MCP

```csharp
[McpServerToolType]
public static class RunnerTool
{
    [McpServerTool, Description("Execute commands in container")]
    public static async Task<ExecuteResult> Execute(
        [Description("Docker/compose definition")] string definition,
        [Description("Commands to run")] string[] commands,
        [Description("Working directory")] string? workDir = null,
        [Description("Environment variables")] Dictionary<string, string>? env = null,
        [Description("Timeout in seconds")] int timeout = 300,
        CancellationToken ct = default)
    {
        // Implementation
    }

    [McpServerTool, Description("Run tests")]
    public static async Task<TestResult> Test(
        [Description("Test framework")] string framework,
        [Description("Files to test")] string[]? files = null,
        [Description("Include coverage")] bool coverage = false,
        CancellationToken ct = default)
    {
        // Implementation
    }

    [McpServerTool, Description("Create preview environment")]
    public static async Task<PreviewResult> CreatePreview(
        [Description("Docker compose definition")] string compose,
        [Description("Service to expose")] string serviceName,
        [Description("Port number")] int port,
        [Description("TTL in seconds")] int ttlSeconds = 3600,
        CancellationToken ct = default)
    {
        // Implementation
    }

    [McpServerTool, Description("Capture screenshot")]
    public static async Task<ScreenshotResult> CaptureScreenshot(
        [Description("Preview ID")] string previewId,
        [Description("Viewport size")] Viewport? viewport = null,
        [Description("Wait for selector or milliseconds")] string? waitFor = null,
        CancellationToken ct = default)
    {
        // Implementation with Puppeteer
    }
}
```

### Custom MCP Creation Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CUSTOM MCP CREATION                                       │
└─────────────────────────────────────────────────────────────────────────────┘

User Request:
"Create an MCP tool that integrates with Jira"

        │
        ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Agent Plans:                                                                 │
│                                                                             │
│ 1. Research Jira API documentation                                         │
│ 2. Design MCP tool interface                                               │
│ 3. Implement MCP server in C#                                              │
│ 4. Write tests                                                              │
│ 5. Containerize                                                              │
│ 6. Deploy to custom MCP group                                               │
│ 7. Register with MCP registry                                               │
│ 8. Document usage                                                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Agent Executes:                                                              │
│                                                                             │
│ Task 1: Research                                                            │
│ - Use search MCP to fetch Jira API docs                                    │
│ - Identify required endpoints                                               │
│ - Note authentication requirements                                         │
│                                                                             │
│ Task 2: Design                                                              │
│ - Define MCP tools:                                                         │
│   - jira_search_issues                                                      │
│   - jira_create_issue                                                       │
│   - jira_update_issue                                                       │
│   - jira_get_issue                                                          │
│ - Define MCP resources:                                                     │
│   - jira://boards/{boardId}                                                 │
│   - jira://issues/{issueKey}                                                │
│                                                                             │
│ Task 3-4: Implement & Test                                                  │
│ - Create project: /mcp-tools/jira-mcp                                       │
│ - Implement MCP server using C# SDK                                        │
│ - Write unit tests                                                          │
│ - Run verification via runner MCP                                          │
│                                                                             │
│ Task 5: Containerize                                                        │
│ - Create Dockerfile                                                         │
│ - Build image                                                               │
│ - Push to internal registry                                                 │
│                                                                             │
│ Task 6-8: Deploy & Register                                                 │
│ - Deploy to mcp-custom-{team-id} container group                           │
│ - Register in MCP Registry                                                  │
│ - Create documentation                                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
        │
        ▼
User configures MCP (provides Jira credentials via dashboard)
MCP is now available to all agents in the team
```

## Skills Architecture

### What are Skills?

Skills are Python scripts that agents can execute directly without MCP overhead. They run in the agent's context with access to the same workspace.

### Skill Storage

```
/skills/
├── system/                       # System skills (read-only for agents)
│   ├── code_analyzer.py
│   ├── doc_generator.py
│   ├── test_generator.py
│   ├── refactor_suggest.py
│   └── security_scan.py
│
├── team-{team-id}/               # Team skills
│   ├── deploy_aws.py
│   └── report_generator.py
│
└── project-{project-id}/         # Project skills
    ├── migrate_db.py
    └── seed_data.py
```

### Skill Structure

```python
#!/usr/bin/env python3
"""
Skill: Code Analyzer
Description: Analyzes code quality and suggests improvements
Version: 1.0.0
Author: System
"""

from typing import List, Optional
from dataclasses import dataclass

# Skill metadata (required)
SKILL_META = {
    "name": "code_analyzer",
    "version": "1.0.0",
    "description": "Analyzes code quality, patterns, and suggests improvements",
    "author": "system",
    "tags": ["code", "quality", "analysis"],
    "inputs": {
        "files": {
            "type": "list[string]",
            "required": True,
            "description": "List of file paths to analyze"
        },
        "checks": {
            "type": "list[string]",
            "required": False,
            "default": ["complexity", "patterns", "security"],
            "description": "Types of checks to run"
        },
        "severity": {
            "type": "string",
            "required": False,
            "default": "warning",
            "enum": ["info", "warning", "error"]
        }
    },
    "outputs": {
        "issues": "list of found issues",
        "metrics": "code metrics",
        "suggestions": "improvement suggestions"
    }
}

@dataclass
class Issue:
    file: str
    line: int
    severity: str
    message: str
    suggestion: str

def run(
    files: List[str],
    checks: Optional[List[str]] = None,
    severity: str = "warning"
) -> dict:
    """
    Main skill entry point.

    Args:
        files: List of file paths to analyze
        checks: Types of checks to run
        severity: Minimum severity level to report

    Returns:
        Analysis results with issues, metrics, and suggestions
    """
    checks = checks or ["complexity", "patterns", "security"]

    issues = []
    metrics = {}
    suggestions = []

    for file_path in files:
        with open(file_path, 'r') as f:
            content = f.read()

        if "complexity" in checks:
            issues.extend(_check_complexity(file_path, content))
        if "patterns" in checks:
            issues.extend(_check_patterns(file_path, content))
        if "security" in checks:
            issues.extend(_check_security(file_path, content))

        metrics[file_path] = _calculate_metrics(content)

    # Filter by severity
    severity_levels = {"info": 0, "warning": 1, "error": 2}
    issues = [
        i for i in issues
        if severity_levels[i.severity] >= severity_levels[severity]
    ]

    suggestions = _generate_suggestions(issues)

    return {
        "issues": [i.__dict__ for i in issues],
        "metrics": metrics,
        "suggestions": suggestions
    }

def _check_complexity(file_path: str, content: str) -> List[Issue]:
    # Implementation
    pass

def _check_patterns(file_path: str, content: str) -> List[Issue]:
    # Implementation
    pass

def _check_security(file_path: str, content: str) -> List[Issue]:
    # Implementation
    pass

def _calculate_metrics(content: str) -> dict:
    # Implementation
    pass

def _generate_suggestions(issues: List[Issue]) -> List[str]:
    # Implementation
    pass
```

### Agent Skill Execution

```typescript
// Agent executes skills directly

interface SkillExecutor {
  execute(name: string, inputs: Record<string, any>): Promise<SkillResult>;
  list(scope: 'system' | 'team' | 'project'): Promise<Skill[]>;
  getMeta(name: string): Promise<SkillMeta>;
}

class PythonSkillExecutor implements SkillExecutor {
  async execute(name: string, inputs: Record<string, any>): Promise<SkillResult> {
    const skillPath = this.findSkill(name);

    // Write inputs to temp file
    const inputFile = `/tmp/skill_input_${Date.now()}.json`;
    await fs.writeFile(inputFile, JSON.stringify(inputs));

    // Execute skill
    const result = await exec('python3', [
      skillPath,
      '--input', inputFile
    ]);

    // Clean up
    await fs.unlink(inputFile);

    return JSON.parse(result.stdout);
  }

  private findSkill(name: string): string {
    // Search order: project -> team -> system
    const searchPaths = [
      `/skills/project-${this.projectId}/${name}.py`,
      `/skills/team-${this.teamId}/${name}.py`,
      `/skills/system/${name}.py`
    ];

    for (const path of searchPaths) {
      if (fs.existsSync(path)) {
        return path;
      }
    }

    throw new Error(`Skill not found: ${name}`);
  }
}

// Usage in agent:
const skillExecutor = new PythonSkillExecutor(projectId, teamId);

const result = await skillExecutor.execute('code_analyzer', {
  files: ['src/auth/login.ts', 'src/auth/middleware.ts'],
  checks: ['complexity', 'security'],
  severity: 'warning'
});

console.log(result.issues);
console.log(result.suggestions);
```

## MCP vs Skills Comparison

| Aspect | MCP | Skills |
|--------|-----|--------|
| **Execution** | Network call to service | Direct Python execution |
| **Overhead** | Higher (serialization, network) | Lower (direct call) |
| **Isolation** | Separate container | Agent context |
| **State** | Can maintain own state | Stateless (workspace access) |
| **Languages** | Any (via HTTP/gRPC) | Python only |
| **Use Case** | External services, persistent state | Code analysis, utilities |
| **Configuration** | Per-project in WebUI | Always available in scope |
| **Discovery** | Via registry | File system scan |

## When to Use What

### Use MCP When:
- Need persistent state (database connections, caches)
- Accessing external services (APIs, databases)
- Need separate credentials management
- Tool needs its own container isolation
- Multiple agents share the same connection

### Use Skills When:
- Quick file/workspace operations
- Code analysis and transformation
- Data processing within workspace
- No external dependencies
- Need low-latency execution
