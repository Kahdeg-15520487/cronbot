#!/usr/bin/env python3
"""
@name code_analyzer
@version 1.0.0
@description Analyzes code files for quality metrics, patterns, and potential issues
@author cronbot
@tags code, analysis, quality

@inputs
@param path (string): Path to the file or directory to analyze
@param language (string)?: Programming language (auto-detected if not specified)
@param checks (array)?: List of checks to run (complexity, security, style, all)

@outputs
results: Analysis results with metrics and recommendations
"""

import json
import os
import sys
import argparse
from pathlib import Path
from typing import Dict, List, Any, Optional


def analyze_file(file_path: str, checks: List[str]) -> Dict[str, Any]:
    """Analyze a single file."""
    result = {
        'file': file_path,
        'metrics': {},
        'issues': [],
        'recommendations': [],
    }

    try:
        with open(file_path, 'r') as f:
            content = f.read()
            lines = content.split('\n')

        # Basic metrics
        result['metrics'] = {
            'lines_of_code': len([l for l in lines if l.strip() and not l.strip().startswith('#')]),
            'total_lines': len(lines),
            'blank_lines': len([l for l in lines if not l.strip()]),
            'comment_lines': len([l for l in lines if l.strip().startswith('#')]),
            'file_size_bytes': os.path.getsize(file_path),
        }

        # Complexity check
        if 'complexity' in checks or 'all' in checks:
            complexity = estimate_complexity(content)
            result['metrics']['estimated_complexity'] = complexity
            if complexity > 20:
                result['issues'].append({
                    'type': 'complexity',
                    'severity': 'warning',
                    'message': f'High complexity score: {complexity}',
                })
                result['recommendations'].append(
                    'Consider breaking down complex functions into smaller ones'
                )

        # TODO lines check
        todo_comments = [i for i, l in enumerate(lines) if 'TODO' in l.upper()]
        if todo_comments:
            result['metrics']['todo_count'] = len(todo_comments)
            result['issues'].append({
                'type': 'todo',
                'severity': 'info',
                'message': f'{len(todo_comments)} TODO comments found',
                'lines': todo_comments,
            })

    except Exception as e:
        result['issues'].append({
            'type': 'error',
            'severity': 'critical',
            'message': str(e),
        })

    return result


def estimate_complexity(content: str) -> int:
    """Estimate code complexity based on control structures."""
    complexity = 1  # Base complexity

    # Count control structures
    keywords = ['if ', 'elif ', 'else:', 'for ', 'while ', 'try:', 'except:', 'with ', 'and ', 'or ']

    for keyword in keywords:
        complexity += content.count(keyword)

    # Count function/method definitions
    complexity += content.count('def ') + content.count('function ') + content.count('class ')

    return complexity


def analyze_path(path: str, checks: List[str]) -> Dict[str, Any]:
    """Analyze a file or directory."""
    path_obj = Path(path)
    results = {
        'path': path,
        'summary': {
            'files_analyzed': 0,
            'total_issues': 0,
            'total_todos': 0,
        },
        'files': [],
    }

    if path_obj.is_file():
        file_result = analyze_file(str(path_obj), checks)
        results['files'].append(file_result)
        results['summary']['files_analyzed'] = 1
        results['summary']['total_issues'] = len(file_result['issues'])
        results['summary']['total_todos'] = file_result['metrics'].get('todo_count', 0)

    elif path_obj.is_dir():
        # Find all code files
        extensions = ['.py', '.js', '.ts', '.cs', '.java', '.go', '.rs', '.rb']
        code_files = []
        for ext in extensions:
            code_files.extend(path_obj.rglob(f'*{ext}'))

        for code_file in code_files[:50]:  # Limit to 50 files
            file_result = analyze_file(str(code_file), checks)
            results['files'].append(file_result)
            results['summary']['files_analyzed'] += 1
            results['summary']['total_issues'] += len(file_result['issues'])
            results['summary']['total_todos'] += file_result['metrics'].get('todo_count', 0)

    return results


def main():
    parser = argparse.ArgumentParser(description='Analyze code for quality metrics')
    parser.add_argument('--args', type=str, help='JSON-encoded arguments')
    args = parser.parse_args()

    if args.args:
        params = json.loads(args.args)
    else:
        params = {}

    path = params.get('path', os.environ.get('WORKSPACE_PATH', '.'))
    checks = params.get('checks', ['all'])
    if isinstance(checks, str):
        checks = [checks]

    results = analyze_path(path, checks)
    print(json.dumps(results, indent=2))


if __name__ == '__main__':
    main()
