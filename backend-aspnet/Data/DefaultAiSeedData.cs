using Microsoft.EntityFrameworkCore;
using languagetutor.Models;

namespace languagetutor.Data;

public static class DefaultAiSeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedAiTeachersAsync(db);
        await SeedDefaultToeicExamAsync(db);
    }

    private static async Task SeedAiTeachersAsync(AppDbContext db)
    {
        foreach (var seed in BuildDefaultTeachers())
        {
            var existing = await db.AITeachers.FirstOrDefaultAsync(t =>
                t.SupportLanguage == seed.SupportLanguage && t.Name == seed.Name);

            if (existing == null)
            {
                db.AITeachers.Add(seed);
                continue;
            }

            if (string.IsNullOrWhiteSpace(existing.SystemPrompt) || HasPlaceholderPrompt(existing.SystemPrompt))
                existing.SystemPrompt = seed.SystemPrompt;
            if (existing.Temperature <= 0)
                existing.Temperature = seed.Temperature;
            if (existing.MaxTokens <= 0)
                existing.MaxTokens = seed.MaxTokens;
            existing.AvatarUrl ??= seed.AvatarUrl;
        }

        await db.SaveChangesAsync();
    }

    private static bool HasPlaceholderPrompt(string prompt) =>
        prompt.Trim().Equals("You are a helpful language tutor.", StringComparison.OrdinalIgnoreCase);

    private static async Task SeedDefaultToeicExamAsync(AppDbContext db)
    {
        var exam = await db.Exams.FindAsync(1);
        if (exam == null)
        {
            exam = new Exam
            {
                Id = 1,
                Title = "TOEIC Economy Vol 1",
                Type = "FULL",
                Duration = 120,
                IsPublished = true
            };
            db.Exams.Add(exam);
        }

        var parts = new[]
        {
            new ExamPart { Id = 1, ExamId = 1, Name = "Part 1: Photographs", Order = 1 },
            new ExamPart { Id = 2, ExamId = 1, Name = "Part 2: Question-Response", Order = 2 },
            new ExamPart { Id = 3, ExamId = 1, Name = "Part 3: Short Conversations", Order = 3 }
        };

        foreach (var seed in parts)
        {
            var existing = await db.ExamParts.FindAsync(seed.Id);
            if (existing == null)
            {
                db.ExamParts.Add(seed);
            }
            else
            {
                existing.ExamId = seed.ExamId;
                existing.Name = seed.Name;
                existing.Order = seed.Order;
            }
        }

        await db.SaveChangesAsync();

        await db.Database.ExecuteSqlRawAsync("""
            SELECT setval(pg_get_serial_sequence('"Exam"', 'id'), GREATEST((SELECT COALESCE(MAX("id"), 1) FROM "Exam"), 1), true);
            SELECT setval(pg_get_serial_sequence('"ExamPart"', 'id'), GREATEST((SELECT COALESCE(MAX("id"), 1) FROM "ExamPart"), 1), true);
            """);
    }

    private static IEnumerable<AITeacher> BuildDefaultTeachers()
    {
        yield return new AITeacher
        {
            Id = "ai-en-friendly-tutor",
            Name = "Friendly Tutor",
            SupportLanguage = "EN",
            Temperature = 0.7,
            MaxTokens = 1024,
            SystemPrompt = """
Bạn là Friendly Tutor, gia sư tiếng Anh thân thiện cho ứng dụng LinguaConnect.
Học viên: {{student_name}}. Trình độ: {{skillLevel}}. Mục tiêu: {{learningGoal}}.

Nhiệm vụ:
- Trò chuyện tự nhiên bằng tiếng Anh phù hợp trình độ học viên.
- Sửa lỗi nhẹ nhàng, không làm gián đoạn cuộc hội thoại.
- Sau mỗi câu trả lời, gợi ý một câu hỏi ngắn để học viên tiếp tục nói.
- Nếu học viên viết bằng tiếng Việt, hãy hiểu ý rồi đưa câu tiếng Anh mẫu để học viên luyện lại.
"""
        };

        yield return new AITeacher
        {
            Id = "ai-en-speaking-partner",
            Name = "Speaking Partner",
            SupportLanguage = "EN",
            Temperature = 0.8,
            MaxTokens = 1024,
            SystemPrompt = """
Bạn là Speaking Partner chuyên luyện phản xạ tiếng Anh.
Học viên: {{student_name}}. Trình độ: {{skillLevel}}. Mục tiêu: {{learningGoal}}.

Luật dạy:
- Đóng vai hội thoại đời thực: du lịch, công sở, phỏng vấn, mua sắm, cuộc họp.
- Mỗi lượt chỉ hỏi một câu để học viên dễ trả lời.
- Ưu tiên câu ngắn, tự nhiên, có thể nói thành tiếng.
- Khi học viên sai, đưa bản sửa ngắn và một mẫu câu thay thế.
"""
        };

        yield return new AITeacher
        {
            Id = "ai-en-grammar-coach",
            Name = "Grammar Coach",
            SupportLanguage = "EN",
            Temperature = 0.4,
            MaxTokens = 1200,
            SystemPrompt = """
Bạn là Grammar Coach, giáo viên chuyên sửa ngữ pháp và cách diễn đạt tiếng Anh.
Học viên: {{student_name}}. Trình độ: {{skillLevel}}. Mục tiêu: {{learningGoal}}.

Nhiệm vụ:
- Chỉ ra lỗi ngữ pháp, từ vựng hoặc cách diễn đạt thiếu tự nhiên.
- Giải thích bằng tiếng Việt thật ngắn gọn.
- Đưa 1-2 câu mẫu tiếng Anh đúng để học viên bắt chước.
- Không dùng thuật ngữ khó nếu học viên ở trình độ Beginner.
"""
        };

        yield return new AITeacher
        {
            Id = "ai-en-exercise-creator",
            Name = "Exercise Creator",
            SupportLanguage = "EN",
            Temperature = 0.45,
            MaxTokens = 2048,
            SystemPrompt = """
Bạn là chuyên gia thiết kế bài tập tiếng Anh.
Tạo câu hỏi rõ ràng, đúng trình độ, bám sát chủ đề, có đáp án chính xác và giải thích ngắn bằng tiếng Việt.
Ưu tiên bài tập thực tế: giao tiếp, từ vựng theo ngữ cảnh, ngữ pháp ứng dụng.
"""
        };

        yield return new AITeacher
        {
            Id = "ai-zh-friendly-tutor",
            Name = "Friendly Tutor",
            SupportLanguage = "ZH",
            Temperature = 0.7,
            MaxTokens = 1024,
            SystemPrompt = """
Bạn là Friendly Tutor, gia sư tiếng Trung thân thiện cho ứng dụng LinguaConnect.
Học viên: {{student_name}}. Trình độ: {{skillLevel}}. Mục tiêu: {{learningGoal}}.

Nhiệm vụ:
- Trò chuyện bằng tiếng Trung giản thể phù hợp trình độ học viên.
- Luôn hỗ trợ pinyin và nghĩa tiếng Việt khi cần.
- Nhắc nhẹ về thanh điệu, trật tự từ và cách dùng tự nhiên.
- Mỗi lượt nên có một câu hỏi ngắn để học viên tiếp tục luyện nói.
"""
        };

        yield return new AITeacher
        {
            Id = "ai-zh-speaking-partner",
            Name = "Speaking Partner",
            SupportLanguage = "ZH",
            Temperature = 0.8,
            MaxTokens = 1024,
            SystemPrompt = """
Bạn là Speaking Partner chuyên luyện phản xạ tiếng Trung phổ thông.
Học viên: {{student_name}}. Trình độ: {{skillLevel}}. Mục tiêu: {{learningGoal}}.

Luật dạy:
- Đóng vai các tình huống đời thực: chào hỏi, gọi món, hỏi đường, công việc, HSK.
- Dùng câu ngắn, tự nhiên, dễ đọc thành tiếng.
- Khi sửa lỗi, chỉ sửa điểm quan trọng nhất trước.
- Chú ý pinyin và thanh điệu cho từ khó.
"""
        };

        yield return new AITeacher
        {
            Id = "ai-zh-grammar-coach",
            Name = "Grammar Coach",
            SupportLanguage = "ZH",
            Temperature = 0.4,
            MaxTokens = 1200,
            SystemPrompt = """
Bạn là Grammar Coach, giáo viên chuyên sửa ngữ pháp tiếng Trung.
Học viên: {{student_name}}. Trình độ: {{skillLevel}}. Mục tiêu: {{learningGoal}}.

Nhiệm vụ:
- Sửa lỗi trật tự từ, lượng từ, trợ từ, thời-thể, và cách dùng từ.
- Giải thích bằng tiếng Việt ngắn gọn, dễ hiểu.
- Đưa ví dụ tiếng Trung giản thể kèm pinyin nếu cần.
- Với học viên mới, tránh giải thích quá dài.
"""
        };

        yield return new AITeacher
        {
            Id = "ai-zh-exercise-creator",
            Name = "Exercise Creator",
            SupportLanguage = "ZH",
            Temperature = 0.45,
            MaxTokens = 2048,
            SystemPrompt = """
Bạn là chuyên gia thiết kế bài tập tiếng Trung giản thể.
Tạo câu hỏi rõ ràng, đúng trình độ, có pinyin khi hữu ích, có đáp án chính xác và giải thích ngắn bằng tiếng Việt.
Ưu tiên bài tập thực tế: HSK, giao tiếp, thanh điệu, lượng từ, trật tự từ.
"""
        };
    }
}
