import { useState, useRef, useEffect } from 'react';
import { useTheme } from '../contexts/ThemeContext';

export default function ThemeSwitcher() {
  const { theme, setTheme } = useTheme();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const getIcon = () => {
    switch (theme) {
      case 'light': return 'light_mode';
      case 'dark': return 'dark_mode';
      case 'system': return 'brightness_auto';
    }
  };

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center justify-center size-10 rounded-full bg-gray-200/50 dark:bg-white/10 text-text-light dark:text-text-dark hover:bg-gray-200 dark:hover:bg-white/20 transition-colors"
        aria-label="Toggle theme"
      >
        <span className="material-symbols-outlined text-[20px]">{getIcon()}</span>
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-36 rounded-xl shadow-lg bg-white dark:bg-background-dark border border-border-light dark:border-border-dark overflow-hidden z-50">
          <div className="py-1">
            <button
              onClick={() => { setTheme('light'); setIsOpen(false); }}
              className={`w-full text-left px-4 py-2 text-sm flex items-center gap-2 hover:bg-gray-100 dark:hover:bg-white/5 transition-colors ${theme === 'light' ? 'text-primary font-bold' : 'text-text-light dark:text-text-dark'}`}
            >
              <span className="material-symbols-outlined text-[18px]">light_mode</span>
              Light
            </button>
            <button
              onClick={() => { setTheme('dark'); setIsOpen(false); }}
              className={`w-full text-left px-4 py-2 text-sm flex items-center gap-2 hover:bg-gray-100 dark:hover:bg-white/5 transition-colors ${theme === 'dark' ? 'text-primary font-bold' : 'text-text-light dark:text-text-dark'}`}
            >
              <span className="material-symbols-outlined text-[18px]">dark_mode</span>
              Dark
            </button>
            <button
              onClick={() => { setTheme('system'); setIsOpen(false); }}
              className={`w-full text-left px-4 py-2 text-sm flex items-center gap-2 hover:bg-gray-100 dark:hover:bg-white/5 transition-colors ${theme === 'system' ? 'text-primary font-bold' : 'text-text-light dark:text-text-dark'}`}
            >
              <span className="material-symbols-outlined text-[18px]">brightness_auto</span>
              System
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
