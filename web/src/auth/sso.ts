import { AuthUser } from './AuthContext';

interface SsoTokenResponse {
  token: string;
}

interface MeResponse {
  userId: string;
  email: string;
  displayName: string;
  roles: string[];
}

export async function loginViaSso(): Promise<AuthUser> {
  const resp = await fetch('/sso-login', { credentials: 'include' });
  if (!resp.ok) {
    throw new Error('SSO login failed: ' + resp.status);
  }
  const data = (await resp.json()) as SsoTokenResponse;
  if (!data.token) throw new Error('SSO response did not contain token');

  const meResp = await fetch('/api/auth/me', {
    headers: { Authorization: 'Bearer ' + data.token },
  });
  if (!meResp.ok) {
    throw new Error('Failed to fetch current user: ' + meResp.status);
  }
  const me = (await meResp.json()) as MeResponse;
  return {
    username: me.userId,
    displayName: me.displayName || me.userId,
    email: me.email,
    token: data.token,
  };
}
