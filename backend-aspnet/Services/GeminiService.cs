using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace languagetutor.Services;

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _liteModel;
    private readonly string _flash20LiteModel;
    private readonly string _flash20Model;
    private readonly string _flashModel;
    private readonly string _proModel;
    private readonly IReadOnlyList<string> _cheapModels;
    private readonly IReadOnlyList<string> _balancedModels;
    private readonly IReadOnlyList<string> _reasoningModels;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public GeminiService(IConfiguration config, IHttpClientFactory factory)
    {
        _http = factory.CreateClient();
        _http.Timeout = TimeSpan.FromSeconds(75);
        _apiKey = config["Gemini:ApiKey"] ?? throw new Exception("Gemini API Key chưa được cấu hình.");
        _liteModel = config["Gemini:LiteModel"] ?? "gemini-2.5-flash-lite";
        _flash20LiteModel = config["Gemini:Flash20LiteModel"] ?? "gemini-2.0-flash-lite";
        _flash20Model = config["Gemini:Flash20Model"] ?? "gemini-2.0-flash";
        _flashModel = config["Gemini:FlashModel"] ?? "gemini-2.5-flash";
        _proModel = config["Gemini:ProModel"] ?? "gemini-2.5-pro";

        _cheapModels = BuildModelList(_liteModel, _flash20LiteModel, _flash20Model, _flashModel);
        _balancedModels = BuildModelList(_flash20LiteModel, _liteModel, _flash20Model, _flashModel, _proModel);
        _reasoningModels = BuildModelList(_flash20Model, _liteModel, _flashModel, _proModel, _flash20LiteModel);
    }

    public async Task<string> GetTutorResponse(
        string userMessage,
        string language,
        string? level,
        string systemPrompt,
        List<ChatHistoryItem> history,
        double? temperature = null,
        int? maxTokens = null)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var targetLanguage = normalizedLanguage == "ZH" ? "tiếng Trung giản thể" : "tiếng Anh";
        var pronunciationLine = normalizedLanguage == "ZH" ? "Phiên âm pinyin đầy đủ." : "Để trống phần này.";

        var systemInstruction = $"""
{systemPrompt}

TRÌNH ĐỘ HỌC VIÊN: {level ?? "Beginner"}.
QUY TẮC PHẢN HỒI BẮT BUỘC:
Trả lời đúng cấu trúc 3 phần, ngăn cách bằng dấu " --- ".
1. Câu trả lời bằng {targetLanguage}, phù hợp trình độ học viên. Không dùng emoji/icon trong phần này.
2. {pronunciationLine}
3. Dịch nghĩa và ghi chú ngắn bằng tiếng Việt.

Ví dụ tiếng Trung: 你好 --- Nǐ hǎo --- Chào bạn.
Ví dụ tiếng Anh: Good morning ---  --- Chào buổi sáng.
""";

        var contents = history
            .Select(h => new { role = h.Role, parts = new[] { new { text = h.Text } } } as object)
            .ToList();
        contents.Add(new { role = "user", parts = new[] { new { text = userMessage } } });

        var body = BuildRequestBody(contents, systemInstruction, null, temperature ?? 0.7, Math.Min(maxTokens ?? 768, 1024));
        var raw = await PostAndReadText(_cheapModels, body);
        return ExtractText(raw);
    }

    public async Task<string> GenerateRandomPracticeText(string language, string? level)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var targetLanguage = normalizedLanguage == "ZH" ? "tiếng Trung giản thể" : "tiếng Anh";
        var prompt = $"""
Nhiệm vụ: Tạo một đoạn văn ngắn hoặc hội thoại ngắn từ 25 đến 40 từ.
Ngôn ngữ: {targetLanguage}.
Trình độ: {level ?? "Beginner"}.
Chủ đề: đời sống, du lịch, công sở hoặc học tập.

Yêu cầu:
- Văn bản tự nhiên, dễ đọc thành tiếng.
- Phù hợp để luyện phát âm.
- Chỉ trả về nội dung văn bản, không giải thích, không markdown, không dấu ngoặc kép.
""";

        var body = BuildRequestBody(UserContents(prompt), null, null, 0.75, 320);
        return ExtractText(await PostAndReadText(_cheapModels, body)).Trim();
    }

    public async Task<string> AnalyzeText(string text, string language)
    {
        var prompt = $"""
Bạn là chuyên gia ngôn ngữ. Hãy phân tích đoạn văn sau:
"{text}"

Ngôn ngữ: {language}.

Trả lời bằng tiếng Việt theo Markdown:
### 1. Dịch nghĩa
### 2. Từ vựng quan trọng
### 3. Cấu trúc ngữ pháp
### 4. Lưu ý văn hóa/ngữ cảnh nếu có
""";

        var body = BuildRequestBody(UserContents(prompt), null, null, 0.35, 1200);
        return ExtractText(await PostAndReadText(_reasoningModels, body));
    }

    public async Task<JsonElement> GenerateLearningCoachPlan(string profileJson, string language)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var targetLanguage = normalizedLanguage == "ZH" ? "tiếng Trung giản thể" : "tiếng Anh";
        var prompt = $$"""
Bạn là AI Learning Coach của LinguaConnect. Nhiệm vụ của bạn là giám sát tiến độ học viên và cá nhân hóa lộ trình học.

Dữ liệu học viên ở dạng JSON:
{{profileJson}}

Yêu cầu:
- Trả lời bằng tiếng Việt, riêng ví dụ/bài tập ngôn ngữ đích dùng {{targetLanguage}}.
- Dựa vào bằng chứng trong dữ liệu. Nếu dữ liệu còn ít, nói rõ và đề xuất lộ trình khởi động.
- Ưu tiên khuyến nghị grammar nếu thấy điểm bài học thấp, chat có nhiều lỗi diễn đạt, hoặc chưa có dữ liệu.
- Không dùng markdown. Không bọc JSON trong ```json.
- Viết JSON gọn: strengths tối đa 3 mục, weaknesses tối đa 3 mục, recommendedLessons tối đa 4 mục, grammarFocus tối đa 3 mục, pronunciationFocus tối đa 3 mục.
- weeklyPlan đúng 5 ngày, mỗi ngày tối đa 2 tasks. Không xuống dòng bên trong string.

Trả về đúng một object JSON theo schema:
{
  "summary": "Tóm tắt ngắn tình trạng học tập hiện tại",
  "levelAssessment": "Nhận định trình độ và nhịp học",
  "strengths": ["Điểm mạnh 1", "Điểm mạnh 2"],
  "weaknesses": [
    {
      "skill": "Grammar/Vocabulary/Pronunciation/Listening/Speaking",
      "issue": "Vấn đề cụ thể",
      "evidence": "Dữ liệu chứng minh",
      "priority": "high/medium/low"
    }
  ],
  "recommendedLessons": [
    {
      "lessonId": "id nếu có, nếu không để null",
      "title": "Tên bài học hoặc chủ đề nên học",
      "reason": "Lý do đề xuất"
    }
  ],
  "weeklyPlan": [
    {
      "day": "Day 1",
      "focus": "Trọng tâm",
      "tasks": ["Việc cần làm 1", "Việc cần làm 2"],
      "durationMinutes": 25
    }
  ],
  "grammarFocus": [
    {
      "topic": "Chủ điểm grammar",
      "explanation": "Giải thích rất ngắn bằng tiếng Việt",
      "microExercise": "Một câu bài tập ngắn",
      "answer": "Đáp án"
    }
  ],
  "pronunciationFocus": [
    {
      "wordOrSound": "Từ hoặc âm cần luyện",
      "tip": "Mẹo luyện ngắn"
    }
  ],
  "nextAction": "Một hành động nên làm ngay bây giờ"
}
""";

        var body = BuildRequestBody(UserContents(prompt), null, "application/json", 0.3, 2600);
        var raw = ExtractText(await PostAndReadText(_reasoningModels, body));

        try
        {
            return ParseJsonObject(raw);
        }
        catch (JsonException)
        {
            var repairPrompt = $"""
Hãy sửa nội dung sau thành một object JSON hợp lệ theo đúng schema đã yêu cầu.
Không thêm markdown, không giải thích.

{raw}
""";
            var flashBody = BuildRequestBody(UserContents(repairPrompt), null, "application/json", 0.1, 2200);
            var flashRaw = ExtractText(await PostAndReadText(_balancedModels, flashBody));
            return ParseJsonObject(flashRaw);
        }
    }

    public async Task<List<JsonElement>> GeneratePersonalizedGrammarPractice(string profileJson, string language, string? level)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var targetLanguage = normalizedLanguage == "ZH" ? "tiếng Trung giản thể" : "tiếng Anh";
        var prompt = $$"""
Bạn là Grammar Coach của LinguaConnect. Hãy tạo bài luyện grammar cá nhân hóa dựa trên dữ liệu học viên.

Dữ liệu học viên:
{{profileJson}}

Ngôn ngữ bài tập: {{targetLanguage}}.
Trình độ tham chiếu: {{level ?? "Beginner"}}.

Yêu cầu:
- Tạo đúng 5 bài tập grammar ngắn, tập trung vào lỗi hoặc khoảng trống năng lực có trong dữ liệu.
- Nếu dữ liệu còn ít, tạo bài khởi động về cấu trúc câu cơ bản, thì hiện tại, câu hỏi, giới từ và trật tự từ.
- Trả về JSON array thuần, không markdown.

Mỗi phần tử có schema:
{
  "type": "multiple-choice" hoặc "fill-in-the-blank",
  "question": "Câu hỏi",
  "options": ["A", "B", "C", "D"],
  "answer": "Đáp án chính xác",
  "explanation": "Giải thích ngắn bằng tiếng Việt"
}
""";

        var body = BuildRequestBody(UserContents(prompt), null, "application/json", 0.35, 1600);
        var raw = ExtractText(await PostAndReadText(_cheapModels, body));

        try
        {
            return ParseJsonArray(raw);
        }
        catch (JsonException)
        {
            return BuildFallbackExercises(normalizedLanguage, "personalized grammar");
        }
    }

    public async Task<List<JsonElement>> GenerateExercises(string language, string? level, string? topic, string? systemPrompt)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var targetLanguage = normalizedLanguage == "ZH" ? "tiếng Trung giản thể" : "tiếng Anh";
        var prompt = $$"""
{{systemPrompt ?? "Bạn là chuyên gia thiết kế bài tập ngôn ngữ."}}

Tạo đúng 5 bài tập cho học viên.
Ngôn ngữ bài tập: {{targetLanguage}}.
Trình độ: {{level ?? "Beginner"}}.
Chủ đề: {{topic ?? "daily conversation"}}.

Yêu cầu JSON bắt buộc:
Trả về một mảng JSON thuần. Mỗi phần tử có cấu trúc:
{
  "type": "multiple-choice" hoặc "fill-in-the-blank",
  "question": "Nội dung câu hỏi",
  "options": ["A", "B", "C", "D"],
  "answer": "Đáp án đúng, phải trùng chính xác một option nếu là multiple-choice",
  "explanation": "Giải thích ngắn bằng tiếng Việt"
}

Luật:
- Với fill-in-the-blank, dùng dấu ____ trong câu hỏi và để options là [].
- Nội dung question/options/answer dùng {{targetLanguage}}.
- Explanation luôn dùng tiếng Việt.
- Không bọc JSON bằng markdown.
""";

        var body = BuildRequestBody(UserContents(prompt), null, "application/json", 0.4, 1600);
        var raw = ExtractText(await PostAndReadText(_cheapModels, body));

        try
        {
            return ParseJsonArray(raw);
        }
        catch (JsonException)
        {
            return BuildFallbackExercises(normalizedLanguage, topic);
        }
    }

    public async Task<JsonElement> GenerateListeningDraft(string rawScript, string? level, int part)
    {
        var partInstructions = part switch
        {
            1 => $"""
TOEIC Part 1 - Photographs:
Dựa vào mô tả ảnh: "{rawScript}".
Tạo đúng 4 câu miêu tả độc lập A, B, C, D bằng tiếng Anh. Chỉ có 1 câu đúng.
Không tạo hội thoại. transcript là toàn bộ lời đọc của audio.
""",
            2 => $"""
TOEIC Part 2 - Question Response:
Bối cảnh: "{rawScript}".
Tạo 1 câu hỏi/câu nói ngắn và 3 phản hồi A, B, C. Chỉ có 1 đáp án đúng.
""",
            _ => $"""
TOEIC Part 3 - Short Conversations:
Kịch bản: "{rawScript}".
Chuyển thành đoạn hội thoại tiếng Anh tự nhiên 2-3 nhân vật và tạo 3 câu hỏi A, B, C, D.
"""
        };

        var prompt = $$"""
Bạn là chuyên gia ra đề TOEIC Listening và đạo diễn giọng đọc Azure Speech.

{{partInstructions}}

Độ khó: {{level ?? "Intermediate"}}.

Quy tắc SSML Azure:
- Root phải là <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>.
- Bên trong dùng một hoặc nhiều thẻ <voice name='en-US-JennyNeural'> hoặc <voice name='en-US-GuyNeural'>.
- Thẻ <break time='1s'/> phải nằm bên trong <voice>.
- Không dùng markdown.

Trả về đúng một object JSON:
{
  "imageUrl": null,
  "ssml": "<speak ...>...</speak>",
  "transcript": "Transcript tiếng Anh đầy đủ",
  "questions": [
    {
      "text": "Question text",
      "options": { "A": "...", "B": "...", "C": "...", "D": "..." },
      "correctAnswer": "A",
      "explanation": "Giải thích bằng tiếng Việt"
    }
  ]
}

Với Part 2 chỉ dùng options A, B, C. Với Part 1, text có thể là "Look at the photo.".
""";

        var body = BuildRequestBody(UserContents(prompt), null, "application/json", 0.35, 2200);
        var raw = ExtractText(await PostAndReadText(_balancedModels, body));
        return ParseJsonObject(raw);
    }

    public async Task<string> GetAIResponse(string context, string userMessage)
    {
        var prompt = $"""
Vai trò: Bạn là giám đốc học thuật của ứng dụng học ngôn ngữ LinguaConnect.
Dữ liệu hệ thống:
{context}

Yêu cầu:
{userMessage}

Trả lời bằng tiếng Việt:
- Phân tích xu hướng học tập.
- Chỉ ra bài học quá khó hoặc quá dễ nếu có dữ liệu.
- Đề xuất 3 hành động cụ thể để cải thiện chất lượng giảng dạy.
""";

        var body = BuildRequestBody(UserContents(prompt), null, null, 0.35, 1200);
        return ExtractText(await PostAndReadText(_reasoningModels, body));
    }

    private async Task<string> PostAndReadText(string modelName, object body)
    {
        return await PostAndReadText(new[] { modelName }, body);
    }

    private async Task<string> PostAndReadText(IEnumerable<string> modelNames, object body)
    {
        GeminiServiceException? lastException = null;

        foreach (var modelName in modelNames)
        {
            try
            {
                return await PostAndReadTextSingleModel(modelName, body);
            }
            catch (GeminiServiceException ex) when (ShouldTryNextModel(ex.StatusCode))
            {
                lastException = ex;
            }
        }

        throw lastException ?? new GeminiServiceException(HttpStatusCode.ServiceUnavailable, "Không có Gemini model khả dụng.");
    }

    private async Task<string> PostAndReadTextSingleModel(string modelName, object body)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) ||
            _apiKey.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new GeminiServiceException(
                HttpStatusCode.ServiceUnavailable,
                "Gemini API key is not configured in the deployment environment.");
        }

        var url = $"{BaseUrl}/{modelName}:generateContent?key={_apiKey}";
        var response = await PostWithRetry(url, body);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<HttpResponseMessage> PostWithRetry(string url, object body)
    {
        HttpResponseMessage? lastResponse = null;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            lastResponse = await _http.PostAsJsonAsync(url, body);
            if (lastResponse.IsSuccessStatusCode)
                return lastResponse;

            if (!ShouldRetry(lastResponse.StatusCode) || attempt == 3)
                break;

            await Task.Delay(GetRetryDelay(lastResponse, attempt));
        }

        var responseBody = lastResponse == null ? "" : await lastResponse.Content.ReadAsStringAsync();
        throw new GeminiServiceException(lastResponse?.StatusCode ?? HttpStatusCode.ServiceUnavailable, responseBody);
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
            return delta > TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : delta;

        if (response.Headers.RetryAfter?.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
                return delay > TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : delay;
        }

        return TimeSpan.FromMilliseconds(700 * attempt * attempt);
    }

    private static object BuildRequestBody(
        List<object> contents,
        string? systemInstruction,
        string? responseMimeType,
        double? temperature,
        int? maxOutputTokens)
    {
        var generationConfig = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(responseMimeType))
            generationConfig["responseMimeType"] = responseMimeType;
        if (temperature.HasValue)
            generationConfig["temperature"] = temperature.Value;
        if (maxOutputTokens.HasValue)
            generationConfig["maxOutputTokens"] = maxOutputTokens.Value;

        var body = new Dictionary<string, object>
        {
            ["contents"] = contents
        };

        if (!string.IsNullOrWhiteSpace(systemInstruction))
            body["system_instruction"] = new { parts = new[] { new { text = systemInstruction } } };
        if (generationConfig.Count > 0)
            body["generationConfig"] = generationConfig;

        return body;
    }

    private static List<object> UserContents(string prompt) =>
        new() { new { role = "user", parts = new[] { new { text = prompt } } } };

    private static string ExtractText(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            throw new GeminiServiceException(HttpStatusCode.BadGateway, rawJson);

        var parts = candidates[0].GetProperty("content").GetProperty("parts");
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textElement))
                return textElement.GetString() ?? "";
        }

        throw new GeminiServiceException(HttpStatusCode.BadGateway, rawJson);
    }

    private static List<JsonElement> ParseJsonArray(string raw)
    {
        var json = StripCodeFence(raw);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
            return root.EnumerateArray().Select(e => e.Clone()).ToList();

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in new[] { "exercises", "questions", "items" })
            {
                if (root.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    return arr.EnumerateArray().Select(e => e.Clone()).ToList();
            }
        }

        throw new JsonException("Gemini did not return a JSON array.");
    }

    private static JsonElement ParseJsonObject(string raw)
    {
        var json = StripCodeFence(raw);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("Gemini did not return a JSON object.");

        return doc.RootElement.Clone();
    }

    private static string StripCodeFence(string raw)
    {
        var text = raw.Trim();
        text = text.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("```", "")
                   .Trim();

        var firstArray = text.IndexOf('[');
        var firstObject = text.IndexOf('{');
        var start = firstArray >= 0 && (firstObject < 0 || firstArray < firstObject) ? firstArray : firstObject;
        if (start < 0) return text;

        var endArray = text.LastIndexOf(']');
        var endObject = text.LastIndexOf('}');
        var end = Math.Max(endArray, endObject);
        return end >= start ? text[start..(end + 1)] : text;
    }

    private static List<JsonElement> BuildFallbackExercises(string language, string? topic)
    {
        var target = language == "ZH" ? "中文" : "English";
        var subject = string.IsNullOrWhiteSpace(topic) ? "daily conversation" : topic;
        var data = new object[]
        {
            new
            {
                type = "multiple-choice",
                question = language == "ZH" ? $"选择关于“{subject}”的正确句子。" : $"Choose the best sentence about {subject}.",
                options = language == "ZH"
                    ? new[] { "我想练习中文。", "我昨天会去。", "他很高兴吗。", "我们是学习。" }
                    : new[] { $"I want to practice {target}.", "I am go yesterday.", "He very happy?", "We is learning." },
                answer = language == "ZH" ? "我想练习中文。" : $"I want to practice {target}.",
                explanation = "Câu đúng có cấu trúc tự nhiên và phù hợp ngữ cảnh."
            },
            new
            {
                type = "fill-in-the-blank",
                question = language == "ZH" ? "我____学习中文。" : "I ____ learning English.",
                options = Array.Empty<string>(),
                answer = language == "ZH" ? "想" : "am",
                explanation = "Điền từ phù hợp để hoàn chỉnh cấu trúc câu."
            }
        };

        return data.Select(item => JsonSerializer.SerializeToElement(item)).ToList();
    }

    private static string NormalizeLanguage(string language) =>
        language.Trim().ToUpperInvariant() switch
        {
            "ZH" or "CN" or "ZH-CN" or "CHINESE" => "ZH",
            _ => "EN"
        };

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests ||
        statusCode == HttpStatusCode.ServiceUnavailable ||
        (int)statusCode >= 500;

    private static bool ShouldTryNextModel(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests ||
        statusCode == HttpStatusCode.Forbidden ||
        statusCode == HttpStatusCode.NotFound ||
        statusCode == HttpStatusCode.ServiceUnavailable ||
        statusCode == HttpStatusCode.BadGateway ||
        statusCode == HttpStatusCode.GatewayTimeout ||
        (int)statusCode >= 500;

    private static IReadOnlyList<string> BuildModelList(params string[] modelNames) =>
        modelNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

public record ChatHistoryItem(string Role, string Text);

public class GeminiServiceException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    public GeminiServiceException(HttpStatusCode statusCode, string responseBody)
        : base($"Gemini API request failed with status {(int)statusCode} ({statusCode}).")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
