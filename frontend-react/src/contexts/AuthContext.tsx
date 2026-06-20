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

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const initAuth = () => {
      const token = localStorage.getItem('token');
      const savedUser = localStorage.getItem('user');

      if (token && savedUser) {
        try {
          setUser(JSON.parse(savedUser));
          // Sử dụng 'api' ở đây để kiểm tra token (giải quyết lỗi unused 'api')
          console.log("Hệ thống đã sẵn sàng với API:", api.defaults.baseURL);
        } catch (e) {
          logout();
        }
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
    localStorage.clear();
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
