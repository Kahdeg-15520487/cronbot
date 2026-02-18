# API Contracts

## Overview

This document defines all API contracts for CronBot, including gRPC services for internal communication and REST endpoints for external access.

## gRPC Services (Internal)

### Orchestrator Service

```protobuf
syntax = "proto3";

package cronbot.orchestrator;

service OrchestratorService {
  // Project Pod Management
  rpc CreateProjectPod(CreateProjectPodRequest) returns (CreateProjectPodResponse);
  rpc DestroyProjectPod(DestroyProjectPodRequest) returns (DestroyProjectPodResponse);
  rpc GetPodStatus(GetPodStatusRequest) returns (GetPodStatusResponse);

  // Agent Management
  rpc ScaleAgents(ScaleAgentsRequest) returns (ScaleAgentsResponse);
  rpc GetAgentStatus(GetAgentStatusRequest) returns (GetAgentStatusResponse);
  rpc PauseAgent(PauseAgentRequest) returns (PauseAgentResponse);
  rpc ResumeAgent(ResumeAgentRequest) returns (ResumeAgentResponse);

  // Preview Management
  rpc CreatePreview(CreatePreviewRequest) returns (CreatePreviewResponse);
  rpc StopPreview(StopPreviewRequest) returns (StopPreviewResponse);
  rpc GetPreviewStatus(GetPreviewStatusRequest) returns (GetPreviewStatusResponse);
}

message CreateProjectPodRequest {
  string project_id = 1;
  ProjectPodConfig config = 2;
}

message ProjectPodConfig {
  string slug = 1;
  string agent_image = 2;
  string executor_image = 3;
  ResourceLimits resource_limits = 4;
  string sandbox_mode = 5;
}

message ResourceLimits {
  double agent_cpu = 1;
  int64 agent_memory_mb = 2;
  double executor_cpu = 3;
  int64 executor_memory_mb = 4;
}

message CreateProjectPodResponse {
  bool success = 1;
  string error = 2;
  string komodo_stack_id = 3;
  repeated ContainerInfo containers = 4;
}

message ContainerInfo {
  string id = 1;
  string name = 2;
  string status = 3;
  string image = 4;
}

message DestroyProjectPodRequest {
  string project_id = 1;
  bool preserve_volumes = 2;
}

message DestroyProjectPodResponse {
  bool success = 1;
  string error = 2;
}

message GetPodStatusRequest {
  string project_id = 1;
}

message GetPodStatusResponse {
  string status = 1;  // running, stopped, degraded
  repeated ContainerInfo containers = 2;
  repeated AgentInfo agents = 3;
}

message AgentInfo {
  string id = 1;
  string task_id = 2;
  string status = 3;
  double cpu_usage = 4;
  int64 memory_usage_mb = 5;
}

message ScaleAgentsRequest {
  string project_id = 1;
  int32 count = 2;
}

message ScaleAgentsResponse {
  bool success = 1;
  string error = 2;
  int32 current_count = 3;
}

message CreatePreviewRequest {
  string project_id = 1;
  string task_id = 2;
  string compose_definition = 3;
  string service_name = 4;
  int32 port = 5;
  int32 ttl_seconds = 6;
}

message CreatePreviewResponse {
  bool success = 1;
  string error = 2;
  string preview_id = 3;
  string url = 4;
  int64 expires_at = 5;
}
```

### Kanban Service

