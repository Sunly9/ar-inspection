import type { Priority } from '../types';

const LABELS: Record<Priority, string> = {
  low: '低',
  medium: '中',
  high: '高',
};

export function PriorityTag({ priority }: { priority: Priority }) {
  return <span className={`tag tag--${priority}`}>{LABELS[priority] ?? priority}</span>;
}
