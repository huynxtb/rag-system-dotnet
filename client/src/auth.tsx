import { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { api, LoginResponse } from './api';

interface AuthState {
  token: string | null;
  email: string | null;
  roles: string[];
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'));
  const [email, setEmail] = useState<string | null>(localStorage.getItem('email'));
  const [roles, setRoles] = useState<string[]>(JSON.parse(localStorage.getItem('roles') || '[]'));

  useEffect(() => {
    if (token) localStorage.setItem('token', token);
    else localStorage.removeItem('token');
  }, [token]);

  const login = async (e: string, p: string) => {
    const { data } = await api.post<LoginResponse>('/auth/login', { email: e, password: p });
    setToken(data.token);
    setEmail(data.email);
    setRoles(data.roles);
    localStorage.setItem('email', data.email);
    localStorage.setItem('roles', JSON.stringify(data.roles));
  };

  const logout = () => {
    setToken(null);
    setEmail(null);
    setRoles([]);
    localStorage.removeItem('token');
    localStorage.removeItem('email');
    localStorage.removeItem('roles');
  };

  return (
    <AuthContext.Provider value={{ token, email, roles, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
};
