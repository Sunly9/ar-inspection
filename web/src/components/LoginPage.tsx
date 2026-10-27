import { useState } from 'react';
import { ApiError } from '../api/client';
import { login } from '../api/auth';
import type { Session } from '../session';

interface Props {
  onSuccess: (session: Session) => void;
}

export function LoginPage({ onSuccess }: Props) {
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('admin123');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (busy) return;

    setBusy(true);
    setError(null);
    try {
      onSuccess(await login(username.trim(), password));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : '登录失败');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="login">
      <form className="login__card" onSubmit={submit}>
        <h1>巡检问题管理</h1>
        <p className="login__subtitle">请使用巡检账号登录</p>

        <label className="login__label">
          账号
          <input
            className="login__input"
            value={username}
            autoComplete="username"
            onChange={e => setUsername(e.target.value)}
          />
        </label>

        <label className="login__label">
          密码
          <input
            className="login__input"
            type="password"
            value={password}
            autoComplete="current-password"
            onChange={e => setPassword(e.target.value)}
          />
        </label>

        {error ? <p className="login__error">⚠ {error}</p> : null}

        <button className="btn login__submit" type="submit" disabled={busy}>
          {busy ? '登录中…' : '登录'}
        </button>

        <div className="login__hint">
          <div>演示账号（角色不同，权限不同）：</div>
          <div>
            管理员 <code>admin / admin123</code>　·　巡检员 <code>inspector / inspector123</code>
          </div>
        </div>
      </form>
    </div>
  );
}
