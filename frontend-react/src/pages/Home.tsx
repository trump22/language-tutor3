import { Link, useNavigate } from 'react-router-dom';
import LanguageSwitcher from '../components/LanguageSwitcher';
import ThemeSwitcher from '../components/ThemeSwitcher';
import { useLanguage } from '../contexts/LanguageContext';
import { useAuth } from '../contexts/AuthContext';
import { useEffect } from 'react'; // Đảm bảo đã import useEffect

export default function Home() {
  const { t } = useLanguage();
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
   if (user) {
      if (user.role === 'ADMIN') {
        navigate('/admin');
      } else {
        navigate('/dashboard');
      }
    }
  }, [user, navigate]);

  return (
    <div className="bg-background-light dark:bg-background-dark font-display text-text-light dark:text-text-dark transition-colors duration-500">
      <div className="relative flex h-auto min-h-screen w-full flex-col overflow-x-hidden">
        
        {/* --- 1. HEADER --- */}
        <header className="sticky top-0 z-50 flex items-center justify-center border-b border-solid border-border-light dark:border-border-dark bg-background-light/80 dark:bg-background-dark/80 backdrop-blur-sm">
          <div className="flex items-center justify-between whitespace-nowrap w-full max-w-7xl px-4 sm:px-6 lg:px-8 py-3">
            <div className="flex items-center gap-4">
              <div className="size-6 text-primary">
                <svg fill="none" viewBox="0 0 48 48" xmlns="http://www.w3.org/2000/svg">
                  <path d="M44 4H30.6666V17.3334H17.3334V30.6666H4V44H44V4Z" fill="currentColor"></path>
                </svg>
              </div>
              <h2 className="text-xl font-bold leading-tight tracking-[-0.015em]">
                Language AI Tutor <span className="text-primary text-xs align-top font-black ml-1">AI</span>
              </h2>
            </div>
            
            <nav className="hidden md:flex flex-1 justify-center items-center gap-9">
              <a className="text-sm font-medium leading-normal hover:text-primary transition-colors" href="#">{t('nav_english_ai')}</a>
              <a className="text-sm font-medium leading-normal hover:text-primary transition-colors" href="#">{t('nav_chinese_ai')}</a>
              <a className="text-sm font-medium leading-normal hover:text-primary transition-colors" href="#">{t('nav_pricing')}</a>
            </nav>

            <div className="flex items-center gap-2">
              <ThemeSwitcher />
              <LanguageSwitcher />
              
              {user ? (
                <div className="flex items-center gap-4 ml-2">
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center text-primary font-bold border border-primary/20">
                      {user.email?.charAt(0).toUpperCase()}
                    </div>
                    <span className="text-sm font-bold hidden sm:block">{user.name || user.email}</span>
                  </div>
                  <button 
                    onClick={logout}
                    className="flex min-w-21 cursor-pointer items-center justify-center rounded-full h-10 px-4 bg-red-500 text-white text-sm font-bold hover:opacity-90 transition-all"
                  >
                    Logout
                  </button>
                </div>
              ) : (
                <>
                  <Link to="/login" className="flex min-w-21 items-center justify-center rounded-full h-10 px-4 bg-gray-200/50 dark:bg-white/10 text-sm font-bold hover:bg-gray-200 dark:hover:bg-white/20 transition-all">
                    {t('btn_login')}
                  </Link>
                  <Link to="/signup" className="flex min-w-21 items-center justify-center rounded-full h-10 px-4 bg-primary text-white text-sm font-bold hover:opacity-90 transition-all">
                    {t('btn_get_started')}
                  </Link>
                </>
              )}
            </div>
          </div>
        </header>

        <main>
          {/* --- 2. HERO SECTION --- */}
          <section className="w-full py-16 lg:py-24 relative overflow-hidden">
            <div className="absolute inset-0 circuit-pattern opacity-[0.03] dark:opacity-[0.07] -z-10"></div>
            <div className="container mx-auto px-4 sm:px-6 lg:px-8">
              <div className="grid lg:grid-cols-2 gap-12 items-center">
                <div className="flex flex-col gap-6 text-center lg:text-left items-center lg:items-start">
                  <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary/10 text-primary text-sm font-bold border border-primary/20">
                    <span className="material-symbols-outlined text-sm">psychology</span>
                    {t('hero_badge')}
                  </div>
                  <h1 className="text-4xl font-black leading-tight tracking-tighter sm:text-5xl md:text-6xl">
                    {t('hero_title_1')}<span className="text-primary">{t('hero_title_2')}</span>
                  </h1>
                  <h2 className="text-lg font-normal text-gray-600 dark:text-gray-400 max-w-2xl leading-relaxed">
                    {t('hero_subtitle')}
                  </h2>
                  
                  {/* Real-time Recognition Visualizer */}
                  <div className="flex flex-col gap-4 w-full max-w-sm mt-4 p-4 rounded-2xl bg-white dark:bg-white/5 border border-border-light dark:border-border-dark shadow-sm">
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-xs font-bold text-gray-400 uppercase tracking-widest">{t('hero_realtime')}</span>
                      <div className="flex gap-1">
                        {[0.1, 0.3, 0.2, 0.5, 0.4].map((delay, i) => (
                          <div key={i} className="waveform-bar" style={{ animationDelay: `${delay}s` }}></div>
                        ))}
                      </div>
                    </div>
                    <div className="space-y-2">
                      <div className="flex items-center gap-3">
                        <div className="size-6 rounded-full bg-blue-100 flex items-center justify-center text-[10px] font-bold text-blue-700">EN</div>
                        <p className="text-sm italic text-gray-500">"The weather in London is..."</p>
                        <span className="material-symbols-outlined text-green-500 text-sm">check_circle</span>
                      </div>
                      <div className="flex items-center gap-3">
                        <div className="size-6 rounded-full bg-red-100 flex items-center justify-center text-[10px] font-bold text-red-600">ZH</div>
                        <p className="text-sm italic text-gray-500">"你好，北京很高兴..."</p>
                        <span className="material-symbols-outlined text-primary text-sm">graphic_eq</span>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Hero Form Card */}
                <div className="w-full max-w-md mx-auto">
                  <div className="bg-white dark:bg-background-dark/80 p-8 rounded-xl shadow-2xl border border-border-light dark:border-border-dark backdrop-blur-xl relative">
                    <div className="absolute -top-4 -right-4 size-12 bg-primary rounded-full flex items-center justify-center text-white ai-pulse">
                      <span className="material-symbols-outlined">smart_toy</span>
                    </div>
                    <h3 className="text-2xl font-bold text-center mb-6">{t('hero_form_title')}</h3>
                    <form className="space-y-4" onSubmit={(e) => e.preventDefault()}>
                      <div>
                        <label className="text-sm font-medium text-gray-700 dark:text-gray-300" htmlFor="email">{t('hero_form_email')}</label>
                        <input className="mt-1 block w-full rounded-lg border-gray-300 dark:border-border-dark shadow-sm focus:border-primary focus:ring-primary sm:text-sm bg-background-light dark:bg-white/5 text-text-light dark:text-text-dark px-4 py-2 outline-none" id="email" placeholder="you@example.com" type="email" />
                      </div>
                      <div>
                        <label className="text-sm font-medium text-gray-700 dark:text-gray-300" htmlFor="language">{t('hero_form_lang')}</label>
                        <select className="mt-1 block w-full rounded-lg border-gray-300 dark:border-border-dark shadow-sm focus:border-primary focus:ring-primary sm:text-sm bg-background-light dark:bg-white/5 text-text-light dark:text-text-dark px-4 py-2 outline-none" id="language">
                          <option value="english">{t('lang_en')}</option>
                          <option value="chinese">{t('lang_zh')}</option>
                        </select>
                      </div>
                      <button className="flex w-full cursor-pointer items-center justify-center rounded-full h-12 px-6 bg-primary text-white text-base font-bold shadow-lg shadow-primary/30 hover:opacity-90 transition-all transform hover:scale-[1.02]">
                        {t('hero_form_btn')}
                      </button>
                      <p className="text-[10px] text-center text-gray-400">{t('hero_form_footer')}</p>
                    </form>
                  </div>
                </div>
              </div>
            </div>
          </section>

          {/* --- 3. AI SENSEI SECTION --- */}
          <section className="w-full py-20 bg-gray-50 dark:bg-[#0c1219] relative overflow-hidden">
            <div className="container mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
              <div className="grid lg:grid-cols-2 gap-16 items-center">
                <div className="order-2 lg:order-1 relative">
                  <div className="relative w-full aspect-square max-w-md mx-auto">
                    <div className="absolute inset-0 bg-primary/20 rounded-full blur-[100px] animate-pulse"></div>
                    <div className="relative rounded-3xl overflow-hidden border border-white/20 shadow-2xl">
                      <img alt="AI Tutor" className="w-full h-full object-cover" src="https://lh3.googleusercontent.com/aida-public/AB6AXuBkCQXkixCpG-l1HlbEM5EYSAa1JzBoT0ZaJ6RPCy-XLRTFhO_7Meuf-RwIkxMkkP2sJbF_-ucQplM-S-0aBUEwX46xS9_cERZTKZsAlCPbJk0rmxANIpGQGFT2bmiprR40x0SPr8BPr7fza285OyxWFnEnabQ-F-eRH6UAo3nM1i1358M9R_CQgM0wSbIavFjhtIXtWR_-abSi9sfdGKE-v_Ql3XEFvSa9mAM-l03xXJIPCuZZ_geuhKoQnGRGo1fwECDscdIf01A" />
                      <div className="absolute inset-0 bg-linear-to-t from-black/80 via-transparent to-transparent"></div>
                      <div className="absolute bottom-6 left-6 right-6">
                        <div className="flex items-center gap-3 bg-black/40 backdrop-blur-md p-3 rounded-2xl border border-white/10">
                          <div className="size-3 bg-green-500 rounded-full animate-pulse"></div>
                          <div>
                            <p className="text-white text-xs font-bold">AI Tutor Online</p>
                            <p className="text-white/60 text-[10px]">Analyzing pronunciation...</p>
                          </div>
                        </div>
                      </div>
                    </div>
                    <div className="absolute -right-8 top-1/4 bg-white dark:bg-background-dark p-4 rounded-2xl shadow-xl border border-primary/20 flex flex-col items-center gap-2">
                      <span className="text-primary font-bold text-xl leading-none">98%</span>
                      <span className="text-[10px] uppercase font-bold text-gray-400">Accuracy</span>
                    </div>
                    <div className="absolute -left-12 bottom-1/3 bg-white dark:bg-background-dark p-3 rounded-full shadow-xl border border-secondary/20 flex items-center gap-2">
                      <span className="material-symbols-outlined text-secondary">mic_external_on</span>
                      <span className="text-xs font-bold pr-2">Listening...</span>
                    </div>
                  </div>
                </div>
                <div className="order-1 lg:order-2 flex flex-col gap-8">
                  <div>
                    <h2 className="text-3xl font-bold tracking-tighter sm:text-4xl">{t('feat_title')}</h2>
                    <p className="mt-4 text-lg text-gray-600 dark:text-gray-400">{t('feat_subtitle')}</p>
                  </div>
                  <div className="space-y-6">
                    <FeatureItem icon="auto_graph" title={t('feat_1_title')} desc={t('feat_1_desc')} />
                    <FeatureItem icon="model_training" title={t('feat_2_title')} desc={t('feat_2_desc')} />
                    <FeatureItem icon="language_chinese_dayi" title={t('feat_3_title')} desc={t('feat_3_desc')} />
                  </div>
                </div>
              </div>
            </div>
          </section>

          {/* --- 4. OPTIMIZED FOR TWO WORLDS --- */}
          <section className="w-full py-20 lg:py-24 bg-white dark:bg-background-dark/50">
            <div className="container mx-auto px-4 sm:px-6 lg:px-8">
              <div className="text-center mb-16">
                <h2 className="text-3xl font-bold tracking-tighter sm:text-4xl">{t('opt_title')}</h2>
                <p className="mt-4 max-w-2xl mx-auto text-gray-600 dark:text-gray-400">{t('opt_subtitle')}</p>
              </div>
              <div className="grid md:grid-cols-2 gap-8">
                <LangModuleCard 
                  img="https://lh3.googleusercontent.com/aida-public/AB6AXuCapRnP7pll2LPpgulh5tKbvjFY1GoFhqnVxTqBrVBYwf26jppfKraov5EOhMcr0-PoaXhQLsYKumikAU6djQEfOcG5H7-aTJv9RZYzLTgBxN5eIs_RiuYY2Oi16a0wIlz9nSbSfoA16aGVmavhbuUn2S1_M0GboiWldArd15rvUObnS7SIBxhrGPBIx4Qi1olcNbNYL_enuw82nKPRw0Pva4QnW7Oc7ZZmxa4xCaYLveg3c1sOaJvRzEgjBI1qHjiSNwtAhuNoAbQ" 
                  flags={["https://lh3.googleusercontent.com/aida-public/AB6AXuBwhMedKgSSK9c3HSrWAtd0aeUo1w3R1K_GLLixIDr-EomndMEX22vqo1AwwFaOyT6cGlkBMyCc5T4-R2NOw1wJKsiaDXmZp5tlK0u30Hg2IhwfqF5e3K73geK5wORiy0PVRxUl2QIoh5th0dmBqEs1GFhf9dpit1a-t79RcvueZ8SBna9WTzdISfBhqAxBVQIc2YyfsjcFyV9LSXHrfEF03nMCf3_NMF5n0rqqU6CyXZa2-z3NaJKnY4f2ZXBOiUSpf3Hg6jPNkPY", "https://lh3.googleusercontent.com/aida-public/AB6AXuAJZa98NWNqXIEoi0D5sFjQRJSErAjRBh__PWf0H1_SYkq6csfONy5DBmymORvkIGivjlSz40ev5nJaOa4hkNbvHOoh-Fa2JB0fT5UNlekU4kBPa1xDxTI2-G0jLoEHeYTPlqNtGyWBaOUsxtTXNZ69IiV7ebJpqAmdFIGM12ifucv6c1QPyTg_qBtemc8FiBKtQ0DFBkY7XphrE0cV2qOqepO3BR-z_GSLdYFpdVR4aN2ThVn9o8aBHNRvt6b4IUPqf54zEIKfPkQ"]}
                  title={t('opt_en_title')}
                  desc={t('opt_en_desc')}
                  items={[{icon: 'record_voice_over', text: t('opt_en_li1')}, {icon: 'edit_note', text: t('opt_en_li2')}]}
                  btnText={t('opt_en_btn')}
                />
                <LangModuleCard 
                  img="https://lh3.googleusercontent.com/aida-public/AB6AXuD7BDnLwrr4QPU1OadWC9vyU1t5zEF9cDT9sShINUQYmPCly_HjVLJZORrwZmkQl7UyxwZzN0fIuph97bIv08xP1aEl_iNzMr1HmZrKuti_YtKXot7hZm852lfZh62Odd4WVJa8OG6BbUSZkTwG83L91k3xlupAGDu5pxH5J0LKh5Cd4LR-A6X553LJF33Ok52g92FMCSBNdnsE8BMy664RJw3Pv1s5FyaqT1xl47S6PnZYZPT6ZULQ8J5FSp-vgPPF8Gx467Qo_yM" 
                  flags={["https://lh3.googleusercontent.com/aida-public/AB6AXuD5Qvlhn6WLNkul76R4GM3f1qBx3SgOgXY-a_MHoMdEEu97qcAZ-MNfcVIiEsN_RuJegM4105D9X7SG9uguGa1cIkVyHz-gTvT67M_6tYtJLBAx8Y60wG_9oVY-lLwbWsRa_u3z10HTCt4QVLDFQus5cyVH0S00XDn2ssih_5Y4CVDDdKvcFjv8TAXmVcDkQxLl6ORRU1yZw51BCO5nGAsITuA3paru0pJ15PQ68ulSMUrGArmRrCh_BmnnqcLaMtwqIUBNjPXLXMM"]}
                  title={t('opt_zh_title')}
                  desc={t('opt_zh_desc')}
                  items={[{icon: 'graphic_eq', text: t('opt_zh_li1')}, {icon: 'draw', text: t('opt_zh_li2')}]}
                  btnText={t('opt_zh_btn')}
                  color="secondary"
                />
              </div>
            </div>
          </section>

          {/* --- 5. COMPARISON TABLE --- */}
          <section className="w-full py-20 lg:py-24 border-t border-border-light dark:border-border-dark">
            <div className="container mx-auto px-4 sm:px-6 lg:px-8">
              <div className="max-w-4xl mx-auto overflow-hidden rounded-3xl border border-border-light dark:border-border-dark shadow-xl">
                <table className="w-full text-left">
                  <thead>
                    <tr className="bg-gray-50 dark:bg-white/5">
                      <th className="p-6 text-sm font-bold uppercase tracking-widest text-gray-400">{t('tab_col1')}</th>
                      <th className="p-6 text-sm font-bold uppercase tracking-widest text-primary text-center">{t('tab_col2')}</th>
                      <th className="p-6 text-sm font-bold uppercase tracking-widest text-gray-400 text-center">{t('tab_col3')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-light dark:divide-border-dark font-medium">
                    <TableRow label={t('tab_row1')} ok1 ok2={false} />
                    <TableRow label={t('tab_row2')} ok1 ok2="radio_button_unchecked" />
                    <TableRow label={t('tab_row3')} ok1 ok2={false} />
                    <TableRow label={t('tab_row4')} ok1 ok2 />
                  </tbody>
                </table>
              </div>
            </div>
          </section>

          {/* --- 6. TESTIMONIALS --- */}
          <section className="w-full py-20 lg:py-24 bg-primary/5 dark:bg-white/5">
            <div className="container mx-auto px-4 sm:px-6 lg:px-8 text-center">
              <h2 className="text-3xl font-bold tracking-tighter mb-12">AI Success Stories</h2>
              <div className="grid lg:grid-cols-2 gap-8 text-left">
                <TestimonialCard 
                  text="The AI tutor for Chinese is insane..." 
                  author="Michael C." 
                  role="Beijing Tech Consultant" 
                  img="https://lh3.googleusercontent.com/aida-public/AB6AXuAwuckD0JNU2ar1GY04G2fTb2Gf4xjmqzCzbA-Xl007WktBYQvGELh6FfFGDIN1IksPcQzpd8i_o1YKRpL2shs5qm_n-5Cog_i14OsEFyAv-qQZRE45KI28hPxwYsVHFiBhcqYLIaEmhrj5YXLzL3R4MwNuE3indFMYsVujpq26Sfn47LclpeD7EuQlGuL2g7zgz_LHxh9mPuQ78gALc_vz8eHfPWuRqT2lRZfBYWWcNfPlgm4T-wqILPuGvB9RSlnXHgnjUcKATd4" 
                />
                <TestimonialCard 
                  text="Leading board meetings in English was terrifying..." 
                  author="Li W." 
                  role="MNC Project Director" 
                  img="https://lh3.googleusercontent.com/aida-public/AB6AXuBkCQXkixCpG-l1HlbEM5EYSAa1JzBoT0ZaJ6RPCy-XLRTFhO_7Meuf-RwIkxMkkP2sJbF_-ucQplM-S-0aBUEwX46xS9_cERZTKZsAlCPbJk0rmxANIpGQGFT2bmiprR40x0SPr8BPr7fza285OyxWFnEnabQ-F-eRH6UAo3nM1i1358M9R_CQgM0wSbIavFjhtIXtWR_-abSi9sfdGKE-v_Ql3XEFvSa9mAM-l03xXJIPCuZZ_geuhKoQnGRGo1fwECDscdIf01A" 
                />
              </div>
            </div>
          </section>
        </main>

        {/* --- 7. FOOTER --- */}
        <footer className="bg-white dark:bg-background-dark border-t border-border-light dark:border-border-dark py-16">
          <div className="container mx-auto px-4 sm:px-6 lg:px-8">
            <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-8">
              <div className="col-span-2 md:col-span-4 lg:col-span-1">
                <div className="flex items-center gap-2">
                  <div className="size-6 text-primary"><svg fill="none" viewBox="0 0 48 48"><path d="M44 4H30.6666V17.3334H17.3334V30.6666H4V44H44V4Z" fill="currentColor"></path></svg></div>
                  <h2 className="text-xl font-bold">LinguaConnect AI</h2>
                </div>
                <p className="mt-4 text-sm text-gray-500">Revolutionizing English and Chinese learning with advanced artificial intelligence.</p>
              </div>
              <FooterColumn title="English AI" links={["Business English", "IELTS Simulation", "Accent Training"]} />
              <FooterColumn title="Chinese AI" links={["HSK Exam Prep", "Tone Recognition", "Character AI"]} />
              <FooterColumn title="Tech" links={["API Access", "Voice Engine", "Neural Net"]} />
              <FooterColumn title="Support" links={["Help Center", "Privacy"]} />
            </div>
            <div className="mt-16 pt-8 border-t border-border-light dark:border-border-dark flex flex-col sm:flex-row justify-between items-center text-sm text-gray-500">
              <p>© 2026 LinguaConnect AI. Built for the future of communication.</p>
              <div className="flex gap-4 mt-4 sm:mt-0">
                <span className="material-symbols-outlined cursor-pointer hover:text-primary">smart_toy</span>
                <span className="material-symbols-outlined cursor-pointer hover:text-primary">language</span>
              </div>
            </div>
          </div>
        </footer>
      </div>
    </div>
  );
}