```protobuf
syntax = "proto3";

package cronbot.kanban;

service KanbanService {
  // Board & Sprint
  rpc GetBoard(GetBoardRequest) returns (GetBoardResponse);
  rpc CreateSprint(CreateSprintRequest) returns (CreateSprintResponse);
  rpc GetSprint(GetSprintRequest) returns (GetSprintResponse);
  rpc UpdateSprintStatus(UpdateSprintStatusRequest) returns (UpdateSprintStatusResponse);

  // Tasks
  rpc CreateTask(CreateTaskRequest) returns (CreateTaskResponse);
  rpc GetTask(GetTaskRequest) returns (GetTaskResponse);
  rpc UpdateTask(UpdateTaskRequest) returns (UpdateTaskResponse);
  rpc MoveTask(MoveTaskRequest) returns (MoveTaskResponse);
  rpc ListTasks(ListTasksRequest) returns (ListTasksResponse);
  rpc AssignTask(AssignTaskRequest) returns (AssignTaskResponse);

  // Comments
  rpc AddComment(AddCommentRequest) returns (AddCommentResponse);
  rpc ListComments(ListCommentsRequest) returns (ListCommentsResponse);
}

message CreateTaskRequest {
  string project_id = 1;
  string title = 2;
  string description = 3;
  string type = 4;  // task, bug, blocker, idea, epic
  int32 story_points = 5;
  string parent_task_id = 6;
}

message CreateTaskResponse {
  string task_id = 1;
  int64 task_number = 2;
}

message Task {
  string id = 1;
  int64 number = 2;
  string project_id = 3;
  string sprint_id = 4;
  string title = 5;
  string description = 6;
  string type = 7;
  string status = 8;
  int32 story_points = 9;
  string assignee_type = 10;
  string assignee_id = 11;
  string parent_task_id = 12;
  string git_branch = 13;
  string git_pr_url = 14;
  string blocked_by_task_id = 15;
  string blocked_reason = 16;
  int64 created_at = 17;
  int64 updated_at = 18;
  int64 started_at = 19;
  int64 completed_at = 20;
}

message MoveTaskRequest {
  string task_id = 1;
  string status = 2;
  string sprint_id = 3;
  int32 column_order = 4;
}

message ListTasksRequest {
  string project_id = 1;
  optional string status = 2;
  optional string type = 3;
  optional string assignee_id = 4;
  optional string sprint_id = 5;
  int32 limit = 6;
  int32 offset = 7;
}

message ListTasksResponse {
  repeated Task tasks = 1;
  int32 total = 2;
}
```

### Git Service

```protobuf
syntax = "proto3";

package cronbot.git;

service GitService {
  // Repository
  rpc CreateRepository(CreateRepositoryRequest) returns (CreateRepositoryResponse);
  rpc CloneExternal(CloneExternalRequest) returns (CloneExternalResponse);

  // Operations
  rpc CreateBranch(CreateBranchRequest) returns (CreateBranchResponse);
  rpc Commit(CommitRequest) returns (CommitResponse);
  rpc Push(PushRequest) returns (PushResponse);
  rpc CreatePullRequest(CreatePullRequestRequest) returns (CreatePullRequestResponse);
  rpc MergePullRequest(MergePullRequestRequest) returns (MergePullRequestResponse);

  // Mirror
  rpc ConfigureMirror(ConfigureMirrorRequest) returns (ConfigureMirrorResponse);
  rpc SyncMirror(SyncMirrorRequest) returns (SyncMirrorResponse);

  // Info
  rpc GetCommits(GetCommitsRequest) returns (GetCommitsResponse);
  rpc GetFileContent(GetFileContentRequest) returns (GetFileContentResponse);
}

message CommitRequest {
  string project_id = 1;
  string branch = 2;
  string message = 3;
  repeated string files = 4;
  string author_name = 5;
  string author_email = 6;
}

message CommitResponse {
  string commit_hash = 1;
  bool loop_detected = 2;
  LoopDetection loop_info = 3;
}

message LoopDetection {
  string file_path = 1;
  int32 line_number = 2;
  string pattern = 3;
  repeated string commits = 4;
}
```

### Runner Service

