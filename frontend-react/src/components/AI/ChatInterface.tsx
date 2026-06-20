import { useState, useRef, useEffect } from 'react';
import api from '../../api/axios';

interface Message { id: string; sender: 'USER' | 'AI'; text: string; }
// Thêm interface cho giáo viên
interface Teacher { id: string; name: string; supportLanguage: string; }

export default function ChatInterface() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isFetchingHistory, setIsFetchingHistory] = useState(true);

  // --- CÁC STATE QUẢN LÝ LỰA CHỌN ---
  const [language, setLanguage] = useState('en'); 
  const [level, setLevel] = useState('Beginner');
  const [personaName, setPersonaName] = useState(''); // Để trống để chờ load từ DB
  const [allTeachers, setAllTeachers] = useState<Teacher[]>([]); // Danh sách giáo viên từ DB

  const [isRecording, setIsRecording] = useState(false);
  const recognitionRef = useRef<any>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // 1. LẤY DANH SÁCH GIÁO VIÊN TỪ DATABASE (Chỉ chạy 1 lần khi load trang)
  useEffect(() => {
    const loadTeachers = async () => {
      try {
        // Sử dụng route công khai /ai/teachers (đã tạo ở Backend) để không bị lỗi 403
        const res = await api.get('/ai/teachers');
        if (res.data.success) {
          setAllTeachers(res.data.data);
        }
      } catch (e) {
        console.error("Lỗi tải danh sách giáo viên:", e);
      }
    };
    loadTeachers();
  }, []);

  // 2. ĐỒNG BỘ PERSONA: Tự động chọn giáo viên đầu tiên khi đổi ngôn ngữ hoặc khi mới load DB xong
  useEffect(() => {
    if (allTeachers.length > 0) {
      const filtered = allTeachers.filter(
        (t) => t.supportLanguage.toLowerCase() === language.toLowerCase() && t.name !== 'Exercise Creator'
      );
      if (filtered.length > 0) {
        setPersonaName(filtered[0].name);
      }
    }
  }, [language, allTeachers]);

  // 3. KHỞI TẠO SPEECH RECOGNITION
  useEffect(() => {
    const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (SpeechRecognition) {
      recognitionRef.current = new SpeechRecognition();
      recognitionRef.current.continuous = false;
      recognitionRef.current.interimResults = true;
      recognitionRef.current.onresult = (e: any) => {
        setInput(Array.from(e.results).map((r: any) => r[0].transcript).join(''));
      };
      recognitionRef.current.onend = () => setIsRecording(false);
    }
  }, []);

  // 4. FETCH LỊCH SỬ CHAT (Chỉ chạy khi đã có personaName)
  useEffect(() => {
    if (!personaName) return;

    const fetchHistory = async () => {
      setIsFetchingHistory(true);
      try {
        const res = await api.get(`/ai/chat-history?language=${language}&personaName=${personaName}`);
        if (res.data.success) {
          setMessages(res.data.data.map((msg: any) => ({ 
            id: msg.id, 
            sender: msg.role === 'model' ? 'AI' : 'USER', 
            text: msg.text 
          })));
        }
      } catch (e) { 
        setMessages([]); 
      } finally { 
        setIsFetchingHistory(false); 
      }
    };
    fetchHistory();
  }, [language, personaName]);

  useEffect(() => { messagesEndRef.current?.scrollIntoView({ behavior: "smooth" }); }, [messages]);

  // 5. LOA ĐỌC THÔNG MINH
  const speakText = (text: string, lang: string) => {
    window.speechSynthesis.cancel();
    let textToRead = text.split('---')[0].trim();
    textToRead = textToRead.replace(/([\u2700-\u27BF]|[\uE000-\uF8FF]|\uD83C[\uDC00-\uDFFF]|\uD83D[\uDC00-\uDFFF]|[\u2011-\u26FF]|\uD83E[\uDD00-\uDDFF])/g, '');
    
    const utterance = new SpeechSynthesisUtterance(textToRead);
    utterance.lang = lang === 'zh' ? 'zh-CN' : 'en-US';
    utterance.rate = 0.85;
    window.speechSynthesis.speak(utterance);
  };

  const toggleRecording = () => {
    if (!recognitionRef.current) return alert("Mic không hỗ trợ.");
    if (isRecording) {
      recognitionRef.current.stop();
    } else {
      recognitionRef.current.lang = language === 'zh' ? 'zh-CN' : 'en-US';
      recognitionRef.current.start();
      setIsRecording(true);
    }
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || isFetchingHistory || isLoading || !personaName) return;

    const userMsg = input.trim();
    setInput('');
    setMessages(prev => [...prev, { id: Date.now().toString(), sender: 'USER', text: userMsg }]);
    setIsLoading(true);

    try {
      const response = await api.post('/ai/chat', { message: userMsg, language, level, personaName });
      setMessages(prev => [...prev, { id: (Date.now()+1).toString(), sender: 'AI', text: response.data.reply }]);
    } catch (err) { 
      setMessages(prev => [...prev, { id: 'err', sender: 'AI', text: 'Lỗi kết nối. Vui lòng kiểm tra Database.' }]);
    } finally { 
      setIsLoading(false); 
    }
  };

  return (
    <div className="flex flex-col h-full bg-white dark:bg-slate-900">
      <header className="p-4 border-b dark:border-slate-800 flex items-center justify-between gap-3 bg-white/80 dark:bg-slate-900/80 backdrop-blur-md sticky top-0 z-10">
        <div className="flex items-center gap-3 text-left">
          <div className="size-10 rounded-2xl bg-primary/10 flex items-center justify-center text-primary shadow-inner">
            <span className="material-symbols-outlined text-xl">psychology</span>
          </div>
          <div>
            <h2 className="font-bold text-sm text-slate-800 dark:text-white">{personaName || 'Đang tải...'}</h2>
            <p className="text-[10px] text-green-500 font-black uppercase tracking-widest flex items-center gap-1">
              <span className="size-1.5 bg-green-500 rounded-full animate-pulse"></span> Đang hoạt động
            </p>
          </div>
        </div>
        
        <div className="flex gap-2">
          {/* STEP 1: NGÔN NGỮ */}
          <select value={language} onChange={(e)=>setLanguage(e.target.value)} className="text-[11px] font-black bg-slate-100 dark:bg-slate-800 p-2.5 rounded-xl border-none outline-none cursor-pointer">
            <option value="en">TIẾNG ANH</option>
            <option value="zh">TIẾNG TRUNG</option>
          </select>
          
          {/* STEP 2: LEVEL */}
          <select value={level} onChange={(e)=>setLevel(e.target.value)} className="text-[11px] font-black bg-slate-100 dark:bg-slate-800 p-2.5 rounded-xl border-none outline-none cursor-pointer">
            <option value="Beginner">CƠ BẢN</option>
            <option value="Intermediate">TRUNG CẤP</option>
            <option value="Advanced">NÂNG CAO</option>
          </select>

          {/* STEP 3: GIÁO VIÊN (ĐỒNG BỘ HOÀN TOÀN TỪ DATABASE) */}
          <select 
            value={personaName} 
            onChange={(e)=>setPersonaName(e.target.value)} 
            className="text-[11px] font-black bg-primary text-white p-2.5 rounded-xl border-none outline-none shadow-lg shadow-primary/20"
          >
            {allTeachers
              .filter((t) => t.supportLanguage.toLowerCase() === language.toLowerCase() && t.name !== 'Exercise Creator')
              .map((t) => (
                <option key={t.id} value={t.name}>
                  {t.name}
                </option>
              ))}
            {allTeachers.filter(t => t.supportLanguage.toLowerCase() === language.toLowerCase() && t.name !== 'Exercise Creator').length === 0 && (
              <option disabled>Không có giáo viên</option>
            )}
          </select>
        </div>
      </header>

      {/* CHAT DISPLAY Area */}
      <div className="flex-1 overflow-y-auto p-6 space-y-6 text-left custom-scrollbar">
        {messages.map((msg) => {
          const isAI = msg.sender === 'AI';
          const parts = msg.text.split('---'); 
          return (
            <div key={msg.id} className={`flex gap-3 animate-in fade-in slide-in-from-bottom-2 duration-300 ${isAI ? '' : 'flex-row-reverse'}`}>
              <div className={`p-4 rounded-2xl max-w-[85%] shadow-sm border ${isAI ? 'bg-slate-50 dark:bg-slate-800 border-slate-100 dark:border-slate-800' : 'bg-primary text-white border-transparent shadow-primary/20'}`}>
                <p className="text-base font-bold leading-relaxed">{parts[0]?.trim()}</p>
                {isAI && language === 'zh' && parts[1] && (
                    <p className="text-xs text-orange-600 dark:text-orange-400 font-mono mt-2 bg-orange-100/50 dark:bg-orange-950/30 px-2 py-1 rounded-lg w-fit italic">
                      {parts[1].trim()}
                    </p>
                )}
                {isAI && parts[2] && (
                    <div className="mt-3 pt-3 border-t border-slate-200 dark:border-slate-700">
                        <p className="text-xs text-slate-500 dark:text-slate-400 font-medium italic">🇻🇳 {parts[2].trim()}</p>
                    </div>
                )}
              </div>
              {isAI && (
                <button onClick={() => speakText(msg.text, language)} className="self-end p-2.5 text-primary hover:bg-primary/10 rounded-full transition-all group active:scale-90">
                  <span className="material-symbols-outlined text-xl group-hover:fill-1">volume_up</span>
                </button>
              )}
            </div>
          );
        })}
        {isLoading && (
          <div className="flex gap-2 items-center p-4 bg-slate-50 dark:bg-slate-800 w-fit rounded-2xl animate-pulse">
            <div className="size-1.5 bg-primary rounded-full animate-bounce"></div>
            <div className="size-1.5 bg-primary rounded-full animate-bounce [animation-delay:-.3s]"></div>
            <div className="size-1.5 bg-primary rounded-full animate-bounce [animation-delay:-.5s]"></div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      <form onSubmit={handleSendMessage} className="p-4 border-t dark:border-slate-800 flex items-center gap-3 bg-white dark:bg-slate-900">
        <div className="flex-1 relative flex items-center">
          <input 
            value={input} 
            onChange={(e)=>setInput(e.target.value)} 
            className="w-full bg-slate-50 dark:bg-slate-950 p-4 rounded-2xl text-sm font-medium outline-none focus:ring-2 focus:ring-primary/20 transition-all shadow-inner" 
            placeholder={isRecording ? "Đang lắng nghe..." : "Nhập tin nhắn hoặc dùng Mic..."} 
          />
        </div>
        <button 
          type="button" 
          onClick={toggleRecording} 
          className={`size-12 rounded-2xl flex items-center justify-center transition-all shadow-lg ${isRecording ? 'bg-red-500 text-white animate-pulse' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 hover:text-primary'}`}
        >
          <span className="material-symbols-outlined">{isRecording ? 'graphic_eq' : 'mic'}</span>
        </button>
        <button 
          type="submit" 
          disabled={isLoading || !input.trim() || !personaName} 
          className="size-12 bg-primary text-white rounded-2xl flex items-center justify-center hover:scale-105 active:scale-95 transition-all shadow-lg shadow-primary/20 disabled:opacity-50 disabled:grayscale"
        >
          <span className="material-symbols-outlined">send</span>
        </button>
      </form>
    </div>
  );
}
