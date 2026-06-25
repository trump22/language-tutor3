import { useState, useEffect } from 'react';
import type { FormEvent, ReactNode } from 'react';
import api from '../api/axios';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
} from 'chart.js';

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend, Filler);

// --- INTERFACES ---
interface ScoreRecord {
  id: string;
  score: number;
  totalQuestions: number;
  createdAt: string;
  user: {
    id: number;
    name: string;
    email: string;
    role: string;
    phoneNumber?: string;
    address?: string;
    dateOfBirth?: string;
    gender?: string;
    languagePreference?: string;
    skillLevel?: string;
    learningGoal?: string;
  };
  lesson: { title: string; course: { title: string } };
}

interface DashboardStats {
  totalCourses: number;
  totalLessons: number;
  totalUsers: number;
  avgScore: number;
  recentScores: ScoreRecord[];
  growthStats?: { date: string; count: number }[];
}

export default function AdminDashboard() {
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<any>(null);
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [chartData, setChartData] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [aiInsight, setAiInsight] = useState('');
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [isSearchFocused, setIsSearchFocused] = useState(false);

  const fetchData = async () => {
    try {
      const res = await api.get('/admin/analytics/dashboard');
      const dashboardData = res.data.data;
      setStats(dashboardData);

      if (dashboardData.growthStats) {
        setChartData({
          labels: dashboardData.growthStats.map((d: any) => d.date),
          datasets: [{
            fill: true,
            label: 'Người dùng mới',
            data: dashboardData.growthStats.map((d: any) => d.count),
            borderColor: '#1978e5',
            backgroundColor: 'rgba(25, 120, 229, 0.1)',
            tension: 0.4,
            borderWidth: 3,
            pointRadius: 0,
            pointHoverRadius: 6,
            pointBackgroundColor: '#1978e5',
          }]
        });
      }
    } catch (error) {
      console.error("Lỗi tải dữ liệu Dashboard:", error);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleAskAI = async () => {
    if (!stats || stats.recentScores.length === 0) return;
    setIsAnalyzing(true);
    try {
      await new Promise(resolve => setTimeout(resolve, 1500));
      setAiInsight(`Dựa trên dữ liệu: Điểm trung bình là ${stats.avgScore}%. Hệ thống ghi nhận sự ổn định trong lộ trình học của các học viên.`);
    } catch (error) {
      setAiInsight("Lỗi kết nối AI.");
    } finally {
      setIsAnalyzing(false);
    }
  };

  // --- LOGIC LỌC CHỈ HIỂN THỊ STUDENT (KHÔNG HIỂN THỊ ADMIN) ---
  const uniqueStudents = Array.from(
    new Set(stats?.recentScores?.map(score => JSON.stringify(score.user)) || [])
  ).map(str => JSON.parse(str as string))
   .filter((u: any) => u.role !== 'ADMIN'); // LỌC ADMIN RA KHỎI DANH SÁCH

  const searchResults = uniqueStudents.filter(student => 
    (student.name.toLowerCase().includes(searchTerm.toLowerCase()) || 
    student.email.toLowerCase().includes(searchTerm.toLowerCase()))
  );

  if (isLoading) return <div className="flex h-64 items-center justify-center text-blue-600 font-bold animate-pulse">Đang tải Dashboard...</div>;

  return (
    <div className="p-8 space-y-8 animate-in fade-in duration-500 text-left">
      <header className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Quản lý hệ thống</h2>
          <p className="text-sm text-slate-500">Quản lý người dùng và theo dõi hiệu suất hệ thống.</p>
        </div>
        
        <div className="flex items-center gap-4">
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-400 text-sm">search</span>
            <input 
              value={searchTerm} 
              onChange={(e) => setSearchTerm(e.target.value)} 
              onFocus={() => setIsSearchFocused(true)} 
              onBlur={() => setTimeout(() => setIsSearchFocused(false), 200)} 
              className="pl-10 pr-4 py-2 bg-white dark:bg-[#1e293b] border border-slate-200 dark:border-slate-700 rounded-lg text-sm w-64 focus:ring-2 focus:ring-primary outline-none" 
              placeholder="Tìm học viên..." 
              type="text" 
            />
            {isSearchFocused && searchTerm && (
              <div className="absolute top-full mt-2 left-0 w-full bg-white dark:bg-[#1e293b] rounded-xl shadow-2xl border border-slate-200 dark:border-slate-700 overflow-hidden z-50">
                {searchResults.length > 0 ? (
                  <ul className="max-h-64 overflow-y-auto py-2">
                    {searchResults.map((student, idx) => (
                      <li key={idx} onClick={() => setEditingUser(student)} className="px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-700 cursor-pointer flex items-center gap-3 border-b last:border-0 border-slate-100 dark:border-slate-700/50 transition-colors text-left">
                        <div className="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center font-bold text-xs text-primary shrink-0">{student.name.charAt(0).toUpperCase()}</div>
                        <div className="min-w-0"><p className="text-sm font-bold truncate">{student.name}</p><p className="text-[10px] text-slate-500 truncate">{student.email}</p></div>
                      </li>
                    ))}
                  </ul>
                ) : (<div className="p-4 text-center text-sm text-slate-500">Không tìm thấy học viên</div>)}
              </div>
            )}
          </div>
          {/* ĐỔI THÀNH ADD USER */}
          <button data-testid="admin-add-user-open" onClick={() => setIsAddModalOpen(true)} className="px-4 py-2 bg-primary text-white text-sm font-bold rounded-lg hover:bg-primary/90 transition-all flex items-center gap-2">
            <span className="material-symbols-outlined text-sm">person_add</span> Thêm người dùng
          </button>
        </div>
      </header>

      {/* METRIC CARDS */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <MetricCard title="Tổng học viên" icon="groups" value={stats?.totalUsers || 0} trend="+12.4%" color="text-blue-500" />
        <MetricCard title="Điểm trung bình" icon="analytics" value={`${stats?.avgScore || 0}`} suffix="/100" trend="+3.1%" color="text-emerald-500" />
        <MetricCard title="Bài học AI" icon="menu_book" value={stats?.totalLessons || 0} trend="Ổn định" color="text-purple-500" />
        <MetricCard title="Khóa học" icon="school" value={stats?.totalCourses || 0} trend="-2.1%" color="text-orange-500" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 bg-white dark:bg-[#1e293b] p-6 rounded-xl border border-slate-200 dark:border-slate-800">
          <h3 className="font-bold text-lg mb-6">Hoạt động học tập</h3>
          <div className="h-75">
            {chartData && <Line data={chartData} options={{ responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true }, x: { grid: { display: false } } } }} />}
          </div>
        </div>

        <div className="bg-white dark:bg-[#1e293b] p-6 rounded-xl border border-slate-200 dark:border-slate-800 flex flex-col">
          <h3 className="font-bold text-lg mb-1">Phân tích chấm điểm AI</h3>
          <p className="text-sm text-slate-500 mb-6 italic">Phân tích hiệu suất</p>
          <div className="flex-1 flex flex-col justify-center items-center text-center space-y-4">
            <span className="material-symbols-outlined text-5xl text-slate-200 dark:text-slate-700">insights</span>
            <button onClick={handleAskAI} disabled={isAnalyzing} className="w-full py-3 bg-slate-50 dark:bg-[#0f172a] hover:bg-slate-100 text-primary font-bold rounded-xl transition-all border border-slate-200 dark:border-slate-700">
              {isAnalyzing ? 'Đang xử lý...' : 'Tạo báo cáo AI'}
            </button>
          </div>
          {aiInsight && <div className="mt-4 p-4 bg-primary/5 rounded-lg border border-primary/10 text-xs leading-relaxed text-left">{aiInsight}</div>}
        </div>
      </div>

      {/* DANH SÁCH HỌC VIÊN - ĐÃ LỌC BỎ ADMIN */}
      <div className="bg-white dark:bg-[#1e293b] rounded-xl border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div className="p-6 border-b border-slate-200 dark:border-slate-800 flex justify-between items-center">
            <h3 className="font-bold text-lg">Phiên học gần đây</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-left">
            <thead>
              <tr className="bg-slate-50 dark:bg-[#0f172a] text-[10px] font-bold uppercase text-slate-400">
                <th className="px-6 py-4">Học viên</th><th className="px-6 py-4">Khóa học</th><th className="px-6 py-4">Bài học</th><th className="px-6 py-4">Điểm AI</th><th className="px-6 py-4 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {stats?.recentScores
                .filter(score => score.user.role !== 'ADMIN') // CHỈ HIỂN THỊ STUDENT
                .map((score) => {
                const percentage = Math.round((score.score / score.totalQuestions) * 100);
                return (
                  <tr key={score.id} className="hover:bg-slate-50/50 dark:hover:bg-slate-800/30 transition-colors">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3 text-left">
                        <div className="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center font-bold text-xs text-primary">{score.user.name.charAt(0).toUpperCase()}</div>
                        <div><p className="text-sm font-bold">{score.user.name}</p><p className="text-[10px] text-slate-500">{score.user.email}</p></div>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-sm font-medium">{score.lesson.course.title}</td>
                    <td className="px-6 py-4 text-sm text-slate-500">{score.lesson.title}</td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-bold">{percentage}%</span>
                        <div className="w-16 h-1.5 bg-slate-100 dark:bg-[#0f172a] rounded-full overflow-hidden"><div className={`h-full ${percentage >= 80 ? 'bg-green-500' : 'bg-amber-500'}`} style={{ width: `${percentage}%` }}></div></div>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <button onClick={() => setEditingUser(score.user)} className="p-2 text-slate-400 hover:text-orange-500 transition-all"><span className="material-symbols-outlined text-[18px]">edit</span></button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* MODALS */}
      {isAddModalOpen && <AddUserModal onClose={() => setIsAddModalOpen(false)} onSuccess={fetchData} />}
      {editingUser && <EditUserModal user={editingUser} onClose={() => setEditingUser(null)} onSuccess={() => { setEditingUser(null); fetchData(); }} />}
    </div>
  );
}

// --- SUB-COMPONENTS ---
function MetricCard({ title, icon, value, suffix = "", trend, color }: any) {
  return (
    <div className="bg-white dark:bg-[#1e293b] p-6 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm transition-all hover:shadow-md text-left">
      <div className="flex items-center justify-between mb-4"><p className="text-sm font-medium text-slate-500">{title}</p><span className={`material-symbols-outlined ${color}`}>{icon}</span></div>
      <p className="text-3xl font-bold">{value}<span className="text-lg text-slate-400 font-medium">{suffix}</span></p>
      <div className="text-[10px] font-bold text-green-600 flex items-center gap-1 mt-2"><span className="material-symbols-outlined text-[12px]">trending_up</span>{trend} <span className="text-slate-400 font-normal ml-1">so với tháng trước</span></div>
    </div>
  );
}

// --- MODAL CHỈNH SỬA: THÊM LỰA CHỌN ROLE ---
function EditUserModal({ user, onClose, onSuccess }: any) {
  const [formData, setFormData] = useState({ 
    name: user.name || '', 
    email: user.email || '', 
    role: user.role || 'STUDENT', // Thêm role
    languagePreference: user.languagePreference || 'en' 
  });
  const [isSaving, setIsSaving] = useState(false);

  const handleSave = async () => {
    if (!window.confirm("Xác nhận lưu các thay đổi này?")) return;
    setIsSaving(true);
    try { 
      await api.put(`/admin/users/${Number(user.id)}`, formData); 
      onSuccess(); 
    } catch (e) { alert('Lỗi khi lưu.'); } finally { setIsSaving(false); }
  };

  const handleDelete = async () => {
    if (!window.confirm("Xóa vĩnh viễn tài khoản này?")) return;
    try { await api.delete(`/admin/users/${Number(user.id)}`); onSuccess(); } catch (e) { alert('Lỗi khi xóa.'); }
  };

  return (
    <div className="fixed inset-0 z-60 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4 overflow-y-auto">
      <div className="bg-white dark:bg-[#1e293b] w-full max-w-2xl rounded-3xl shadow-2xl overflow-hidden border dark:border-slate-800 animate-in zoom-in-95">
        <div className="p-8 border-b dark:border-slate-800 flex justify-between items-center"><h2 className="text-2xl font-black">Chi tiết tài khoản</h2><button onClick={onClose} className="text-slate-400 hover:text-red-500 transition-colors"><span className="material-symbols-outlined">close</span></button></div>
        <div className="p-8 space-y-6 text-left">
          <div className="grid grid-cols-2 gap-6 p-6 bg-slate-50 dark:bg-[#0f172a] rounded-2xl border dark:border-slate-800">
            <div className="space-y-2 col-span-2 md:col-span-1"><label className="text-xs font-bold text-slate-500 uppercase">Họ và tên</label><input value={formData.name} onChange={e => setFormData({...formData, name: e.target.value})} className="w-full p-3 bg-white dark:bg-[#1e293b] border dark:border-slate-700 rounded-xl text-sm outline-none" /></div>
            <div className="space-y-2 col-span-2 md:col-span-1"><label className="text-xs font-bold text-slate-500 uppercase">Email</label><input value={formData.email} onChange={e => setFormData({...formData, email: e.target.value})} className="w-full p-3 bg-white dark:bg-[#1e293b] border dark:border-slate-700 rounded-xl text-sm outline-none" /></div>
            
            {/* THÊM CHỈNH SỬA ROLE */}
            <div className="space-y-2 col-span-2 md:col-span-1">
                <label className="text-xs font-bold text-slate-500 uppercase">Vai trò hệ thống</label>
                <select value={formData.role} onChange={e => setFormData({...formData, role: e.target.value})} className="w-full p-3 bg-white dark:bg-[#1e293b] border dark:border-slate-700 rounded-xl text-sm outline-none">
                    <option value="STUDENT">Học viên</option>
                    <option value="ADMIN">Quản trị viên</option>
                </select>
            </div>

            <div className="col-span-2 flex justify-end gap-3 mt-4"><button onClick={onClose} className="px-6 py-2.5 text-sm font-bold text-slate-500">Hủy</button><button onClick={handleSave} disabled={isSaving} className="px-6 py-2.5 bg-orange-600 text-white text-sm font-bold rounded-xl shadow-lg">{isSaving ? 'Đang lưu...' : 'Lưu thay đổi'}</button></div>
          </div>
          <div className="pt-6 border-t dark:border-slate-800 flex justify-between items-center"><p className="text-xs text-slate-500 italic">Lưu ý: Nếu đổi sang ADMIN, người dùng này sẽ không hiện trong danh sách này nữa.</p><button onClick={handleDelete} className="px-5 py-2 border border-red-200 text-red-500 text-xs font-bold rounded-lg hover:bg-red-50 transition-colors">Xóa tài khoản</button></div>
        </div>
      </div>
    </div>
  );
}

// --- MODAL THÊM MỚI: THÊM CHỌN ROLE ---
function AddUserModal({ onClose, onSuccess }: any) {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    password: '',
    phoneNumber: '',
    dateOfBirth: '',
    gender: '',
    address: '',
    role: 'STUDENT',
    languagePreference: 'EN',
    skillLevel: 'Beginner',
    learningGoal: '',
  });
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState('');

  const updateField = (field: keyof typeof formData, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    setError('');

    try {
      await api.post('/admin/users', {
        name: formData.name.trim(),
        email: formData.email.trim(),
        password: formData.password,
        role: formData.role,
        phoneNumber: formData.phoneNumber.trim() || null,
        dateOfBirth: formData.dateOfBirth || null,
        gender: formData.gender || null,
        address: formData.address.trim() || null,
        languagePreference: formData.languagePreference,
        skillLevel: formData.skillLevel,
        learningGoal: formData.learningGoal.trim() || null,
      });
      onSuccess();
      onClose();
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Lỗi khi tạo tài khoản.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div data-testid="admin-add-user-modal" className="fixed inset-0 z-60 flex items-center justify-center bg-slate-950/55 backdrop-blur-sm p-4">
      <div className="w-full max-w-2xl bg-white dark:bg-slate-900 rounded-xl shadow-2xl border border-slate-200 dark:border-slate-800 animate-in zoom-in-95 overflow-hidden text-left">
        <div className="px-8 pt-8 pb-5 flex items-start justify-between">
          <div>
            <h3 className="text-2xl font-black tracking-tight text-slate-900 dark:text-white">Thêm người dùng mới</h3>
            <p className="mt-3 text-sm leading-relaxed text-slate-500 max-w-md">
              Tạo hồ sơ người dùng mới để bắt đầu quản lý lộ trình học tập và các cấp mốc đào tạo.
            </p>
          </div>
          <button onClick={onClose} className="size-9 rounded-full flex items-center justify-center text-slate-400 hover:text-slate-900 hover:bg-slate-100 dark:hover:bg-slate-800 dark:hover:text-white transition-colors">
            <span className="material-symbols-outlined text-xl">close</span>
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="px-8 pb-6 grid grid-cols-1 md:grid-cols-2 gap-x-5 gap-y-4">
            <ModalField label="Họ và tên">
              <input data-testid="admin-user-name" required value={formData.name} onChange={(e) => updateField('name', e.target.value)} className="modal-input" placeholder="VD: Eleanor Rigby" />
            </ModalField>

            <ModalField label="Email">
              <input data-testid="admin-user-email" required type="email" value={formData.email} onChange={(e) => updateField('email', e.target.value)} className="modal-input" placeholder="student@example.com" />
            </ModalField>

            <ModalField label="Mật khẩu">
              <input data-testid="admin-user-password" required type="password" value={formData.password} onChange={(e) => updateField('password', e.target.value)} className="modal-input" placeholder="••••••••" />
            </ModalField>

            <ModalField label="Số điện thoại">
              <input data-testid="admin-user-phone" value={formData.phoneNumber} onChange={(e) => updateField('phoneNumber', e.target.value)} className="modal-input" placeholder="0901234567" />
            </ModalField>

            <ModalField label="Ngày sinh">
              <input data-testid="admin-user-dob" type="date" value={formData.dateOfBirth} onChange={(e) => updateField('dateOfBirth', e.target.value)} className="modal-input" />
            </ModalField>

            <ModalField label="Giới tính">
              <select data-testid="admin-user-gender" value={formData.gender} onChange={(e) => updateField('gender', e.target.value)} className="modal-input">
                <option value="">Chọn giới tính</option>
                <option value="MALE">Nam</option>
                <option value="FEMALE">Nữ</option>
                <option value="OTHER">Khác</option>
              </select>
            </ModalField>

            <ModalField label="Địa chỉ" className="md:col-span-2">
              <input data-testid="admin-user-address" value={formData.address} onChange={(e) => updateField('address', e.target.value)} className="modal-input" placeholder="Nhập địa chỉ của bạn" />
            </ModalField>

            <ModalField label="Chức vụ">
              <select data-testid="admin-user-role" value={formData.role} onChange={(e) => updateField('role', e.target.value)} className="modal-input">
                <option value="STUDENT">Học viên</option>
                <option value="ADMIN">Quản trị viên</option>
              </select>
            </ModalField>

            <ModalField label="Ngôn ngữ">
              <select data-testid="admin-user-language" value={formData.languagePreference} onChange={(e) => updateField('languagePreference', e.target.value)} className="modal-input">
                <option value="EN">Tiếng Anh</option>
                <option value="ZH">Tiếng Trung</option>
              </select>
            </ModalField>

            <ModalField label="Trình độ">
              <select data-testid="admin-user-level" value={formData.skillLevel} onChange={(e) => updateField('skillLevel', e.target.value)} className="modal-input">
                <option value="Beginner">Cơ bản</option>
                <option value="Intermediate">Trung cấp</option>
                <option value="Advanced">Nâng cao</option>
              </select>
            </ModalField>

            <ModalField label="Mục tiêu học tập" className="md:col-span-2">
              <input data-testid="admin-user-goal" value={formData.learningGoal} onChange={(e) => updateField('learningGoal', e.target.value)} className="modal-input" placeholder="VD: Giao tiếp công việc, luyện phát âm, TOEIC..." />
            </ModalField>

            {error && (
              <div data-testid="admin-add-user-error" className="md:col-span-2 rounded-xl bg-red-50 dark:bg-red-950/20 border border-red-100 dark:border-red-900/50 text-red-600 dark:text-red-300 px-4 py-3 text-sm font-semibold">
                {error}
              </div>
            )}
          </div>

          <div className="px-8 py-5 bg-slate-50 dark:bg-slate-950 border-t border-slate-100 dark:border-slate-800 flex items-center justify-end gap-4">
            <button type="button" onClick={onClose} className="px-6 py-3 rounded-xl text-sm font-black text-slate-500 hover:text-slate-900 dark:hover:text-white transition-colors">
              Hủy bỏ
            </button>
            <button data-testid="admin-add-user-submit" type="submit" disabled={isSaving} className="px-7 py-3 rounded-xl bg-primary text-white text-sm font-black shadow-lg shadow-primary/25 hover:bg-primary/90 disabled:opacity-60 transition-all">
              {isSaving ? 'Đang lưu...' : 'Lưu người dùng'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

function ModalField({ label, className = '', children }: { label: string; className?: string; children: ReactNode }) {
  return (
    <label className={`block space-y-2 ${className}`}>
      <span className="text-[11px] font-black uppercase tracking-wide text-slate-500 dark:text-slate-400">{label}</span>
      {children}
    </label>
  );
}