```protobuf
syntax = "proto3";

package cronbot.runner;

service RunnerService {
  rpc Execute(ExecuteRequest) returns (ExecuteResponse);
  rpc Test(TestRequest) returns (TestResponse);
  rpc CreatePreview(CreatePreviewRequest) returns (CreatePreviewResponse);
  rpc CaptureScreenshot(CaptureScreenshotRequest) returns (CaptureScreenshotResponse);
  rpc StopPreview(StopPreviewRequest) returns (StopPreviewResponse);
  rpc RunVerification(RunVerificationRequest) returns (RunVerificationResponse);
}

message ExecuteRequest {
  string project_id = 1;
  string definition = 2;  // JSON dockerfile/compose definition
  repeated string commands = 3;
  string work_dir = 4;
  map<string, string> env = 5;
  int32 timeout_seconds = 6;
}

message ExecuteResponse {
  string status = 1;  // success, failure, timeout
  int32 exit_code = 2;
  string stdout = 3;
  string stderr = 4;
  repeated Artifact artifacts = 5;
  int64 duration_ms = 6;
}

message Artifact {
  string name = 1;
  string path = 2;
  int64 size_bytes = 3;
}

message TestRequest {
  string project_id = 1;
  string framework = 2;  // jest, vitest, pytest, xunit, auto
  string command = 3;
  bool coverage = 4;
  repeated string files = 5;
}

message TestResponse {
  string status = 1;
  int32 passed = 2;
  int32 failed = 3;
  int32 skipped = 4;
  Coverage coverage = 5;
  repeated TestFailure failures = 6;
  int64 duration_ms = 7;
}

message Coverage {
  double lines = 1;
  double branches = 2;
  double functions = 3;
}

message TestFailure {
  string test_name = 1;
  string file = 2;
  string error = 3;
  string stack_trace = 4;
}

message CaptureScreenshotRequest {
  string preview_id = 1;
  Viewport viewport = 2;
  string wait_for = 3;  // selector or ms
  bool full_page = 4;
}

message Viewport {
  int32 width = 1;
  int32 height = 2;
}

message CaptureScreenshotResponse {
  bool success = 1;
  string image_base64 = 2;
  string image_url = 3;
  repeated ConsoleLog console = 4;
  repeated NetworkCall network = 5;
  repeated string errors = 6;
}

message ConsoleLog {
  string type = 1;
  string message = 2;
}

message NetworkCall {
  string url = 1;
  string method = 2;
  int32 status = 3;
}

message RunVerificationRequest {
  string preview_id = 1;
  string script = 2;  // Puppeteer verification script
  int32 timeout_seconds = 3;
}

message RunVerificationResponse {
  bool success = 1;
  string result = 2;  // JSON result from script
  string error = 3;
}
```

### Permission Service

```protobuf
syntax = "proto3";

package cronbot.permission;

service PermissionService {
  rpc CheckPermission(CheckPermissionRequest) returns (CheckPermissionResponse);
  rpc GetUserRoles(GetUserRolesRequest) returns (GetUserRolesResponse);
  rpc GrantPermission(GrantPermissionRequest) returns (GrantPermissionResponse);
  rpc RevokePermission(RevokePermissionRequest) returns (RevokePermissionResponse);
}

message CheckPermissionRequest {
  string user_id = 1;
  string resource_type = 2;  // project, task, team, mcp, skill
  string resource_id = 3;
  string permission = 4;  // read, write, execute, manage
}

message CheckPermissionResponse {
  bool allowed = 1;
  string reason = 2;
}

message GetUserRolesRequest {
  string user_id = 1;
  optional string team_id = 2;
  optional string project_id = 3;
}

message GetUserRolesResponse {
  repeated Role roles = 1;
}

message Role {
  string resource_type = 1;
  string resource_id = 2;
  string role = 3;
  repeated string permissions = 4;
}
```

### Notification Service

```protobuf
syntax = "proto3";

package cronbot.notification;

service NotificationService {
  rpc SendNotification(SendNotificationRequest) returns (SendNotificationResponse);
  rpc GetNotifications(GetNotificationsRequest) returns (GetNotificationsResponse);
  rpc MarkAsRead(MarkAsReadRequest) returns (MarkAsReadResponse);
  rpc Subscribe(SubscribeRequest) returns (stream Notification);
}

message SendNotificationRequest {
  string user_id = 1;
  string type = 2;
  string title = 3;
  string message = 4;
  optional string project_id = 5;
  optional string task_id = 6;
  map<string, string> data = 7;
  string channel = 8;  // telegram, web, email
  string priority = 9;  // low, normal, high, urgent
}

message Notification {
  string id = 1;
  string type = 2;
  string title = 3;
  string message = 4;
  map<string, string> data = 5;
  int64 created_at = 6;
}
```

### Memory Service

```protobuf
syntax = "proto3";

package cronbot.memory;

service MemoryService {
  rpc StoreContext(StoreContextRequest) returns (StoreContextResponse);
  rpc GetContext(GetContextRequest) returns (GetContextResponse);
  rpc CompactContext(CompactContextRequest) returns (CompactContextResponse);
  rpc ArchiveConversation(ArchiveConversationRequest) returns (ArchiveConversationResponse);
  rpc SearchArchive(SearchArchiveRequest) returns (SearchArchiveResponse);
  rpc StoreDecision(StoreDecisionRequest) returns (StoreDecisionResponse);
}

message StoreContextRequest {
  string task_id = 1;
  string context_json = 2;
  int32 token_count = 3;
}

message GetContextRequest {
  string task_id = 1;
  bool include_archive = 2;
}

message GetContextResponse {
  string context_json = 1;
  int32 token_count = 2;
  string archive_id = 3;
}

message CompactContextRequest {
  string task_id = 1;
  int32 max_tokens = 2;
}

message CompactContextResponse {
  string compacted_context = 1;
  int32 new_token_count = 2;
  string archive_id = 3;
}

message SearchArchiveRequest {
  string project_id = 1;
  string query = 2;
  int32 limit = 3;
}

message SearchArchiveResponse {
  repeated SearchResult results = 1;
}

message SearchResult {
  string task_id = 1;
  string archive_id = 2;
  string excerpt = 3;
  double relevance = 4;
}
```

