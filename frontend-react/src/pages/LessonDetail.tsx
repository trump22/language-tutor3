import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../api/axios';

interface Exercise {
  type: string;
  question: string;
  options?: string[];
  answer: string;
  explanation: string;
}

type RawExercise = Record<string, unknown>;

const CJK_REGEX = /[\u3400-\u9fff]/;

function asText(value: unknown) {
  if (typeof value === 'string') return value.trim();
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  return '';
}

function readFirstText(source: RawExercise, keys: string[]) {
  for (const key of keys) {
    const value = asText(source[key]);
    if (value) return value;
  }
  return '';
}

function normalizeOptions(value: unknown) {
  if (Array.isArray(value)) {
    return value.map(asText).filter(Boolean);
  }

  if (typeof value === 'string') {
    try {
      const parsed = JSON.parse(value);
      if (Array.isArray(parsed)) return parsed.map(asText).filter(Boolean);
    } catch {
      return value.split('|').map(asText).filter(Boolean);
    }
  }

  return [];
}

function normalizeType(value: string, options: string[]) {
  const normalized = value.toLowerCase().trim();
  if (normalized.includes('multiple') || normalized.includes('choice') || normalized === 'mcq') {
    return 'multiple-choice';
  }
  if (normalized.includes('order')) return 'ordering';
  if (normalized.includes('fill') || normalized.includes('blank')) return 'fill-in-the-blank';
  return options.length > 0 ? 'multiple-choice' : 'fill-in-the-blank';
}

function parseContent(content: unknown): unknown {
  if (typeof content !== 'string') return content;

  try {
    return JSON.parse(content);
  } catch {
    return content;
  }
}

function normalizeExercise(item: unknown): Exercise | null {
  if (!item || typeof item !== 'object') return null;

  const raw = item as RawExercise;
  const options = normalizeOptions(raw.options);
  const question = readFirstText(raw, ['question', 'text', 'prompt', 'sentence']);
  const answer = readFirstText(raw, ['answer', 'correctAnswer', 'correct_answer', 'solution']);
  const explanation = readFirstText(raw, ['explanation', 'reason', 'feedback']) || 'Chưa có giải thích cho câu này.';
  const type = normalizeType(readFirstText(raw, ['type', 'kind']), options);

  if (!question || !answer) return null;

  return {
    type,
    question,
    options,
    answer,
    explanation,
  };
}

function normalizeExercises(content: unknown): Exercise[] {
  const parsed = parseContent(content);

  if (Array.isArray(parsed)) {
    return parsed.map(normalizeExercise).filter(Boolean) as Exercise[];
  }

  if (parsed && typeof parsed === 'object') {
    const record = parsed as Record<string, unknown>;
    const nested = record.exercises || record.items || record.questions || record.data || record.content;
    if (nested && nested !== parsed) {
      return normalizeExercises(nested);
    }
  }

  return [];
}

function normalizeAnswer(value: string) {
  return value.trim().toLocaleLowerCase();
}

function textLang(value: string) {
  return CJK_REGEX.test(value) ? 'zh-CN' : undefined;
}

