import { spawn } from 'child_process';
import fs from 'fs/promises';
import path from 'path';
import { SkillMeta, SkillInput, OperationResult } from '../types.js';
import { createLogger } from '../logger.js';

const logger = createLogger('skills');

/**
 * Skill executor for running Python scripts.
 */
export class SkillExecutor {
  private skillsPath: string;
  private workspacePath: string;
  private loadedSkills: Map<string, SkillMeta> = new Map();

  constructor(skillsPath: string, workspacePath: string) {
    this.skillsPath = skillsPath;
    this.workspacePath = workspacePath;
  }

  /**
   * Initialize the skill executor and discover available skills.
   */
  async initialize(): Promise<void> {
    await this.discoverSkills();
    logger.info({ skillCount: this.loadedSkills.size }, 'Skill executor initialized');
  }

  /**
   * Discover all available skills.
   */
  private async discoverSkills(): Promise<void> {
    const scopes = ['system', 'team', 'project'];

    for (const scope of scopes) {
      const scopePath = path.join(this.skillsPath, scope);

      try {
        const entries = await fs.readdir(scopePath, { withFileTypes: true });

        for (const entry of entries) {
          if (entry.isFile() && entry.name.endsWith('.py')) {
            await this.loadSkill(scope, entry.name);
          }
        }
      } catch {
        // Scope directory doesn't exist, skip
        logger.debug({ scope }, 'Skill scope directory not found');
      }
    }
  }

  /**
   * Load a skill and parse its metadata.
   */
  private async loadSkill(scope: string, filename: string): Promise<void> {
    const filePath = path.join(this.skillsPath, scope, filename);

    try {
      const content = await fs.readFile(filePath, 'utf-8');
      const meta = this.parseSkillMeta(content, filename);

      if (meta) {
        const skillKey = `${scope}:${meta.name}`;
        this.loadedSkills.set(skillKey, meta);
        logger.debug({ skill: meta.name, scope }, 'Skill loaded');
      }
    } catch (error) {
      logger.warn({ filename, error }, 'Failed to load skill');
    }
  }

  /**
   * Parse skill metadata from Python docstring.
   */
  private parseSkillMeta(content: string, filename: string): SkillMeta | null {
    // Extract docstring
    const docstringMatch = content.match(/"""([\s\S]*?)"""/);
    if (!docstringMatch) {
      return null;
    }

    const docstring = docstringMatch[1];
    const lines = docstring.split('\n').map(l => l.trim());

    let name = filename.replace('.py', '');
    let version = '1.0.0';
    let description = '';
    let author = 'unknown';
    const tags: string[] = [];
    const inputs: Record<string, SkillInput> = {};
    const outputs: Record<string, string> = {};

    let currentSection = '';
    let currentInput: string | null = null;

    for (const line of lines) {
      if (line.startsWith('@name ')) {
        name = line.substring(6).trim();
      } else if (line.startsWith('@version ')) {
        version = line.substring(9).trim();
      } else if (line.startsWith('@author ')) {
        author = line.substring(8).trim();
      } else if (line.startsWith('@tags ')) {
        tags.push(...line.substring(6).trim().split(',').map(t => t.trim()));
      } else if (line === '@inputs') {
        currentSection = 'inputs';
      } else if (line === '@outputs') {
        currentSection = 'outputs';
      } else if (line.startsWith('@param ')) {
        currentSection = 'inputs';
        const match = line.match(/@param\s+(\w+)\s+\((\w+)(\?)?\):\s*(.*)/);
        if (match) {
          const [, paramName, paramType, optional, paramDesc] = match;
          currentInput = paramName;
          inputs[paramName] = {
            type: paramType,
            required: !optional,
            description: paramDesc,
          };
        }
      } else if (line.startsWith('@returns ')) {
        currentSection = 'outputs';
        const match = line.match(/@returns\s+(\w+):\s*(.*)/);
        if (match) {
          const [, outputName, outputDesc] = match;
          outputs[outputName] = outputDesc;
        }
      } else if (line && !line.startsWith('@') && currentSection === 'inputs' && currentInput) {
        inputs[currentInput].description += ' ' + line;
      } else if (line && !line.startsWith('@') && !currentSection) {
        description += (description ? ' ' : '') + line;
      }
    }

    return {
      name,
      version,
      description: description.trim(),
      author,
      tags,
      inputs,
      outputs,
    };
  }

  /**
   * Get all available skills.
   */
  getAvailableSkills(): Array<{ key: string; meta: SkillMeta }> {
    return Array.from(this.loadedSkills.entries()).map(([key, meta]) => ({ key, meta }));
  }

  /**
   * Get a specific skill.
   */
  getSkill(name: string): SkillMeta | undefined {
    // Try to find skill by name across all scopes
    for (const [key, meta] of this.loadedSkills) {
      if (key.endsWith(`:${name}`) || meta.name === name) {
        return meta;
      }
    }
    return undefined;
  }

  /**
   * Execute a skill.
   */
  async execute(
    skillName: string,
    args: Record<string, unknown>
  ): Promise<OperationResult> {
    // Find the skill
    let skillPath: string | null = null;
    let skillMeta: SkillMeta | undefined;

    for (const scope of ['project', 'team', 'system']) {
      const testPath = path.join(this.skillsPath, scope, `${skillName}.py`);
      try {
        await fs.access(testPath);
        skillPath = testPath;
        skillMeta = this.loadedSkills.get(`${scope}:${skillName}`);
        break;
      } catch {
        // Try next scope
      }
    }

    if (!skillPath) {
      return {
        success: false,
        error: `Skill '${skillName}' not found`,
      };
    }

    // Validate required inputs
    if (skillMeta) {
      for (const [inputName, inputDef] of Object.entries(skillMeta.inputs)) {
        if (inputDef.required && !(inputName in args)) {
          return {
            success: false,
            error: `Required input '${inputName}' not provided`,
          };
        }
      }
    }

    logger.info({ skill: skillName, args }, 'Executing skill');

    try {
      const result = await this.runPythonScript(skillPath, args);
      return result;
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      logger.error({ skill: skillName, error: message }, 'Skill execution failed');

      return {
        success: false,
        error: message,
      };
    }
  }

  /**
   * Run a Python script with arguments.
   */
  private runPythonScript(
    scriptPath: string,
    args: Record<string, unknown>
  ): Promise<OperationResult> {
    return new Promise((resolve) => {
      const argsJson = JSON.stringify(args);

      const process = spawn('python3', [scriptPath, '--args', argsJson], {
        cwd: this.workspacePath,
        env: {
          ...process.env,
          WORKSPACE_PATH: this.workspacePath,
        },
      });

      let stdout = '';
      let stderr = '';

      process.stdout.on('data', (data) => {
        stdout += data.toString();
      });

      process.stderr.on('data', (data) => {
        stderr += data.toString();
      });

      process.on('close', (code) => {
        if (code === 0) {
          // Try to parse JSON output
          try {
            const output = JSON.parse(stdout);
            resolve({
              success: true,
              data: output,
            });
          } catch {
            resolve({
              success: true,
              data: stdout.trim(),
            });
          }
        } else {
          resolve({
            success: false,
            error: stderr || `Process exited with code ${code}`,
          });
        }
      });

      process.on('error', (error) => {
        resolve({
          success: false,
          error: error.message,
        });
      });
    });
  }

  /**
   * Reload skills from disk.
   */
  async reload(): Promise<void> {
    this.loadedSkills.clear();
    await this.discoverSkills();
    logger.info({ skillCount: this.loadedSkills.size }, 'Skills reloaded');
  }
}
