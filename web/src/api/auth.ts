import { request } from './client';
import { saveSession, type Session } from '../session';

export async function login(username: string, password: string): Promise<Session> {
  const session = await request<Session>('/api/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  });
  saveSession(session);
  return session;
}
