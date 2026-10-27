export type Role = 'inspector' | 'admin';

export interface AuthUser {
  username: string;
  displayName: string;
  role: Role;
}

export interface Session {
  token: string;
  user: AuthUser;
}

const KEY = 'inspection.session';

export function loadSession(): Session | null {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Session;
    if (!parsed?.token || !parsed?.user?.role) return null;
    return parsed;
  } catch {
    return null;
  }
}

export function saveSession(session: Session): void {
  localStorage.setItem(KEY, JSON.stringify(session));
}

export function clearSession(): void {
  localStorage.removeItem(KEY);
}