/** --- SUB-COMPONENTS --- **/

function FeatureItem({ icon, title, desc }: { icon: string, title: string, desc: string }) {
  return (
    <div className="flex gap-4">
      <div className="flex-none size-12 rounded-xl bg-primary/10 flex items-center justify-center text-primary">
        <span className="material-symbols-outlined">{icon}</span>
      </div>
      <div>
        <h4 className="font-bold text-lg">{title}</h4>
        <p className="text-gray-500 text-sm leading-relaxed">{desc}</p>
      </div>
    </div>
  );
}

function LangModuleCard({ img, flags, title, desc, items, btnText, color = "primary" }: any) {
  const accentClass = color === "primary" ? "text-primary border-primary/20 bg-primary/5" : "text-secondary border-secondary/20 bg-secondary/5";
  const btnClass = color === "primary" ? "bg-primary/5 border-primary/20 text-primary hover:bg-primary" : "bg-secondary/5 border-secondary/20 text-secondary hover:bg-secondary";

  return (
    <div className={`group relative flex flex-col rounded-3xl border border-border-light dark:border-border-dark overflow-hidden bg-background-light dark:bg-background-dark transition-all duration-300 hover:border-${color}/50`}>
      <div className="h-56 relative overflow-hidden">
        <img alt={title} className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105" src={img} />
        <div className="absolute inset-0 bg-linear-to-t from-background-dark/90 via-background-dark/20 to-transparent"></div>
        <div className="absolute bottom-6 left-6 flex items-center gap-2">
          {flags.map((f: string, i: number) => <img key={i} alt="flag" className="w-6 h-4 object-cover rounded-sm shadow" src={f} />)}
          <h3 className="text-white text-2xl font-bold ml-2">{title}</h3>
        </div>
      </div>
      <div className="p-8 space-y-6">
        <p className="text-gray-500 text-sm">{desc}</p>
        <ul className="space-y-3">
          {items.map((item: any, i: number) => (
            <li key={i} className="flex items-center gap-3 text-sm">
              <span className={`material-symbols-outlined text-lg ${accentClass}`}>
  {item.icon}
</span>
            </li>
          ))}
        </ul>
        <button className={`w-full py-3 rounded-xl border font-bold hover:text-white transition-all ${btnClass}`}>{btnText}</button>
      </div>
    </div>
  );
}

