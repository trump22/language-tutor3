import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function Dashboard() {
  const { user } = useAuth();
  const navigate = useNavigate();

  return (
    <div className="max-w-7xl mx-auto space-y-10 animate-in fade-in duration-700">
      
      {/* 1. HEADER / WELCOME SECTION */}
      <header className="flex flex-wrap justify-between items-end gap-4 text-left">
        <div>
          <h2 className="text-4xl font-black text-slate-900 dark:text-white mb-2 tracking-tighter">
            Chào mừng trở lại, {user?.name?.split(' ')[0] || 'Học viên'}!
          </h2>
          <p className="text-slate-500 italic font-medium">
            "Giới hạn ngôn ngữ là giới hạn thế giới của tôi." - Wittgenstein
          </p>
        </div>
        
        <div className="flex items-center gap-3">
          <button className="size-11 rounded-full bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-600 flex items-center justify-center hover:bg-slate-50 transition-colors">
            <span className="material-symbols-outlined">notifications</span>
          </button>
          <div className="px-5 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl flex items-center gap-2 shadow-sm">
            <span className="material-symbols-outlined text-orange-500 fill-1">local_fire_department</span>
            <span className="font-black text-slate-900 dark:text-white">Chuỗi 14 ngày</span>
          </div>
        </div>
      </header>

      <div className="grid grid-cols-12 gap-8">
        
        {/* LEFT COLUMN (8/12) */}
        <div className="col-span-12 lg:col-span-8 space-y-10">
          
          {/* QUICK ACTIONS - ĐÃ TÍCH HỢP ĐIỀU HƯỚNG */}
          <section className="text-left">
            <h3 className="text-lg font-black text-slate-900 dark:text-white mb-4 uppercase tracking-widest opacity-50">Thao tác nhanh</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <ActionButton 
                icon="forum" 
                label="Chat với AI" 
                color="bg-primary text-white shadow-primary/20" 
                onClick={() => navigate('/chat')} 
              />
              <ActionButton 
                icon="mic" 
                label="Luyện phát âm" 
                color="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-800 dark:text-white" 
                onClick={() => navigate('/pronunciation')} 
              />
              <ActionButton 
                icon="play_circle" 
                label="Bài gần nhất" 
                color="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-800 dark:text-white" 
                onClick={() => navigate('/courses')} 
              />
            </div>
          </section>

          {/* LEARNING PROGRESS */}
          <section className="bg-white dark:bg-slate-900 rounded-[2.5rem] p-8 border border-slate-200 dark:border-slate-800 shadow-sm text-left">
            <h3 className="text-xl font-black text-slate-900 dark:text-white mb-8 flex items-center gap-2">
              <span className="size-2 rounded-full bg-primary animate-pulse"></span>
              Tổng quan tiến độ học tập
            </h3>
            <div className="space-y-8">
              <ProgressItem flag="🇬🇧" title="Tiếng Anh" level="Nâng cao" percent={75} />
              <ProgressItem flag="🇨🇳" title="Tiếng Trung" level="Cơ bản" percent={40} />
            </div>
          </section>

          {/* RECOMMENDED SCENARIOS */}
          <section className="text-left">
            <div className="flex justify-between items-center mb-6">
              <h3 className="text-xl font-black text-slate-900 dark:text-white">Gợi ý cho bạn</h3>
              <button onClick={() => navigate('/courses')} className="text-primary text-sm font-black uppercase tracking-tighter hover:underline">Xem tất cả</button>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <ScenarioCard 
                category="Tiếng Anh công việc" 
                title="Điều phối cuộc họp" 
                desc="Luyện thành ngữ và cách diễn đạt chuyên nghiệp."
                img="https://images.unsplash.com/photo-1542744173-8e7e53415bb0?q=80&w=500"
              />
              <ScenarioCard 
                category="Văn hóa & Ẩm thực" 
                title="Gọi món Dim Sum" 
                desc="Học tên món ăn và phép lịch sự bằng tiếng Trung."
                img="https://images.unsplash.com/photo-1563245339-6b2e440222b1?q=80&w=500"
                color="bg-orange-500"
              />
            </div>
          </section>
        </div>

        {/* RIGHT COLUMN (4/12) */}
        <div className="col-span-12 lg:col-span-4 space-y-8">
          
          {/* STREAK CARD */}
          <section className="bg-linear-to-br from-primary to-blue-700 rounded-[2.5rem] p-8 text-white shadow-2xl shadow-primary/30 relative overflow-hidden text-left">
            <div className="relative z-10">
              <div className="flex justify-between items-start mb-6">
                <div>
                  <h3 className="font-black text-2xl tracking-tighter">Chuỗi 14 ngày!</h3>
                  <p className="text-blue-100 text-sm font-medium">Bạn đang duy trì rất tốt.</p>
                </div>
                <span className="material-symbols-outlined text-4xl text-orange-400 fill-1">local_fire_department</span>
              </div>
              <div className="grid grid-cols-7 gap-2 mb-8">
                {['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'].map((d, i) => (
                  <div key={i} className="flex flex-col items-center gap-2">
                    <span className="text-[10px] font-black opacity-60">{d}</span>
                    <div className={`size-2 rounded-full ${i < 3 ? 'bg-white ring-4 ring-white/20' : 'bg-blue-400/30'}`}></div>
                  </div>
                ))}
              </div>
              <div className="bg-white/10 backdrop-blur-md rounded-2xl p-4 border border-white/10">
                <p className="text-[10px] font-black uppercase tracking-widest mb-3 opacity-70">Huy hiệu gần đây</p>
                <div className="flex gap-3">
                  <BadgeIcon icon="dark_mode" />
                  <BadgeIcon icon="auto_stories" active />
                  <BadgeIcon icon="stars" />
                </div>
              </div>
            </div>
            <div className="absolute -bottom-10 -right-10 size-40 bg-white/5 rounded-full blur-3xl"></div>
          </section>

          {/* UPCOMING SESSIONS */}
          <section className="bg-white dark:bg-slate-900 rounded-[2.5rem] p-8 border border-slate-200 dark:border-slate-800 shadow-sm text-left">
            <h3 className="text-lg font-black mb-6">Buổi học sắp tới</h3>
            <div className="space-y-4">
              <SessionItem date="24" month="Th3" title="AI luyện phát âm" time="16:00" active />
              <SessionItem date="25" month="Th3" title="Phòng luyện ngữ pháp" time="10:30" />
            </div>
          </section>

          {/* COMMUNITY NEWS */}
          <section className="relative overflow-hidden rounded-[2.5rem] p-8 bg-slate-900 text-white min-h-50 flex flex-col justify-end text-left">
            <div className="absolute top-0 right-0 size-40 bg-primary/20 rounded-full -mr-16 -mt-16 blur-3xl"></div>
            <span className="bg-primary/20 text-primary text-[10px] font-black px-3 py-1 rounded-full w-fit mb-3 border border-primary/30 uppercase tracking-widest">Cộng đồng</span>
            <h4 className="font-bold text-xl leading-tight mb-2 tracking-tight">Giao lưu tiếng Trung</h4>
            <p className="text-slate-400 text-xs mb-4">Thứ Sáu hằng tuần lúc 18:00. Luyện nói tự nhiên cùng bạn học.</p>
            <button className="text-primary text-xs font-black flex items-center gap-2 group">
                Đăng ký tham gia 
                <span className="material-symbols-outlined text-sm group-hover:translate-x-1 transition-transform">arrow_forward</span>
            </button>
          </section>
        </div>
      </div>
    </div>
  );
}

