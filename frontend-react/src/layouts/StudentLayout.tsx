import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useState, useEffect } from 'react';

export default function StudentLayout() {
  const { user, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const [isDarkMode, setIsDarkMode] = useState(() => {
    return localStorage.getItem('theme') === 'dark' || 
           (!('theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches);
  });

  useEffect(() => {
    const root = window.document.documentElement;
    if (isDarkMode) {
      root.classList.add('dark');
      localStorage.setItem('theme', 'dark');
    } else {
      root.classList.remove('dark');
      localStorage.setItem('theme', 'light');
    }
  }, [isDarkMode]);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div data-testid="student-layout" className="flex min-h-screen bg-background-light dark:bg-background-dark text-slate-900 dark:text-slate-100 font-sans transition-colors duration-300">
      
      {/* --- SIDEBAR NAVIGATION --- */}
      <aside className="w-72 bg-white dark:bg-slate-900 border-r border-slate-200 dark:border-slate-800 flex flex-col fixed h-full z-30">
        <div className="p-6 flex items-center gap-3">
          <div className="w-10 h-10 bg-primary rounded-xl flex items-center justify-center text-white shadow-lg shadow-primary/20">
            <span className="material-symbols-outlined">translate</span>
          </div>
          <div>
            <h1 className="text-slate-900 dark:text-white font-black text-lg leading-tight tracking-tighter">LinguaConnect</h1>
            <p className="text-slate-500 text-[10px] font-bold uppercase tracking-widest">Gia sư AI</p>
          </div>
        </div>

        <nav className="flex-1 px-4 space-y-1 mt-4">
          <NavItem icon="dashboard" label="Tổng quan" to="/dashboard" active={location.pathname === '/dashboard'} />
          <NavItem icon="auto_graph" label="Cố vấn học tập" to="/coach" active={location.pathname === '/coach'} />
          <NavItem icon="book_5" label="Khóa học" to="/courses" active={location.pathname.startsWith('/courses')} />
          <NavItem icon="smart_toy" label="Luyện với AI" to="/chat" active={location.pathname === '/chat'} />
         <NavItem icon="mic" label="Phát âm" to="/pronunciation" active={location.pathname === '/pronunciation'} />
          <NavItem icon="settings" label="Cài đặt" to="/settings" active={location.pathname === '/settings'} />
        </nav>

        {/* Profile & Theme Toggle */}
        <div className="p-4 mt-auto space-y-3">
          <button 
            onClick={() => setIsDarkMode(!isDarkMode)}
            className="w-full flex items-center gap-3 px-4 py-2 rounded-xl text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors text-sm font-bold"
          >
            <span className="material-symbols-outlined">{isDarkMode ? 'light_mode' : 'dark_mode'}</span>
            {isDarkMode ? 'Chế độ sáng' : 'Chế độ tối'}
          </button>

          <div className="bg-primary/5 dark:bg-primary/10 rounded-2xl p-4 border border-primary/10">
            <div className="flex items-center gap-3 mb-3">
              <div className="w-10 h-10 rounded-full bg-blue-100 text-primary flex items-center justify-center font-black text-sm">
                {user?.name?.charAt(0) || 'U'}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-bold text-slate-900 dark:text-white truncate">{user?.name || 'Học viên'}</p>
                <p className="text-[10px] text-slate-500 uppercase font-bold tracking-tighter">Gói học viên</p>
              </div>
            </div>
            <button onClick={handleLogout} className="w-full py-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-[10px] font-black uppercase text-red-500 hover:bg-red-50 transition-colors">
              Đăng xuất
            </button>
          </div>
        </div>
      </aside>

      {/* --- MAIN CONTENT AREA --- */}
      <main className="flex-1 ml-72 p-8">
        <Outlet />
      </main>
    </div>
  );
}

function NavItem({ icon, label, to, active }: { icon: string, label: string, to: string, active: boolean }) {
  return (
    <Link to={to} className={`flex items-center gap-3 px-4 py-3 rounded-xl transition-all ${
      active 
        ? "bg-primary/10 text-primary shadow-xs" 
        : "text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800"
    }`}>
      <span className={`material-symbols-outlined text-[22px] ${active ? 'fill-1' : ''}`}>{icon}</span>
      <span className="font-bold text-sm tracking-tight">{label}</span>
    </Link>
  );
}
