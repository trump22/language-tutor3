// Thêm chữ "type" vào trước ReactNode và xóa "React" không dùng đến
import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import api from '../api/axios';

interface User {
  id: string | number;
  email: string;
  name?: string;
  role: 'STUDENT' | 'ADMIN';
  phoneNumber?: string;
  address?: string;
  dateOfBirth?: string;
  languagePreference?: string;
  skillLevel?: string;
  learningGoal?: string;
}

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (userData: User, token: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

function hasExpired(token: string) {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return typeof payload.exp !== 'number' || payload.exp * 1000 <= Date.now();
  } catch {
    return true;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const initAuth = () => {
      const token = localStorage.getItem('token');
      const savedUser = localStorage.getItem('user');

      if (token && savedUser && !hasExpired(token)) {
        try {
          setUser(JSON.parse(savedUser));
          console.log("Hệ thống đã sẵn sàng với API:", api.defaults.baseURL);
        } catch (e) {
          logout();
        }
      } else if (token || savedUser) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
      }
      setLoading(false);
    };
    initAuth();
  }, []);

  const login = (userData: User, token: string) => {
    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(userData));
    setUser(userData);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, loading, login, logout }}>
      {!loading && children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) throw new Error('useAuth must be used within an AuthProvider');
  return context;
}