// --- SUB-COMPONENTS ---

function ActionButton({ icon, label, color, onClick }: any) {
  return (
    <button 
      onClick={onClick}
      className={`flex flex-col items-center justify-center p-8 rounded-4xl transition-all hover:scale-[1.05] active:scale-95 shadow-lg group ${color}`}
    >
      <span className="material-symbols-outlined text-3xl mb-3 group-hover:rotate-12 transition-transform">{icon}</span>
      <span className="font-black text-[10px] uppercase tracking-widest">{label}</span>
    </button>
  );
}

function ProgressItem({ flag, title, level, percent }: any) {
  return (
    <div className="text-left">
      <div className="flex justify-between items-center mb-3">
        <div className="flex items-center gap-3">
          <span className="text-3xl">{flag}</span>
          <div>
            <p className="font-black text-sm dark:text-white leading-none">{title}</p>
            <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mt-1">{level}</p>
          </div>
        </div>
        <span className="text-primary font-black text-lg">{percent}%</span>
      </div>
      <div className="w-full bg-slate-100 dark:bg-slate-800 rounded-full h-2 overflow-hidden">
        <div className="bg-primary h-full rounded-full transition-all duration-1000 shadow-[0_0_12px_rgba(25,120,229,0.4)]" style={{ width: `${percent}%` }}></div>
      </div>
    </div>
  );
}

function ScenarioCard({ category, title, desc, img, color = "bg-primary" }: any) {
  return (
    <div className="group relative overflow-hidden rounded-4xl aspect-16/10 bg-slate-200 dark:bg-slate-800 border border-slate-200 dark:border-slate-800 shadow-sm cursor-pointer">
      <div className="absolute inset-0 bg-cover bg-center transition-transform duration-700 group-hover:scale-110" style={{ backgroundImage: `url(${img})` }}></div>
      <div className="absolute inset-0 bg-linear-to-t from-slate-900 via-slate-900/20 to-transparent p-6 flex flex-col justify-end text-left">
        <span className={`${color} text-white text-[9px] font-black px-3 py-1 rounded-full uppercase w-fit mb-3 shadow-lg`}>{category}</span>
        <h4 className="text-white font-black text-xl tracking-tight mb-1">{title}</h4>
        <p className="text-slate-300 text-xs font-medium">{desc}</p>
      </div>
    </div>
  );
}

function BadgeIcon({ icon, active }: any) {
  return (
    <div className={`size-10 rounded-full flex items-center justify-center transition-all ${active ? 'bg-yellow-400 text-blue-900 scale-110 shadow-lg shadow-yellow-400/20' : 'bg-white/10 text-white'}`}>
      <span className="material-symbols-outlined text-lg">{icon}</span>
    </div>
  );
}

function SessionItem({ date, month, title, time, active }: any) {
  return (
    <div className={`flex gap-4 items-center p-4 rounded-2xl transition-all ${active ? 'bg-primary/5 border border-primary/10 shadow-sm' : 'hover:bg-slate-50 dark:hover:bg-slate-800/50'}`}>
      <div className={`size-12 rounded-xl flex flex-col items-center justify-center ${active ? 'bg-primary text-white shadow-lg shadow-primary/20' : 'bg-slate-100 dark:bg-slate-800 text-slate-400 font-bold'}`}>
        <span className="text-[10px] font-black uppercase leading-none">{month}</span>
        <span className="text-lg font-black">{date}</span>
      </div>
      <div className="flex-1 text-left">
        <p className="font-black text-sm dark:text-white leading-tight">{title}</p>
        <p className="text-[10px] text-slate-500 font-bold uppercase mt-1">{time} • Cố vấn AI</p>
      </div>
      {active && <span className="material-symbols-outlined text-primary text-sm">video_call</span>}
    </div>
  );
}