### MCP Service

```protobuf
syntax = "proto3";

package cronbot.mcp;

service McpService {
  rpc RegisterMcp(RegisterMcpRequest) returns (RegisterMcpResponse);
  rpc UpdateMcp(UpdateMcpRequest) returns (UpdateMcpResponse);
  rpc DeleteMcp(DeleteMcpRequest) returns (DeleteMcpResponse);
  rpc ListMcps(ListMcpsRequest) returns (ListMcpsResponse);
  rpc GetMcpConfig(GetMcpConfigRequest) returns (GetMcpConfigResponse);
  rpc EnableMcpForProject(EnableMcpForProjectRequest) returns (EnableMcpForProjectResponse);
  rpc DisableMcpForProject(DisableMcpForProjectRequest) returns (DisableMcpForProjectResponse);
  rpc TestMcpConnection(TestMcpConnectionRequest) returns (TestMcpConnectionResponse);
}

message RegisterMcpRequest {
  string name = 1;
  string version = 2;
  string description = 3;
  string group_type = 4;  // system, project, custom, external
  optional string team_id = 5;
  optional string project_id = 6;
  string transport = 7;  // stdio, sse, http
  optional string command = 8;
  optional string url = 9;
  optional string container_image = 10;
  map<string, string> environment_vars = 11;
  repeated string required_config = 12;
}

message TestMcpConnectionRequest {
  string mcp_id = 1;
}

message TestMcpConnectionResponse {
  bool success = 1;
  string error = 2;
  repeated ToolInfo tools = 3;
  repeated ResourceInfo resources = 4;
}

message ToolInfo {
  string name = 1;
  string description = 2;
  string input_schema = 3;
}

message ResourceInfo {
  string uri = 1;
  string name = 2;
  string description = 3;
}

message GetMcpConfigRequest {
  string project_id = 1;
}

message GetMcpConfigResponse {
  repeated McpConfig mcps = 1;
}

message McpConfig {
  string name = 1;
  string transport = 2;
  string url = 3;
  string command = 4;
  repeated string tools = 5;
  repeated string resources = 6;
  map<string, string> environment = 7;
}
```

## REST API (External)

### Base URL

```
https://cronbot.{traefik-id}.traefik.me/api/v1
```

### Authentication

All endpoints (except auth) require JWT Bearer token:

```
Authorization: Bearer <jwt_token>
```

### Auth Endpoints

```yaml
POST /auth/login
  Body: { username: string, password: string }
  Response: { token: string, user: User }

POST /auth/register
  Body: { username: string, email: string, password: string }
  Response: { token: string, user: User }

POST /auth/link-telegram
  Body: { telegram_id: number }
  Response: { link_url: string }

GET /auth/me
  Response: { user: User, teams: Team[], current_context: Context }
```

### Users

```yaml
GET /users/me
  Response: User

PATCH /users/me
  Body: { display_name?: string, avatar_url?: string }
  Response: User

GET /users/me/teams
  Response: Team[]

GET /users/me/notifications
  Query: { unread_only?: boolean, limit?: number, offset?: number }
  Response: { notifications: Notification[], total: number }

POST /users/me/notifications/{id}/read
  Response: { success: boolean }
```

### Teams

```yaml
GET /teams
  Response: Team[]

POST /teams
  Body: { name: string, description?: string }
  Response: Team

GET /teams/{teamId}
  Response: Team

PATCH /teams/{teamId}
  Body: { name?: string, description?: string, settings?: object }
  Response: Team

DELETE /teams/{teamId}
  Response: { success: boolean }

GET /teams/{teamId}/members
  Response: TeamMember[]

POST /teams/{teamId}/members
  Body: { user_id: string, role: string }
  Response: TeamMember

PATCH /teams/{teamId}/members/{userId}
  Body: { role: string }
  Response: TeamMember

DELETE /teams/{teamId}/members/{userId}
  Response: { success: boolean }
```

