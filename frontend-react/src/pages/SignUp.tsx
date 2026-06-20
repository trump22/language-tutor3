import { useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Apple,
  ArrowRight,
  BarChart3,
  CalendarDays,
  ChevronDown,
  Home,
  Languages,
  Lock,
  Mail,
  Phone,
  User,
  type LucideIcon,
} from 'lucide-react';
import LanguageSwitcher from '../components/LanguageSwitcher';
import ThemeSwitcher from '../components/ThemeSwitcher';
import { useAuth } from '../contexts/AuthContext';
import api from '../api/axios';

type SignUpForm = {
  name: string;
  email: string;
  phoneNumber: string;
  address: string;
  password: string;
  languagePreference: string;
  skillLevel: string;
  learningGoal: string;
  dateOfBirth: string;
};

const initialFormData: SignUpForm = {
  name: '',
  email: '',
  phoneNumber: '',
  address: '',
  password: '',
  languagePreference: 'en',
  skillLevel: 'beginner',
  learningGoal: 'general',
  dateOfBirth: '',
};

const today = new Date().toISOString().split('T')[0];

export default function SignUp() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [formData, setFormData] = useState<SignUpForm>(initialFormData);
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const updateForm = (field: keyof SignUpForm, value: string) => {
    setFormData((current) => ({ ...current, [field]: value }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!acceptedTerms) {
      setError('Bạn cần đồng ý với điều khoản dịch vụ và chính sách bảo mật.');
      return;
    }

    try {
      setIsLoading(true);
      setError(null);

      const response = await api.post('/auth/register', formData);
      const { token, user } = response.data;

      login(user, token);
      navigate('/dashboard');
    } catch (err: unknown) {
      const apiError = err as { response?: { data?: { message?: string } } };
      setError(apiError.response?.data?.message || 'Đăng ký thất bại, vui lòng thử lại.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSocialSignUp = (provider: 'Google' | 'Apple') => {
    alert(`Tính năng đăng ký bằng ${provider} đang được tích hợp.`);
  };

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-background-dark font-display text-slate-950 dark:text-text-dark transition-colors duration-300">
      <Link
        to="/"
        className="absolute left-5 top-5 z-20 flex size-10 items-center justify-center rounded-full bg-white text-slate-600 shadow-sm ring-1 ring-slate-200 transition hover:text-primary dark:bg-white/10 dark:text-white dark:ring-white/10"
        aria-label="Quay về trang chủ"
      >
        <span className="material-symbols-outlined">arrow_back</span>
      </Link>

      <div className="absolute right-5 top-5 z-20 flex items-center gap-2">
        <ThemeSwitcher />
        <LanguageSwitcher />
      </div>

      <main className="flex min-h-screen items-center justify-center px-4 py-20 sm:px-6">
        <section className="w-full max-w-[440px] rounded-lg bg-white p-6 shadow-xl shadow-slate-200/80 ring-1 ring-slate-200 dark:bg-slate-950 dark:shadow-black/30 dark:ring-slate-800 sm:p-8">
          <div className="mb-7">
            <h1 className="text-2xl font-black tracking-tight text-slate-950 dark:text-white sm:text-3xl">
              Tạo tài khoản của bạn
            </h1>
            <p className="mt-2 text-sm font-medium text-slate-500 dark:text-slate-400">
              Start your journey to bilingual excellence today.
            </p>
          </div>

          {error && (
            <div data-testid="signup-error" className="mb-5 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-200">
              {error}
            </div>
          )}

          <form className="space-y-4" onSubmit={handleSubmit}>
            <InputField
              id="name"
              label="Họ và tên"
              icon={User}
              placeholder="Nguyễn Văn A"
              value={formData.name}
              autoComplete="name"
              onChange={(event) => updateForm('name', event.target.value)}
            />

            <InputField
              id="email"
              label="Địa chỉ Email"
              type="email"
              icon={Mail}
              placeholder="example@email.com"
              value={formData.email}
              autoComplete="email"
              onChange={(event) => updateForm('email', event.target.value)}
            />

            <InputField
              id="phoneNumber"
              label="Số điện thoại"
              type="tel"
              icon={Phone}
              placeholder="090 123 4567"
              value={formData.phoneNumber}
              autoComplete="tel"
              onChange={(event) => updateForm('phoneNumber', event.target.value)}
            />

            <InputField
              id="address"
              label="Địa chỉ"
              icon={Home}
              placeholder="Nhập địa chỉ của bạn"
              value={formData.address}
              autoComplete="street-address"
              onChange={(event) => updateForm('address', event.target.value)}
            />

            <div className="grid gap-4 sm:grid-cols-[1fr_1fr]">
              <InputField
                id="password"
                label="Mật khẩu"
                type="password"
                icon={Lock}
                placeholder="••••••••"
                value={formData.password}
                autoComplete="new-password"
                onChange={(event) => updateForm('password', event.target.value)}
              />

              <div>
                <span className="mb-2 block text-xs font-black uppercase tracking-wide text-slate-500 dark:text-slate-400">
                  Chọn khóa học
                </span>
                <div className="grid grid-cols-2 gap-3">
                  <SelectField
                    id="languagePreference"
                    label="Ngôn ngữ"
                    icon={Languages}
                    value={formData.languagePreference}
                    onChange={(event) => updateForm('languagePreference', event.target.value)}
                    options={[
                      { value: 'en', label: 'Anh' },
                      { value: 'zh', label: 'Trung' },
                    ]}
                  />
                  <SelectField
                    id="skillLevel"
                    label="Trình độ"
                    icon={BarChart3}
                    value={formData.skillLevel}
                    onChange={(event) => updateForm('skillLevel', event.target.value)}
                    options={[
                      { value: 'beginner', label: 'Cơ bản' },
                      { value: 'intermediate', label: 'Trung cấp' },
                      { value: 'advanced', label: 'Nâng cao' },
                    ]}
                  />
                </div>
              </div>
            </div>

            <InputField
              id="dateOfBirth"
              label="Ngày sinh"
              type="date"
              icon={CalendarDays}
              value={formData.dateOfBirth}
              max={today}
              autoComplete="bday"
              onChange={(event) => updateForm('dateOfBirth', event.target.value)}
            />

            <label className="flex items-start gap-3 pt-1 text-xs font-medium text-slate-500 dark:text-slate-400">
              <input
                data-testid="accept-terms"
                className="mt-0.5 size-4 rounded border-slate-300 text-primary accent-primary"
                type="checkbox"
                checked={acceptedTerms}
                onChange={(event) => setAcceptedTerms(event.target.checked)}
              />
              <span>
                Tôi đồng ý với các{' '}
                <Link to="/terms" className="font-bold text-primary hover:underline">
                  Điều khoản Dịch vụ
                </Link>{' '}
                và{' '}
                <Link to="/privacy" className="font-bold text-primary hover:underline">
                  Chính sách Bảo mật
                </Link>
                .
              </span>
            </label>

            <button
              data-testid="signup-submit"
              className="flex h-12 w-full items-center justify-center gap-2 rounded-lg bg-primary px-5 text-sm font-black text-white shadow-lg shadow-primary/25 transition hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
              type="submit"
              disabled={isLoading}
            >
              {isLoading ? 'Đang đăng ký...' : 'Đăng ký ngay'}
              <ArrowRight className="size-4" aria-hidden="true" />
            </button>
          </form>

          <div className="my-7 flex items-center gap-4">
            <div className="h-px flex-1 bg-slate-200 dark:bg-slate-800" />
            <span className="text-[10px] font-black uppercase tracking-wider text-slate-400">
              Or sign up with
            </span>
            <div className="h-px flex-1 bg-slate-200 dark:bg-slate-800" />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <button
              type="button"
              onClick={() => handleSocialSignUp('Google')}
              className="flex h-10 items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white text-sm font-black text-slate-700 transition hover:bg-slate-50 dark:border-slate-800 dark:bg-white/5 dark:text-white dark:hover:bg-white/10"
            >
              <span className="text-base font-black text-[#4285f4]">G</span>
              Google
            </button>
            <button
              type="button"
              onClick={() => handleSocialSignUp('Apple')}
              className="flex h-10 items-center justify-center gap-2 rounded-lg border border-slate-200 bg-white text-sm font-black text-slate-700 transition hover:bg-slate-50 dark:border-slate-800 dark:bg-white/5 dark:text-white dark:hover:bg-white/10"
            >
              <Apple className="size-4" aria-hidden="true" />
              Apple
            </button>
          </div>

          <p className="mt-5 text-center text-sm font-medium text-slate-500 dark:text-slate-400">
            Đã có tài khoản?
            <Link to="/login" className="ml-1 font-black text-primary hover:underline">
              Đăng nhập
            </Link>
          </p>
        </section>
      </main>
    </div>
  );
}

type InputFieldProps = {
  id: string;
  label: string;
  value: string;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
  icon: LucideIcon;
  type?: string;
  placeholder?: string;
  autoComplete?: string;
  max?: string;
};

function InputField({
  id,
  label,
  value,
  onChange,
  icon: Icon,
  type = 'text',
  placeholder,
  autoComplete,
  max,
}: InputFieldProps) {
  return (
    <div>
      <label className="mb-2 block text-sm font-bold text-slate-700 dark:text-slate-200" htmlFor={id}>
        {label}
      </label>
      <div className="relative">
        <Icon className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
        <input
          className="h-11 w-full rounded-lg border border-slate-200 bg-slate-50 px-10 text-sm font-semibold text-slate-800 outline-none transition placeholder:text-slate-400 focus:border-primary focus:bg-white focus:ring-4 focus:ring-primary/10 dark:border-slate-800 dark:bg-slate-900 dark:text-white dark:focus:bg-slate-950"
          id={id}
          type={type}
          placeholder={placeholder}
          value={value}
          onChange={onChange}
          autoComplete={autoComplete}
          max={max}
          required
        />
      </div>
    </div>
  );
}

type SelectFieldProps = {
  id: keyof Pick<SignUpForm, 'languagePreference' | 'skillLevel'>;
  label: string;
  value: string;
  onChange: (event: ChangeEvent<HTMLSelectElement>) => void;
  icon: LucideIcon;
  options: Array<{ value: string; label: string }>;
};

function SelectField({ id, label, value, onChange, icon: Icon, options }: SelectFieldProps) {
  return (
    <label className="block">
      <span className="mb-1 block text-[10px] font-black uppercase tracking-wide text-slate-400">
        {label}
      </span>
      <span className="relative block">
        <Icon className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
        <select
          id={id}
          value={value}
          onChange={onChange}
          className="h-11 w-full appearance-none rounded-lg border border-slate-200 bg-slate-50 pl-8 pr-7 text-xs font-black text-slate-700 outline-none transition focus:border-primary focus:bg-white focus:ring-4 focus:ring-primary/10 dark:border-slate-800 dark:bg-slate-900 dark:text-white dark:focus:bg-slate-950"
        >
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        <ChevronDown className="pointer-events-none absolute right-2.5 top-1/2 size-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
      </span>
    </label>
  );
}
