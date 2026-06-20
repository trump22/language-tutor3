import { useState } from 'react';
import api from '../../api/axios';

// --- INTERFACES ---
interface Question {
  text: string;
  options: Record<string, string>; // { "A": "...", "B": "..." }
  correctAnswer: string;
  explanation: string;
}

interface ListeningDraft {
  part: number;
  audioUrl: string;
  transcript: string;
  questions: Question[];
  imageUrl?: string; // THÊM KHAI BÁO ẢNH CHO PART 1
}

export default function AdminListeningCreator() {
  // Config States
  const [examPartId, setExamPartId] = useState<string>('1'); // ID của Part trong DB
  const [partFormat, setPartFormat] = useState<number>(3); // Part 1, 2, hoặc 3
  const [level, setLevel] = useState('Intermediate');
  const [rawScript, setRawScript] = useState('');

  // Process States
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [draftData, setDraftData] = useState<ListeningDraft | null>(null);
  const [error, setError] = useState('');

  // Lấy Base URL của Backend để gắn vào thẻ <audio>
  const BACKEND_URL = api.defaults.baseURL?.replace('/api', '') || 'http://localhost:5000';

  const handleGenerateListening = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!rawScript.trim()) return;
    
    setIsLoading(true);
    setError('');
    setDraftData(null);
    
    try {
      // Gọi API Xưởng đúc AI
      const response = await api.post('/ai/admin/draft-listening', { 
        rawScript, 
        level, 
        part: partFormat 
      });
      
      setDraftData(response.data.data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'AI gặp sự cố khi tạo Audio. Hãy thử lại!');
      console.error(err);
    } finally { 
      setIsLoading(false); 
    }
  };

  const handleSaveToDatabase = async () => {
    if (!draftData || !examPartId) return;
    setIsSaving(true);
    
    try {
      // Gọi API Lưu vào Database Prisma
      await api.post('/ai/admin/save-listening', { 
        examPartId: Number(examPartId),
        audioUrl: draftData.audioUrl,
        transcript: draftData.transcript,
        questions: draftData.questions
      });

      alert(`🎉 Đã lưu bộ đề TOEIC Part ${partFormat} vào Database thành công!`); 
      setRawScript('');
      setDraftData(null);
      
    } catch (err: any) { 
        console.error("Lỗi chi tiết:", err.response?.data || err.message);
        alert(err.response?.data?.message || "Lỗi khi lưu vào Database. Vui lòng kiểm tra lại Exam Part ID."); 
    } finally { 
        setIsSaving(false); 
    }
  };

  return (
    <div className="p-8 space-y-8 animate-in fade-in duration-500">
      {/* HEADER SECTION */}
      <header className="flex justify-between items-end text-left">
        <div>
          <h1 className="text-3xl font-black tracking-tight flex items-center gap-3">
            <span className="material-symbols-outlined text-primary text-4xl">headphones</span>
            Phòng tạo audio TOEIC
          </h1>
          <p className="text-slate-500 mt-1 italic">Ứng dụng Gemini & Azure TTS để sản xuất đề thi Listening tự động.</p>
        </div>
      </header>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-8">
        
        {/* PANEL TRÁI: CẤU HÌNH */}
        <div className="xl:col-span-1 space-y-6">
          <div className="bg-white dark:bg-[#1e293b] p-6 rounded-4xl border border-slate-200 dark:border-slate-800 shadow-sm sticky top-8 text-left">
            
            <form onSubmit={handleGenerateListening} className="space-y-5">
              
              {/* Lưu vào Exam Part ID */}
              <div className="space-y-2">
                <label className="text-[10px] font-black text-slate-400 uppercase ml-1">ID phần thi đích</label>
                <div className="flex bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden focus-within:ring-2 focus-within:ring-primary">
                  <span className="flex items-center justify-center px-4 bg-slate-100 dark:bg-slate-800 text-slate-500 font-bold border-r border-slate-200 dark:border-slate-800">
                    ID
                  </span>
                  <input 
                    type="number" 
                    value={examPartId} 
                    onChange={(e) => setExamPartId(e.target.value)} 
                    className="w-full p-3 text-sm outline-none bg-transparent font-bold text-primary" 
                    required 
                  />
                </div>
              </div>

              <hr className="border-slate-100 dark:border-slate-800 my-4" />

              {/* TOEIC Format & Level */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label className="text-[10px] font-black text-slate-400 uppercase ml-1">Định dạng TOEIC</label>
                  <select 
                    value={partFormat} 
                    onChange={(e) => setPartFormat(Number(e.target.value))} 
                    className="w-full bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 rounded-xl p-3 text-sm outline-none font-bold text-slate-700 dark:text-slate-300"
                  >
                    <option value={1}>Part 1 (Ảnh)</option>
                    <option value={2}>Part 2 (Hỏi đáp)</option>
                    <option value={3}>Part 3 (Hội thoại)</option>
                  </select>
                </div>
                <div className="space-y-2">
                  <label className="text-[10px] font-black text-slate-400 uppercase ml-1">Độ khó</label>
                  <select 
                    value={level} 
                    onChange={(e) => setLevel(e.target.value)} 
                    className="w-full bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 rounded-xl p-3 text-sm outline-none"
                  >
                    <option value="Beginner">Cơ bản</option>
                    <option value="Intermediate">Trung cấp</option>
                    <option value="Advanced">Nâng cao</option>
                  </select>
                </div>
              </div>

              {/* Raw Script Input */}
              <div className="space-y-2">
                <label className="text-[10px] font-black text-slate-400 uppercase ml-1">
                  {partFormat === 1 ? 'Mô tả ảnh để tạo đáp án nhiễu' : 'Kịch bản / Tình huống thô'}
                </label>
                <textarea 
                  value={rawScript} 
                  onChange={(e) => setRawScript(e.target.value)} 
                  rows={6}
                  placeholder={partFormat === 1 ? "VD: Một người đàn ông đang cầm ly cà phê gần cửa sổ..." : "VD: Hai người đang nói về chuyến bay bị trễ ở sân bay..."} 
                  className="w-full bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 rounded-xl p-3 text-sm focus:ring-2 focus:ring-primary outline-none resize-none" 
                  required 
                />
              </div>

              <button 
                type="submit" 
                disabled={isLoading || !rawScript.trim()}
                className="w-full mt-6 bg-primary text-white font-black py-4 rounded-2xl flex items-center justify-center gap-3 hover:bg-primary/90 transition-all shadow-lg shadow-primary/20 disabled:opacity-50"
              >
                {isLoading ? <span className="material-symbols-outlined animate-spin">progress_activity</span> : <span className="material-symbols-outlined">auto_awesome</span>}
                {isLoading ? 'Đang tạo audio...' : 'Tạo audio và câu hỏi'}
              </button>
            </form>
          </div>
        </div>

        {/* PANEL PHẢI: KẾT QUẢ REVIEW */}
        <div className="xl:col-span-2 space-y-6 text-left">
          {error && (
            <div className="bg-red-50 dark:bg-red-900/10 text-red-500 p-4 rounded-2xl border border-red-100 dark:border-red-900/20 flex items-center gap-3 font-bold text-sm">
              <span className="material-symbols-outlined">warning</span> {error}
            </div>
          )}

          {!isLoading && !draftData && (
            <div className="h-full min-h-100 flex flex-col items-center justify-center text-slate-300 dark:text-slate-700 bg-white dark:bg-[#1e293b] rounded-[2.5rem] border-2 border-dashed border-slate-200 dark:border-slate-800 transition-colors">
              <span className="material-symbols-outlined text-7xl mb-4 opacity-20">graphic_eq</span>
              <p className="text-sm font-bold uppercase tracking-widest">Nhập kịch bản để tạo audio Azure</p>
            </div>
          )}

          {draftData && (
            <div className="space-y-6 animate-in slide-in-from-bottom-4 duration-500">
              
              {/* 1. AUDIO PLAYER TILE */}
              <div className="bg-white dark:bg-[#1e293b] p-8 rounded-4xl border border-slate-200 dark:border-slate-800 shadow-sm relative overflow-hidden">
                <div className="absolute top-0 left-0 w-full h-1 bg-linear-to-r from-blue-500 to-purple-500"></div>
                <h4 className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-4 flex items-center gap-2">
                  <span className="material-symbols-outlined text-sm">play_circle</span>
                  Nghe thử audio
                </h4>
                {/* Custom Audio Player Wrapper for better styling */}
                <div className="bg-slate-50 dark:bg-slate-900 rounded-2xl p-4 border border-slate-200 dark:border-slate-800">
                  <audio controls src={`${BACKEND_URL}${draftData.audioUrl}`} className="w-full outline-none" />
                </div>
              </div>

              {/* --- THÊM: KHU VỰC HIỂN THỊ ẢNH CHO PART 1 --- */}
              {draftData.imageUrl && draftData.part === 1 && (
                <div className="bg-white dark:bg-[#1e293b] p-6 rounded-4xl border border-slate-200 dark:border-slate-800 shadow-sm flex justify-center">
                  <img 
                    src={draftData.imageUrl} 
                    alt="Ảnh TOEIC Part 1 do AI tạo" 
                    className="max-h-112.5 w-full object-contain rounded-2xl border-2 border-slate-100 dark:border-slate-700 bg-slate-50"
                  />
                </div>
              )}

              {/* 2. TRANSCRIPT TILE */}
              <div className="bg-white dark:bg-[#1e293b] p-8 rounded-4xl border border-slate-200 dark:border-slate-800 shadow-sm">
                <h4 className="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-4 flex items-center gap-2">
                  <span className="material-symbols-outlined text-sm">subtitles</span>
                  Bản chép lời đã tạo
                </h4>
                <div className="p-4 bg-slate-50 dark:bg-slate-900/50 rounded-2xl border border-slate-100 dark:border-slate-800">
                  <p className="text-slate-700 dark:text-slate-300 text-sm leading-relaxed whitespace-pre-wrap font-medium">
                    {draftData.transcript}
                  </p>
                </div>
              </div>

              {/* 3. QUESTIONS LIST */}
              {draftData.questions.map((q, index) => (
                <div key={index} className="bg-white dark:bg-[#1e293b] p-8 rounded-4xl border border-slate-200 dark:border-slate-800 shadow-sm relative group">
                  <div className="absolute top-0 left-8 w-12 h-1 bg-primary rounded-b-full shadow-[0_4px_10px_rgba(25,120,229,0.5)]"></div>
                  
                  <div className="flex justify-between items-center mb-6">
                    <span className="px-3 py-1 bg-primary/10 text-primary text-[10px] font-black uppercase tracking-widest rounded-md">
                      {/* SỬA TITLE CHO PART 1 */}
                      {draftData.part === 1 ? 'Đáp án A, B, C, D' : `Câu hỏi #${index + 1}`}
                    </span>
                  </div>

                  {/* CHỈ HIỂN THỊ CÂU HỎI TEXT NẾU LÀ PART 2 HOẶC 3 */}
                  {draftData.part !== 1 && (
                    <h4 className="text-xl font-bold text-slate-800 dark:text-white mb-6 leading-relaxed">{q.text}</h4>
                  )}

                  {/* Cấu trúc map Object Options (A, B, C, D) */}
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 mb-6">
                    {Object.entries(q.options).map(([key, opt]) => (
                      <div key={key} className={`p-4 rounded-2xl border transition-all text-sm font-bold flex items-center gap-3 ${key === q.correctAnswer ? 'bg-emerald-50 dark:bg-emerald-900/10 border-emerald-200 dark:border-emerald-800 text-emerald-600' : 'bg-slate-50 dark:bg-[#0f172a] border-slate-100 dark:border-slate-800 text-slate-500'}`}>
                        <span className="size-6 rounded-lg bg-white dark:bg-slate-800 border flex items-center justify-center text-[10px] font-black">
                          {key}
                        </span>
                        {opt}
                      </div>
                    ))}
                  </div>

                  {/* Giải thích của AI */}
                  <div className="mt-6 pt-6 border-t border-slate-100 dark:border-slate-800">
                    <div className="flex gap-3">
                      <span className="material-symbols-outlined text-primary text-sm mt-0.5">lightbulb</span>
                      <p className="text-sm text-slate-500 dark:text-slate-400 leading-relaxed italic">
                        <span className="font-black text-slate-700 dark:text-slate-200 not-italic uppercase text-[10px] mr-2">Giải thích:</span> 
                        {q.explanation}
                      </p>
                    </div>
                  </div>
                </div>
              ))}

              {/* SAVE BUTTON */}
              <div className="pt-6">
                <button 
                  onClick={handleSaveToDatabase}
                  disabled={isSaving}
                  className="w-full bg-emerald-600 hover:bg-emerald-500 text-white font-black py-5 rounded-4xl flex items-center justify-center gap-3 shadow-2xl hover:scale-[1.01] transition-all disabled:opacity-50"
                >
                  {isSaving ? <span className="material-symbols-outlined animate-spin">sync</span> : <span className="material-symbols-outlined">save</span>}
                  {isSaving ? 'Đang lưu vào database...' : 'Duyệt và lưu vào database'}
                </button>
              </div>

            </div>
          )}
          
        </div>
      </div>
    </div>
  );
}
