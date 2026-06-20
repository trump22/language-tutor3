import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';

// --- LAYOUTS ---
import AdminLayout from './layouts/AdminLayout';
import StudentLayout from './layouts/StudentLayout'; 

// --- PAGES ---
import Home from './pages/Home';
import Login from './pages/Login';
import SignUp from './pages/SignUp';
import ForgotPassword from './pages/ForgotPassword';
import ChatAI from './pages/ChatAI';
import AdminDashboard from './pages/AdminDashboard';
import Dashboard from './pages/Dashboard';
import LearningCoach from './pages/LearningCoach';
import Pronunciation from './pages/Pronunciation';
import Courses from './pages/Courses';
import AdminAITools from './pages/admin/AdminAITools';
import AdminListeningCreator from './pages/admin/AdminListeningCreator'; // <--- ĐÃ THÊM IMPORT
import LessonDetail from './pages/LessonDetail';
import CourseDetail from './pages/CourseDetail';
import AdminAIConfig from './pages/AdminAIConfig';

// --- CONTEXTS ---
import { LanguageProvider } from './contexts/LanguageContext';
import { ThemeProvider } from './contexts/ThemeContext';
import { AuthProvider } from './contexts/AuthContext';

// ==========================================
// 1. TRẠM GÁC BẢO VỆ CHO ADMIN
// ==========================================
const AdminProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const userStr = localStorage.getItem('user');
  if (!userStr) return <Navigate to="/login" replace />;
  try {
    const user = JSON.parse(userStr);
    if (user.role !== 'ADMIN') return <Navigate to="/dashboard" replace />;
    return <>{children}</>;
  } catch (error) {
    return <Navigate to="/login" replace />;
  }
};

// ==========================================
// 2. TRẠM GÁC BẢO VỆ CHO HỌC VIÊN (STUDENT)
// ==========================================
const StudentProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const userStr = localStorage.getItem('user');
  if (!userStr) return <Navigate to="/login" replace />;
  return <>{children}</>;
};

export default function App() {
  return (
    <AuthProvider>
      <ThemeProvider>
        <LanguageProvider>
          <Router>
            <Routes>
              {/* --- CÁC TRANG PUBLIC --- */}
              <Route path="/" element={<Home />} />
              <Route path="/login" element={<Login />} />
              <Route path="/signup" element={<SignUp />} />
              <Route path="/forgot-password" element={<ForgotPassword />} />
              
              {/* --- VÙNG HỌC VIÊN --- */}
              <Route 
                path="/" 
                element={
                  <StudentProtectedRoute>
                    <StudentLayout />
                  </StudentProtectedRoute>
                }
              >
                <Route path="dashboard" element={<Dashboard />} />
                <Route path="coach" element={<LearningCoach />} />
                <Route path="chat" element={<ChatAI />} />
                <Route path="pronunciation" element={<Pronunciation />} />
                <Route path="courses" element={<Courses />} />
                <Route path="courses/:courseId" element={<CourseDetail />} />
                <Route path="lessons/:lessonId" element={<LessonDetail />} />
              </Route>

              {/* --- VÙNG ADMIN --- */}
              <Route 
                path="/admin" 
                element={
                  <AdminProtectedRoute>
                    <AdminLayout />
                  </AdminProtectedRoute>
                }
              >
                <Route index element={<AdminDashboard />} />
                <Route path="ai-config" element={<AdminAIConfig />} />
                <Route path="ai-tools" element={<AdminAITools />} />
                {/* <--- ĐÃ THÊM ROUTE AUDIO STUDIO TẠI ĐÂY ---> */}
                <Route path="listening-creator" element={<AdminListeningCreator />} />
              </Route>

              {/* --- ROUTE MẶC ĐỊNH (404) --- */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </Router>
        </LanguageProvider>
      </ThemeProvider>
    </AuthProvider>
  );
}
