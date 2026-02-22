'use client';

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Task, tasksApi } from '@/lib/api';
import { useState } from 'react';
import clsx from 'clsx';
import Link from 'next/link';

interface Column {
  id: string;
  name: string;
  color: string;
}

interface KanbanBoardProps {
  projectId: string;
  tasks: Task[];
  columns: Column[];
}

export function KanbanBoard({ projectId, tasks, columns }: KanbanBoardProps) {
  const queryClient = useQueryClient();
  const [draggedTask, setDraggedTask] = useState<Task | null>(null);

  const updateTaskStatus = useMutation({
    mutationFn: ({ taskId, status }: { taskId: string; status: string }) =>
      tasksApi.update(taskId, { status: status as Task['status'] }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', projectId] });
    },
  });

  const handleDragStart = (e: React.DragEvent, task: Task) => {
    setDraggedTask(task);
    e.dataTransfer.effectAllowed = 'move';
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  };

  const handleDrop = (e: React.DragEvent, status: string) => {
    e.preventDefault();
    if (draggedTask && draggedTask.status !== status) {
      updateTaskStatus.mutate({ taskId: draggedTask.id, status });
    }
    setDraggedTask(null);
  };

  const handleDragEnd = () => {
    setDraggedTask(null);
  };

  const getTasksByStatus = (status: string) => {
    return tasks.filter((task) => task.status === status);
  };

  return (
    <div className="flex gap-4 h-full overflow-x-auto pb-4">
      {columns.map((column) => {
        const columnTasks = getTasksByStatus(column.id);

        return (
          <div
            key={column.id}
            className="flex-shrink-0 w-72 flex flex-col"
            onDragOver={handleDragOver}
            onDrop={(e) => handleDrop(e, column.id)}
          >
            {/* Column Header */}
            <div className={clsx('px-3 py-2 rounded-t-lg', column.color)}>
              <div className="flex items-center justify-between">
                <h3 className="font-medium text-gray-700">{column.name}</h3>
                <span className="text-sm text-gray-500 bg-white px-2 py-0.5 rounded-full">
                  {columnTasks.length}
                </span>
              </div>
            </div>

            {/* Column Content */}
            <div className="flex-1 bg-gray-50 rounded-b-lg p-2 overflow-y-auto min-h-[200px]">
              <div className="space-y-2">
                {columnTasks.map((task) => (
                  <TaskCard
                    key={task.id}
                    task={task}
                    onDragStart={(e) => handleDragStart(e, task)}
                    onDragEnd={handleDragEnd}
                    isDragging={draggedTask?.id === task.id}
                  />
                ))}
              </div>

              {columnTasks.length === 0 && (
                <div className="text-center py-8 text-gray-400 text-sm">
                  No tasks
                </div>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}

interface TaskCardProps {
  task: Task;
  onDragStart: (e: React.DragEvent) => void;
  onDragEnd: () => void;
  isDragging: boolean;
}

function TaskCard({ task, onDragStart, onDragEnd, isDragging }: TaskCardProps) {
  const typeColors: Record<string, string> = {
    task: 'bg-blue-100 text-blue-700',
    bug: 'bg-red-100 text-red-700',
    feature: 'bg-purple-100 text-purple-700',
    idea: 'bg-yellow-100 text-yellow-700',
    epic: 'bg-indigo-100 text-indigo-700',
    blocker: 'bg-red-200 text-red-800',
  };

  return (
    <Link
      href={`/tasks/${task.id}`}
      draggable
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
      className={clsx(
        'block bg-white rounded-lg shadow-sm p-3 cursor-grab active:cursor-grabbing border border-gray-200 hover:shadow-md hover:border-primary-300 transition-all',
        isDragging && 'opacity-50'
      )}
    >
      <div className="flex items-start justify-between gap-2 mb-2">
        <span className="text-xs text-gray-500">#{task.number}</span>
        <span className={clsx('text-xs px-1.5 py-0.5 rounded', typeColors[task.type] || typeColors.task)}>
          {task.type}
        </span>
      </div>

      <h4 className="font-medium text-gray-900 text-sm mb-1 line-clamp-2">
        {task.title}
      </h4>

      {task.description && (
        <p className="text-xs text-gray-500 line-clamp-2 mb-2">
          {task.description}
        </p>
      )}

      <div className="flex items-center justify-between mt-2 pt-2 border-t border-gray-100">
        <div className="flex items-center gap-2">
          {task.storyPoints && (
            <span className="text-xs bg-gray-100 text-gray-600 px-1.5 py-0.5 rounded">
              {task.storyPoints} pts
            </span>
          )}
          {task.gitBranch && (
            <span className="text-xs text-gray-400 truncate max-w-[100px]">
              {task.gitBranch}
            </span>
          )}
        </div>

        {task.assigneeType && (
          <div className="w-6 h-6 bg-primary-100 rounded-full flex items-center justify-center">
            <span className="text-xs text-primary-600">
              {task.assigneeType === 'agent' ? '🤖' : '👤'}
            </span>
          </div>
        )}
      </div>
    </Link>
  );
}
