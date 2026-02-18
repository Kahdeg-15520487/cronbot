import { SkillMeta, OperationResult } from '../types.js';
/**
 * Skill executor for running Python scripts.
 */
export declare class SkillExecutor {
    private skillsPath;
    private workspacePath;
    private loadedSkills;
    constructor(skillsPath: string, workspacePath: string);
    /**
     * Initialize the skill executor and discover available skills.
     */
    initialize(): Promise<void>;
    /**
     * Discover all available skills.
     */
    private discoverSkills;
    /**
     * Load a skill and parse its metadata.
     */
    private loadSkill;
    /**
     * Parse skill metadata from Python docstring.
     */
    private parseSkillMeta;
    /**
     * Get all available skills.
     */
    getAvailableSkills(): Array<{
        key: string;
        meta: SkillMeta;
    }>;
    /**
     * Get a specific skill.
     */
    getSkill(name: string): SkillMeta | undefined;
    /**
     * Execute a skill.
     */
    execute(skillName: string, args: Record<string, unknown>): Promise<OperationResult>;
    /**
     * Run a Python script with arguments.
     */
    private runPythonScript;
    /**
     * Reload skills from disk.
     */
    reload(): Promise<void>;
}
//# sourceMappingURL=executor.d.ts.map