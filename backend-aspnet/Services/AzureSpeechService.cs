using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using System.Net;
using System.Text;
using System.Xml.Linq;
namespace languagetutor.Services;

public class AzureSpeechService
{
    private readonly string _speechKey;
    private readonly string _speechRegion;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AzureSpeechService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public AzureSpeechService(
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<AzureSpeechService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _speechKey = config["Azure:SpeechKey"]?.Trim() ?? "";
        _speechRegion = config["Azure:SpeechRegion"]?.Trim() ?? "";
        _env = env;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_speechKey) &&
        !_speechKey.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(_speechRegion);

    public string Region => _speechRegion;

    public async Task<SpeechServiceHealth> CheckHealthAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return new SpeechServiceHealth(
                false,
                "Azure Speech chưa được cấu hình.",
                null);
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{_speechRegion}.tts.speech.microsoft.com/cognitiveservices/voices/list");
            request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", _speechKey);

            using var response = await _httpClientFactory
                .CreateClient()
                .SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? new SpeechServiceHealth(true, "Azure Speech REST API is reachable.", (int)response.StatusCode)
                : new SpeechServiceHealth(
                    false,
                    BuildRestErrorMessage(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken)),
                    (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Speech health request failed for region {Region}.", _speechRegion);
            return new SpeechServiceHealth(false, "Không kết nối được Azure Speech REST API.", null);
        }
    }

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

    public async Task<SpeechSynthesisResult2> SynthesizeSpeech(
        string ssml,
        string fileName,
        string? fallbackText = null)
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
            XDocument.Parse(ssml);
            var synthesis = await CallSynthesisRestAsync(ssml);

            if (!synthesis.Success &&
                synthesis.StatusCode == HttpStatusCode.BadRequest &&
                !string.IsNullOrWhiteSpace(fallbackText))
            {
                _logger.LogWarning(
                    "Gemini SSML was rejected by Azure Speech. Retrying with normalized single-voice SSML.");
                synthesis = await CallSynthesisRestAsync(BuildFallbackSsml(fallbackText));
            }

            if (!synthesis.Success)
            {
                return new SpeechSynthesisResult2
                {
                    Success = false,
                    Message = BuildRestErrorMessage(synthesis.StatusCode, synthesis.ErrorBody)
                };
            }

            var audioBytes = synthesis.AudioBytes;
            if (audioBytes.Length <= 44)
            {
                return new SpeechSynthesisResult2
                {
                    Success = false,
                    Message = "Azure Speech trả về file audio rỗng."
                };
            }

            var uploadsPath = GetUploadsPath();
            Directory.CreateDirectory(uploadsPath);
            var audioPath = Path.Combine(uploadsPath, $"{fileName}.wav");
            await File.WriteAllBytesAsync(audioPath, audioBytes);

            return new SpeechSynthesisResult2
            {
                Success = true,
                AudioPath = $"uploads/{fileName}.wav"
            };
        }
        catch (System.Xml.XmlException ex)
        {
            _logger.LogWarning(ex, "Gemini returned invalid SSML.");
            return new SpeechSynthesisResult2
            {
                Success = false,
                Message = "Kịch bản SSML do Gemini tạo không hợp lệ. Hãy thử tạo lại."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Speech REST synthesis threw an exception in region {Region}.", _speechRegion);
            return new SpeechSynthesisResult2
            {
                Success = false,
                Message = "Không thể gọi Azure Speech REST API hoặc ghi file audio trên App Service."
            };
        }
    }

    private async Task<SpeechRestResponse> CallSynthesisRestAsync(string ssml)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://{_speechRegion}.tts.speech.microsoft.com/cognitiveservices/v1");
        request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", _speechKey);
        request.Headers.TryAddWithoutValidation("X-Microsoft-OutputFormat", "riff-16khz-16bit-mono-pcm");
        request.Headers.TryAddWithoutValidation("User-Agent", "LanguageTutor");
        request.Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");

        using var response = await _httpClientFactory
            .CreateClient()
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (response.IsSuccessStatusCode)
        {
            return new SpeechRestResponse(
                true,
                response.StatusCode,
                await response.Content.ReadAsByteArrayAsync(),
                "");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogWarning(
            "Azure Speech REST synthesis failed. Region: {Region}, Status: {StatusCode}, Body: {Body}",
            _speechRegion,
            (int)response.StatusCode,
            responseBody);

        return new SpeechRestResponse(false, response.StatusCode, Array.Empty<byte>(), responseBody);
    }

    private static string BuildFallbackSsml(string text)
    {
        XNamespace synthesis = "http://www.w3.org/2001/10/synthesis";
        var document = new XDocument(
            new XElement(
                synthesis + "speak",
                new XAttribute("version", "1.0"),
                new XAttribute(XNamespace.Xml + "lang", "en-US"),
                new XElement(
                    synthesis + "voice",
                    new XAttribute("name", "en-US-JennyNeural"),
                    text.Trim())));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private string GetUploadsPath()
    {
        var homePath = Environment.GetEnvironmentVariable("HOME");
        return string.IsNullOrWhiteSpace(homePath)
            ? Path.Combine(_env.ContentRootPath, "uploads")
            : Path.Combine(homePath, "data", "uploads");
    }

    private static string BuildRestErrorMessage(HttpStatusCode statusCode, string responseBody)
    {
        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return "Azure Speech từ chối xác thực. Speech key hoặc region không đúng với Speech resource.";
        }

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            return "Azure Speech đã hết quota hoặc đang bị giới hạn số lượt gọi.";
        }

        if (statusCode == HttpStatusCode.BadRequest ||
            responseBody.Contains("SSML", StringComparison.OrdinalIgnoreCase))
        {
            return "Azure Speech từ chối SSML hoặc voice đang dùng. Hãy thử tạo lại với nội dung ngắn hơn.";
        }

        return $"Azure Speech REST API trả về HTTP {(int)statusCode}.";
    }
}

public record SpeechServiceHealth(bool Success, string Message, int? ProviderStatus);
internal record SpeechRestResponse(
    bool Success,
    HttpStatusCode StatusCode,
    byte[] AudioBytes,
    string ErrorBody);

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
