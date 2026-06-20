import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import LanguageSwitcher from '../components/LanguageSwitcher';
import ThemeSwitcher from '../components/ThemeSwitcher';
import { useLanguage } from '../contexts/LanguageContext';
import { useAuth } from '../contexts/AuthContext';
import api from '../api/axios';
// import api from '../api/axios'; // Tạm ẩn để test giả lập

export default function Login() {
  const { t } = useLanguage();
  const { login } = useAuth();
  const navigate = useNavigate();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

 const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setIsLoading(true);
      setError(null);

      // --- KẾT NỐI BACKEND THẬT ---
      // Bắn email và password lên cổng 5000 (Express)
      const response = await api.post('/auth/login', { email, password });
      
      // Lấy dữ liệu thật do Backend trả về
      const { token, user } = response.data;

      // Nạp vào hệ thống và chuyển hướng
      login(user, token);
      navigate('/dashboard'); 
      if (user.role === 'ADMIN') {
        navigate('/admin'); // Admin đi cổng VIP
      } else {
        navigate('/dashboard'); // Học viên đi cổng thường
      }

    } 

     catch (err: any) {
      console.error('Login error:', err);
      setError(err.response?.data?.message || 'Email hoặc mật khẩu không chính xác');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSocialSignIn = (provider: string) => {
    alert(`Tính năng đăng nhập bằng ${provider} đang được tích hợp!`);
  };

  return (
    <div className="flex min-h-screen bg-white dark:bg-background-dark font-display text-text-light dark:text-text-dark relative transition-colors duration-500">
      <Link to="/" className="absolute top-6 left-6 z-50 flex items-center justify-center size-10 rounded-full bg-black/5 dark:bg-white/10 text-text-light dark:text-text-dark hover:bg-black/10 dark:hover:bg-white/20 transition-colors lg:text-white lg:bg-white/10">
        <span className="material-symbols-outlined">arrow_back</span>
      </Link>

      <div className="absolute top-6 right-6 z-50 flex items-center gap-2">
        <ThemeSwitcher />
        <LanguageSwitcher />
      </div>

      <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden bg-background-dark items-center justify-center p-12">
        <div className="absolute inset-0 circuit-pattern opacity-10"></div>
        <div className="absolute inset-0 bg-linear-to-br from-primary/20 via-transparent to-transparent"></div>
        
        <div className="relative z-10 max-w-lg w-full">
          <Link to="/" className="flex items-center gap-3 mb-12 hover:opacity-80 transition-opacity">
            <div className="size-8 text-primary">
              <svg fill="none" viewBox="0 0 48 48" xmlns="http://www.w3.org/2000/svg">
                <path d="M44 4H30.6666V17.3334H17.3334V30.6666H4V44H44V4Z" fill="currentColor"></path>
              </svg>
            </div>
            <h2 className="text-2xl font-bold tracking-tight text-white">LinguaConnect <span className="text-primary text-sm align-top font-black ml-1 uppercase">AI</span></h2>
          </Link>

          <div className="relative group">
            <div className="absolute -inset-1 bg-linear-to-r from-primary to-ai-glow rounded-3xl blur opacity-25 group-hover:opacity-40 transition duration-1000"></div>
            <div className="relative rounded-2xl overflow-hidden border border-white/10 shadow-2xl bg-background-dark/80 backdrop-blur-md">
              <img alt="AI Tutor" className="w-full aspect-square object-cover" src="https://images.unsplash.com/photo-1675271591211-126ad94e495d?auto=format&fit=crop&q=80&w=800" />
              <div className="absolute bottom-0 left-0 right-0 bg-linear-to-t from-black/90 p-8">
                <h3 className="text-2xl font-bold text-white mb-2">{t('auth_side_title')}</h3>
                <p className="text-gray-300">{t('auth_side_desc')}</p>
              </div>
            </div>
          </div>

          <div className="mt-12 flex gap-8">
            <div className="flex flex-col">
              <span className="text-primary font-bold text-2xl">24/7</span>
              <span className="text-gray-500 text-sm uppercase font-semibold">{t('auth_side_stat1')}</span>
            </div>
            <div className="flex flex-col">
              <span className="text-white font-bold text-2xl">99%</span>
              <span className="text-gray-500 text-sm uppercase font-semibold">{t('auth_side_stat2')}</span>
            </div>
            <div className="flex flex-col">
              <span className="text-secondary font-bold text-2xl">HSK/IELTS</span>
              <span className="text-gray-500 text-sm uppercase font-semibold">{t('auth_side_stat3')}</span>
            </div>
          </div>
        </div>
      </div>

      <div className="w-full lg:w-1/2 flex items-center justify-center p-6 sm:p-12 bg-white dark:bg-background-dark transition-colors duration-500">
        <div className="w-full max-w-md space-y-8">
          <div className="text-center lg:text-left">
            <h1 className="text-3xl font-black tracking-tight">{t('login_title')}</h1>
            <p className="mt-2 text-gray-500 dark:text-gray-400">{t('login_subtitle')}</p>
            {error && <div data-testid="login-error" className="mt-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded-xl text-sm animate-shake">{error}</div>}
          </div>

          <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
            <div className="space-y-4">
              <InputGroup 
                label={t('login_email')} 
                type="email" 
                id="email" 
                placeholder="name@company.com" 
                value={email}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => setEmail(e.target.value)}
              />
              <InputGroup 
                label={t('login_pass')} 
                type="password" 
                id="password" 
                placeholder="••••••••" 
                value={password}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => setPassword(e.target.value)}
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="flex items-center">
                <input className="h-4 w-4 text-primary border-gray-300 dark:border-border-dark rounded cursor-pointer" id="remember-me" type="checkbox" />
                <label className="ml-2 block text-sm text-gray-700 dark:text-gray-300 cursor-pointer" htmlFor="remember-me">{t('login_rem')}</label>
              </div>
              <Link to="/forgot-password" className="text-sm font-bold text-primary hover:opacity-80 transition-colors">{t('login_forgot')}</Link>
            </div>

            <button data-testid="login-submit" disabled={isLoading} className="w-full flex justify-center py-3.5 px-4 border border-transparent text-sm font-bold rounded-full text-white bg-primary hover:bg-primary/90 transition-all transform hover:scale-[1.01] ai-glow-effect disabled:opacity-50" type="submit">
              {isLoading ? 'Connecting to AI...' : t('login_btn')}
            </button>
          </form>

          <div className="mt-6">
            <div className="relative">
              <div className="absolute inset-0 flex items-center"><div className="w-full border-t border-border-light dark:border-border-dark"></div></div>
              <div className="relative flex justify-center text-sm">
                <span className="px-2 bg-white dark:bg-background-dark text-gray-500 font-medium">{t('login_or')}</span>
              </div>
            </div>
            <div className="mt-6 grid grid-cols-2 gap-4">
              <button onClick={() => handleSocialSignIn('Google')} type="button" className="flex w-full items-center justify-center gap-3 px-4 py-3 border border-border-light dark:border-border-dark rounded-xl bg-white dark:bg-white/5 hover:bg-gray-50 dark:hover:bg-white/10 transition-colors text-sm font-bold">Google</button>
              <button onClick={() => handleSocialSignIn('Apple')} type="button" className="flex w-full items-center justify-center gap-3 px-4 py-3 border border-border-light dark:border-border-dark rounded-xl bg-white dark:bg-white/5 hover:bg-gray-50 dark:hover:bg-white/10 transition-colors text-sm font-bold">Apple</button>
            </div>
          </div>

          <p className="text-center text-sm text-gray-500 dark:text-gray-400">
            {t('login_new')} <Link to="/signup" className="font-bold text-primary hover:underline ml-1">{t('login_signup')}</Link>
          </p>
        </div>
      </div>
    </div>
  );
}

function InputGroup({ label, type, id, placeholder, value, onChange }: any) {
  return (
    <div>
      <label className="block text-sm font-semibold text-gray-700 dark:text-gray-300 mb-1" htmlFor={id}>{label}</label>
      <input 
        className="block w-full px-4 py-3 border border-border-light dark:border-border-dark rounded-xl focus:ring-2 focus:ring-primary bg-background-light dark:bg-white/5 transition-all outline-none" 
        id={id} required type={type} placeholder={placeholder} value={value} onChange={onChange}
      />
    </div>
  );
}
