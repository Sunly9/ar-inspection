import type { Issue, Status } from '../types';
import { PriorityTag } from './PriorityTag';
import { StatusSelect } from './StatusSelect';

interface Props {
  issues: Issue[];
  savingId: string | null;
  onStatusChange: (issue: Issue, status: Status) => void;
  onDelete: (issue: Issue) => void;
}

function formatTime(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function IssueTable({ issues, savingId, onStatusChange, onDelete }: Props) {
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>标题 / 描述</th>
            <th className="col-narrow">优先级</th>
            <th className="col-narrow">状态</th>
            <th>坐标 (x, y, z)</th>
            <th>上报时间</th>
            <th className="col-action">操作</th>
          </tr>
        </thead>
        <tbody>
          {issues.map(issue => {
            const saving = savingId === issue.id;
            return (
              <tr key={issue.id} className={saving ? 'row--saving' : undefined}>
                <td>
                  <div className="cell-title">{issue.title}</div>
                  {issue.description ? <div className="cell-desc">{issue.description}</div> : null}
                </td>
                <td className="col-narrow">
                  <PriorityTag priority={issue.priority} />
                </td>
                <td className="col-narrow">
                  <StatusSelect
                    value={issue.status}
                    disabled={saving}
                    onChange={next => onStatusChange(issue, next)}
                  />
                </td>
                <td className="mono">
                  {issue.position.x.toFixed(2)}, {issue.position.y.toFixed(2)},{' '}
                  {issue.position.z.toFixed(2)}
                </td>
                <td className="mono">{formatTime(issue.createdAt)}</td>
                <td className="col-action">
                  <button
                    className="btn btn--small btn--danger"
                    disabled={saving}
                    onClick={() => onDelete(issue)}
                  >
                    删除
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
