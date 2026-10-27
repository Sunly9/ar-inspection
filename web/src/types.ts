export type Priority = 'low' | 'medium' | 'high';
export type Status = 'open' | 'in_progress' | 'resolved';

export interface Position {
  x: number;
  y: number;
  z: number;
}

/** 与 Go 后端、Unity 端字段保持完全一致 */
export interface Issue {
  id: string;
  title: string;
  description: string;
  priority: Priority;
  status: Status;
  position: Position;
  normal?: Position;
  reporter: string;
  statusChangedBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface IssueListData {
  items: Issue[];
}
