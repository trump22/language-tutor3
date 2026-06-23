using System.Net;
using System.Text;
using System.Text.Json;
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

    public async Task<PronunciationResult> AnalyzePronunciation(
        string wavPath,
        string referenceText,
        string language = "en-US",
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new PronunciationResult
            {
                Success = false,
                Message = "Azure Speech chưa được cấu hình. Hãy thêm Azure__SpeechKey và Azure__SpeechRegion trong App Service."
            };
        }

        try
        {
            var audioBytes = await File.ReadAllBytesAsync(wavPath, cancellationToken);
            var validationMessage = ValidatePronunciationWav(audioBytes);
            if (validationMessage != null)
            {
                return new PronunciationResult { Success = false, Message = validationMessage };
            }

            var assessmentConfig = JsonSerializer.Serialize(new
            {
                ReferenceText = referenceText.Trim(),
                GradingSystem = "HundredMark",
                Granularity = "Phoneme",
                Dimension = "Comprehensive",
                EnableMiscue = "True",
                EnableProsodyAssessment = "True"
            });
            var assessmentHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(assessmentConfig));
            var requestUrl =
                $"https://{_speechRegion}.stt.speech.microsoft.com/" +
                $"speech/recognition/conversation/cognitiveservices/v1" +
                $"?language={Uri.EscapeDataString(language)}&format=detailed";

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", _speechKey);
            request.Headers.TryAddWithoutValidation("Pronunciation-Assessment", assessmentHeader);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Content = new ByteArrayContent(audioBytes);
            request.Content.Headers.TryAddWithoutValidation(
                "Content-Type",
                "audio/wav; codecs=audio/pcm; samplerate=16000");

            using var response = await _httpClientFactory
                .CreateClient()
                .SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Azure pronunciation REST request failed. Region: {Region}, Status: {StatusCode}, Body: {Body}",
                    _speechRegion,
                    (int)response.StatusCode,
                    responseBody);
                return new PronunciationResult
                {
                    Success = false,
                    Message = BuildPronunciationErrorMessage(response.StatusCode, responseBody)
                };
            }

            return ParsePronunciationResponse(responseBody);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PronunciationResult
            {
                Success = false,
                Message = "Azure Speech phản hồi quá chậm. Vui lòng thử lại."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure pronunciation REST assessment failed in region {Region}.", _speechRegion);
            return new PronunciationResult
            {
                Success = false,
                Message = "Không thể phân tích phát âm. Vui lòng kiểm tra file ghi âm và thử lại."
            };
        }
    }

    private static PronunciationResult ParsePronunciationResponse(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var recognitionStatus = GetString(root, "RecognitionStatus");
        if (!string.Equals(recognitionStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            var message = recognitionStatus switch
            {
                "InitialSilenceTimeout" => "Không phát hiện giọng nói. Hãy nói gần micro và thử lại.",
                "BabbleTimeout" => "Âm thanh có quá nhiều tiếng ồn. Hãy thử lại ở nơi yên tĩnh.",
                "NoMatch" => "Không nhận diện được nội dung đã đọc. Hãy chọn đúng ngôn ngữ và đọc lại.",
                _ => $"Azure Speech không nhận diện được giọng nói ({recognitionStatus ?? "Unknown"})."
            };
            return new PronunciationResult { Success = false, Message = message };
        }

        if (!TryGetProperty(root, "NBest", out var nBest) ||
            nBest.ValueKind != JsonValueKind.Array ||
            nBest.GetArrayLength() == 0)
        {
            return new PronunciationResult
            {
                Success = false,
                Message = "Azure Speech không trả về dữ liệu chấm phát âm."
            };
        }

        var best = nBest[0];
        var words = new List<PronunciationWordResult>();
        if (TryGetProperty(best, "Words", out var wordsElement) &&
            wordsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var wordElement in wordsElement.EnumerateArray())
            {
                var phonemes = new List<PronunciationPhonemeResult>();
                if (TryGetProperty(wordElement, "Phonemes", out var phonemesElement) &&
                    phonemesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var phonemeElement in phonemesElement.EnumerateArray())
                    {
                        phonemes.Add(new PronunciationPhonemeResult
                        {
                            Phoneme = GetString(phonemeElement, "Phoneme") ?? "",
                            Score = GetDouble(phonemeElement, "AccuracyScore")
                        });
                    }
                }

                words.Add(new PronunciationWordResult
                {
                    Word = GetString(wordElement, "Word") ?? "",
                    AccuracyScore = GetDouble(wordElement, "AccuracyScore"),
                    ErrorType = GetString(wordElement, "ErrorType") ?? "None",
                    Phonemes = phonemes
                });
            }
        }

        return new PronunciationResult
        {
            Success = true,
            RecognizedText =
                GetString(best, "Display") ??
                GetString(root, "DisplayText") ??
                "",
            Scores = new PronunciationScores
            {
                PronunciationScore = (float)GetDouble(best, "PronScore"),
                AccuracyScore = (float)GetDouble(best, "AccuracyScore"),
                FluencyScore = (float)GetDouble(best, "FluencyScore"),
                CompletenessScore = (float)GetDouble(best, "CompletenessScore"),
                ProsodyScore = TryGetProperty(best, "ProsodyScore", out var prosody)
                    ? (float?)GetDouble(prosody)
                    : null
            },
            Words = words
        };
    }

    private static string? ValidatePronunciationWav(byte[] audioBytes)
    {
        if (audioBytes.Length < 44 ||
            Encoding.ASCII.GetString(audioBytes, 0, 4) != "RIFF" ||
            Encoding.ASCII.GetString(audioBytes, 8, 4) != "WAVE")
        {
            return "File ghi âm không phải WAV hợp lệ.";
        }

        var channels = BitConverter.ToUInt16(audioBytes, 22);
        var sampleRate = BitConverter.ToUInt32(audioBytes, 24);
        var bitsPerSample = BitConverter.ToUInt16(audioBytes, 34);
        if (channels != 1 || sampleRate != 16000 || bitsPerSample != 16)
        {
            return "Audio phải là WAV PCM mono, 16 kHz, 16-bit.";
        }

        var bytesPerSecond = sampleRate * channels * bitsPerSample / 8d;
        var durationSeconds = (audioBytes.Length - 44) / bytesPerSecond;
        return durationSeconds > 30
            ? "Đoạn ghi âm dài hơn 30 giây. Hãy đọc đoạn ngắn hơn."
            : null;
    }

    private static string BuildPronunciationErrorMessage(HttpStatusCode statusCode, string responseBody)
    {
        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return "Azure Speech từ chối xác thực. Speech key hoặc region không đúng.";
        if (statusCode == HttpStatusCode.TooManyRequests)
            return "Azure Speech đã hết quota hoặc đang giới hạn số lượt chấm.";
        if (statusCode == HttpStatusCode.BadRequest)
            return "Azure Speech từ chối file audio. Hãy ghi âm dưới 30 giây và thử lại.";

        return string.IsNullOrWhiteSpace(responseBody)
            ? $"Azure Speech trả về HTTP {(int)statusCode}."
            : $"Azure Speech không thể chấm phát âm (HTTP {(int)statusCode}).";
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? GetString(JsonElement element, string name) =>
        TryGetProperty(element, name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double GetDouble(JsonElement element, string name) =>
        TryGetProperty(element, name, out var value) ? GetDouble(value) : 0;

    private static double GetDouble(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) ? number : 0;

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
