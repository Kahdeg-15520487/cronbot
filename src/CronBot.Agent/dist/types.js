/**
 * Autonomy levels for agent behavior control.
 * - Level 0: Reactive - Read-only, responds when spoken to
 * - Level 1: Cautious - Read/analyze, plans before executing, approval on writes
 * - Level 2: Balanced - Autonomous on tasks, approval on major changes
 * - Level 3: Full Autonomy - Full sandbox autonomy, only security-critical ops need approval
 */
export var AutonomyLevel;
(function (AutonomyLevel) {
    AutonomyLevel[AutonomyLevel["Reactive"] = 0] = "Reactive";
    AutonomyLevel[AutonomyLevel["Cautious"] = 1] = "Cautious";
    AutonomyLevel[AutonomyLevel["Balanced"] = 2] = "Balanced";
    AutonomyLevel[AutonomyLevel["FullAutonomy"] = 3] = "FullAutonomy";
})(AutonomyLevel || (AutonomyLevel = {}));
/**
 * Agent status enum matching the database.
 */
export var AgentStatus;
(function (AgentStatus) {
    AgentStatus["Idle"] = "idle";
    AgentStatus["Working"] = "working";
    AgentStatus["Paused"] = "paused";
    AgentStatus["Blocked"] = "blocked";
    AgentStatus["Error"] = "error";
    AgentStatus["Terminated"] = "terminated";
})(AgentStatus || (AgentStatus = {}));
/**
 * Task status enum matching the database.
 */
export var TaskStatus;
(function (TaskStatus) {
    TaskStatus["Backlog"] = "backlog";
    TaskStatus["Sprint"] = "sprint";
    TaskStatus["InProgress"] = "in_progress";
    TaskStatus["Review"] = "review";
    TaskStatus["Blocked"] = "blocked";
    TaskStatus["Done"] = "done";
    TaskStatus["Cancelled"] = "cancelled";
})(TaskStatus || (TaskStatus = {}));
/**
 * Task type enum.
 */
export var TaskType;
(function (TaskType) {
    TaskType["Task"] = "task";
    TaskType["Bug"] = "bug";
    TaskType["Blocker"] = "blocker";
    TaskType["Idea"] = "idea";
    TaskType["Epic"] = "epic";
})(TaskType || (TaskType = {}));
/**
 * Phases of agent operation.
 */
export var AgentPhase;
(function (AgentPhase) {
    AgentPhase["Initializing"] = "initializing";
    AgentPhase["ReadingContext"] = "reading_context";
    AgentPhase["Planning"] = "planning";
    AgentPhase["Executing"] = "executing";
    AgentPhase["Verifying"] = "verifying";
    AgentPhase["Committing"] = "committing";
    AgentPhase["WaitingApproval"] = "waiting_approval";
    AgentPhase["Blocked"] = "blocked";
    AgentPhase["Completed"] = "completed";
    AgentPhase["Error"] = "error";
})(AgentPhase || (AgentPhase = {}));
/**
 * Types of blockers that can be detected.
 */
export var BlockerType;
(function (BlockerType) {
    BlockerType["CodeLoop"] = "code_loop";
    BlockerType["VerificationLoop"] = "verification_loop";
    BlockerType["ToolFailure"] = "tool_failure";
    BlockerType["AgentStuck"] = "agent_stuck";
    BlockerType["DependencyIssue"] = "dependency_issue";
    BlockerType["ScopeCreep"] = "scope_creep";
})(BlockerType || (BlockerType = {}));
//# sourceMappingURL=types.js.map