export default function LessonDetail() {
  const { lessonId } = useParams();
  const navigate = useNavigate();

  const [lessonTitle, setLessonTitle] = useState('');
  const [exercises, setExercises] = useState<Exercise[]>([]);
  const [currentQuestion, setCurrentQuestion] = useState(0);
  const [userAnswers, setUserAnswers] = useState<{ [key: number]: string }>({});
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [score, setScore] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchLesson = async () => {
      try {
        const response = await api.get(`/courses/lessons/${lessonId}`);
        const data = response.data.data || response.data;

        setLessonTitle(data.title || 'Bài học');
        setExercises(normalizeExercises(data.content));
        setCurrentQuestion(0);
        setUserAnswers({});
        setIsSubmitted(false);
        setScore(0);
      } catch (err) {
        console.error('Lỗi tải bài học:', err);
        setError('Không thể tải bài học. Vui lòng thử lại sau.');
      } finally {
        setIsLoading(false);
      }
    };

    if (lessonId) void fetchLesson();
  }, [lessonId]);

  const handleAnswerChange = (answer: string) => {
    if (isSubmitted) return;
    setUserAnswers(prev => ({ ...prev, [currentQuestion]: answer }));
  };

  const handleSubmit = async () => {
    let currentScore = 0;

    exercises.forEach((ex, index) => {
      const userAnswer = normalizeAnswer(userAnswers[index] || '');
      const correctAnswer = normalizeAnswer(ex.answer);

      if (userAnswer === correctAnswer) {
        currentScore += 1;
      }
    });

    setScore(currentScore);
    setIsSubmitted(true);

    try {
      await api.post(`/courses/lessons/${lessonId}/score`, {
        score: currentScore,
        totalQuestions: exercises.length,
        completionTime: 120,
      });
    } catch (err) {
      console.error('Lỗi khi lưu điểm:', err);
    }
  };

  if (isLoading) {
    return <div className="p-8 text-center text-slate-500 font-bold mt-20 animate-pulse">Đang tải bài học...</div>;
  }

  if (error) {
    return <div className="p-8 text-center text-red-500 font-bold mt-20">{error}</div>;
  }

  if (exercises.length === 0) {
    return <div className="p-8 text-center text-slate-500 mt-20">Bài học này chưa có nội dung bài tập.</div>;
  }

  const currentEx = exercises[currentQuestion];
  const progressPercentage = ((currentQuestion + 1) / exercises.length) * 100;
  const currentUserAnswer = userAnswers[currentQuestion] || '';
  const isCurrentAnswerCorrect = normalizeAnswer(currentUserAnswer) === normalizeAnswer(currentEx.answer);

  return (
    <div className="max-w-3xl mx-auto p-6 md:p-12 font-display min-h-screen">
      <div className="mb-8">
        <button onClick={() => navigate(-1)} className="flex items-center text-slate-400 hover:text-primary mb-4 transition-colors font-medium text-sm">
          <span className="material-symbols-outlined mr-1">arrow_back</span> Quay lại
        </button>
        <h1 className="text-3xl font-bold text-slate-900 dark:text-white whitespace-pre-wrap" lang={textLang(lessonTitle)}>
          {lessonTitle}
        </h1>

        <div className="mt-6 flex items-center gap-4">
          <div className="flex-1 bg-slate-200 dark:bg-slate-700 h-3 rounded-full overflow-hidden">
            <div
              className="bg-primary h-full rounded-full transition-all duration-500"
              style={{ width: `${progressPercentage}%` }}
            />
          </div>
          <span className="text-sm font-bold text-slate-500">
            {currentQuestion + 1} / {exercises.length}
          </span>
        </div>
      </div>

      <div className="bg-white dark:bg-slate-900 p-8 rounded-3xl shadow-sm border border-slate-200 dark:border-slate-800">
        <div className="inline-block px-3 py-1 bg-primary/10 text-primary font-bold text-xs rounded-lg uppercase tracking-wider mb-6">
          {currentEx.type === 'multiple-choice' ? 'Trắc nghiệm' : currentEx.type === 'ordering' ? 'Sắp xếp câu' : 'Điền vào chỗ trống'}
        </div>

        <h2 className="text-xl md:text-2xl font-bold text-slate-800 dark:text-slate-100 mb-8 leading-relaxed whitespace-pre-wrap" lang={textLang(currentEx.question)}>
          {currentEx.question}
        </h2>

        {currentEx.type === 'multiple-choice' && currentEx.options && currentEx.options.length > 0 && (
          <div className="space-y-3">
            {currentEx.options.map((opt, i) => {
              const isSelected = currentUserAnswer === opt;
              let btnClass = 'w-full text-left p-4 rounded-xl border-2 font-medium transition-all whitespace-pre-wrap ';

              if (isSubmitted) {
                if (opt === currentEx.answer) {
                  btnClass += 'bg-green-50 border-green-500 text-green-700 dark:bg-green-900/30';
                } else if (isSelected && opt !== currentEx.answer) {
                  btnClass += 'bg-red-50 border-red-500 text-red-700 dark:bg-red-900/30';
                } else {
                  btnClass += 'border-slate-200 text-slate-400 opacity-50 dark:border-slate-700';
                }
              } else {
                btnClass += isSelected
                  ? 'bg-primary/5 border-primary text-primary'
                  : 'bg-white border-slate-200 text-slate-600 hover:border-primary/50 dark:bg-slate-800 dark:border-slate-700 dark:text-slate-200';
              }

              return (
                <button
                  key={`${opt}-${i}`}
                  disabled={isSubmitted}
                  onClick={() => handleAnswerChange(opt)}
                  className={btnClass}
                  lang={textLang(opt)}
                >
                  <span className="inline-block w-8 font-bold opacity-50">{String.fromCharCode(65 + i)}.</span>
                  {opt}
                </button>
              );
            })}
          </div>
        )}

        {(currentEx.type === 'fill-in-the-blank' || currentEx.type === 'ordering') && (
          <div>
            <input
              type="text"
              disabled={isSubmitted}
              value={currentUserAnswer}
              onChange={(e) => handleAnswerChange(e.target.value)}
              placeholder="Nhập câu trả lời của bạn..."
              className={`w-full p-4 text-lg font-medium border-2 rounded-xl outline-none transition-all ${
                isSubmitted
                  ? isCurrentAnswerCorrect
                    ? 'bg-green-50 border-green-500 text-green-700'
                    : 'bg-red-50 border-red-500 text-red-700'
                  : 'bg-slate-50 border-slate-200 focus:border-primary dark:bg-slate-800 dark:border-slate-700 dark:text-white'
              }`}
              lang={textLang(currentEx.answer)}
            />
          </div>
        )}

        {isSubmitted && (
          <div className="mt-8 p-5 bg-blue-50 dark:bg-blue-900/20 rounded-2xl border border-blue-100 dark:border-blue-900/50">
            <h4 className="font-bold text-blue-800 dark:text-blue-400 flex items-center gap-2 mb-2">
              <span className="material-symbols-outlined">psychology</span>
              AI giải thích:
            </h4>
            <p className="text-blue-900 dark:text-blue-200 text-sm leading-relaxed whitespace-pre-wrap" lang={textLang(currentEx.explanation)}>
              {currentEx.explanation}
            </p>
            {currentEx.type !== 'multiple-choice' && !isCurrentAnswerCorrect && (
              <p className="mt-3 text-sm font-bold text-green-600" lang={textLang(currentEx.answer)}>
                Đáp án đúng là: {currentEx.answer}
              </p>
            )}
          </div>
        )}
      </div>

      <div className="mt-8 flex justify-between items-center">
        <button
          disabled={currentQuestion === 0}
          onClick={() => setCurrentQuestion(prev => prev - 1)}
          className="px-6 py-3 rounded-xl font-bold text-slate-500 hover:bg-slate-100 disabled:opacity-30 transition-all dark:hover:bg-slate-800"
        >
          Câu trước
        </button>

        {!isSubmitted ? (
          currentQuestion === exercises.length - 1 ? (
            <button
              onClick={handleSubmit}
              className="px-8 py-3 rounded-xl font-bold bg-green-500 hover:bg-green-600 text-white shadow-lg transition-all"
            >
              Nộp bài
            </button>
          ) : (
            <button
              onClick={() => setCurrentQuestion(prev => prev + 1)}
              className="px-8 py-3 rounded-xl font-bold bg-primary hover:bg-primary/90 text-white shadow-lg transition-all flex items-center gap-2"
            >
              Câu tiếp <span className="material-symbols-outlined text-sm">arrow_forward</span>
            </button>
          )
        ) : (
          <div className="text-right">
            <div className="text-2xl font-black text-slate-900 dark:text-white mb-1">
              Điểm: <span className="text-primary">{score}/{exercises.length}</span>
            </div>
            {currentQuestion < exercises.length - 1 && (
              <button
                onClick={() => setCurrentQuestion(prev => prev + 1)}
                className="text-primary font-bold hover:underline"
              >
                Xem câu tiếp theo
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
