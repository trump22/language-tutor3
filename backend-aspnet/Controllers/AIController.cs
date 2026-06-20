using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using languagetutor.Data;
using languagetutor.DTOs;
using languagetutor.Models;
using languagetutor.Services;

namespace languagetutor.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GeminiService _gemini;
    private readonly AzureSpeechService _azureSpeech;
    private readonly IWebHostEnvironment _env;

    public AIController(AppDbContext db, GeminiService gemini, AzureSpeechService azureSpeech, IWebHostEnvironment env)
    {
        _db = db;
        _gemini = gemini;
        _azureSpeech = azureSpeech;
        _env = env;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst("userId")?.Value ?? User.FindFirst("id")?.Value ?? "0");

    // GET /api/ai/teachers
    [HttpGet("teachers")]
    public async Task<IActionResult> GetAllTeachers()
    {
        var teachers = await _db.AITeachers.OrderBy(t => t.CreatedAt).ToListAsync();
        return Ok(new { success = true, data = teachers });
    }

    // POST /api/ai/chat
    [HttpPost("chat")]
    public async Task<IActionResult> ChatWithTutor([FromBody] ChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { message = "Message is required." });

        var userId = GetUserId();
        var lang = NormalizeAiLanguage(req.Language);
        var personaName = string.IsNullOrWhiteSpace(req.PersonaName) ? "Friendly Tutor" : req.PersonaName.Trim();

        var student = await _db.Users.FindAsync(userId);
        var teacher = await _db.AITeachers.FirstOrDefaultAsync(t =>
            t.SupportLanguage == lang && t.Name == personaName);

        teacher ??= await _db.AITeachers
            .Where(t => t.SupportLanguage == lang && t.Name != "Exercise Creator")
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (teacher == null)
            return NotFound(new { message = "Chưa có prompt giáo viên AI cho ngôn ngữ này. Hãy chạy backend fixed hoặc tạo persona trong trang Admin AI Configuration." });

        var rawHistory = await _db.ChatMessages
            .Where(m => m.UserId == userId && m.Language == lang && m.PersonaName == teacher.Name)
            .OrderByDescending(m => m.CreatedAt)
            .Take(20)
            .ToListAsync();
        rawHistory.Reverse();

        var formattedHistory = new List<ChatHistoryItem>();
        var expectedRole = "user";
        foreach (var msg in rawHistory)
        {
            var role = msg.Role == "model" ? "model" : "user";
            if (role == expectedRole)
            {
                formattedHistory.Add(new ChatHistoryItem(role, msg.Text));
                expectedRole = expectedRole == "user" ? "model" : "user";
            }
        }

        if (formattedHistory.Count > 0 && formattedHistory[^1].Role == "user")
            formattedHistory.RemoveAt(formattedHistory.Count - 1);

        var finalPrompt = ReplacePromptVariables(teacher.SystemPrompt, student);

        try
        {
            var reply = await _gemini.GetTutorResponse(
                req.Message,
                lang,
                req.Level,
                finalPrompt,
                formattedHistory,
                teacher.Temperature,
                teacher.MaxTokens);

            _db.ChatMessages.AddRange(
                new ChatMessage { UserId = userId, Role = "user", Text = req.Message, Language = lang, PersonaName = teacher.Name },
                new ChatMessage { UserId = userId, Role = "model", Text = reply, Language = lang, PersonaName = teacher.Name }
            );
            await _db.SaveChangesAsync();

            return Ok(new { reply, personaName = teacher.Name });
        }
        catch (GeminiServiceException ex)
        {
            return StatusCode(ToClientStatus(ex.StatusCode), new
            {
                success = false,
                message = "Gemini đang quá tải hoặc chưa phản hồi được. Vui lòng thử lại sau.",
                providerStatus = (int)ex.StatusCode
            });
        }
    }

    // GET /api/ai/chat-history
    [HttpGet("chat-history")]
    public async Task<IActionResult> GetChatHistory([FromQuery] string? language, [FromQuery] string? personaName)
    {
        var userId = GetUserId();
        var lang = NormalizeAiLanguage(language);
        var persona = string.IsNullOrWhiteSpace(personaName) ? "Friendly Tutor" : personaName.Trim();

        var history = await _db.ChatMessages
            .Where(m => m.UserId == userId && m.Language == lang && m.PersonaName == persona)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return Ok(new { success = true, data = history });
    }

    // POST /api/ai/generate-practice-text
    [HttpPost("generate-practice-text")]
    public async Task<IActionResult> GeneratePracticeText([FromBody] GeneratePracticeTextRequest req)
    {
        try
        {
            var text = await _gemini.GenerateRandomPracticeText(req.Language, req.Level);
            return Ok(new { success = true, text });
        }
        catch (GeminiServiceException ex)
        {
            return StatusCode(ToClientStatus(ex.StatusCode), new { success = false, message = "Không tạo được văn bản luyện phát âm từ Gemini.", providerStatus = (int)ex.StatusCode });
        }
    }

    // POST /api/ai/pronunciation/evaluate
    [HttpPost("pronunciation/evaluate")]
    public async Task<IActionResult> EvaluatePronunciation()
    {
        string? tempPath = null;

        try
        {
            var form = await Request.ReadFormAsync();
            var audio = form.Files["audio"] ?? form.Files["Audio"];
            var referenceText = form["referenceText"].FirstOrDefault() ?? form["ReferenceText"].FirstOrDefault();
            var language = form["language"].FirstOrDefault() ?? form["Language"].FirstOrDefault();

            if (audio == null) return BadRequest(new { message = "Không tìm thấy file audio." });
            if (string.IsNullOrWhiteSpace(referenceText))
                return BadRequest(new { message = "Reference text is required." });

            if (!audio.FileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) &&
                !audio.ContentType.Contains("wav", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Audio gửi lên phải là WAV PCM. Frontend mới đã tự ghi âm đúng định dạng này." });
            }

            var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);
            tempPath = Path.Combine(uploadsPath, $"temp_{Guid.NewGuid():N}.wav");

            await using (var stream = System.IO.File.Create(tempPath))
                await audio.CopyToAsync(stream);

            var result = await _azureSpeech.AnalyzePronunciation(tempPath, referenceText, language ?? "en-US");
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            _db.PronunciationRecords.Add(new PronunciationRecord
            {
                UserId = GetUserId(),
                ReferenceText = referenceText,
                OverallScore = result.Scores!.PronunciationScore,
                AccuracyScore = result.Scores.AccuracyScore,
                FluencyScore = result.Scores.FluencyScore,
                CompletenessScore = result.Scores.CompletenessScore,
                ProsodyScore = result.Scores.ProsodyScore,
                WordDetails = JsonSerializer.Serialize(result.Words ?? new List<PronunciationWordResult>())
            });
            await _db.SaveChangesAsync();

            return Ok(new { success = true, data = result });
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempPath) && System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    // POST /api/ai/analyze-text
    [HttpPost("analyze-text")]
    public async Task<IActionResult> AnalyzeText([FromBody] AnalyzeTextRequest req)
    {
        try
        {
            var analysis = await _gemini.AnalyzeText(req.Text, req.Language);
            return Ok(new { analysis });
        }
        catch (GeminiServiceException ex)
        {
            return StatusCode(ToClientStatus(ex.StatusCode), new { success = false, message = "Không phân tích được văn bản từ Gemini.", providerStatus = (int)ex.StatusCode });
        }
    }

    // POST /api/ai/generate-exercises
    [HttpPost("generate-exercises")]
    public async Task<IActionResult> GenerateExercises([FromBody] GenerateExercisesRequest req)
    {
        try
        {
            var lang = NormalizeAiLanguage(req.Language);
            var teacher = await _db.AITeachers.FirstOrDefaultAsync(t =>
                t.SupportLanguage == lang && t.Name == "Exercise Creator");

            var exercises = await _gemini.GenerateExercises(lang, req.Level, req.Topic, teacher?.SystemPrompt);
            return Ok(new { success = true, exercises });
        }
        catch (GeminiServiceException ex)
        {
            return StatusCode(ToClientStatus(ex.StatusCode), new
            {
                success = false,
                message = GetGeminiErrorMessage(ex.StatusCode),
                providerStatus = (int)ex.StatusCode
            });
        }
    }

    // GET /api/ai/learning-coach
    [HttpGet("learning-coach")]
    public async Task<IActionResult> GetLearningCoach([FromQuery] string? language)
    {
        var lang = NormalizeAiLanguage(language);
        var profile = await BuildLearningProfile(GetUserId(), lang);
        var profileJson = SerializeCoachProfile(profile);

        try
        {
            var plan = await _gemini.GenerateLearningCoachPlan(profileJson, lang);
            return Ok(new
            {
                success = true,
                source = "gemini",
                data = plan,
                profile = BuildProfileSummary(profile)
            });
        }
        catch (GeminiServiceException ex)
        {
            return Ok(new
            {
                success = true,
                source = "fallback",
                message = "Gemini chưa phản hồi được nên hệ thống đang dùng lộ trình gợi ý từ dữ liệu hiện có.",
                providerStatus = (int)ex.StatusCode,
                data = BuildFallbackLearningPlan(profile, lang),
                profile = BuildProfileSummary(profile)
            });
        }
        catch (JsonException)
        {
            return Ok(new
            {
                success = true,
                source = "fallback",
                message = "Gemini trả về JSON không hợp lệ nên hệ thống đang dùng lộ trình gợi ý từ dữ liệu hiện có.",
                data = BuildFallbackLearningPlan(profile, lang),
                profile = BuildProfileSummary(profile)
            });
        }
    }

    // POST /api/ai/personalized-grammar-practice
    [HttpPost("personalized-grammar-practice")]
    public async Task<IActionResult> GeneratePersonalizedGrammarPractice([FromBody] PersonalizedGrammarPracticeRequest? req)
    {
        var lang = NormalizeAiLanguage(req?.Language);
        var profile = await BuildLearningProfile(GetUserId(), lang);
        var profileJson = SerializeCoachProfile(profile);

        try
        {
            var exercises = await _gemini.GeneratePersonalizedGrammarPractice(profileJson, lang, req?.Level ?? profile.Student.SkillLevel);
            return Ok(new { success = true, source = "gemini", exercises });
        }
        catch (GeminiServiceException ex)
        {
            return Ok(new
            {
                success = true,
                source = "fallback",
                message = "Gemini chưa phản hồi được nên hệ thống đang tạo bài grammar khởi động.",
                providerStatus = (int)ex.StatusCode,
                exercises = BuildFallbackGrammarPractice(lang)
            });
        }
        catch (JsonException)
        {
            return Ok(new
            {
                success = true,
                source = "fallback",
                message = "Gemini trả về JSON không hợp lệ nên hệ thống đang tạo bài grammar khởi động.",
                exercises = BuildFallbackGrammarPractice(lang)
            });
        }
    }

    // POST /api/ai/admin/draft-listening
    [HttpPost("admin/draft-listening")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DraftListeningTest([FromBody] DraftListeningRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RawScript))
            return BadRequest(new { message = "Vui lòng nhập kịch bản." });

        try
        {
            var part = req.Part ?? 3;
            var aiDraft = await _gemini.GenerateListeningDraft(req.RawScript, req.Level, part);
            var ssml = aiDraft.GetProperty("ssml").GetString() ?? "";
            if (string.IsNullOrWhiteSpace(ssml))
                return BadRequest(new { message = "Gemini không trả về SSML hợp lệ." });

            var fileName = $"listening_p{part}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var audioResult = await _azureSpeech.SynthesizeSpeech(ssml, fileName);
            if (!audioResult.Success)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    success = false,
                    service = "azure-speech",
                    message = audioResult.Message
                });

            return Ok(new
            {
                success = true,
                data = new
                {
                    part,
                    audioUrl = $"/{audioResult.AudioPath}",
                    ssml,
                    transcript = aiDraft.TryGetProperty("transcript", out var t) ? t.GetString() : null,
                    questions = aiDraft.TryGetProperty("questions", out var q) ? q : default,
                    imageUrl = aiDraft.TryGetProperty("imageUrl", out var img) ? img.GetString() : null
                }
            });
        }
        catch (GeminiServiceException ex)
        {
            return StatusCode(ToClientStatus(ex.StatusCode), new { success = false, message = "Không tạo được đề Listening từ Gemini.", providerStatus = (int)ex.StatusCode });
        }
        catch (JsonException)
        {
            return BadRequest(new { success = false, message = "Gemini trả về JSON không hợp lệ. Hãy thử tạo lại." });
        }
        catch (Exception ex)
        {
            HttpContext.RequestServices
                .GetRequiredService<ILogger<AIController>>()
                .LogError(ex, "Unexpected error while drafting TOEIC listening audio.");

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                service = "audio-generation",
                message = "Máy chủ gặp lỗi khi tạo audio. Kiểm tra cấu hình Azure Speech và thử lại."
            });
        }
    }

    // POST /api/ai/admin/save-listening
    [HttpPost("admin/save-listening")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> SaveListeningDraft([FromBody] SaveListeningRequest req)
    {
        if (req.ExamPartId <= 0 || req.Questions == null || req.Questions.Count == 0)
            return BadRequest(new { message = "Thiếu dữ liệu: examPartId hoặc danh sách câu hỏi không hợp lệ." });

        var group = new QuestionGroup
        {
            PartId = req.ExamPartId,
            AudioUrl = req.AudioUrl,
            Passage = req.Transcript,
            Transcript = req.Transcript,
            SsmlScript = req.Ssml,
            Questions = req.Questions.Select(q => new Question
            {
                Type = "MCQ",
                Text = q.Text,
                Options = JsonSerializer.Serialize(q.Options),
                CorrectAnswer = q.CorrectAnswer,
                Explanation = q.Explanation
            }).ToList()
        };

        _db.QuestionGroups.Add(group);
        await _db.SaveChangesAsync();

        return StatusCode(201, new { success = true, message = "Đã lưu bộ câu hỏi Listening vào database thành công.", data = group });
    }

    private async Task<LearningCoachProfile> BuildLearningProfile(int userId, string language)
    {
        var student = await _db.Users.FindAsync(userId);

        var scoreRows = await _db.Scores
            .Where(s => s.UserId == userId)
            .Include(s => s.Lesson)
            .ThenInclude(l => l.Course)
            .OrderByDescending(s => s.CreatedAt)
            .Take(50)
            .ToListAsync();

        var pronunciationRows = await _db.PronunciationRecords
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(12)
            .ToListAsync();

        var chatRows = await _db.ChatMessages
            .Where(m => m.UserId == userId && m.Language == language)
            .OrderByDescending(m => m.CreatedAt)
            .Take(30)
            .ToListAsync();

        var lessonRows = await _db.Lessons
            .Include(l => l.Course)
            .OrderBy(l => l.Course.Title)
            .ThenBy(l => l.Title)
            .Take(80)
            .ToListAsync();

        var recentScores = scoreRows.Select(s =>
        {
            var percentage = s.TotalQuestions > 0
                ? Math.Round((double)s.ScoreValue / s.TotalQuestions * 100, 1)
                : 0;

            return new ScoreSnapshot(
                s.LessonId,
                s.Lesson?.Title ?? "Unknown lesson",
                s.Lesson?.Course?.Title ?? "Unknown course",
                s.ScoreValue,
                s.TotalQuestions,
                percentage,
                s.CreatedAt);
        }).ToList();

        var weakLessons = recentScores
            .Where(s => s.Percentage < 75)
            .Take(8)
            .Select(s => new WeakLessonSnapshot(
                s.LessonId,
                s.LessonTitle,
                s.CourseTitle,
                s.Percentage,
                s.Percentage < 60 ? "high" : "medium"))
            .ToList();

        var pronunciation = pronunciationRows.Select(p => new PronunciationSnapshot(
            p.ReferenceText,
            Math.Round(p.OverallScore, 1),
            Math.Round(p.AccuracyScore, 1),
            Math.Round(p.FluencyScore, 1),
            Math.Round(p.CompletenessScore, 1),
            p.ProsodyScore.HasValue ? Math.Round(p.ProsodyScore.Value, 1) : null,
            p.CreatedAt)).ToList();

        var availableLessons = lessonRows.Select(l => new LessonSuggestionSnapshot(
            l.Id,
            l.Title,
            l.Course?.Title ?? "Course",
            l.Course?.Language ?? language)).ToList();

        var stats = new LearningStatsSnapshot(
            language,
            recentScores.Count,
            recentScores.Count > 0 ? Math.Round(recentScores.Average(s => s.Percentage), 1) : null,
            pronunciation.Count,
            pronunciation.Count > 0 ? Math.Round(pronunciation.Average(p => p.OverallScore), 1) : null,
            chatRows.Count);

        return new LearningCoachProfile(
            new StudentCoachSnapshot(
                student?.Id ?? userId,
                student?.Name ?? "Học viên",
                student?.LanguagePreference,
                student?.SkillLevel,
                student?.LearningGoal),
            stats,
            recentScores.Take(20).ToList(),
            weakLessons,
            pronunciation,
            ExtractLowPronunciationWords(pronunciationRows),
            chatRows.OrderBy(m => m.CreatedAt)
                .Select(m => new ChatSnapshot(m.Role, m.PersonaName, Truncate(m.Text, 240), m.CreatedAt))
                .ToList(),
            availableLessons);
    }

    private static string SerializeCoachProfile(LearningCoachProfile profile) =>
        JsonSerializer.Serialize(profile, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

    private static object BuildProfileSummary(LearningCoachProfile profile) => new
    {
        profile.Stats,
        weakLessons = profile.WeakLessons.Take(5),
        lowPronunciationWords = profile.LowPronunciationWords.Take(8)
    };

    private static JsonElement BuildFallbackLearningPlan(LearningCoachProfile profile, string language)
    {
        var targetLanguage = language == "ZH" ? "tiếng Trung" : "tiếng Anh";
        var averageText = profile.Stats.AverageLessonScore.HasValue
            ? $"{profile.Stats.AverageLessonScore}%"
            : "chưa có đủ điểm bài học";
        var pronunciationText = profile.Stats.AveragePronunciationScore.HasValue
            ? $"{profile.Stats.AveragePronunciationScore}%"
            : "chưa có đủ dữ liệu phát âm";

        var recommended = profile.WeakLessons.Take(3)
            .Select(w => new
            {
                lessonId = (string?)w.LessonId,
                title = w.LessonTitle,
                reason = $"Điểm gần đây {w.Percentage}% nên cần ôn lại."
            } as object)
            .ToList();

        if (recommended.Count == 0)
        {
            var firstLesson = profile.AvailableLessons.FirstOrDefault();
            recommended.Add(new
            {
                lessonId = firstLesson?.LessonId,
                title = firstLesson?.Title ?? $"Grammar nền tảng {targetLanguage}",
                reason = "Chưa có đủ dữ liệu điểm yếu, nên bắt đầu bằng bài nền tảng và đo lại sau."
            });
        }

        var pronunciationFocus = profile.LowPronunciationWords.Take(4)
            .Select(w => new
            {
                wordOrSound = w.Word,
                tip = $"Đọc chậm lại và kiểm tra âm của từ này. Điểm gần đây: {w.AccuracyScore}%."
            } as object)
            .ToList();

        if (pronunciationFocus.Count == 0)
        {
            pronunciationFocus.Add(new
            {
                wordOrSound = language == "ZH" ? "thanh điệu" : "word stress",
                tip = "Ghi âm lại một câu ngắn mỗi ngày và so sánh điểm accuracy/fluency."
            });
        }

        var plan = new
        {
            summary = $"Hệ thống đã ghi nhận điểm bài học trung bình {averageText} và phát âm trung bình {pronunciationText}.",
            levelAssessment = profile.Stats.CompletedLessons == 0
                ? "Dữ liệu còn ít. Hãy hoàn thành ít nhất 2 bài học và 1 lượt phát âm để AI đánh giá chính xác hơn."
                : "Có thể bắt đầu cá nhân hóa theo bài học yếu và lỗi phát âm gần đây.",
            strengths = profile.Stats.AverageLessonScore >= 80
                ? new[] { "Duy trì điểm bài học tốt", "Có nền tảng để chuyển sang bài luyện theo ngữ cảnh" }
                : new[] { "Đã có dữ liệu học tập để theo dõi", "Có thể cải thiện nhanh nếu luyện theo lỗi cụ thể" },
            weaknesses = new object[]
            {
                new
                {
                    skill = "Grammar",
                    issue = profile.WeakLessons.FirstOrDefault()?.LessonTitle ?? "Cần củng cố cấu trúc câu cơ bản",
                    evidence = profile.WeakLessons.Count > 0 ? "Có bài học dưới 75%." : "Chưa có đủ dữ liệu lỗi, dùng lộ trình khởi động.",
                    priority = profile.WeakLessons.Any(w => w.Priority == "high") ? "high" : "medium"
                },
                new
                {
                    skill = "Pronunciation",
                    issue = profile.LowPronunciationWords.FirstOrDefault()?.Word ?? "Cần luyện phát âm đều đặn",
                    evidence = profile.LowPronunciationWords.Count > 0 ? "Có từ dưới 80 điểm accuracy." : "Chưa có đủ dữ liệu từ phát âm.",
                    priority = profile.LowPronunciationWords.Count > 0 ? "medium" : "low"
                }
            },
            recommendedLessons = recommended,
            weeklyPlan = new object[]
            {
                new { day = "Day 1", focus = "Grammar review", tasks = new[] { "Làm bài grammar cá nhân hóa", "Ghi lại 3 lỗi sai thường gặp" }, durationMinutes = 25 },
                new { day = "Day 2", focus = "AI conversation", tasks = new[] { "Chat với Grammar Coach 10 phút", "Yêu cầu AI sửa từng câu" }, durationMinutes = 20 },
                new { day = "Day 3", focus = "Pronunciation", tasks = new[] { "Ghi âm một đoạn ngắn", "Luyện lại các từ có điểm thấp" }, durationMinutes = 20 },
                new { day = "Day 4", focus = "Lesson practice", tasks = new[] { "Ôn bài được đề xuất", "Làm lại bài kiểm tra" }, durationMinutes = 30 },
                new { day = "Day 5", focus = "Mixed review", tasks = new[] { "Tạo 5 câu mới với chủ điểm grammar", "Chat với Speaking Partner" }, durationMinutes = 25 }
            },
            grammarFocus = new object[]
            {
                new
                {
                    topic = language == "ZH" ? "Trật tự từ cơ bản" : "Simple sentence structure",
                    explanation = "Ưu tiên câu ngắn, đúng chủ ngữ - động từ - bổ ngữ trước khi mở rộng.",
                    microExercise = language == "ZH" ? "Sắp xếp: 我 / 学习 / 中文" : "Fill in: She ____ English every day.",
                    answer = language == "ZH" ? "我学习中文。" : "studies"
                },
                new
                {
                    topic = language == "ZH" ? "Câu hỏi cơ bản" : "Question forms",
                    explanation = "Luyện câu hỏi ngắn để cải thiện phản xạ hội thoại.",
                    microExercise = language == "ZH" ? "Dịch: What do you like?" : "Make a question: You like coffee.",
                    answer = language == "ZH" ? "你喜欢什么？" : "Do you like coffee?"
                }
            },
            pronunciationFocus,
            nextAction = "Bấm tạo bài grammar cá nhân hóa, làm 5 câu đầu tiên rồi quay lại AI chat để sửa lỗi."
        };

        return JsonSerializer.SerializeToElement(plan);
    }

    private static List<JsonElement> BuildFallbackGrammarPractice(string language)
    {
        var exercises = language == "ZH"
            ? new object[]
            {
                new { type = "multiple-choice", question = "Chọn câu đúng.", options = new[] { "我学习中文。", "我中文学习。", "学习我中文。", "中文我学习。" }, answer = "我学习中文。", explanation = "Trật tự cơ bản là chủ ngữ + động từ + tân ngữ." },
                new { type = "fill-in-the-blank", question = "你____什么？", options = Array.Empty<string>(), answer = "喜欢", explanation = "喜欢 dùng để hỏi hoặc nói về điều mình thích." },
                new { type = "multiple-choice", question = "Câu nào có nghĩa là 'I am very busy today'?", options = new[] { "我今天很忙。", "我很今天忙。", "今天我忙很。", "忙我今天很。" }, answer = "我今天很忙。", explanation = "Thời gian thường đặt trước hoặc sau chủ ngữ." },
                new { type = "fill-in-the-blank", question = "我____中国菜。", options = Array.Empty<string>(), answer = "喜欢", explanation = "喜欢 + danh từ để nói thích một thứ gì đó." },
                new { type = "multiple-choice", question = "Chọn câu hỏi tự nhiên.", options = new[] { "你好吗？", "你很吗好？", "好吗你？", "很你好吗？" }, answer = "你好吗？", explanation = "Câu hỏi chào hỏi cơ bản dùng 你好吗？" }
            }
            : new object[]
            {
                new { type = "multiple-choice", question = "Choose the correct sentence.", options = new[] { "She studies English every day.", "She study English every day.", "She studying English every day.", "She is study English every day." }, answer = "She studies English every day.", explanation = "Với chủ ngữ she/he/it ở hiện tại đơn, động từ thêm -s." },
                new { type = "fill-in-the-blank", question = "I ____ coffee in the morning.", options = Array.Empty<string>(), answer = "drink", explanation = "Với chủ ngữ I ở hiện tại đơn, dùng động từ nguyên mẫu." },
                new { type = "multiple-choice", question = "Choose the correct question.", options = new[] { "Do you like English?", "You like English?", "Does you like English?", "Are you like English?" }, answer = "Do you like English?", explanation = "Câu hỏi hiện tại đơn với you dùng Do + subject + verb." },
                new { type = "fill-in-the-blank", question = "He ____ to school by bus.", options = Array.Empty<string>(), answer = "goes", explanation = "He ở hiện tại đơn cần động từ thêm -es với go." },
                new { type = "multiple-choice", question = "Choose the natural sentence.", options = new[] { "I am interested in music.", "I interested music.", "I am interest in music.", "I interested in music." }, answer = "I am interested in music.", explanation = "Cấu trúc đúng là be interested in + noun." }
            };

        return exercises.Select(item => JsonSerializer.SerializeToElement(item)).ToList();
    }

    private static List<LowPronunciationWord> ExtractLowPronunciationWords(IEnumerable<PronunciationRecord> records)
    {
        var words = new List<LowPronunciationWord>();

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.WordDetails)) continue;

            try
            {
                using var doc = JsonDocument.Parse(record.WordDetails);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) continue;

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var word = GetJsonString(item, "word");
                    var score = GetJsonDouble(item, "accuracyScore");
                    if (string.IsNullOrWhiteSpace(word) || !score.HasValue || score.Value >= 80) continue;

                    words.Add(new LowPronunciationWord(
                        word,
                        Math.Round(score.Value, 1),
                        GetJsonString(item, "errorType"),
                        record.CreatedAt));
                }
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return words
            .GroupBy(w => w.Word, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(w => w.AccuracyScore).First())
            .OrderBy(w => w.AccuracyScore)
            .Take(12)
            .ToList();
    }

    private static string? GetJsonString(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                return property.Value.GetString();
        }

        return null;
    }

    private static double? GetJsonDouble(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)) continue;

            if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDouble(out var value))
                return value;
        }

        return null;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";

    private static string ReplacePromptVariables(string prompt, User? student) =>
        prompt
            .Replace("{{student_name}}", student?.Name ?? "Học viên")
            .Replace("{{skillLevel}}", student?.SkillLevel ?? "Beginner")
            .Replace("{{learningGoal}}", student?.LearningGoal ?? "General");

    private static string NormalizeAiLanguage(string? language) =>
        (language ?? "EN").Trim().ToUpperInvariant() switch
        {
            "ZH" or "CN" or "ZH-CN" or "CHINESE" => "ZH",
            _ => "EN"
        };

    private static int ToClientStatus(System.Net.HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code is >= 400 and < 600 ? code : StatusCodes.Status502BadGateway;
    }

    private static string GetGeminiErrorMessage(System.Net.HttpStatusCode statusCode) =>
        statusCode switch
        {
            System.Net.HttpStatusCode.BadRequest =>
                "Gemini từ chối yêu cầu. Hãy kiểm tra Gemini__ApiKey và tên model trong Azure App Service.",
            System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden =>
                "Gemini API key không hợp lệ hoặc chưa được cấp quyền.",
            System.Net.HttpStatusCode.NotFound =>
                "Model Gemini đang cấu hình không tồn tại hoặc không còn được hỗ trợ.",
            System.Net.HttpStatusCode.TooManyRequests =>
                "Gemini đã hết hạn mức hoặc đang giới hạn số lượt gọi. Vui lòng thử lại sau.",
            System.Net.HttpStatusCode.ServiceUnavailable =>
                "Gemini chưa được cấu hình hoặc đang tạm thời không sẵn sàng.",
            _ => "Gemini chưa phản hồi được. Vui lòng thử lại sau."
        };

    private sealed record LearningCoachProfile(
        StudentCoachSnapshot Student,
        LearningStatsSnapshot Stats,
        List<ScoreSnapshot> RecentScores,
        List<WeakLessonSnapshot> WeakLessons,
        List<PronunciationSnapshot> RecentPronunciation,
        List<LowPronunciationWord> LowPronunciationWords,
        List<ChatSnapshot> RecentChats,
        List<LessonSuggestionSnapshot> AvailableLessons);

    private sealed record StudentCoachSnapshot(
        int Id,
        string Name,
        string? LanguagePreference,
        string? SkillLevel,
        string? LearningGoal);

    private sealed record LearningStatsSnapshot(
        string Language,
        int CompletedLessons,
        double? AverageLessonScore,
        int PronunciationAttempts,
        double? AveragePronunciationScore,
        int RecentChatMessages);

    private sealed record ScoreSnapshot(
        string LessonId,
        string LessonTitle,
        string CourseTitle,
        int Score,
        int TotalQuestions,
        double Percentage,
        DateTime CreatedAt);

    private sealed record WeakLessonSnapshot(
        string LessonId,
        string LessonTitle,
        string CourseTitle,
        double Percentage,
        string Priority);

    private sealed record PronunciationSnapshot(
        string ReferenceText,
        double OverallScore,
        double AccuracyScore,
        double FluencyScore,
        double CompletenessScore,
        double? ProsodyScore,
        DateTime CreatedAt);

    private sealed record LowPronunciationWord(
        string Word,
        double AccuracyScore,
        string? ErrorType,
        DateTime CreatedAt);

    private sealed record ChatSnapshot(
        string Role,
        string PersonaName,
        string Text,
        DateTime CreatedAt);

    private sealed record LessonSuggestionSnapshot(
        string LessonId,
        string Title,
        string CourseTitle,
        string Language);
}
