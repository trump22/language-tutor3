import { useState } from 'react';
// Import LanguageCode dưới dạng type để đúng chuẩn TypeScript mới
import { useLanguage, type LanguageCode } from '../contexts/LanguageContext';

const languages: { code: LanguageCode; name: string; flag: string }[] = [
  { code: 'en', name: 'Tiếng Anh', flag: 'https://flagcdn.com/w40/us.png' },
  { code: 'vi', name: 'Tiếng Việt', flag: 'https://flagcdn.com/w40/vn.png' },
  { code: 'zh', name: 'Tiếng Trung', flag: 'https://flagcdn.com/w40/cn.png' },
];

export default function LanguageSwitcher() {
  const [isOpen, setIsOpen] = useState(false);
  const { language, setLanguage } = useLanguage();
  
  const currentLang = languages.find(l => l.code === language) || languages[0];

  return (
    <div className="relative">
      {/* Nút bấm chính */}
      <button 
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center justify-center size-10 rounded-full bg-black/5 dark:bg-white/10 hover:bg-black/10 dark:hover:bg-white/20 transition-all overflow-hidden border-2 border-transparent hover:border-primary/50 cursor-pointer"
        title="Đổi ngôn ngữ"
      >
        <img 
          src={currentLang.flag} 
          alt={currentLang.name} 
          className="w-full h-full object-cover scale-110" 
        />
      </button>
      
      {/* Menu thả xuống */}
      {isOpen && (
        <>
          {/* Lớp phủ để đóng menu khi click ra ngoài */}
          <div 
            className="fixed inset-0 z-40 bg-transparent" 
            onClick={() => setIsOpen(false)}
          ></div>
          
          <div className="absolute right-0 mt-3 w-44 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-2xl z-50 overflow-hidden animate-in fade-in zoom-in duration-200">
            <div className="py-1">
              {languages.map((lang) => (
                <button
                  key={lang.code}
                  onClick={() => {
                    setLanguage(lang.code);
                    setIsOpen(false);
                  }}
                  className={`flex items-center gap-3 w-full px-4 py-3 text-sm text-left hover:bg-gray-50 dark:hover:bg-white/5 transition-colors ${
                    currentLang.code === lang.code 
                      ? 'bg-primary/10 text-primary font-bold' 
                      : 'text-gray-700 dark:text-gray-300'
                  }`}
                >
                  <img 
                    src={lang.flag} 
                    alt={lang.name} 
                    className="w-5 h-5 rounded-full object-cover shadow-sm" 
                  />
                  <span>{lang.name}</span>
                  {currentLang.code === lang.code && (
                    <div className="ml-auto size-1.5 rounded-full bg-primary"></div>
                  )}
                </button>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
