import { useCallback, useEffect, useMemo, useState } from 'react';
import { ApiError } from './api/client';
import { deleteIssue, listIssues, updateStatus } from './api/issues';
import type { Issue, Status } from './types';
import { ErrorBanner } from './components/ErrorBanner';
import { IssueTable } from './components/IssueTable';
import { LoginPage } from './components/LoginPage';
import { clearSession, loadSession, type Session } from './session';

type Filter = Status | 'all';

const FILTERS: { value: Filter; label: string }[] = [
  { value: 'all', label: '全部' },
  { value: 'open', label: '待处理' },
  { value: 'in_progress', label: '处理中' },
  { value: 'resolved', label: '已解决' },
];

export default function App() {
  const [session, setSession] = useState<Session | null>(() => loadSession());
  const [issues, setIssues] = useState<Issue[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<Filter>('all');
  const [savingId, setSavingId] = useState<string | null>(null);

  const canEdit = session?.user.role === 'admin';

  const signOut = useCallback(() => {
    clearSession();
    setSession(null);
    setIssues([]);
    setError(null);
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setIssues(await listIssues());
    } catch (e) {
      if (e instanceof ApiError && e.status === 401) {
        signOut();
        return;
      }
      setError(e instanceof ApiError ? e.message : '加载失败：未知错误');
    } finally {
      setLoading(false);
    }
  }, [signOut]);

  useEffect(() => {
    if (session) void load();
  }, [session, load]);

  const handleStatusChange = useCallback(
    async (issue: Issue, status: Status) => {
      if (!canEdit) return;

      setSavingId(issue.id);
      setError(null);
      try {
        const updated = await updateStatus(issue.id, status);
        setIssues(prev => prev.map(item => (item.id === updated.id ? updated : item)));
      } catch (e) {
        if (e instanceof ApiError && e.status === 401) {
          signOut();
          return;
        }
        setError(e instanceof ApiError ? e.message : '修改状态失败');
      } finally {
        setSavingId(null);
      }
    },
    [canEdit, signOut],
  );

  const handleDelete = useCallback(
    async (issue: Issue) => {
      if (!canEdit) return;
      const ok = window.confirm(`确定删除「${issue.title}」？删除后不可恢复。`);
      if (!ok) return;

      setSavingId(issue.id);
      setError(null);
      try {
        await deleteIssue(issue.id);
        setIssues(prev => prev.filter(item => item.id !== issue.id));
      } catch (e) {
        if (e instanceof ApiError && e.status === 401) {
          signOut();
          return;
        }
        setError(e instanceof ApiError ? e.message : '删除失败');
      } finally {
        setSavingId(null);
      }
    },
    [canEdit, signOut],
  );

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

  if (!session) {
    return <LoginPage onSuccess={next => setSession(next)} />;
  }

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

        <div className="header__right">
          <span className="user">
            {session.user.displayName}
            <span className={canEdit ? 'user__role user__role--admin' : 'user__role'}>
              {canEdit ? '管理员' : '巡检员'}
            </span>
          </span>
          <button className="btn" onClick={() => void load()} disabled={loading}>
            {loading ? '刷新中…' : '刷新'}
          </button>
          <button className="btn btn--ghost" onClick={signOut}>
            退出
          </button>
        </div>
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

      {!canEdit ? (
        <p className="readonly-hint">当前是巡检员账号：只能查看，修改状态和删除需要管理员登录。</p>
      ) : null}

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
          canEdit={canEdit}
          savingId={savingId}
          onStatusChange={handleStatusChange}
          onDelete={handleDelete}
        />
      )}
    </div>
  );
}
