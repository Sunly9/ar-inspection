import { useCallback, useEffect, useMemo, useState } from 'react';
import { ApiError } from './api/client';
import { deleteIssue, listIssues, updateStatus } from './api/issues';
import type { Issue, Status } from './types';
import { ErrorBanner } from './components/ErrorBanner';
import { IssueTable } from './components/IssueTable';

type Filter = Status | 'all';

const FILTERS: { value: Filter; label: string }[] = [
  { value: 'all', label: '全部' },
  { value: 'open', label: '待处理' },
  { value: 'in_progress', label: '处理中' },
  { value: 'resolved', label: '已解决' },
];

export default function App() {
  const [issues, setIssues] = useState<Issue[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<Filter>('all');
  const [savingId, setSavingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setIssues(await listIssues());
    } catch (e) {
      setError(e instanceof ApiError ? e.message : '加载失败：未知错误');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const handleStatusChange = useCallback(async (issue: Issue, status: Status) => {
    setSavingId(issue.id);
    setError(null);
    try {
      const updated = await updateStatus(issue.id, status);
      setIssues(prev => prev.map(item => (item.id === updated.id ? updated : item)));
    } catch (e) {
      setError(e instanceof ApiError ? e.message : '修改状态失败');
    } finally {
      setSavingId(null);
    }
  }, []);

  const handleDelete = useCallback(async (issue: Issue) => {
    const ok = window.confirm(`确定删除「${issue.title}」？删除后不可恢复。`);
    if (!ok) return;

    setSavingId(issue.id);
    setError(null);
    try {
      await deleteIssue(issue.id);
      setIssues(prev => prev.filter(item => item.id !== issue.id));
    } catch (e) {
      setError(e instanceof ApiError ? e.message : '删除失败');
    } finally {
      setSavingId(null);
    }
  }, []);

  const visible = useMemo(
    () => (filter === 'all' ? issues : issues.filter(issue => issue.status === filter)),
    [issues, filter],
  );

  const counts = useMemo(
    () => ({
      open: issues.filter(i => i.status === 'open').length,
      in_progress: issues.filter(i => i.status === 'in_progress').length,
      resolved: issues.filter(i => i.status === 'resolved').length,
    }),
    [issues],
  );

  return (
    <div className="page">
      <header className="header">
        <div>
          <h1>巡检问题管理</h1>
          <p className="subtitle">
            共 {issues.length} 条 · 待处理 {counts.open} · 处理中 {counts.in_progress} · 已解决{' '}
            {counts.resolved}
          </p>
        </div>
        <button className="btn" onClick={() => void load()} disabled={loading}>
          {loading ? '刷新中…' : '刷新'}
        </button>
      </header>

      <nav className="filters" aria-label="按状态筛选">
        {FILTERS.map(item => (
          <button
            key={item.value}
            className={filter === item.value ? 'chip chip--active' : 'chip'}
            onClick={() => setFilter(item.value)}
          >
            {item.label}
          </button>
        ))}
      </nav>

      {error ? <ErrorBanner message={error} onRetry={() => void load()} /> : null}

      {loading && issues.length === 0 ? (
        <p className="hint">加载中…</p>
      ) : visible.length === 0 ? (
        <p className="hint">
          {issues.length === 0
            ? '还没有问题记录。先在 Unity 场景里点一个设备，填好信息提交试试。'
            : '当前筛选条件下没有数据。'}
        </p>
      ) : (
        <IssueTable
          issues={visible}
          savingId={savingId}
          onStatusChange={handleStatusChange}
          onDelete={handleDelete}
        />
      )}
    </div>
  );
}
