import { useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/axios';

interface Weakness {
  skill: string;
  issue: string;
  evidence: string;
  priority: 'high' | 'medium' | 'low' | string;
}

interface RecommendedLesson {
  lessonId?: string | null;
  title: string;
  reason: string;
}

interface WeeklyTask {
  day: string;
  focus: string;
  tasks: string[];
  durationMinutes: number;
}

interface GrammarFocus {
  topic: string;
  explanation: string;
  microExercise: string;
  answer: string;
}

interface PronunciationFocus {
  wordOrSound: string;
  tip: string;
}

interface CoachPlan {
  summary: string;
  levelAssessment: string;
  strengths: string[];
  weaknesses: Weakness[];
  recommendedLessons: RecommendedLesson[];
  weeklyPlan: WeeklyTask[];
  grammarFocus: GrammarFocus[];
  pronunciationFocus: PronunciationFocus[];
  nextAction: string;
}

interface CoachStats {
  language: string;
  completedLessons: number;
  averageLessonScore?: number | null;
  pronunciationAttempts: number;
  averagePronunciationScore?: number | null;
  recentChatMessages: number;
}

interface GrammarExercise {
  type: string;
  question: string;
  options?: string[];
  answer: string;
  explanation: string;
}

const priorityStyles: Record<string, string> = {
  high: 'bg-red-50 text-red-600 border-red-100 dark:bg-red-950/20 dark:text-red-300 dark:border-red-900/40',
  medium: 'bg-amber-50 text-amber-600 border-amber-100 dark:bg-amber-950/20 dark:text-amber-300 dark:border-amber-900/40',
  low: 'bg-emerald-50 text-emerald-600 border-emerald-100 dark:bg-emerald-950/20 dark:text-emerald-300 dark:border-emerald-900/40',
};

export default function LearningCoach() {
  const navigate = useNavigate();
  const [language, setLanguage] = useState('EN');
  const [level, setLevel] = useState('Beginner');
  const [plan, setPlan] = useState<CoachPlan | null>(null);
  const [stats, setStats] = useState<CoachStats | null>(null);
  const [source, setSource] = useState('');
  const [message, setMessage] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isGeneratingGrammar, setIsGeneratingGrammar] = useState(false);
  const [grammarExercises, setGrammarExercises] = useState<GrammarExercise[]>([]);

  useEffect(() => {
    void loadCoach();
  }, [language]);

  const loadCoach = async () => {
    setIsLoading(true);
    setMessage('');
    setGrammarExercises([]);

    try {
      const response = await api.get(`/ai/learning-coach?language=${language}`);
      setPlan(response.data.data);
      setStats(response.data.profile?.stats || null);
      setSource(response.data.source || 'gemini');
      setMessage(response.data.source === 'fallback' ? '' : response.data.message || '');
    } catch (error: unknown) {
      setPlan(null);
      setMessage('Không tải được lộ trình cá nhân hóa. Vui lòng kiểm tra backend hoặc đăng nhập lại.');
    } finally {
      setIsLoading(false);
    }
  };

  const generateGrammarPractice = async () => {
    setIsGeneratingGrammar(true);
    setMessage('');

    try {
      const response = await api.post('/ai/personalized-grammar-practice', { language, level });
      setGrammarExercises(response.data.exercises || []);
      setSource(response.data.source || source);
      setMessage(response.data.message || '');
    } catch (error: unknown) {
      setMessage('Không tạo được bài grammar cá nhân hóa.');
    } finally {
      setIsGeneratingGrammar(false);
    }
  };

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto space-y-6 animate-pulse">
        <div className="h-28 rounded-[2rem] bg-slate-200 dark:bg-slate-800" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 h-96 rounded-[2rem] bg-slate-200 dark:bg-slate-800" />
          <div className="h-96 rounded-[2rem] bg-slate-200 dark:bg-slate-800" />
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto space-y-8 animate-in fade-in duration-500 text-left">
      <header className="flex flex-col xl:flex-row xl:items-end justify-between gap-5">
        <div>
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary/10 text-primary text-[10px] font-black uppercase tracking-widest mb-4">
            <span className="material-symbols-outlined text-sm">auto_graph</span>
            Cố vấn học tập AI
          </div>
          <h1 className="text-4xl font-black tracking-tighter text-slate-900 dark:text-white">Lộ trình cá nhân hóa</h1>
          <p className="text-slate-500 mt-2 max-w-2xl">
            AI tổng hợp điểm bài học, lịch sử chat và phát âm để đề xuất trọng tâm luyện tập tiếp theo.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <select value={language} onChange={(e) => setLanguage(e.target.value)} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl px-4 py-3 text-xs font-black outline-none">
            <option value="EN">Tiếng Anh</option>
            <option value="ZH">Tiếng Trung</option>
          </select>
          <select value={level} onChange={(e) => setLevel(e.target.value)} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl px-4 py-3 text-xs font-black outline-none">
            <option value="Beginner">Cơ bản</option>
            <option value="Intermediate">Trung cấp</option>
            <option value="Advanced">Nâng cao</option>
          </select>
          <button onClick={loadCoach} className="px-5 py-3 rounded-2xl bg-slate-900 dark:bg-white text-white dark:text-slate-900 text-xs font-black uppercase tracking-widest">
            Làm mới
          </button>
        </div>
      </header>

      {message && (
        <div className="p-4 rounded-2xl border border-amber-200 dark:border-amber-900/50 bg-amber-50 dark:bg-amber-950/20 text-amber-700 dark:text-amber-300 text-sm font-semibold">
          {message}
        </div>
      )}

      <section className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <MetricCard icon="school" label="Bài đã chấm" value={stats?.completedLessons ?? 0} />
        <MetricCard icon="percent" label="Điểm bài học TB" value={formatScore(stats?.averageLessonScore)} />
        <MetricCard icon="mic" label="Lượt phát âm" value={stats?.pronunciationAttempts ?? 0} />
        <MetricCard icon="forum" label="Tin chat gần đây" value={stats?.recentChatMessages ?? 0} />
      </section>

      {plan ? (
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-8">
          <main className="xl:col-span-2 space-y-8">
            <section className="bg-white dark:bg-slate-900 rounded-[2rem] p-7 border border-slate-200 dark:border-slate-800 shadow-sm">
              <div className="flex items-start justify-between gap-4 mb-6">
                <div>
                  <h2 className="text-xl font-black text-slate-900 dark:text-white">Tổng quan</h2>
                  <p className="text-sm text-slate-500 mt-2 leading-relaxed">{plan.summary}</p>
                </div>
                <span className="px-3 py-1 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-500 text-[10px] font-black uppercase">
                  {source}
                </span>
              </div>
              <div className="p-5 rounded-2xl bg-primary/5 border border-primary/10">
                <p className="text-xs font-black uppercase tracking-widest text-primary mb-2">Đánh giá trình độ</p>
                <p className="text-sm font-semibold text-slate-700 dark:text-slate-200 leading-relaxed">{plan.levelAssessment}</p>
              </div>
            </section>

            <section className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <InfoPanel title="Điểm mạnh" icon="verified">
                <ul className="space-y-3">
                  {safeList(plan.strengths).map((item) => (
                    <li key={item} className="flex gap-3 text-sm text-slate-600 dark:text-slate-300">
                      <span className="material-symbols-outlined text-emerald-500 text-lg">check_circle</span>
                      {item}
                    </li>
                  ))}
                </ul>
              </InfoPanel>

              <InfoPanel title="Cần cải thiện" icon="priority_high">
                <div className="space-y-3">
                  {safeList(plan.weaknesses).map((item) => (
                    <div key={`${item.skill}-${item.issue}`} className={`p-4 rounded-2xl border ${priorityStyles[item.priority] || priorityStyles.medium}`}>
                      <div className="flex justify-between gap-3 mb-2">
                        <p className="font-black text-sm">{item.skill}</p>
                        <span className="text-[10px] font-black uppercase">{item.priority}</span>
                      </div>
                      <p className="text-sm font-semibold">{item.issue}</p>
                      <p className="text-xs opacity-75 mt-1">{item.evidence}</p>
                    </div>
                  ))}
                </div>
              </InfoPanel>
            </section>

            <section className="bg-white dark:bg-slate-900 rounded-[2rem] p-7 border border-slate-200 dark:border-slate-800 shadow-sm">
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-xl font-black text-slate-900 dark:text-white">Kế hoạch 5 ngày</h2>
                <span className="material-symbols-outlined text-primary">calendar_month</span>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {safeList(plan.weeklyPlan).map((day) => (
                  <div key={day.day} className="p-5 rounded-2xl bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-800">
                    <div className="flex items-center justify-between mb-3">
                      <p className="font-black text-primary text-sm">{day.day}</p>
                      <p className="text-[10px] font-black text-slate-400 uppercase">{day.durationMinutes} phút</p>
                    </div>
                    <h3 className="font-black text-slate-900 dark:text-white mb-3">{day.focus}</h3>
                    <ul className="space-y-2">
                      {safeList(day.tasks).map((task) => (
                        <li key={task} className="text-sm text-slate-600 dark:text-slate-300 flex gap-2">
                          <span className="size-1.5 rounded-full bg-primary mt-2 shrink-0" />
                          {task}
                        </li>
                      ))}
                    </ul>
                  </div>
                ))}
              </div>
            </section>

            <section className="bg-white dark:bg-slate-900 rounded-[2rem] p-7 border border-slate-200 dark:border-slate-800 shadow-sm">
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
                <div>
                  <h2 className="text-xl font-black text-slate-900 dark:text-white">Ngữ pháp cá nhân hóa</h2>
                  <p className="text-sm text-slate-500 mt-1">Tạo bài luyện dựa trên điểm yếu và lịch sử học tập.</p>
                </div>
                <button onClick={generateGrammarPractice} disabled={isGeneratingGrammar} className="px-5 py-3 rounded-2xl bg-primary text-white text-xs font-black uppercase tracking-widest shadow-lg shadow-primary/20 disabled:opacity-50">
                  {isGeneratingGrammar ? 'Đang tạo...' : 'Tạo bài ngữ pháp'}
                </button>
              </div>

              {grammarExercises.length > 0 ? (
                <div className="space-y-4">
                  {grammarExercises.map((exercise, index) => (
                    <div key={`${exercise.question}-${index}`} className="p-5 rounded-2xl bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-800">
                      <div className="flex items-center gap-2 mb-3">
                        <span className="px-2 py-1 rounded-lg bg-primary/10 text-primary text-[10px] font-black uppercase">#{index + 1}</span>
                        <span className="text-[10px] font-black uppercase text-slate-400">{exercise.type}</span>
                      </div>
                      <p className="font-bold text-slate-900 dark:text-white mb-3">{exercise.question}</p>
                      {safeList(exercise.options).length > 0 && (
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-2 mb-3">
                          {safeList(exercise.options).map((option) => (
                            <div key={option} className={`p-3 rounded-xl text-sm font-semibold border ${option === exercise.answer ? 'bg-emerald-50 border-emerald-200 text-emerald-700 dark:bg-emerald-950/20 dark:border-emerald-900/50 dark:text-emerald-300' : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-300'}`}>
                              {option}
                            </div>
                          ))}
                        </div>
                      )}
                      <p className="text-xs text-slate-500">
                        <span className="font-black text-slate-700 dark:text-slate-200">Đáp án:</span> {exercise.answer} · {exercise.explanation}
                      </p>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="p-5 rounded-2xl border border-dashed border-slate-200 dark:border-slate-800 text-sm text-slate-500">
                  Bấm nút tạo bài ngữ pháp để AI sinh 5 câu luyện theo hồ sơ học tập hiện tại.
                </div>
              )}
            </section>
          </main>

          <aside className="space-y-8">
            <InfoPanel title="Bài nên học" icon="route">
              <div className="space-y-4">
                {safeList(plan.recommendedLessons).map((lesson) => (
                  <button key={`${lesson.lessonId || lesson.title}`} onClick={() => lesson.lessonId && navigate(`/lessons/${lesson.lessonId}`)} className="w-full p-4 rounded-2xl bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-800 text-left hover:border-primary/40 transition-colors">
                    <p className="font-black text-sm text-slate-900 dark:text-white">{lesson.title}</p>
                    <p className="text-xs text-slate-500 mt-1 leading-relaxed">{lesson.reason}</p>
                  </button>
                ))}
              </div>
            </InfoPanel>

            <InfoPanel title="Trọng tâm ngữ pháp" icon="edit_note">
              <div className="space-y-4">
                {safeList(plan.grammarFocus).map((item) => (
                  <div key={item.topic} className="p-4 rounded-2xl bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-800">
                    <p className="font-black text-sm text-slate-900 dark:text-white">{item.topic}</p>
                    <p className="text-xs text-slate-500 mt-1 leading-relaxed">{item.explanation}</p>
                    <div className="mt-3 p-3 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800">
                      <p className="text-xs font-semibold text-slate-700 dark:text-slate-200">{item.microExercise}</p>
                      <p className="text-[11px] text-primary font-black mt-1">Đáp án: {item.answer}</p>
                    </div>
                  </div>
                ))}
              </div>
            </InfoPanel>

            <InfoPanel title="Phát âm" icon="record_voice_over">
              <div className="space-y-3">
                {safeList(plan.pronunciationFocus).map((item) => (
                  <div key={item.wordOrSound} className="p-4 rounded-2xl bg-slate-50 dark:bg-slate-950 border border-slate-100 dark:border-slate-800">
                    <p className="font-black text-sm text-slate-900 dark:text-white">{item.wordOrSound}</p>
                    <p className="text-xs text-slate-500 mt-1 leading-relaxed">{item.tip}</p>
                  </div>
                ))}
              </div>
            </InfoPanel>

            <div className="p-6 rounded-[2rem] bg-slate-900 text-white shadow-xl">
              <p className="text-[10px] font-black uppercase tracking-widest text-primary mb-3">Việc cần làm tiếp theo</p>
              <p className="text-sm font-semibold leading-relaxed">{plan.nextAction}</p>
            </div>
          </aside>
        </div>
      ) : (
        <div className="p-10 rounded-[2rem] border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 text-center">
          <p className="text-slate-500 font-semibold">Chưa có dữ liệu lộ trình để hiển thị.</p>
        </div>
      )}
    </div>
  );
}

function MetricCard({ icon, label, value }: { icon: string; label: string; value: string | number }) {
  return (
    <div className="bg-white dark:bg-slate-900 p-5 rounded-[1.5rem] border border-slate-200 dark:border-slate-800 shadow-sm">
      <div className="size-10 rounded-2xl bg-primary/10 text-primary flex items-center justify-center mb-4">
        <span className="material-symbols-outlined text-xl">{icon}</span>
      </div>
      <p className="text-2xl font-black text-slate-900 dark:text-white">{value}</p>
      <p className="text-[10px] font-black uppercase tracking-widest text-slate-400 mt-1">{label}</p>
    </div>
  );
}

function InfoPanel({ title, icon, children }: { title: string; icon: string; children: ReactNode }) {
  return (
    <section className="bg-white dark:bg-slate-900 rounded-[2rem] p-6 border border-slate-200 dark:border-slate-800 shadow-sm">
      <div className="flex items-center gap-2 mb-5">
        <span className="material-symbols-outlined text-primary">{icon}</span>
        <h2 className="font-black text-slate-900 dark:text-white">{title}</h2>
      </div>
      {children}
    </section>
  );
}

function formatScore(value?: number | null) {
  return typeof value === 'number' ? `${value}%` : '-';
}

function safeList<T>(value: T[] | undefined | null) {
  return Array.isArray(value) ? value : [];
}
