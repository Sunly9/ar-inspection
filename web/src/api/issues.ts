import { request } from './client';
import type { Issue, IssueListData, Status } from '../types';

export async function listIssues(): Promise<Issue[]> {
  const data = await request<IssueListData>('/api/issues');
  return data.items ?? [];
}

export async function updateStatus(id: string, status: Status): Promise<Issue> {
  return request<Issue>(`/api/issues/${encodeURIComponent(id)}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  });
}

export async function deleteIssue(id: string): Promise<void> {
  await request<{ id: string }>(`/api/issues/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}
