import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useState, useEffect } from 'react';

export default function AdminLayout() {
  const { user, logout } = useAuth(); 
  const location = useLocation();
  const navigate = useNavigate(); 

  const [isDarkMode, setIsDarkMode] = useState(() => {
    return localStorage.getItem('theme') === 'dark' || 
           (!('theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches);
  });

  useEffect(() => {
    if (isDarkMode) {
      document.documentElement.classList.add('dark');
      localStorage.setItem('theme', 'dark');
    } else {
      document.documentElement.classList.remove('dark');
      localStorage.setItem('theme', 'light');
    }
  }, [isDarkMode]);

  const handleLogout = async () => {
    try {
      await logout();
      navigate('/login'); 
    } catch (error) {
      console.error("Lỗi đăng xuất:", error);
    }
  };

  return (
    <div data-testid="admin-layout" className="flex h-screen overflow-hidden bg-background-light dark:bg-background-dark text-slate-900 dark:text-slate-100 font-sans transition-colors duration-300">
      
      {/* SIDEBAR */}
      <aside className="w-64 bg-white dark:bg-[#1e293b] border-r border-slate-200 dark:border-slate-800 flex flex-col z-20">
        <div className="p-6 flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg bg-primary flex items-center justify-center text-white shadow-lg shadow-primary/20">
            <span className="material-symbols-outlined">translate</span>
          </div>
          <div>
            <h1 className="text-lg font-bold tracking-tight text-slate-900 dark:text-white leading-none">LinguistAI</h1>
            <p className="text-[10px] text-slate-500 dark:text-slate-400 mt-1 font-bold uppercase tracking-widest">Quản trị</p>
          </div>
        </div>

        <nav className="flex-1 px-4 py-4 space-y-1">
          <SidebarItem icon="dashboard" label="Tổng quan" to="/admin" active={location.pathname === '/admin'} />
          <SidebarItem icon="magic_button" label="Tạo bài tập" to="/admin/ai-tools" active={location.pathname === '/admin/ai-tools'} />
          
          {/* <--- ĐÃ THÊM MENU AUDIO STUDIO ---> */}
          <SidebarItem icon="headphones" label="Phòng nghe" to="/admin/listening-creator" active={location.pathname === '/admin/listening-creator'} />
          
          <SidebarItem icon="psychology" label="Cấu hình AI" to="/admin/ai-config" active={location.pathname === '/admin/ai-config'} />
          <SidebarItem icon="analytics" label="Báo cáo" to="#" active={false} />
          
          <div className="pt-4 pb-2">
            <p className="px-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest">Nền tảng</p>
          </div>
          <SidebarItem icon="settings" label="Cài đặt" to="#" active={false} />
        </nav>
        
        <div className="p-4 border-t border-slate-200 dark:border-slate-800 space-y-3">
          <button 
            onClick={() => setIsDarkMode(!isDarkMode)}
            className="w-full flex items-center gap-3 px-3 py-2 rounded-xl text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
          >
            <span className="material-symbols-outlined">{isDarkMode ? 'light_mode' : 'dark_mode'}</span>
            <span className="text-sm font-bold">{isDarkMode ? 'Chế độ sáng' : 'Chế độ tối'}</span>
          </button>

          <div className="flex items-center justify-between p-2 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-100 dark:border-slate-700">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-full bg-primary/10 text-primary flex items-center justify-center font-bold text-xs">
                {user?.name?.charAt(0) || 'A'}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-xs font-bold truncate">{user?.name || 'Admin'}</p>
              </div>
            </div>
            <button data-testid="logout-submit" onClick={handleLogout} className="text-slate-400 hover:text-red-500 p-1" title="Đăng xuất">
              <span className="material-symbols-outlined text-sm">logout</span>
            </button>
          </div>
        </div>
      </aside>

      <main className="flex-1 flex flex-col overflow-y-auto">
        <Outlet />
      </main>
    </div>
  );
}

function SidebarItem({ icon, label, to, active }: { icon: string, label: string, to: string, active: boolean }) {
  return (
    <Link 
      to={to} 
      className={`flex items-center gap-3 px-3 py-2.5 rounded-xl transition-all ${
        active 
          ? "bg-primary text-white shadow-lg shadow-primary/30" 
          : "text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-primary"
      }`}
    >
      <span className={`material-symbols-outlined text-[20px] ${active ? 'fill-1' : ''}`}>{icon}</span>
      <span className={`text-sm font-bold ${active ? '' : 'tracking-tight'}`}>{label}</span>
    </Link>
  );
}
