import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../api/axios';

// --- INTERFACES ---
interface Course {
  id: number; // Đã chuẩn hóa kiểu dữ liệu Prisma
  title: string;
  description: string;
  language: string;
  level: string;
  thumbnail?: string;
  totalLessons?: number;
}

const languageLabel: Record<string, string> = {
  EN: 'Tiếng Anh',
  ZH: 'Tiếng Trung',
};

const levelLabel: Record<string, string> = {
  Beginner: 'Cơ bản',
  Intermediate: 'Trung cấp',
  Advanced: 'Nâng cao',
};

export default function Courses() {
  const [courses, setCourses] = useState<Course[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [activeFilter, setActiveFilter] = useState('ALL');

  useEffect(() => {
    const fetchCourses = async () => {
      try {
        const response = await api.get('/courses');
        const data = response.data.data || response.data.courses || response.data;
        setCourses(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error('Lỗi tải khóa học:', err);
        setError('Không thể tải danh sách khóa học lúc này.');
      } finally {
        setIsLoading(false);
      }
    };
    fetchCourses();
  }, []);

  // Logic lọc theo ngôn ngữ
  const filteredCourses = courses.filter(course => {
    if (activeFilter === 'ALL') return true;
    return course.language.toUpperCase() === activeFilter;
  });

  return (
    <div data-testid="courses-page" className="max-w-7xl mx-auto space-y-10 animate-in fade-in duration-700 text-left">
      
      {/* 1. HEADER & FILTER SECTION */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
        <div>
          <h2 className="text-4xl font-black tracking-tighter text-slate-900 dark:text-white">Khám phá khóa học</h2>
          <p className="text-slate-500 dark:text-slate-400 mt-2 font-medium">Chọn lộ trình phù hợp để bắt đầu hành trình chinh phục ngôn ngữ.</p>
        </div>
        
        {/* Tabs Lọc Ngôn ngữ chuyên nghiệp */}
        <div className="flex p-1 bg-slate-200 dark:bg-slate-800/50 rounded-2xl w-fit border border-slate-300/50 dark:border-slate-700/50 shadow-sm">
          {['ALL', 'EN', 'ZH'].map((lang) => (
            <button 
              key={lang}
              onClick={() => setActiveFilter(lang)} 
              className={`px-6 py-2 text-[11px] font-black uppercase tracking-widest rounded-xl transition-all ${
                activeFilter === lang 
                  ? 'bg-white dark:bg-slate-700 text-primary shadow-xl scale-105' 
                  : 'text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'
              }`}
            >
              {lang === 'ALL' ? 'Tất cả' : languageLabel[lang]}
            </button>
          ))}
        </div>
      </div>

      {/* 2. CONTENT AREA */}
      {isLoading ? (
        <div className="flex flex-col justify-center items-center py-40 gap-4">
          <div className="size-12 border-[3px] border-primary border-t-transparent rounded-full animate-spin"></div>
          <p className="text-[10px] font-black text-primary animate-pulse uppercase tracking-[0.3em]">Đang chuẩn bị giáo trình...</p>
        </div>
      ) : error ? (
        <div className="bg-red-50 dark:bg-red-900/10 text-red-500 p-8 rounded-[2.5rem] text-center font-bold border border-red-100 dark:border-red-900/30">
          <span className="material-symbols-outlined text-4xl mb-2">error</span>
          <p>{error}</p>
        </div>
      ) : filteredCourses.length === 0 ? (
        <div className="py-40 text-center text-slate-400 bg-white dark:bg-slate-900/50 rounded-[3rem] border-2 border-dashed border-slate-200 dark:border-slate-800">
          <span className="material-symbols-outlined text-6xl mb-4 opacity-20">auto_stories</span>
          <p className="font-bold uppercase tracking-widest text-xs">Chưa có khóa học nào được tìm thấy</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-8">
          {filteredCourses.map((course) => (
            <Link 
              key={course.id} 
              to={`/courses/${course.id}`} 
              className="group bg-white dark:bg-slate-900 rounded-[2.5rem] border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm hover:shadow-[0_20px_50px_rgba(25,120,229,0.15)] hover:border-primary/40 transition-all duration-500 flex flex-col"
            >
              {/* Thumbnail Container */}
              <div className="h-48 bg-slate-100 dark:bg-slate-800 relative overflow-hidden">
                {course.thumbnail ? (
                  <img src={course.thumbnail} alt={course.title} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700" />
                ) : (
                  <div className="w-full h-full flex items-center justify-center bg-linear-to-br from-primary/10 to-primary/5 text-primary">
                    <span className="material-symbols-outlined text-6xl opacity-10 group-hover:scale-125 transition-transform duration-500">menu_book</span>
                  </div>
                )}
                
                {/* Language Badge */}
                <div className="absolute top-5 left-5 bg-white/90 dark:bg-slate-900/90 backdrop-blur-md text-[9px] font-black px-3 py-1.5 rounded-full text-slate-800 dark:text-slate-100 shadow-sm border border-white/20 uppercase tracking-tighter">
                  {course.language.toUpperCase() === 'EN' ? '🇬🇧 Tiếng Anh' : '🇨🇳 Tiếng Trung'}
                </div>
              </div>

              {/* Course Info */}
              <div className="p-7 flex-1 flex flex-col">
                <div className="flex items-center gap-2 mb-4">
                  <span className="text-[10px] font-black uppercase tracking-widest text-primary bg-primary/10 px-2 py-1 rounded-lg">
                    Trình độ: {levelLabel[course.level] || course.level || 'Cơ bản'}
                  </span>
                </div>
                
                <h3 className="font-bold text-xl mb-3 text-slate-900 dark:text-white line-clamp-2 leading-tight group-hover:text-primary transition-colors">
                  {course.title}
                </h3>
                
                <p className="text-sm text-slate-500 dark:text-slate-400 line-clamp-2 mb-8 leading-relaxed font-medium">
                  {course.description}
                </p>

                {/* Footer Info */}
                <div className="pt-6 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between mt-auto">
                  <div className="flex items-center gap-1.5 text-slate-400 text-[10px] font-black uppercase tracking-widest">
                    <span className="material-symbols-outlined text-sm">list_alt</span>
                    {course.totalLessons || 0} bài học
                  </div>
                  
                  <div className="size-10 rounded-2xl bg-slate-50 dark:bg-slate-800 text-slate-400 group-hover:bg-primary group-hover:text-white transition-all flex items-center justify-center shadow-inner">
                    <span className="material-symbols-outlined text-sm">play_arrow</span>
                  </div>
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