### Projects

```yaml
GET /teams/{teamId}/projects
  Response: Project[]

POST /teams/{teamId}/projects
  Body:
    name: string
    description?: string
    git_mode?: "internal" | "external" | "import"
    external_git_url?: string
    template?: string
    autonomy_level?: 0-3
    sandbox_mode?: "standard" | "hardened" | "maximum"
  Response: Project

GET /projects/{projectId}
  Response: Project

PATCH /projects/{projectId}
  Body: { name?: string, description?: string, autonomy_level?: number, settings?: object }
  Response: Project

DELETE /projects/{projectId}
  Response: { success: boolean }

GET /projects/{projectId}/status
  Response:
    pod_status: string
    agents: Agent[]
    active_tasks: number
    current_sprint: Sprint

GET /projects/{projectId}/settings
  Response: ProjectSettings

PATCH /projects/{projectId}/settings
  Body: ProjectSettings
  Response: ProjectSettings
```

### Sprints

```yaml
GET /projects/{projectId}/sprints
  Response: Sprint[]

POST /projects/{projectId}/sprints
  Body: { name?: string, goal?: string, start_date?: string, end_date?: string }
  Response: Sprint

GET /projects/{projectId}/sprints/current
  Response: Sprint

GET /sprints/{sprintId}
  Response: Sprint

POST /sprints/{sprintId}/start
  Body: { task_ids: string[] }
  Response: Sprint

POST /sprints/{sprintId}/complete
  Response: Sprint

POST /sprints/{sprintId}/cancel
  Body: { reason?: string }
  Response: Sprint

GET /sprints/{sprintId}/review
  Response: SprintReview

POST /sprints/{sprintId}/review
  Body: { approved: boolean, notes?: string }
  Response: Sprint

GET /sprints/{sprintId}/retrospective
  Response: SprintRetrospective
```

### Tasks

```yaml
GET /projects/{projectId}/tasks
  Query: { status?, type?, assignee?, sprint?, limit?, offset? }
  Response: { tasks: Task[], total: number }

POST /projects/{projectId}/tasks
  Body:
    title: string
    description?: string
    type?: "task" | "bug" | "blocker" | "idea" | "epic"
    story_points?: number
    parent_task_id?: string
  Response: Task

GET /tasks/{taskId}
  Response: Task

PATCH /tasks/{taskId}
  Body: { title?: string, description?: string, status?: string, story_points?: number }
  Response: Task

POST /tasks/{taskId}/move
  Body: { status?: string, sprint_id?: string, position?: number }
  Response: Task

POST /tasks/{taskId}/assign
  Body: { assignee_type: string, assignee_id: string }
  Response: Task

POST /tasks/{taskId}/block
  Body: { blocked_by_task_id: string, reason: string }
  Response: Task

POST /tasks/{taskId}/unblock
  Response: Task

GET /tasks/{taskId}/comments
  Response: Comment[]

POST /tasks/{taskId}/comments
  Body: { content: string }
  Response: Comment

GET /tasks/{taskId}/state
  Response: TaskState

GET /tasks/{taskId}/commits
  Response: Commit[]

GET /tasks/{taskId}/preview
  Response: Preview
```

### MCP Configuration

```yaml
GET /mcps
  Query: { group_type?, team_id?, project_id? }
  Response: Mcp[]

POST /mcps
  Body: RegisterMcpRequest
  Response: Mcp

GET /mcps/{mcpId}
  Response: Mcp

PATCH /mcps/{mcpId}
  Body: { name?, description?, environment_vars?, required_config? }
  Response: Mcp

DELETE /mcps/{mcpId}
  Response: { success: boolean }

POST /mcps/{mcpId}/test
  Response: { success: boolean, tools: string[], resources: string[], error?: string }

GET /projects/{projectId}/mcps
  Response: { mcp_id: string, enabled: boolean, enabled_tools: string[] }[]

POST /projects/{projectId}/mcps/{mcpId}/enable
  Body: { enabled_tools?: string[], config_override?: object }
  Response: { success: boolean }

POST /projects/{projectId}/mcps/{mcpId}/disable
  Response: { success: boolean }
```

### Skills

