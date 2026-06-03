import { createContext, useCallback, useContext, useEffect, useMemo, useState, ReactNode } from 'react';

export interface AuthUser {
  username: string;
  displayName: string;
  email: string;
  token: string;
}

interface AuthContextValue {
  user: AuthUser | null;
  login: (user: AuthUser) => void;
  logout: () => void;
}

const STORAGE_KEY = 'releasehub.auth';

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  });

  useEffect(() => {
    if (user) localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
    else localStorage.removeItem(STORAGE_KEY);
  }, [user]);

  const login = useCallback((u: AuthUser) => setUser(u), []);
  const logout = useCallback(() => setUser(null), []);

  const value = useMemo(() => ({ user, login, logout }), [user, login, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
