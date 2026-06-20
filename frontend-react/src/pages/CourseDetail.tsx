import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import api from '../api/axios';

interface Lesson { id: string; title: string; }
interface Course { id: string; title: string; description: string; language: string; lessons: Lesson[]; }

export default function CourseDetail() {
  const { courseId } = useParams();
  const navigate = useNavigate();
  const [course, setCourse] = useState<Course | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchCourseDetail = async () => {
      try {
        const response = await api.get(`/courses/${courseId}`);
        setCourse(response.data.data);
      } catch (err) {
        setError("Không thể tải thông tin khóa học.");
      } finally {
        setIsLoading(false);
      }
    };
    if (courseId) fetchCourseDetail();
  }, [courseId]);

  if (isLoading) return (
    <div className="min-h-screen flex items-center justify-center bg-background-light dark:bg-background-dark">
      <div className="flex flex-col items-center gap-4">
        <div className="size-10 border-4 border-primary border-t-transparent rounded-full animate-spin"></div>
        <p className="text-xs font-black uppercase tracking-[0.3em] text-primary">Đang chuẩn bị giáo trình...</p>
      </div>
    </div>
  );

  if (error || !course) return <div className="p-20 text-center text-red-500 font-bold">{error || "Khóa học không tồn tại"}</div>;

  return (
    <div className="bg-background-light dark:bg-background-dark min-h-screen font-sans pb-20 transition-colors duration-500">
      
      {/* Header Banner */}
      <div className="bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 relative">
        <div className="max-w-5xl mx-auto px-6 py-12 md:py-20 relative z-10">
          <button onClick={() => navigate('/courses')} className="flex items-center gap-2 text-slate-400 hover:text-primary mb-10 transition-all font-bold text-xs uppercase tracking-widest group">
            <span className="material-symbols-outlined text-sm group-hover:-translate-x-1 transition-transform">arrow_back</span> 
            Quay lại thư viện
          </button>

          <div className="flex flex-col gap-6">
            <div className="px-4 py-1.5 bg-primary/10 text-primary font-black text-[10px] rounded-full uppercase tracking-[0.2em] w-fit border border-primary/20">
              {course.language === 'EN' ? '🇬🇧 Lộ trình tiếng Anh' : '🇨🇳 Lộ trình tiếng Trung'}
            </div>
            <h1 className="text-4xl md:text-6xl font-black text-slate-900 dark:text-white tracking-tighter">
              {course.title}
            </h1>
            <p className="max-w-2xl text-slate-500 dark:text-slate-400 text-lg leading-relaxed font-medium">
              {course.description}
            </p>
          </div>
        </div>
        {/* Decor effect */}
        <div className="absolute top-0 right-0 w-1/3 h-full bg-linear-to-l from-primary/5 to-transparent pointer-events-none"></div>
      </div>

      {/* Lesson List Section */}
      <div className="max-w-5xl mx-auto px-6 mt-16">
        <div className="flex items-center justify-between mb-10">
          <h2 className="text-2xl font-black text-slate-900 dark:text-white flex items-center gap-3">
            <span className="size-8 rounded-lg bg-primary/10 text-primary flex items-center justify-center">
              <span className="material-symbols-outlined text-lg">format_list_numbered</span>
            </span>
            Lộ trình học
          </h2>
          <span className="px-4 py-1 bg-slate-200 dark:bg-slate-800 rounded-full text-[10px] font-black text-slate-500 uppercase tracking-widest">
            {course.lessons.length} bài học
          </span>
        </div>

        {course.lessons.length === 0 ? (
          <div className="p-20 text-center bg-white dark:bg-slate-900 rounded-[2.5rem] border-2 border-dashed border-slate-200 dark:border-slate-800 text-slate-400 font-bold">
            Bài học đang được chuẩn bị...
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-4">
            {course.lessons.map((lesson, index) => (
              <Link 
                key={lesson.id} 
                to={`/lessons/${lesson.id}`}
                className="group flex items-center justify-between p-6 md:p-8 bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 hover:border-primary hover:shadow-2xl hover:shadow-primary/10 transition-all duration-300"
              >
                <div className="flex items-center gap-6">
                  <div className="size-14 rounded-2xl bg-slate-100 dark:bg-slate-800 text-slate-400 font-black text-xl group-hover:bg-primary group-hover:text-white transition-all flex items-center justify-center shadow-inner">
                    {(index + 1).toString().padStart(2, '0')}
                  </div>
                  <div>
                    <p className="text-[10px] font-black text-primary uppercase tracking-[0.2em] mb-1">Bài {(index+1)}</p>
                    <h3 className="text-xl font-bold text-slate-800 dark:text-slate-100 group-hover:text-primary transition-colors">
                      {lesson.title}
                    </h3>
                  </div>
                </div>
                <div className="flex items-center gap-3 px-5 py-2 rounded-2xl bg-slate-50 dark:bg-slate-800/50 text-slate-400 group-hover:bg-primary group-hover:text-white transition-all font-black text-[10px] uppercase tracking-widest">
                  Bắt đầu <span className="material-symbols-outlined text-sm group-hover:translate-x-1 transition-transform">play_arrow</span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