```yaml
GET /skills
  Query: { scope?, team_id?, project_id? }
  Response: Skill[]

POST /skills
  Body:
    name: string
    description?: string
    scope: "system" | "team" | "project"
    team_id?: string
    project_id?: string
    file_content: string
  Response: Skill

GET /skills/{skillId}
  Response: Skill

PATCH /skills/{skillId}
  Body: { name?, description?, file_content? }
  Response: Skill

DELETE /skills/{skillId}
  Response: { success: boolean }

GET /projects/{projectId}/skills
  Response: { skill_id: string, enabled: boolean }[]

POST /projects/{projectId}/skills/{skillId}/enable
  Response: { success: boolean }

POST /projects/{projectId}/skills/{skillId}/disable
  Response: { success: boolean }
```

### Previews

```yaml
GET /projects/{projectId}/previews
  Response: Preview[]

GET /previews/{previewId}
  Response: Preview

DELETE /previews/{previewId}
  Response: { success: boolean }

GET /previews/{previewId}/screenshot
  Response: { image_url: string, taken_at: string }

POST /previews/{previewId}/screenshot
  Body: { viewport?: { width: number, height: number }, wait_for?: string }
  Response: { image_url: string, taken_at: string }
```

### Templates

```yaml
GET /templates
  Response: Template[]

GET /templates/{templateName}
  Response: Template
```

### Git (Proxy to Gitea)

```yaml
GET /projects/{projectId}/repo/browse
  Query: { path?, ref? }
  Response: { type: "file" | "dir", content?: string, entries?: string[] }

GET /projects/{projectId}/repo/commits
  Query: { branch?, limit?, offset? }
  Response: Commit[]

GET /projects/{projectId}/repo/pulls
  Response: PullRequest[]

GET /projects/{projectId}/repo/pulls/{prId}
  Response: PullRequest

POST /projects/{projectId}/repo/pulls/{prId}/review
  Body: { action: "approve" | "request_changes", comment?: string }
  Response: PullRequest

POST /projects/{projectId}/repo/pulls/{prId}/merge
  Response: PullRequest
```

### Dashboard

```yaml
GET /dashboard/overview
  Response:
    teams: { id: string, name: string, projects_count: number }[]
    active_sprints: Sprint[]
    recent_activity: Activity[]
    notifications_unread: number

GET /dashboard/projects/{projectId}
  Response:
    project: Project
    sprint: Sprint
    tasks: { by_status: Record<string, number> }
    agents: Agent[]
    previews: Preview[]
    recent_commits: Commit[]

GET /dashboard/agents/{agentId}
  Response:
    agent: Agent
    current_task: Task
    resource_usage: { cpu: number, memory: number }
    recent_actions: Action[]
```

### WebSocket

```yaml
WS /ws/dashboard
  # Real-time updates
  Messages:
    - type: "task_updated" | "agent_status" | "sprint_progress" | "notification"
    - data: object
```

## Response Models

### User

```typescript
interface User {
  id: string;
  username: string;
  email: string | null;
  telegram_id: number | null;
  display_name: string | null;
  avatar_url: string | null;
  is_active: boolean;
  created_at: string;
}
```

### Team

```typescript
interface Team {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  is_personal: boolean;
  owner_id: string;
  settings: Record<string, any>;
  created_at: string;
}
```

### Project

```typescript
interface Project {
  id: string;
  team_id: string;
  name: string;
  slug: string;
  description: string | null;
  git_mode: "internal" | "external" | "import";
  external_git_url: string | null;
  internal_repo_url: string | null;
  autonomy_level: number;
  sandbox_mode: "standard" | "hardened" | "maximum";
  template_name: string | null;
  max_agents: number;
  is_active: boolean;
  created_at: string;
}
```

### Task

```typescript
interface Task {
  id: string;
  number: number;
  project_id: string;
  sprint_id: string | null;
  title: string;
  description: string | null;
  type: "task" | "bug" | "blocker" | "idea" | "epic";
  status: "backlog" | "sprint" | "in_progress" | "review" | "blocked" | "done" | "cancelled";
  story_points: number | null;
  assignee_type: "agent" | "user" | null;
  assignee_id: string | null;
  parent_task_id: string | null;
  git_branch: string | null;
  git_pr_url: string | null;
  blocked_by_task_id: string | null;
  blocked_reason: string | null;
  created_at: string;
  updated_at: string;
  started_at: string | null;
  completed_at: string | null;
}
```

### Error Response

```typescript
interface ErrorResponse {
  error: {
    code: string;
    message: string;
    details?: Record<string, any>;
  };
}
```
