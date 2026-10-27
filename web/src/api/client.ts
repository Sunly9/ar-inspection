import { loadSession } from '../session';

/** 后端地址：默认本机，可用 VITE_API_BASE 覆盖 */
export const API_BASE =
  (import.meta.env.VITE_API_BASE as string | undefined) ?? 'http://127.0.0.1:8080';

export class ApiError extends Error {
  status?: number;

  constructor(message: string, status?: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

/**
 * 统一请求：自动带 Authorization，拆开 {"data": ...} 信封，
 * 非 2xx 时抛出带后端 message 的 ApiError（401 由调用方处理登录态）。
 */
export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(init?.headers as Record<string, string> | undefined),
  };

  const session = loadSession();
  if (session?.token) {
    headers.Authorization = `Bearer ${session.token}`;
  }

  let res: Response;
  try {
    res = await fetch(`${API_BASE}${path}`, { ...init, headers });
  } catch {
    throw new ApiError(`无法连接后端，请确认 Go 服务已启动（${API_BASE}）`);
  }

  const text = await res.text();
  let body: unknown = undefined;
  if (text) {
    try {
      body = JSON.parse(text) as unknown;
    } catch {
      body = undefined;
    }
  }

  if (!res.ok) {
    const message = (body as { error?: { message?: string } } | undefined)?.error?.message;
    throw new ApiError(message ?? `请求失败（HTTP ${res.status}）`, res.status);
  }

  return (body as { data: T }).data;
}
