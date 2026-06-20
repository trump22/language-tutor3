// src/pages/ChatAI.tsx
import ChatInterface from '../components/AI/ChatInterface';

export default function ChatAI() {
  return (
    <div className="flex h-[calc(100vh-2rem)] w-full bg-background-light dark:bg-background-dark font-display text-slate-900 dark:text-slate-100 overflow-hidden rounded-[2.5rem] border border-slate-200 dark:border-slate-800 shadow-2xl">
      
      {/* Khung Chat Tràn viền */}
      <main className="flex-1 flex flex-col h-full bg-white dark:bg-slate-900">
        <ChatInterface />
      </main>

      {/* Sidebar Phân tích bên phải */}
      <aside className="w-80 border-l border-slate-100 dark:border-slate-800 overflow-y-auto hidden xl:flex flex-col bg-slate-50/30 dark:bg-slate-900/50 backdrop-blur-md">
        <div className="p-8 border-b dark:border-slate-800">
          <h3 className="font-black text-xs uppercase tracking-widest text-slate-400 mb-4">Ngữ cảnh bài học</h3>
          <div className="bg-primary/10 rounded-2xl p-5 border border-primary/10">
            <p className="text-[10px] font-black text-primary uppercase mb-1">Tình huống hiện tại</p>
            <p className="text-sm font-bold text-slate-800 dark:text-white">Luyện hội thoại tự do</p>
          </div>
        </div>
        
        <div className="p-8 space-y-8 text-left">
          <div>
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-black text-xs uppercase tracking-widest text-slate-400">Phân tích</h3>
              <span className="px-2 py-0.5 bg-green-500/10 text-green-500 text-[9px] font-black rounded-full uppercase">Trực tiếp</span>
            </div>
            <div className="bg-white dark:bg-slate-800 p-4 rounded-2xl shadow-sm border border-slate-100 dark:border-slate-700">
              <div className="flex justify-between items-center mb-2"><span className="text-[11px] font-bold">Điểm lưu loát</span><span className="text-xs font-black text-green-500">100%</span></div>
              <div className="h-1.5 w-full bg-slate-100 dark:bg-slate-900 rounded-full overflow-hidden"><div className="h-full bg-green-500 w-full rounded-full transition-all duration-1000"></div></div>
            </div>
          </div>
          
          <div>
            <h3 className="font-black text-xs uppercase tracking-widest text-slate-400 mb-4">Kho từ vựng</h3>
            <div className="py-10 text-center border-2 border-dashed border-slate-100 dark:border-slate-800 rounded-4xl">
               <span className="material-symbols-outlined text-slate-200 dark:text-slate-700 text-4xl mb-2">bookmark</span>
               <p className="text-[10px] text-slate-400 font-bold uppercase tracking-tighter">Từ vựng đã lưu sẽ hiện ở đây</p>
            </div>
          </div>
        </div>
      </aside>
    </div>
  );
}
