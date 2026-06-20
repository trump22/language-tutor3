using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
namespace languagetutor.Services;

public class AzureSpeechService
{
    private readonly string _speechKey;
    private readonly string _speechRegion;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AzureSpeechService> _logger;

    public AzureSpeechService(
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<AzureSpeechService> logger)
    {
        _speechKey = config["Azure:SpeechKey"]?.Trim() ?? "";
        _speechRegion = config["Azure:SpeechRegion"]?.Trim() ?? "";
        _env = env;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_speechKey) &&
        !_speechKey.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(_speechRegion);

    public string Region => _speechRegion;

    public async Task<PronunciationResult> AnalyzePronunciation(string wavPath, string referenceText, string language = "en-US")
    {
        if (!IsConfigured)
        {
            return new PronunciationResult
            {
                Success = false,
                Message = "Azure Speech chưa được cấu hình. Hãy thêm Azure__SpeechKey và Azure__SpeechRegion trong App Service."
            };
        }

        var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
        speechConfig.SpeechRecognitionLanguage = language;

        var pronunciationConfig = new PronunciationAssessmentConfig(
            referenceText,
            GradingSystem.HundredMark,
            Granularity.Phoneme,
            enableMiscue: true)
        {
            PhonemeAlphabet = "IPA",
            NBestPhonemeCount = 3
        };
        pronunciationConfig.EnableProsodyAssessment();

        using var audioInput = AudioConfig.FromWavFileInput(wavPath);
        using var recognizer = new SpeechRecognizer(speechConfig, audioInput);
        pronunciationConfig.ApplyTo(recognizer);

        var result = await recognizer.RecognizeOnceAsync();

        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            var assessment = PronunciationAssessmentResult.FromResult(result);
            var words = assessment.Words.Select(word => new PronunciationWordResult
            {
                Word = word.Word,
                AccuracyScore = word.AccuracyScore,
                ErrorType = word.ErrorType,
                Phonemes = word.Phonemes?.Select(p => new PronunciationPhonemeResult
                {
                    Phoneme = p.Phoneme,
                    Score = p.AccuracyScore
                }).ToList() ?? new List<PronunciationPhonemeResult>()
            }).ToList();

            return new PronunciationResult
            {
                Success = true,
                RecognizedText = result.Text,
                Scores = new PronunciationScores
                {
                    PronunciationScore = (float)assessment.PronunciationScore,
                    AccuracyScore = (float)assessment.AccuracyScore,
                    FluencyScore = (float)assessment.FluencyScore,
                    CompletenessScore = (float)assessment.CompletenessScore,
                    ProsodyScore = (float?)assessment.ProsodyScore
                },
                Words = words
            };
        }

        var cancellation = CancellationDetails.FromResult(result);
        var detail = cancellation.Reason == CancellationReason.Error
            ? $" {cancellation.ErrorDetails}"
            : "";
        return new PronunciationResult
        {
            Success = false,
            Message = $"Không nhận diện được giọng nói: {result.Reason}.{detail}"
        };
    }

    public async Task<SpeechSynthesisResult2> SynthesizeSpeech(string ssml, string fileName)
    {
        if (!IsConfigured)
        {
            return new SpeechSynthesisResult2
            {
                Success = false,
                Message = "Azure Speech chưa được cấu hình. Hãy thêm Azure__SpeechKey và Azure__SpeechRegion trong App Service."
            };
        }

        try
        {
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

            var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);
            var audioPath = Path.Combine(uploadsPath, $"{fileName}.wav");

            using var audioOutput = AudioConfig.FromWavFileOutput(audioPath);
            using var synthesizer = new SpeechSynthesizer(speechConfig, audioOutput);
            using var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                return new SpeechSynthesisResult2
                {
                    Success = true,
                    AudioPath = $"uploads/{fileName}.wav"
                };
            }

            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            _logger.LogWarning(
                "Azure Speech synthesis failed. Region: {Region}, Reason: {Reason}, ErrorCode: {ErrorCode}, Details: {Details}",
                _speechRegion,
                cancellation.Reason,
                cancellation.ErrorCode,
                cancellation.ErrorDetails);

            return new SpeechSynthesisResult2
            {
                Success = false,
                Message = BuildSynthesisErrorMessage(cancellation)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Speech SDK threw an exception while creating audio in region {Region}.", _speechRegion);
            return new SpeechSynthesisResult2
            {
                Success = false,
                Message = "Azure Speech SDK không thể tạo audio. Hãy kiểm tra Speech key, region và cấu hình nền tảng App Service."
            };
        }
    }

    private static string BuildSynthesisErrorMessage(SpeechSynthesisCancellationDetails cancellation)
    {
        var details = cancellation.ErrorDetails ?? "";
        if (details.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
            details.Contains("401", StringComparison.OrdinalIgnoreCase) ||
            details.Contains("403", StringComparison.OrdinalIgnoreCase))
        {
            return "Azure Speech từ chối xác thực. Speech key hoặc region không đúng với Speech resource.";
        }

        if (details.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
            details.Contains("429", StringComparison.OrdinalIgnoreCase))
        {
            return "Azure Speech đã hết quota hoặc đang bị giới hạn số lượt gọi.";
        }

        if (details.Contains("SSML", StringComparison.OrdinalIgnoreCase) ||
            details.Contains("invalid xml", StringComparison.OrdinalIgnoreCase))
        {
            return "Kịch bản SSML do AI tạo không hợp lệ. Hãy thử tạo lại với nội dung ngắn hơn.";
        }

        return $"Azure Speech không thể tổng hợp audio ({cancellation.ErrorCode}). Kiểm tra Speech key và region.";
    }
}

public class PronunciationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? RecognizedText { get; set; }
    public PronunciationScores? Scores { get; set; }
    public List<PronunciationWordResult>? Words { get; set; }
}

public class PronunciationWordResult
{
    public string Word { get; set; } = string.Empty;
    public double AccuracyScore { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public List<PronunciationPhonemeResult> Phonemes { get; set; } = new();
}

public class PronunciationPhonemeResult
{
    public string Phoneme { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class PronunciationScores
{
    public float PronunciationScore { get; set; }
    public float AccuracyScore { get; set; }
    public float FluencyScore { get; set; }
    public float CompletenessScore { get; set; }
    public float? ProsodyScore { get; set; }
}

public class SpeechSynthesisResult2
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AudioPath { get; set; }
}
