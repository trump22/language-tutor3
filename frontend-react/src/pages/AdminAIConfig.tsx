import { useState, useEffect, useRef } from 'react';
import api from '../api/axios';

// --- CẤU HÌNH MAPPING NGÔN NGỮ ---
const LANG_MAP: Record<string, string> = {
  'English': 'EN',
  'Chinese': 'ZH'
};

const REVERSE_LANG_MAP: Record<string, string> = {
  'EN': 'Tiếng Anh',
  'ZH': 'Tiếng Trung'
};

const LANG_LABELS: Record<string, string> = {
  English: 'Tiếng Anh',
  Chinese: 'Tiếng Trung',
};

const displayLang = (lang: string) => LANG_LABELS[lang] || lang;

// --- INTERFACES ---
interface Teacher {
  id: string; // ID là chuỗi CUID từ Prisma
  name: string;
  systemPrompt: string;
  supportLanguage: string;
  temperature: number; 
  maxTokens: number;   
}

export default function AdminAIConfig() {
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [selectedTeacher, setSelectedTeacher] = useState<Teacher | null>(null);
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  // 1. Logic chọn Ngôn ngữ (Chỉ còn Anh và Trung)
  const [activeLang, setActiveLang] = useState('English');
  const languages = ['English', 'Chinese'];

  // 2. Logic tạo Persona mới
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [newPersonaName, setNewPersonaName] = useState('');

  // 3. Logic Test Chat
  const [testMessage, setTestMessage] = useState('');
  const [chatHistory, setChatHistory] = useState<{ role: 'ai' | 'user'; text: string }[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const chatEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => { fetchTeachers(); }, []);
  // Tự động chuyển giáo viên khi đổi Tab ngôn ngữ
  useEffect(() => {
    const targetCode = LANG_MAP[activeLang];
    const firstInLang = teachers.find((t) => t.supportLanguage === targetCode);
    if (firstInLang) setSelectedTeacher(firstInLang);
  }, [activeLang, teachers]);

  useEffect(() => { chatEndRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [chatHistory]);

  const fetchTeachers = async () => {
    try {
      const res = await api.get('/admin/ai-teachers');
      const data = res.data.data;
      setTeachers(data);
      
      const targetCode = LANG_MAP[activeLang];
      const firstInLang = data.find((t: Teacher) => t.supportLanguage === targetCode);
      if (firstInLang) setSelectedTeacher(firstInLang);
      else if (data.length > 0) setSelectedTeacher(data[0]);
    } catch (e) {
      console.error("Lỗi lấy danh sách giáo viên:", e);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!selectedTeacher) return;
    setIsSaving(true);
    try {
      // ĐÃ SỬA: Không dùng Number() vì ID là chuỗi CUID
      await api.put(`/admin/ai-teachers/${selectedTeacher.id}`, {
        systemPrompt: selectedTeacher.systemPrompt,
        temperature: selectedTeacher.temperature,
        maxTokens: selectedTeacher.maxTokens,
        name: selectedTeacher.name,
        supportLanguage: selectedTeacher.supportLanguage
      });
      alert("Cấu hình AI đã được cập nhật thành công!");
      fetchTeachers(); 
    } catch (e) {
      alert("Lỗi khi lưu cấu hình.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleCreateTeacher = async () => {
    if (!newPersonaName) return;
    try {
      const res = await api.post('/admin/ai-teachers', {
        name: newPersonaName,
        supportLanguage: LANG_MAP[activeLang], // Gửi 'EN' hoặc 'ZH'
        systemPrompt: "You are a helpful language tutor.",
        temperature: 0.7,
        maxTokens: 512
      });
      const newTeacher = res.data.data;
      setTeachers([...teachers, newTeacher]);
      setSelectedTeacher(newTeacher);
      setIsAddModalOpen(false);
      setNewPersonaName('');
      alert(`Đã tạo persona ${newTeacher.name} cho ${displayLang(activeLang)}!`);
    } catch (e) {
      alert("Lỗi khi tạo giáo viên mới.");
    }
  };

  const handleTestChat = async () => {
    if (!testMessage.trim() || !selectedTeacher) return;
    const userMsg = testMessage;
    setChatHistory(prev => [...prev, { role: 'user', text: userMsg }]);
    setTestMessage('');
    setIsTyping(true);
    try {
      // Đã cập nhật payload để khớp với Controller/Service mới (có thêm level)
      const res = await api.post('/ai/chat', {
        message: userMsg,
        language: selectedTeacher.supportLanguage.toLowerCase(),
        personaName: selectedTeacher.name,
        level: 'Beginner' // Mặc định level để test format trả về
      });
      setChatHistory(prev => [...prev, { role: 'ai', text: res.data.reply }]);
    } catch (e) {
      setChatHistory(prev => [...prev, { role: 'ai', text: "Lỗi kết nối AI. Kiểm tra lại tên Persona trong DB." }]);
    } finally {
      setIsTyping(false);
    }
  };

  const filteredTeachers = teachers.filter(t => t.supportLanguage === LANG_MAP[activeLang]);

  if (loading) return <div className="flex h-64 items-center justify-center text-primary font-bold animate-pulse text-lg">Đang tải cấu hình AI...</div>;

  return (
    <div className="p-8 space-y-8 animate-in fade-in duration-500 text-left">
      <header>
        <h1 className="text-3xl font-black tracking-tighter text-slate-900 dark:text-white">Cấu hình prompt giáo viên AI</h1>
        <p className="text-slate-500 mt-2 text-sm italic">Quản lý và tinh chỉnh "linh hồn" cho các giáo viên AI tiếng Anh và tiếng Trung.</p>
      </header>

      {/* 1. LANGUAGE TABS */}
      <div className="border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
        <div className="flex gap-10">
          {languages.map(lang => (
            <button 
              key={lang}
              onClick={() => { setActiveLang(lang); setChatHistory([]); }}
              className={`pb-4 text-sm font-black uppercase tracking-widest border-b-4 transition-all ${activeLang === lang ? 'border-primary text-primary' : 'border-transparent text-slate-400 hover:text-slate-600'}`}
            >
              {displayLang(lang)}
            </button>
          ))}
        </div>
        <button 
          onClick={() => setIsAddModalOpen(true)}
          className="mb-4 px-5 py-2.5 bg-primary text-white rounded-xl text-xs font-black uppercase tracking-tighter hover:bg-primary/90 transition-all flex items-center gap-2 shadow-lg shadow-primary/20"
        >
          <span className="material-symbols-outlined text-sm">add_circle</span> Tạo persona {displayLang(activeLang)}
        </button>
      </div>

      {/* 2. PERSONA GRID */}
      <section>
        <h2 className="text-xs font-black text-slate-400 uppercase tracking-[0.3em] mb-4 flex items-center gap-2">
          <span className="size-2 rounded-full bg-primary"></span> Persona {displayLang(activeLang)} trong database
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-5">
          {filteredTeachers.map(t => (
            <div 
              key={t.id}
              onClick={() => { setSelectedTeacher(t); setChatHistory([]); }}
              className={`p-5 rounded-4xl border-2 transition-all cursor-pointer flex flex-col gap-3 ${selectedTeacher?.id === t.id ? 'border-primary bg-primary/5 shadow-inner' : 'border-slate-100 dark:border-slate-800 bg-white dark:bg-slate-900 hover:border-primary/30'}`}
            >
              <div className="flex items-center justify-between">
                <div className={`size-10 rounded-2xl flex items-center justify-center ${selectedTeacher?.id === t.id ? 'bg-primary text-white' : 'bg-slate-100 dark:bg-slate-800 text-slate-400'}`}>
                  <span className="material-symbols-outlined text-xl">
                    {t.name.toLowerCase().includes('friendly') ? 'mood' : 'psychology'}
                  </span>
                </div>
                {selectedTeacher?.id === t.id && <span className="text-[9px] font-black text-primary uppercase tracking-widest animate-pulse">Đang chọn</span>}
              </div>
              <div>
                <h3 className="font-bold text-slate-900 dark:text-white leading-tight">{t.name}</h3>
                <p className="text-[10px] text-slate-400 font-bold uppercase mt-1 tracking-tighter">
                   Lộ trình {REVERSE_LANG_MAP[t.supportLanguage]}
                </p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* 3. EDITOR & TEST CHAT */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-8">
        <div className="xl:col-span-2 space-y-6">
          <div className="bg-white dark:bg-slate-900 p-8 rounded-[2.5rem] border border-slate-200 dark:border-slate-800 shadow-sm relative overflow-hidden">
            <div className="absolute top-0 left-0 w-1.5 h-full bg-primary"></div>
            <div className="flex items-center justify-between mb-6">
              <h2 className="font-black flex items-center gap-2 text-sm uppercase tracking-widest text-slate-400">
                <span className="material-symbols-outlined text-primary">edit_note</span> Chỉ dẫn hệ thống
              </h2>
              <span className="px-3 py-1 bg-slate-50 dark:bg-slate-800 text-slate-400 rounded-full text-[9px] font-black">
                ID: {selectedTeacher?.id.substring(0, 12)}...
              </span>
            </div>
            <textarea 
              className="w-full h-96 rounded-2xl border-none dark:bg-slate-950 p-6 font-mono text-sm outline-none focus:ring-2 focus:ring-primary/20 transition-all leading-relaxed"
              value={selectedTeacher?.systemPrompt || ''}
              onChange={(e) => setSelectedTeacher(prev => prev ? {...prev, systemPrompt: e.target.value} : null)}
              placeholder="Mô tả hành vi AI, có thể dùng {{student_name}}, {{skillLevel}}..."
            />
          </div>

          <div className="bg-white dark:bg-slate-900 p-8 rounded-[2.5rem] border border-slate-200 dark:border-slate-800 shadow-sm grid grid-cols-1 md:grid-cols-2 gap-10">
            <div className="space-y-5">
              <div className="flex justify-between text-xs font-black uppercase tracking-widest"><label>Độ sáng tạo</label><span className="text-primary font-mono text-base">{selectedTeacher?.temperature || 0.7}</span></div>
              <input type="range" min="0" max="1" step="0.1" className="w-full accent-primary h-1.5 rounded-full appearance-none bg-slate-100 dark:bg-slate-800 cursor-pointer" value={selectedTeacher?.temperature || 0} onChange={(e) => setSelectedTeacher(prev => prev ? {...prev, temperature: parseFloat(e.target.value)} : null)} />
            </div>
            <div className="space-y-5">
              <div className="flex justify-between text-xs font-black uppercase tracking-widest"><label>Số token tối đa</label><span className="text-primary font-mono text-base">{selectedTeacher?.maxTokens || 512}</span></div>
              <input type="range" min="64" max="2048" step="64" className="w-full accent-primary h-1.5 rounded-full appearance-none bg-slate-100 dark:bg-slate-800 cursor-pointer" value={selectedTeacher?.maxTokens || 0} onChange={(e) => setSelectedTeacher(prev => prev ? {...prev, maxTokens: parseInt(e.target.value)} : null)} />
            </div>
          </div>
        </div>

        {/* TEST SIMULATION */}
        <div className="flex flex-col h-full bg-white dark:bg-slate-900 rounded-[2.5rem] border border-slate-200 dark:border-slate-800 shadow-xl overflow-hidden min-h-150">
          <div className="p-6 border-b dark:border-slate-800 flex justify-between items-center bg-slate-50/50 dark:bg-slate-800/30">
            <span className="text-xs font-black uppercase tracking-widest text-primary flex items-center gap-2"><span className="material-symbols-outlined text-sm">forum</span> Kiểm thử phản hồi</span>
            <button onClick={() => setChatHistory([])} className="text-[10px] font-black uppercase text-slate-400 hover:text-red-500 transition-colors">Xóa</button>
          </div>
          
          <div className="flex-1 p-6 overflow-y-auto space-y-5 custom-scrollbar text-left bg-slate-50/20 dark:bg-transparent">
            {chatHistory.map((m, i) => (
              <div key={i} className={`flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                <div className={`max-w-[90%] p-4 rounded-3xl text-sm font-medium leading-relaxed ${m.role === 'user' ? 'bg-primary text-white rounded-tr-none' : 'bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 rounded-tl-none shadow-sm'}`}>
                  {/* Hiển thị format 3 phần nếu có dấu --- */}
                  {m.text.includes('---') ? (
                    <div className="space-y-2">
                       <p className="font-bold">{m.text.split('---')[0]}</p>
                       <p className="text-xs text-orange-500 italic">{m.text.split('---')[1]}</p>
                       <p className="text-xs text-slate-500 border-t pt-1">{m.text.split('---')[2]}</p>
                    </div>
                  ) : m.text}
                </div>
              </div>
            ))}
            {isTyping && <div className="text-[10px] text-primary font-black animate-pulse uppercase tracking-widest">AI đang suy nghĩ...</div>}
            <div ref={chatEndRef} />
          </div>

          <div className="p-6 border-t dark:border-slate-800">
            <div className="relative">
              <input type="text" value={testMessage} onChange={(e) => setTestMessage(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && handleTestChat()} placeholder={`Chat thử với ${selectedTeacher?.name}...`} className="w-full py-4 pl-5 pr-14 rounded-2xl border-none bg-slate-100 dark:bg-slate-950 text-sm font-medium outline-none focus:ring-2 focus:ring-primary/20 shadow-inner" />
              <button onClick={handleTestChat} className="absolute right-3 top-1/2 -translate-y-1/2 size-10 bg-primary text-white rounded-xl flex items-center justify-center hover:scale-110 active:scale-95 transition-all shadow-lg shadow-primary/20"><span className="material-symbols-outlined text-sm">send</span></button>
            </div>
          </div>
        </div>
      </div>

      <footer className="mt-12 pt-8 border-t border-slate-200 dark:border-slate-800 flex justify-between items-center">
        <button onClick={fetchTeachers} className="px-6 py-2.5 text-slate-500 font-black text-[11px] uppercase tracking-widest hover:text-slate-800 transition-colors">Hủy thay đổi</button>
        <button onClick={handleSave} disabled={isSaving} className="px-12 py-3.5 bg-primary text-white rounded-2xl text-xs font-black uppercase tracking-widest shadow-2xl shadow-primary/40 hover:scale-105 active:scale-95 transition-all">
          {isSaving ? 'Đang đồng bộ...' : 'Lưu cấu hình'}
        </button>
      </footer>

      {/* MODAL NEW PERSONA */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-100 flex items-center justify-center bg-slate-950/80 backdrop-blur-md p-4">
          <div className="bg-white dark:bg-slate-900 p-10 rounded-[3rem] w-full max-w-md shadow-2xl border border-white/10 animate-in zoom-in-95 text-left">
            <h2 className="text-2xl font-black tracking-tighter mb-2">Persona mới</h2>
            <p className="text-slate-500 text-sm mb-8 font-medium italic">Tạo giáo viên mới cho lộ trình {displayLang(activeLang)}.</p>
            <input className="w-full p-4 bg-slate-50 dark:bg-slate-950 border-none rounded-2xl mb-8 outline-none focus:ring-2 focus:ring-primary text-sm font-bold shadow-inner" placeholder="VD: Bạn đồng hành giao tiếp" value={newPersonaName} onChange={(e) => setNewPersonaName(e.target.value)} autoFocus />
            <div className="flex gap-4">
              <button onClick={() => setIsAddModalOpen(false)} className="flex-1 py-4 text-slate-400 text-xs font-black uppercase tracking-widest">Hủy</button>
              <button onClick={handleCreateTeacher} className="flex-1 py-4 bg-primary text-white rounded-2xl text-xs font-black uppercase tracking-widest shadow-xl shadow-primary/20">Tạo persona</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
