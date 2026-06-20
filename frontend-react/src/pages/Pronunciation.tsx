import { useState, useRef, useEffect } from 'react';
import api from '../api/axios';

// --- HÀM HỖ TRỢ CHỌN MÀU ĐỘNG THEO ĐIỂM SỐ CHUẨN AZURE ---
const getScoreColor = (score: number, type: 'bg' | 'text' | 'stroke') => {
  if (score < 60) {
    if (type === 'bg') return 'bg-red-600 dark:bg-red-500';
    if (type === 'text') return 'text-red-600 dark:text-red-500';
    if (type === 'stroke') return 'stroke-red-600 dark:stroke-red-500';
  } else if (score < 80) {
    if (type === 'bg') return 'bg-yellow-500 dark:bg-yellow-400';
    if (type === 'text') return 'text-yellow-500 dark:text-yellow-400';
    if (type === 'stroke') return 'stroke-yellow-500 dark:stroke-yellow-400';
  } else {
    if (type === 'bg') return 'bg-emerald-600 dark:bg-emerald-500';
    if (type === 'text') return 'text-emerald-600 dark:text-emerald-500';
    if (type === 'stroke') return 'stroke-emerald-600 dark:stroke-emerald-500';
  }
  return '';
};

export default function Pronunciation() {
  const [language, setLanguage] = useState('en');
  const [level, setLevel] = useState('Beginner');
  const [originalText, setOriginalText] = useState("Today was a beautiful day. We had a great time taking a long walk outside in the morning.");
  
  const [isRecording, setIsRecording] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  
  const [resultData, setResultData] = useState<any>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  // STATE QUẢN LÝ BẬT/TẮT CÁC LỖI
  const [activeErrors, setActiveErrors] = useState({
    Mispronunciations: true,
    Omissions: true,
    Insertions: true,
    UnexpectedBreak: true,
    Monotone: true,
  });

  const toggleError = (errorKey: keyof typeof activeErrors) => {
    setActiveErrors(prev => ({ ...prev, [errorKey]: !prev[errorKey] }));
  };

  const audioContextRef = useRef<AudioContext | null>(null);
  const sourceRef = useRef<MediaStreamAudioSourceNode | null>(null);
  const processorRef = useRef<ScriptProcessorNode | null>(null);
  const silentGainRef = useRef<GainNode | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const audioBuffersRef = useRef<Float32Array[]>([]);
  const sampleRateRef = useRef(16000);

  const generateNewText = async () => {
    setIsGenerating(true);
    setFeedback(null);
    setResultData(null);
    try {
      const res = await api.post('/ai/generate-practice-text', { language, level });
      setOriginalText(res.data.text);
    } catch (error) {
      setOriginalText("Lỗi khi tạo văn bản. Vui lòng thử lại!");
    } finally {
      setIsGenerating(false);
    }
  };

  const cleanupRecording = () => {
    processorRef.current?.disconnect();
    sourceRef.current?.disconnect();
    silentGainRef.current?.disconnect();
    streamRef.current?.getTracks().forEach(track => track.stop());

    if (audioContextRef.current && audioContextRef.current.state !== 'closed') {
      void audioContextRef.current.close();
    }

    processorRef.current = null;
    sourceRef.current = null;
    silentGainRef.current = null;
    streamRef.current = null;
    audioContextRef.current = null;
  };

  useEffect(() => {
    return () => cleanupRecording();
  }, []);

  const startRecording = async () => {
    if (isRecording || isLoading) return;

    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          channelCount: 1,
          echoCancellation: true,
          noiseSuppression: true,
        },
      });

      const AudioContextCtor = window.AudioContext || (window as any).webkitAudioContext;
      const audioContext = new AudioContextCtor();
      const source = audioContext.createMediaStreamSource(stream);
      const processor = audioContext.createScriptProcessor(4096, 1, 1);
      const silentGain = audioContext.createGain();

      audioBuffersRef.current = [];
      sampleRateRef.current = audioContext.sampleRate;
      silentGain.gain.value = 0;

      processor.onaudioprocess = (event) => {
        const input = event.inputBuffer.getChannelData(0);
        audioBuffersRef.current.push(new Float32Array(input));
      };

      source.connect(processor);
      processor.connect(silentGain);
      silentGain.connect(audioContext.destination);

      streamRef.current = stream;
      audioContextRef.current = audioContext;
      sourceRef.current = source;
      processorRef.current = processor;
      silentGainRef.current = silentGain;

      setIsRecording(true);
      setResultData(null);
      setFeedback(null);
    } catch (error) {
      cleanupRecording();
      alert("Vui lòng cấp quyền Micro!");
    }
  };

  const stopRecording = () => {
    if (!isRecording) return;

    setIsRecording(false);
    const recordedBuffers = audioBuffersRef.current;
    const inputSampleRate = sampleRateRef.current;
    cleanupRecording();

    if (recordedBuffers.length === 0) {
      setFeedback("Vui lòng bấm và giữ mic để đọc.");
      return;
    }

    const mergedSamples = mergeAudioBuffers(recordedBuffers);
    const wavSamples = resampleAudio(mergedSamples, inputSampleRate, 16000);
    const audioBlob = encodeWav(wavSamples, 16000);
    void sendAudioToServer(audioBlob);
  };

  const sendAudioToServer = async (audioBlob: Blob) => {
    setIsLoading(true);
    const formData = new FormData();
    formData.append('audio', audioBlob, 'recording.wav');
    formData.append('referenceText', originalText);
    formData.append('language', language === 'zh' ? 'zh-CN' : 'en-US');

    try {
      const response = await api.post('/ai/pronunciation/evaluate', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      setResultData(response.data.data);
    } catch (error: any) {
      setFeedback(error?.response?.data?.message || 'Lỗi phân tích giọng nói.');
    } finally {
      setIsLoading(false);
    }
  };

  // BÔI MÀU CHỮ & RENDER TOOLTIP
  const renderHighlightedText = () => {
    if (!resultData || !resultData.words) return originalText;

    return resultData.words.map((wordObj: any, index: number) => {
      let colorClass = "text-slate-800 dark:text-slate-200"; 
      let decoration = "";
      let isVisible = true;
      
      const score = wordObj.accuracyScore;
      const errType = wordObj.errorType;

      if (errType === "Omission") {
        if (activeErrors.Omissions) {
          colorClass = "text-slate-400 opacity-50 bg-slate-100 dark:bg-slate-800 px-1 rounded"; 
          decoration = "line-through";
        }
      } else if (errType === "Insertion") {
        if (activeErrors.Insertions) {
          colorClass = "text-white font-bold bg-red-600 px-1 rounded mx-1";
        } else {
          isVisible = false; 
        }
      } else if (errType === "UnexpectedBreak") {
        if (activeErrors.UnexpectedBreak) decoration = "underline decoration-pink-400 decoration-wavy underline-offset-4";
      } else if (errType === "Monotone") {
        if (activeErrors.Monotone) colorClass = "text-purple-600 dark:text-purple-400 font-bold bg-purple-100 dark:bg-purple-900/30 px-1 rounded";
      } else {
        if (score < 80 && activeErrors.Mispronunciations) {
          if (score < 60) colorClass = "text-white font-bold bg-red-600 px-1 rounded";
          else colorClass = "text-slate-900 font-bold bg-yellow-400 px-1 rounded"; 
        }
      }

      if (!isVisible) return null;

      return (
        <span key={index} className="relative group inline-block mx-0.5 cursor-pointer leading-loose">
          <span className={`${colorClass} ${decoration} transition-all`}>
            {wordObj.word}
          </span>

          <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 hidden group-hover:flex flex-col bg-slate-800 text-white text-xs rounded-xl shadow-2xl p-3 z-50 min-w-30 pointer-events-none animate-in fade-in slide-in-from-bottom-2">
            <div className="text-center font-bold border-b border-slate-600 pb-2 mb-2">
              <span className="text-slate-400 font-normal mr-1">Từ:</span> {wordObj.word} <br/>
              <span className="text-slate-400 font-normal mr-1">Điểm:</span> 
              <span className={getScoreColor(score, 'text')}>{score}</span>
            </div>
            
            {wordObj.phonemes && wordObj.phonemes.length > 0 ? (
              <div className="flex justify-center gap-3">
                {wordObj.phonemes.map((p: any, idx: number) => (
                  <div key={idx} className="flex flex-col items-center">
                    <span className={`font-mono text-sm ${getScoreColor(p.score, 'text')}`}>{p.phoneme}</span>
                    <span className="text-[10px] text-slate-400 font-bold">{p.score}</span>
                  </div>
                ))}
              </div>
            ) : (
              <span className="text-slate-400 italic text-center">Không có âm vị</span>
            )}
            <div className="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-slate-800"></div>
          </div>
        </span>
      );
    });
  };

  const overallScore = resultData?.scores?.pronunciationScore || 0;

  return (
    <div className="max-w-7xl mx-auto w-full space-y-8 animate-in fade-in duration-700 text-left p-6">
      
      {/* HEADER */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 bg-white dark:bg-slate-900 p-6 rounded-3xl border border-slate-200 dark:border-slate-800 shadow-sm">
        <div className="flex gap-4 items-center">
          <select value={language} onChange={(e) => setLanguage(e.target.value)} className="bg-slate-50 dark:bg-slate-800 rounded-xl px-4 py-2 text-sm font-bold outline-none cursor-pointer">
            <option value="en">Tiếng Anh (Mỹ)</option>
            <option value="zh">Tiếng Trung (CN)</option>
          </select>
          <select value={level} onChange={(e) => setLevel(e.target.value)} className="bg-slate-50 dark:bg-slate-800 rounded-xl px-4 py-2 text-sm font-bold outline-none cursor-pointer">
            <option value="Beginner">A1 Cơ bản</option>
            <option value="Intermediate">B1 Trung cấp</option>
          </select>
          <button onClick={generateNewText} disabled={isGenerating} className="px-5 py-2 bg-primary text-white rounded-xl font-bold text-xs uppercase shadow-lg shadow-primary/20 hover:scale-105 transition-all flex items-center gap-2">
            <span className="material-symbols-outlined text-sm">{isGenerating ? 'sync' : 'refresh'}</span>
            Lấy bài mới
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        
        {/* KHU VỰC ĐỌC VÀ CHÚ THÍCH LỖI */}
        <div className="lg:col-span-3 space-y-6">
          <div className="bg-white dark:bg-[#212121] p-10 rounded-3xl border border-slate-200 dark:border-slate-700 shadow-sm flex flex-col md:flex-row gap-8">
            <div className="flex-1">
              <span className="text-xs font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest mb-6 block">Kết quả đánh giá</span>
              <div className="text-2xl leading-relaxed font-medium text-slate-800 dark:text-slate-200 min-h-37.5">
                {resultData ? renderHighlightedText() : originalText}
              </div>
            </div>

            <div className="w-full md:w-70 bg-slate-50 dark:bg-[#2d2d2d] p-5 rounded-2xl h-fit border border-slate-100 dark:border-slate-700">
              <h4 className="text-sm font-bold text-slate-800 dark:text-white mb-4">Lỗi phát âm</h4>
              <div className="space-y-4">
                <ErrorLegend color="bg-yellow-400" label="Phát âm sai" isActive={activeErrors.Mispronunciations} onToggle={() => toggleError('Mispronunciations')} />
                <ErrorLegend color="bg-slate-400" label="Bỏ sót từ" isActive={activeErrors.Omissions} onToggle={() => toggleError('Omissions')} />
                <ErrorLegend color="bg-red-600" label="Thêm từ" isActive={activeErrors.Insertions} onToggle={() => toggleError('Insertions')} />
                <ErrorLegend color="bg-pink-400" label="Ngắt nghỉ bất thường" isActive={activeErrors.UnexpectedBreak} onToggle={() => toggleError('UnexpectedBreak')} />
                <ErrorLegend color="bg-purple-500" label="Thiếu ngữ điệu" isActive={activeErrors.Monotone} onToggle={() => toggleError('Monotone')} />
              </div>
            </div>
          </div>

          <div className="bg-white dark:bg-[#212121] p-8 rounded-3xl border border-slate-200 dark:border-slate-700 flex flex-col items-center shadow-sm">
            <button 
              onMouseDown={startRecording} onMouseUp={stopRecording} onMouseLeave={stopRecording}
              onTouchStart={startRecording} onTouchEnd={stopRecording}
              className={`w-20 h-20 rounded-full flex items-center justify-center text-white transition-all shadow-xl select-none ${isRecording ? 'bg-red-500 animate-pulse' : isLoading ? 'bg-slate-500' : 'bg-primary hover:scale-105'}`}
            >
              <span className="material-symbols-outlined text-4xl">{isRecording ? 'mic' : isLoading ? 'sync' : 'mic_none'}</span>
            </button>
            <p className="mt-4 font-bold text-sm text-slate-500 uppercase tracking-widest">
              {isRecording ? 'Thả ra để gửi' : isLoading ? 'Đang phân tích...' : 'Bấm giữ để đọc'}
            </p>
            {feedback && <p className="mt-3 text-sm font-semibold text-red-500">{feedback}</p>}
          </div>
        </div>

        {/* BẢNG ĐIỂM CHUẨN AZURE */}
        <div className="lg:col-span-1">
          <div className="bg-white dark:bg-[#212121] p-6 rounded-3xl border border-slate-200 dark:border-slate-700 shadow-sm h-full flex flex-col">
            <h3 className="font-bold text-lg text-slate-800 dark:text-white mb-8">Điểm phát âm</h3>
            
            {/* COMPONENT VÒNG TRÒN ĐỘNG NẰM Ở ĐÂY */}
            <div className="flex flex-col items-center justify-center mb-10">
              <AnimatedScoreCircle score={overallScore} />
              
              <div className="flex gap-3 mt-6 text-xs font-bold text-slate-400">
                 <span className="flex items-center gap-1"><span className="w-3 h-3 bg-red-600 rounded-sm"></span> 0~59</span>
                 <span className="flex items-center gap-1"><span className="w-3 h-3 bg-yellow-500 rounded-sm"></span> 60~79</span>
                 <span className="flex items-center gap-1"><span className="w-3 h-3 bg-emerald-600 rounded-sm"></span> 80~100</span>
              </div>
            </div>

            <div className="h-px bg-slate-200 dark:bg-slate-700 w-full mb-8"></div>
            <h4 className="font-bold text-sm text-slate-800 dark:text-white mb-6">Chi tiết điểm</h4>
            
            {/* CÁC THANH ĐIỂM DƯỚI CŨNG ĐÃ ĐƯỢC THIẾT KẾ ĐỂ CHẠY ANIMATION TỪ 0 LÊN */}
            <div className="space-y-6 flex-1">
              <BreakdownBar label="Độ chính xác" score={resultData?.scores?.accuracyScore || 0} />
              <BreakdownBar label="Độ lưu loát" score={resultData?.scores?.fluencyScore || 0} />
              <BreakdownBar label="Độ hoàn chỉnh" score={resultData?.scores?.completenessScore || 0} />
              <BreakdownBar label="Ngữ điệu" score={resultData?.scores?.prosodyScore || 0} />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function mergeAudioBuffers(buffers: Float32Array[]) {
  const totalLength = buffers.reduce((sum, buffer) => sum + buffer.length, 0);
  const result = new Float32Array(totalLength);
  let offset = 0;

  for (const buffer of buffers) {
    result.set(buffer, offset);
    offset += buffer.length;
  }

  return result;
}

function resampleAudio(input: Float32Array, inputSampleRate: number, outputSampleRate: number) {
  if (inputSampleRate === outputSampleRate) return input;

  const ratio = inputSampleRate / outputSampleRate;
  const outputLength = Math.round(input.length / ratio);
  const output = new Float32Array(outputLength);

  for (let i = 0; i < outputLength; i++) {
    const sourceIndex = i * ratio;
    const before = Math.floor(sourceIndex);
    const after = Math.min(before + 1, input.length - 1);
    const weight = sourceIndex - before;
    output[i] = input[before] * (1 - weight) + input[after] * weight;
  }

  return output;
}

function encodeWav(samples: Float32Array, sampleRate: number) {
  const bytesPerSample = 2;
  const buffer = new ArrayBuffer(44 + samples.length * bytesPerSample);
  const view = new DataView(buffer);

  writeAscii(view, 0, 'RIFF');
  view.setUint32(4, 36 + samples.length * bytesPerSample, true);
  writeAscii(view, 8, 'WAVE');
  writeAscii(view, 12, 'fmt ');
  view.setUint32(16, 16, true);
  view.setUint16(20, 1, true);
  view.setUint16(22, 1, true);
  view.setUint32(24, sampleRate, true);
  view.setUint32(28, sampleRate * bytesPerSample, true);
  view.setUint16(32, bytesPerSample, true);
  view.setUint16(34, 16, true);
  writeAscii(view, 36, 'data');
  view.setUint32(40, samples.length * bytesPerSample, true);

  let offset = 44;
  for (const sample of samples) {
    const clamped = Math.max(-1, Math.min(1, sample));
    view.setInt16(offset, clamped < 0 ? clamped * 0x8000 : clamped * 0x7fff, true);
    offset += bytesPerSample;
  }

  return new Blob([buffer], { type: 'audio/wav' });
}

function writeAscii(view: DataView, offset: number, value: string) {
  for (let i = 0; i < value.length; i++) {
    view.setUint8(offset + i, value.charCodeAt(i));
  }
}

// ==========================================
// MỚI: COMPONENT VÒNG TRÒN ĐIỂM (ANIMATED)
// ==========================================
function AnimatedScoreCircle({ score }: { score: number }) {
  const [animatedScore, setAnimatedScore] = useState(0);

  // Mẹo để kích hoạt CSS Transition: Đặt state là 0, sau đó dùng useEffect đẩy nó lên điểm thật
  useEffect(() => {
    // Reset về 0 trước khi cập nhật điểm mới để luôn có hiệu ứng chạy
    setAnimatedScore(0);
    const timer = setTimeout(() => {
      setAnimatedScore(score);
    }, 100);
    return () => clearTimeout(timer);
  }, [score]);

  // Tính toán chu vi hình tròn (C = 2 * PI * r)
  const radius = 42;
  const circumference = 2 * Math.PI * radius;
  // Tính độ dài nét đứt để tạo thanh tiến trình
  const strokeDashoffset = circumference - (animatedScore / 100) * circumference;

  return (
    <div className="relative size-40">
      <svg className="w-full h-full -rotate-90 transform" viewBox="0 0 100 100">
        {/* Vòng nền xám */}
        <circle 
          cx="50" cy="50" r={radius} 
          className="stroke-slate-200 dark:stroke-slate-600/50" 
          strokeWidth="10" 
          fill="none" 
        />
        {/* Vòng màu chạy tiến trình */}
        <circle 
          cx="50" cy="50" r={radius} 
          className={`${getScoreColor(score, 'stroke')} transition-all duration-1500 ease-out`} 
          strokeWidth="10" 
          fill="none" 
          strokeDasharray={circumference} 
          strokeDashoffset={strokeDashoffset} 
          strokeLinecap="round" // Giúp đầu thanh tiến trình được bo tròn đẹp mắt
        />
      </svg>
      {/* Hiển thị con số ở giữa */}
      <div className="absolute inset-0 flex items-center justify-center">
        <span className="text-5xl font-light text-slate-800 dark:text-slate-100">{score}</span>
      </div>
    </div>
  );
}

// COMPONENT THANH CHỈ SỐ NẰM NGANG
function BreakdownBar({ label, score }: { label: string, score: number }) {
  const [animatedScore, setAnimatedScore] = useState(0);

  useEffect(() => {
    setAnimatedScore(0);
    const timer = setTimeout(() => setAnimatedScore(score), 100);
    return () => clearTimeout(timer);
  }, [score]);

  return (
    <div className="w-full">
      <div className="flex justify-between items-center mb-1.5">
        <span className="text-[13px] font-medium text-slate-600 dark:text-slate-300 flex items-center">
          {label} <span className="material-symbols-outlined text-[14px] ml-1 text-slate-400 cursor-help">info</span>
        </span>
        <span className="text-[13px] font-bold text-slate-800 dark:text-white">{score} / 100</span>
      </div>
      <div className="h-2.5 w-full bg-slate-200 dark:bg-slate-700 rounded overflow-hidden">
        <div 
          className={`h-full ${getScoreColor(score, 'bg')} transition-all duration-1500 ease-out`} 
          style={{ width: `${animatedScore}%` }}
        ></div>
      </div>
    </div>
  );
}

// COMPONENT NÚT SWITCH LỖI
function ErrorLegend({ color, label, isActive, onToggle }: { color: string, label: string, isActive: boolean, onToggle: () => void }) {
  return (
    <div className="flex items-center justify-between group">
      <div className="flex items-center gap-3">
        <span className={`w-5 h-5 rounded flex items-center justify-center text-[10px] font-bold text-white shadow-sm transition-opacity ${isActive ? color : 'bg-slate-300 dark:bg-slate-700'}`}></span>
        <span className="text-xs font-medium text-slate-700 dark:text-slate-300 flex items-center">
          {label} <span className="material-symbols-outlined text-[14px] ml-1 text-slate-400 cursor-help">info</span>
        </span>
      </div>
      <div 
        onClick={onToggle}
        className={`w-9 h-5 rounded-full flex items-center px-0.5 cursor-pointer transition-colors duration-300 ${isActive ? 'bg-blue-500 justify-end' : 'bg-slate-300 dark:bg-slate-600 justify-start'}`}
      >
        <div className="w-4 h-4 bg-white rounded-full shadow-md transform transition-transform"></div>
      </div>
    </div>
  );
}
