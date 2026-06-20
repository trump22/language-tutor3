import { useState } from 'react';
import { Link } from 'react-router-dom';
import LanguageSwitcher from '../components/LanguageSwitcher';
import ThemeSwitcher from '../components/ThemeSwitcher';
import { useLanguage } from '../contexts/LanguageContext';
import api from '../api/axios'; // Import cỗ máy Axios của chúng ta

export default function ForgotPassword() {
  const { t } = useLanguage();
  const [email, setEmail] = useState('');
  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
  const [message, setMessage] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setStatus('loading');
    
    try {
      // Gửi yêu cầu đến Backend Express
      await api.post('/auth/forgot-password', { email });
      setStatus('success');
      setMessage('Một liên kết khôi phục đã được gửi đến email của bạn.');
    } catch (err: any) {
      setStatus('error');
      setMessage(err.response?.data?.message || 'Có lỗi xảy ra, vui lòng thử lại sau.');
    }
  };

  return (
    <div className="flex min-h-screen bg-white dark:bg-gray-900 font-sans text-slate-900 dark:text-slate-100 antialiased relative">
      {/* Nút Quay lại Login */}
      <Link to="/login" className="absolute top-6 left-6 z-50 flex items-center justify-center size-10 rounded-full bg-black/5 dark:bg-white/10 hover:bg-black/10 dark:hover:bg-white/20 transition-colors lg:text-white">
        <span className="material-symbols-outlined">arrow_back</span>
      </Link>

      {/* Switchers */}
      <div className="absolute top-6 right-6 z-50 flex items-center gap-2">
        <ThemeSwitcher />
        <LanguageSwitcher />
      </div>

      {/* Left Side: Art & Branding */}
      <div className="hidden lg:flex lg:w-1/2 relative bg-blue-600 overflow-hidden items-center justify-center">
        <div className="absolute inset-0 opacity-40 bg-linear-to-br from-blue-600 via-blue-900 to-black"></div>
        <div className="absolute inset-0" style={{ backgroundImage: 'radial-gradient(circle at 2px 2px, rgba(255,255,255,0.05) 1px, transparent 0)', backgroundSize: '40px 40px' }}></div>
        <div className="relative z-10 p-12 text-white max-w-xl">
          <Link to="/" className="mb-8 flex items-center gap-3 hover:opacity-80 transition-opacity">
            <div className="bg-white/20 p-2 rounded-lg backdrop-blur-md">
              <span className="material-symbols-outlined text-3xl">translate</span>
            </div>
            <h1 className="text-2xl font-bold tracking-tight">LinguaConnect AI</h1>
          </Link>
          <div className="space-y-6">
            <h2 className="text-5xl font-extrabold leading-tight">{t('forgot_side_title')}</h2>
            <p className="text-lg text-blue-100">{t('forgot_side_desc')}</p>
          </div>
          <div className="mt-12 rounded-2xl overflow-hidden border border-white/10 shadow-2xl">
            <img 
              alt="AI visualization" 
              className="w-full h-auto object-cover aspect-video" 
              src="https://images.unsplash.com/photo-1620712943543-bcc4628c9759?auto=format&fit=crop&q=80&w=800" 
            />
          </div>
        </div>
      </div>

      {/* Right Side: Form */}
      <div className="flex-1 flex flex-col items-center justify-center p-6 sm:p-12 lg:p-24 bg-white dark:bg-gray-900">
        <div className="w-full max-w-md space-y-8">
          <div className="lg:hidden flex items-center gap-2 mb-12">
            <span className="material-symbols-outlined text-blue-600 text-3xl">translate</span>
            <span className="text-xl font-bold">LinguaConnect AI</span>
          </div>

          <div className="space-y-2 text-center lg:text-left">
            <h2 className="text-3xl font-bold tracking-tight text-slate-900 dark:text-white">{t('forgot_title')}</h2>
            <p className="text-slate-500 dark:text-slate-400">{t('forgot_subtitle')}</p>
          </div>

          {status === 'success' ? (
            <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-xl text-green-700 dark:text-green-400 text-sm animate-in fade-in zoom-in">
              {message}
              <Link to="/login" className="block mt-4 font-bold underline">Quay lại đăng nhập</Link>
            </div>
          ) : (
            <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
              <div className="space-y-4">
                <div className="flex flex-col gap-2">
                  <label className="text-sm font-medium text-slate-700 dark:text-slate-300" htmlFor="email">{t('forgot_email')}</label>
                  <div className="relative">
                    <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-xl">mail</span>
                    <input 
                      className="block w-full rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900/50 px-10 py-3 text-slate-900 dark:text-white placeholder-slate-400 focus:border-blue-600 focus:ring-2 focus:ring-blue-600/20 transition-all outline-none" 
                      id="email" 
                      placeholder="name@example.com" 
                      required 
                      type="email"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                    />
                  </div>
                </div>
              </div>

              {status === 'error' && (
                <p className="text-red-500 text-xs italic">{message}</p>
              )}

              <div className="pt-2">
                <button 
                  disabled={status === 'loading'}
                  className="w-full flex items-center justify-center gap-2 bg-blue-600 hover:bg-blue-700 text-white font-bold py-3.5 px-4 rounded-lg transition-all shadow-lg shadow-blue-600/20 disabled:opacity-50" 
                  type="submit"
                >
                  <span>{status === 'loading' ? 'Sending...' : t('forgot_btn')}</span>
                  <span className="material-symbols-outlined text-lg">arrow_forward</span>
                </button>
              </div>

              <div className="flex items-center justify-center pt-4">
                <Link to="/login" className="flex items-center gap-2 text-sm font-semibold text-blue-600 hover:text-blue-700 transition-colors">
                  <span className="material-symbols-outlined text-lg">arrow_back</span>
                  {t('forgot_back')}
                </Link>
              </div>
            </form>
          )}

          <div className="mt-16 text-center">
            <p className="text-xs text-slate-400 dark:text-slate-500 uppercase tracking-widest font-medium">
              {t('forgot_secure')}
            </p>
            <div className="mt-4 flex justify-center gap-6 text-slate-400 text-xs">
              <a className="hover:text-blue-600 transition-colors" href="#">{t('forgot_privacy')}</a>
              <a className="hover:text-blue-600 transition-colors" href="#">{t('forgot_terms')}</a>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}