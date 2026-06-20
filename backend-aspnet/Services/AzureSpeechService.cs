using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
namespace languagetutor.Services;

public class AzureSpeechService
{
    private readonly string _speechKey;
    private readonly string _speechRegion;
    private readonly IWebHostEnvironment _env;

    public AzureSpeechService(IConfiguration config, IWebHostEnvironment env)
    {
        _speechKey = config["Azure:SpeechKey"] ?? throw new Exception("Azure Speech Key chưa cấu hình.");
        _speechRegion = config["Azure:SpeechRegion"] ?? "eastus";
        _env = env;
    }

    public async Task<PronunciationResult> AnalyzePronunciation(string wavPath, string referenceText, string language = "en-US")
    {
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
        var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
        speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

        var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsPath);
        var audioPath = Path.Combine(uploadsPath, $"{fileName}.wav");

        using var audioOutput = AudioConfig.FromWavFileOutput(audioPath);
        using var synthesizer = new SpeechSynthesizer(speechConfig, audioOutput);

        var result = await synthesizer.SpeakSsmlAsync(ssml);
        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
        {
            return new SpeechSynthesisResult2 { Success = true, AudioPath = $"uploads/{fileName}.wav" };
        }

        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
        return new SpeechSynthesisResult2
        {
            Success = false,
            Message = cancellation.Reason == CancellationReason.Error
                ? cancellation.ErrorDetails
                : "Azure không thể tổng hợp giọng nói."
        };
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
