using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using languagetutor.Data;
using languagetutor.DTOs;
using languagetutor.Models;
using languagetutor.Services;

namespace languagetutor.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GeminiService _gemini;

    public AdminController(AppDbContext db, GeminiService gemini)
    {
        _db = db; _gemini = gemini;
    }

    // ==================== QUẢN LÝ USER ====================

    // POST /api/admin/users
    [HttpPost("users")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { message = "Email đã tồn tại." });

        var user = new User
        {
            Email = req.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Name = req.Name,
            Role = string.IsNullOrWhiteSpace(req.Role) ? "STUDENT" : req.Role.Trim().ToUpperInvariant(),
            PhoneNumber = req.PhoneNumber,
            Address = req.Address,
            DateOfBirth = req.DateOfBirth,
            Gender = req.Gender,
            LanguagePreference = req.LanguagePreference,
            SkillLevel = req.SkillLevel,
            LearningGoal = req.LearningGoal
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return StatusCode(201, new { message = "Tạo tài khoản thành công.", user });
    }

    // GET /api/admin/users/search?q=...
    [HttpGet("users/search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string? q)
    {
        if (string.IsNullOrEmpty(q)) return BadRequest(new { message = "Vui lòng nhập từ khóa." });
        var users = await _db.Users
            .Where(u => u.Email.ToLower().Contains(q.ToLower()) || u.Name.ToLower().Contains(q.ToLower()))
            .Select(u => new { u.Id, u.Email, u.Name, u.Role, u.PhoneNumber, u.Address, u.DateOfBirth, u.Gender, u.LanguagePreference, u.SkillLevel, u.LearningGoal, u.CreatedAt })
            .ToListAsync();
        return Ok(users);
    }

    // PUT /api/admin/users/:id/role
    [HttpPut("users/{id:int}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.Role = req.Role;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Đã cập nhật quyền thành công.", user });
    }

    // PUT /api/admin/users/:id
    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (req.Name != null) user.Name = req.Name;
        if (req.Email != null) user.Email = req.Email;
        if (req.PhoneNumber != null) user.PhoneNumber = req.PhoneNumber;
        if (req.Address != null) user.Address = req.Address;
        if (req.DateOfBirth != null) user.DateOfBirth = req.DateOfBirth;
        if (req.Gender != null) user.Gender = req.Gender;
        if (req.Role != null) user.Role = req.Role.Trim().ToUpperInvariant();
        if (req.LanguagePreference != null) user.LanguagePreference = req.LanguagePreference;
        if (req.SkillLevel != null) user.SkillLevel = req.SkillLevel;
        if (req.LearningGoal != null) user.LearningGoal = req.LearningGoal;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = user });
    }

    // DELETE /api/admin/users/:id
    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _db.Scores.Where(s => s.UserId == id).ExecuteDeleteAsync();
        await _db.ChatMessages.Where(c => c.UserId == id).ExecuteDeleteAsync();
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Đã xóa người dùng thành công." });
    }

    // ==================== QUẢN LÝ BÀI HỌC ====================

    // GET /api/admin/lessons/search?q=...
    [HttpGet("lessons/search")]
    public async Task<IActionResult> SearchLessons([FromQuery] string? q)
    {
        if (string.IsNullOrEmpty(q)) return BadRequest(new { message = "Vui lòng nhập từ khóa tìm kiếm." });
        var lessons = await _db.Lessons
            .Where(l => l.Title.ToLower().Contains(q.ToLower()))
            .Include(l => l.Course)
            .ToListAsync();
        return Ok(lessons);
    }

    // POST /api/admin/lessons/generate-exercises
    [HttpPost("lessons/generate-exercises")]
    public async Task<IActionResult> GenerateAndSaveExercises([FromBody] GenerateExercisesAdminRequest req)
    {
        var lesson = await _db.Lessons.FindAsync(req.LessonId);
        if (lesson == null) return NotFound(new { message = "Không tìm thấy bài học." });

        var context = $"Ngôn ngữ: {req.Language}, Trình độ: {req.Level}, Chủ đề: {req.Topic}";
        var exercises = await _gemini.GetAIResponse(context, "Hãy tạo 5 câu hỏi trắc nghiệm cho bài học này.");
        lesson.Content = exercises;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Bài tập đã được tạo và lưu thành công!", lesson });
    }

    // ==================== ANALYTICS & AI CONFIG ====================

    // GET /api/admin/analytics/dashboard
    [HttpGet("analytics/dashboard")]
    public async Task<IActionResult> GetDashboardAnalytics()
    {
        var totalUsers = await _db.Users.CountAsync(u => u.Role == "STUDENT");
        var totalCourses = await _db.Courses.CountAsync();
        var totalLessons = await _db.Lessons.CountAsync();

        var allScores = await _db.Scores.Select(s => new { s.ScoreValue, s.TotalQuestions }).ToListAsync();
        var avgScore = allScores.Count > 0
            ? (int)Math.Round(allScores.Average(s => (double)s.ScoreValue / s.TotalQuestions * 100))
            : 0;

        var recentScores = await _db.Scores
            .OrderByDescending(s => s.CreatedAt).Take(10)
            .Include(s => s.User).Include(s => s.Lesson).ThenInclude(l => l.Course)
            .Select(s => new {
                s.Id, s.ScoreValue, s.TotalQuestions, s.CreatedAt,
                user = new { s.User.Id, s.User.Name, s.User.Email, s.User.Role, s.User.PhoneNumber, s.User.Address, s.User.DateOfBirth, s.User.Gender, s.User.LanguagePreference, s.User.SkillLevel, s.User.LearningGoal },
                lesson = new { s.Lesson.Title, course = new { s.Lesson.Course.Title } } })
            .ToListAsync();

        var rng = new Random();
        var growthStats = new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" }
            .Select((d, i) => new { date = d, count = new[] { 12, 19, 15, 25, 22, 30, rng.Next(15, 35) }[i] });

        return Ok(new { success = true, data = new { totalUsers, totalCourses, totalLessons, avgScore, recentScores, growthStats } });
    }

    // PUT /api/admin/ai-config/:teacherId
    [HttpPut("ai-config/{teacherId}")]
    public async Task<IActionResult> UpdateAITeacherConfig(string teacherId, [FromBody] UpdateAITeacherRequest req)
    {
        var teacher = await _db.AITeachers.FindAsync(teacherId);
        if (teacher == null) return NotFound();
        ApplyTeacherUpdate(teacher, req);
        teacher.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Cập nhật AI thành công.", teacher });
    }

    // ==================== AI TEACHER (route: /api/admin/ai-teachers) ====================

    // GET /api/admin/ai-teachers
    [HttpGet("ai-teachers")]
    public async Task<IActionResult> GetAllTeachers()
    {
        var teachers = await _db.AITeachers.OrderBy(t => t.CreatedAt).ToListAsync();
        return Ok(new { success = true, data = teachers });
    }

    // POST /api/admin/ai-teachers
    [HttpPost("ai-teachers")]
    public async Task<IActionResult> CreateTeacher([FromBody] CreateAITeacherRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) ||
            string.IsNullOrWhiteSpace(req.SystemPrompt) ||
            string.IsNullOrWhiteSpace(req.SupportLanguage))
        {
            return BadRequest(new { message = "Vui lòng nhập tên persona, prompt và ngôn ngữ hỗ trợ." });
        }

        var language = NormalizeAiLanguage(req.SupportLanguage);
        var exists = await _db.AITeachers.AnyAsync(t =>
            t.SupportLanguage == language && t.Name.ToLower() == req.Name.Trim().ToLower());
        if (exists)
            return BadRequest(new { message = "Persona này đã tồn tại trong ngôn ngữ đã chọn." });

        var teacher = new AITeacher
        {
            Name = req.Name.Trim(),
            SystemPrompt = req.SystemPrompt.Trim(),
            SupportLanguage = language,
            AvatarUrl = string.IsNullOrWhiteSpace(req.AvatarUrl) ? null : req.AvatarUrl.Trim(),
            Temperature = ClampTemperature(req.Temperature ?? 0.7),
            MaxTokens = ClampMaxTokens(req.MaxTokens ?? 1024)
        };

        _db.AITeachers.Add(teacher);
        await _db.SaveChangesAsync();
        return StatusCode(201, new { success = true, data = teacher });
    }

    // PUT /api/admin/ai-teachers/:id
    [HttpPut("ai-teachers/{id}")]
    public async Task<IActionResult> UpdateTeacherConfig(string id, [FromBody] UpdateAITeacherRequest req)
    {
        var teacher = await _db.AITeachers.FindAsync(id);
        if (teacher == null) return NotFound();
        ApplyTeacherUpdate(teacher, req);
        teacher.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = teacher });
    }

    // DELETE /api/admin/ai-teachers/:id
    [HttpDelete("ai-teachers/{id}")]
    public async Task<IActionResult> DeleteTeacher(string id)
    {
        var teacher = await _db.AITeachers.FindAsync(id);
        if (teacher == null) return NotFound();

        _db.AITeachers.Remove(teacher);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Đã xóa persona AI." });
    }

    private static void ApplyTeacherUpdate(AITeacher teacher, UpdateAITeacherRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.Name)) teacher.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.SystemPrompt)) teacher.SystemPrompt = req.SystemPrompt.Trim();
        if (!string.IsNullOrWhiteSpace(req.SupportLanguage)) teacher.SupportLanguage = NormalizeAiLanguage(req.SupportLanguage);
        if (!string.IsNullOrWhiteSpace(req.AvatarUrl)) teacher.AvatarUrl = req.AvatarUrl.Trim();
        if (req.Temperature.HasValue) teacher.Temperature = ClampTemperature(req.Temperature.Value);
        if (req.MaxTokens.HasValue) teacher.MaxTokens = ClampMaxTokens(req.MaxTokens.Value);
    }

    private static string NormalizeAiLanguage(string language) =>
        language.Trim().ToUpperInvariant() switch
        {
            "ZH" or "CN" or "CHINESE" => "ZH",
            _ => "EN"
        };

    private static double ClampTemperature(double value) => Math.Clamp(value, 0, 1);

    private static int ClampMaxTokens(int value) => Math.Clamp(value, 64, 4096);
}
