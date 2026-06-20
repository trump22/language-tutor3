import { useState, useEffect } from 'react';
import api from '../../api/axios';

// --- INTERFACES ---
interface Exercise {
  type: string;
  question: string;
  options?: string[];
  answer: string;
  explanation: string;
}

interface Course {
  id: string;
  title: string;
}

export default function AdminAITools() {
  const [topic, setTopic] = useState('');
  const [language, setLanguage] = useState('EN');
  const [level, setLevel] = useState('Beginner');
  
  const [courses, setCourses] = useState<Course[]>([]);
  const [selectedCourseId, setSelectedCourseId] = useState('');

  // State Tạo khóa học mới
  const [isCreatingCourse, setIsCreatingCourse] = useState(false);
  const [newCourseTitle, setNewCourseTitle] = useState('');
  const [newCourseDescription, setNewCourseDescription] = useState('');

  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [exercises, setExercises] = useState<Exercise[]>([]);
  const [error, setError] = useState('');

  useEffect(() => { fetchCourses(); }, []);

  const fetchCourses = async () => {
    try {
      const response = await api.get('/courses');
      const data = response.data.data || response.data.courses || response.data;
      if (Array.isArray(data) && data.length > 0) {
        setCourses(data);
        setSelectedCourseId(data[0].id);
      }
    } catch (err) { console.error('Lỗi tải khóa học:', err); }
  };

  const handleGenerateExercises = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!topic.trim()) return;
    setIsLoading(true);
    setError('');
    setExercises([]);
    try {
      const response = await api.post('/ai/generate-exercises', { language, level, topic });
      setExercises(response.data.exercises || response.data);
    } catch (err: any) {
      const message = err.response?.data?.message;
      const providerStatus = err.response?.data?.providerStatus;
      setError(
        message
          ? `${message}${providerStatus ? ` (Gemini HTTP ${providerStatus})` : ''}`
          : 'AI gặp sự cố khi soạn bài. Hãy thử lại!'
      );
    } finally { setIsLoading(false); }
  };

  const handleSaveExercises = async () => {
    if (exercises.length === 0) return;
    setIsSaving(true);
    try {
      let finalCourseId = selectedCourseId;

      // 1. XỬ LÝ TẠO COURSE MỚI (NẾU CHỌN TAB NEW COURSE)
      if (isCreatingCourse) {
        if (!newCourseTitle.trim()) {
            setIsSaving(false);
            return alert("Vui lòng nhập tên khóa học!");
        }
        const courseRes = await api.post('/courses', {
          title: newCourseTitle,
          description: newCourseDescription,
          language: language,
        });
        // Lấy ID trả về từ Backend (CUID String)
        finalCourseId = courseRes.data.data.id || courseRes.data.id; 
      }

      // 2. LƯU BÀI HỌC VÀO DATABASE
      // SỬA LỖI: Không ép kiểu Number cho ID (vì DB dùng String/CUID) 
      // SỬA LỖI: Đổi key 'exercises' thành 'content' để khớp Prisma
      await api.post('/courses/lessons/ai-generate', { 
        courseId: String(finalCourseId), 
        title: topic, 
        content: exercises 
      });

      alert(`🎉 Đã lưu bài tập vào hệ thống thành công!`); 
      setExercises([]); 
      setTopic('');
      
      if (isCreatingCourse) {
        setNewCourseTitle(''); 
        setNewCourseDescription('');
        setIsCreatingCourse(false);
        fetchCourses(); // Cập nhật lại danh sách dropdown
      }
    } catch (err: any) { 
        console.error("Lỗi chi tiết:", err.response?.data || err.message);
        alert(err.response?.data?.message || "Lỗi khi lưu vào Database. Vui lòng kiểm tra lại ID khóa học."); 
    } finally { setIsSaving(false); }
  };

  return (
    <div className="p-8 space-y-8 animate-in fade-in duration-500">
      {/* HEADER SECTION */}
      <header className="flex justify-between items-end text-left">
        <div>
          <h1 className="text-3xl font-black tracking-tight flex items-center gap-3">
            <span className="material-symbols-outlined text-primary text-4xl">magic_button</span>
            Trình tạo nội dung AI
          </h1>
          <p className="text-slate-500 mt-1 italic">Sử dụng trí tuệ nhân tạo để kiến tạo nội dung học tập chất lượng cao.</p>
        </div>
      </header>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-8">
        
        {/* PANEL TRÁI: CẤU HÌNH */}
        <div className="xl:col-span-1 space-y-6">
          <div className="bg-white dark:bg-[#1e293b] p-6 rounded-4xl border border-slate-200 dark:border-slate-800 shadow-sm sticky top-8">
            
            <div className="mb-8 p-1 bg-slate-100 dark:bg-[#0f172a] rounded-2xl flex border border-slate-200 dark:border-slate-800">
              <button 
                onClick={() => setIsCreatingCourse(false)}
                className={`flex-1 text-[10px] font-black uppercase tracking-widest py-2.5 rounded-xl transition-all ${!isCreatingCourse ? 'bg-white dark:bg-slate-700 text-primary shadow-lg' : 'text-slate-400 hover:text-slate-600'}`}
              >
                Khóa học có sẵn
              </button>
              <button 
                onClick={() => setIsCreatingCourse(true)}
                className={`flex-1 text-[10px] font-black uppercase tracking-widest py-2.5 rounded-xl transition-all ${isCreatingCourse ? 'bg-primary text-white shadow-lg shadow-primary/20' : 'text-slate-400 hover:text-slate-600'}`}
              >
                + Khóa học mới
              </button>
            </div>

            <form onSubmit={handleGenerateExercises} className="space-y-5 text-left">
              {isCreatingCourse ? (
                <div className="space-y-4 animate-in slide-in-from-top-2 duration-300">
                  <div className="space-y-2">
                    <label className="text-[10px] font-black text-slate-400 uppercase ml-1">Tên khóa học mới</label>
                    <input value={newCourseTitle} onChange={(e) => setNewCourseTitle(e.target.value)} placeholder="VD: Giao tiếp hằng ngày" className="w-full bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 rounded-xl p-3 text-sm focus:ring-2 focus:ring-primary outline-none" />
                  </div>
                  <div className="space-y-2">
                    <label className="text-[10px] font-black text-slate-400 uppercase ml-1">Mô tả ngắn</label>
                    <textarea value={newCourseDescription} onChange={(e) => setNewCourseDescription(e.target.value)} rows={2} className="w-full bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 rounded-xl p-3 text-sm resize-none outline-none" placeholder="Khóa học này tập trung vào nội dung nào?" />
                  </div>
                </div>
              ) : (
                <div className="space-y-2">
                  <label className="text-[10px] font-black text-slate-400 uppercase ml-1">Chọn khóa học đích</label>
                  <select value={selectedCourseId} onChange={(e) => setSelectedCourseId(e.target.value)} className="w-full bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 rounded-xl p-3 text-sm outline-none font-bold text-primary">
                    {courses.map(c => <option key={c.id} value={c.id}>{c.title}</option>)}
                  </select>
                </div>
              )}

              <hr className="border-slate-100 dark:border-slate-800 my-4" />

              <div className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <label className="text-[10px] font-black text-slate-400 uppercase ml-1">Ngôn ngữ</label>
                    <select value={language} onChange={(e) => setLanguage(e.target.value)} className="w-full bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 rounded-xl p-3 text-sm outline-none">
                      <option value="EN">Tiếng Anh</option>
                      <option value="ZH">Tiếng Trung</option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <label className="text-[10px] font-black text-slate-400 uppercase ml-1">Trình độ</label>
                    <select value={level} onChange={(e) => setLevel(e.target.value)} className="w-full bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 rounded-xl p-3 text-sm outline-none">
                      <option value="Beginner">A1 Cơ bản</option>
                      <option value="Intermediate">B1 Trung cấp</option>
                      <option value="Advanced">C1 Nâng cao</option>
                    </select>
                  </div>
                </div>

                <div className="space-y-2">
                  <label className="text-[10px] font-black text-slate-400 uppercase ml-1">Chủ đề bài học</label>
                  <input type="text" value={topic} onChange={(e) => setTopic(e.target.value)} placeholder="VD: Gọi cà phê" className="w-full bg-slate-50 dark:bg-slate-950 border-slate-200 dark:border-slate-800 rounded-xl p-3 text-sm font-bold focus:ring-1 focus:ring-primary outline-none" required />
                </div>
              </div>

              <button 
                type="submit" 
                disabled={isLoading || !topic.trim()}
                className="w-full mt-6 bg-primary text-white font-black py-4 rounded-2xl flex items-center justify-center gap-3 hover:bg-primary/90 transition-all shadow-lg shadow-primary/20 disabled:opacity-50"
              >
                {isLoading ? <span className="material-symbols-outlined animate-spin">progress_activity</span> : <span className="material-symbols-outlined">psychology</span>}
                {isLoading ? 'AI đang soạn bài...' : 'Tạo bài học AI'}
              </button>
            </form>
          </div>
        </div>

        {/* PANEL PHẢI: KẾT QUẢ */}
        <div className="xl:col-span-2 space-y-6">
          {error && (
            <div className="bg-red-50 dark:bg-red-900/10 text-red-500 p-4 rounded-2xl border border-red-100 dark:border-red-900/20 flex items-center gap-3 font-bold text-sm">
              <span className="material-symbols-outlined">warning</span> {error}
            </div>
          )}

          {!isLoading && exercises.length === 0 && (
            <div className="h-100 flex flex-col items-center justify-center text-slate-300 dark:text-slate-700 bg-white dark:bg-[#1e293b] rounded-[2.5rem] border-2 border-dashed border-slate-200 dark:border-slate-800 transition-colors">
              <span className="material-symbols-outlined text-7xl mb-4 opacity-20">auto_awesome_motion</span>
              <p className="text-sm font-bold uppercase tracking-widest">Cấu hình bên trái để bắt đầu soạn thảo</p>
            </div>
          )}

          {exercises.map((ex, index) => (
            <div key={index} className="bg-white dark:bg-[#1e293b] p-8 rounded-4xl border border-slate-200 dark:border-slate-800 shadow-sm relative group animate-in slide-in-from-bottom-4 duration-500 text-left" style={{ animationDelay: `${index * 100}ms` }}>
              <div className="absolute top-0 left-8 w-12 h-1 bg-primary rounded-b-full shadow-[0_4px_10px_rgba(25,120,229,0.5)]"></div>
              
              <div className="flex justify-between items-center mb-6">
                <span className="px-3 py-1 bg-primary/10 text-primary text-[10px] font-black uppercase tracking-widest rounded-md">
                  Câu #{index + 1} • {ex.type.replace('-', ' ')}
                </span>
              </div>

              <h4 className="text-xl font-bold text-slate-800 dark:text-white mb-6 leading-relaxed">{ex.question}</h4>

              {ex.type === 'multiple-choice' && ex.options && (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3 mb-6">
                  {ex.options.map((opt, i) => (
                    <div key={i} className={`p-4 rounded-2xl border transition-all text-sm font-bold flex items-center gap-3 ${opt === ex.answer ? 'bg-emerald-50 dark:bg-emerald-900/10 border-emerald-200 dark:border-emerald-800 text-emerald-600' : 'bg-slate-50 dark:bg-[#0f172a] border-slate-100 dark:border-slate-800 text-slate-500'}`}>
                      <span className="size-6 rounded-lg bg-white dark:bg-slate-800 border flex items-center justify-center text-[10px] font-black">{String.fromCharCode(65 + i)}</span>
                      {opt}
                    </div>
                  ))}
                </div>
              )}

              {(ex.type === 'fill-in-the-blank' || ex.type === 'ordering') && (
                <div className="mb-6 p-4 bg-emerald-50 dark:bg-emerald-900/10 rounded-2xl border border-emerald-100 dark:border-emerald-800 flex items-center gap-3">
                  <span className="text-[10px] font-black text-emerald-600 uppercase tracking-tighter">Đáp án đúng:</span>
                  <span className="font-mono font-bold text-emerald-600 text-lg">{ex.answer}</span>
                </div>
              )}

              <div className="mt-6 pt-6 border-t border-slate-100 dark:border-slate-800">
                <div className="flex gap-3">
                  <span className="material-symbols-outlined text-primary text-sm">info</span>
                  <p className="text-sm text-slate-500 dark:text-slate-400 leading-relaxed italic">
                    <span className="font-black text-slate-700 dark:text-slate-200 not-italic uppercase text-[10px] mr-2">AI giải thích:</span> 
                    {ex.explanation}
                  </p>
                </div>
              </div>
            </div>
          ))}

          {exercises.length > 0 && (
            <div className="pt-6 animate-in fade-in zoom-in duration-500">
              <button 
                onClick={handleSaveExercises}
                disabled={isSaving}
                className="w-full bg-slate-900 dark:bg-white text-white dark:text-slate-900 font-black py-5 rounded-4xl flex items-center justify-center gap-3 shadow-2xl hover:scale-[1.02] transition-all disabled:opacity-50"
              >
                {isSaving ? <span className="material-symbols-outlined animate-spin">sync</span> : <span className="material-symbols-outlined">cloud_upload</span>}
                {isSaving ? 'Đang đồng bộ database...' : 'Hoàn tất và lưu vào khóa học'}
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
