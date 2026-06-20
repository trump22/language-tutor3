// Xóa "React" và thêm "type" trước ReactNode
import { createContext, useContext, useState, type ReactNode } from 'react';
import { translations } from '../i18n/translations';

export type LanguageCode = 'en' | 'vi' | 'zh';

interface LanguageContextType {
  language: LanguageCode;
  setLanguage: (lang: LanguageCode) => void;
  // Giữ nguyên logic lấy key từ file translations của bạn
  t: (key: keyof typeof translations['en']) => string;
}

const LanguageContext = createContext<LanguageContextType | undefined>(undefined);

export const LanguageProvider = ({ children }: { children: ReactNode }) => {
  const [language, setLanguage] = useState<LanguageCode>('vi');

  const t = (key: keyof typeof translations['en']) => {
    // Đảm bảo trả về ngôn ngữ đã chọn, nếu không có thì về tiếng Anh, nếu không nữa thì trả về chính cái Key đó
    return translations[language][key] || translations['en'][key] || key;
  };

  return (
    <LanguageContext.Provider value={{ language, setLanguage, t }}>
      {children}
    </LanguageContext.Provider>
  );
};

export const useLanguage = () => {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error('useLanguage must be used within a LanguageProvider');
  }
  return context;
};
