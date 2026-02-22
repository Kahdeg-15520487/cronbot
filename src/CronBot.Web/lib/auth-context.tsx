'use client';

import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { api } from './api';

export interface AuthUser {
  id: string;
  username: string;
  email?: string;
  displayName?: string;
  avatarUrl?: string;
}

interface AuthContextType {
  user: AuthUser | null;
  token: string | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<void>;
  register: (username: string, password: string, email?: string, displayName?: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const TOKEN_KEY = 'cronbot_token';
const USER_KEY = 'cronbot_user';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Load token from localStorage on mount
  useEffect(() => {
    const storedToken = localStorage.getItem(TOKEN_KEY);
    const storedUser = localStorage.getItem(USER_KEY);

    if (storedToken && storedUser) {
      try {
        const parsedUser = JSON.parse(storedUser);
        setToken(storedToken);
        setUser(parsedUser);

        // Set default authorization header
        api.defaults.headers.common['Authorization'] = `Bearer ${storedToken}`;

        // Verify token is still valid
        api.get('/auth/me')
          .then((res) => {
            setUser(res.data);
            localStorage.setItem(USER_KEY, JSON.stringify(res.data));
          })
          .catch(() => {
            // Token is invalid, clear everything
            localStorage.removeItem(TOKEN_KEY);
            localStorage.removeItem(USER_KEY);
            delete api.defaults.headers.common['Authorization'];
            setToken(null);
            setUser(null);
          });
      } catch {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
      }
    }

    setIsLoading(false);
  }, []);

  const login = async (username: string, password: string) => {
    const response = await api.post<{ token: string; user: AuthUser }>('/auth/login', {
      username,
      password,
    });

    const { token: newToken, user: newUser } = response.data;

    localStorage.setItem(TOKEN_KEY, newToken);
    localStorage.setItem(USER_KEY, JSON.stringify(newUser));

    api.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;

    setToken(newToken);
    setUser(newUser);
  };

  const register = async (username: string, password: string, email?: string, displayName?: string) => {
    const response = await api.post<{ token: string; user: AuthUser }>('/auth/register', {
      username,
      password,
      email,
      displayName,
    });

    const { token: newToken, user: newUser } = response.data;

    localStorage.setItem(TOKEN_KEY, newToken);
    localStorage.setItem(USER_KEY, JSON.stringify(newUser));

    api.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;

    setToken(newToken);
    setUser(newUser);
  };

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    delete api.defaults.headers.common['Authorization'];
    setToken(null);
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isLoading,
        isAuthenticated: !!user && !!token,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