function TableRow({ label, ok1, ok2 }: any) {
  const renderIcon = (val: any) => {
    if (val === true) return <span className="material-symbols-outlined text-primary">check_circle</span>;
    if (val === false) return <span className="material-symbols-outlined text-gray-400">cancel</span>;
    return <span className="material-symbols-outlined text-gray-400">{val}</span>;
  };

  return (
    <tr>
      <td className="p-6">{label}</td>
      <td className="p-6 text-center">{renderIcon(ok1)}</td>
      <td className="p-6 text-center">{renderIcon(ok2)}</td>
    </tr>
  );
}

function TestimonialCard({ text, author, role, img }: any) {
  return (
    <div className="flex flex-col justify-between rounded-2xl bg-white dark:bg-background-dark/50 border border-border-light dark:border-border-dark p-8 shadow-sm">
      <div>
        <div className="flex gap-1 text-secondary mb-4">
          {[...Array(5)].map((_, i) => <span key={i} className="material-symbols-outlined text-sm">star</span>)}
        </div>
        <p className="text-gray-600 dark:text-gray-400 italic text-lg leading-relaxed">"{text}"</p>
      </div>
      <div className="flex items-center gap-4 mt-8">
        <img alt="Portrait" className="h-14 w-14 rounded-full object-cover" src={img} />
        <div>
          <p className="font-bold">{author}</p>
          <p className="text-sm text-gray-500">{role}</p>
        </div>
      </div>
    </div>
  );
}

function FooterColumn({ title, links }: { title: string, links: string[] }) {
  return (
    <div>
      <h3 className="font-bold tracking-wide">{title}</h3>
      <ul className="mt-4 space-y-2 text-sm text-gray-600 dark:text-gray-400">
        {links.map((link, i) => <li key={i}><a className="hover:text-primary transition-colors" href="#">{link}</a></li>)}
      </ul>
    </div>
  );
}