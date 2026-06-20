using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using languagetutor.Data;
using languagetutor.DTOs;
using languagetutor.Models;

namespace languagetutor.Controllers;

[ApiController]
[Route("api/courses")]
public class CourseController : ControllerBase
{
    private readonly AppDbContext _db;

    public CourseController(AppDbContext db) => _db = db;

    private int GetUserId() =>
        int.Parse(User.FindFirst("userId")?.Value ?? User.FindFirst("id")?.Value ?? "0");

    // GET /api/courses  - Public
    [HttpGet]
    public async Task<IActionResult> GetAllCourses()
    {
        var courses = await _db.Courses
            .Select(c => new { c.Id, c.Title, c.Description, c.Language })
            .ToListAsync();
        return Ok(new { success = true, data = courses });
    }

    // GET /api/courses/:id
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetCourseById(string id)
    {
        var course = await _db.Courses
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound(new { success = false, message = "Course not found." });

        return Ok(new { success = true, data = new {
            course.Id, course.Title, course.Description, course.Language,
            lessons = course.Lessons.Select(l => new { l.Id, l.Title }) } });
    }

    // GET /api/courses/lessons/:lessonId
    [HttpGet("lessons/{lessonId}")]
    [Authorize]
    public async Task<IActionResult> GetLessonById(string lessonId)
    {
        var lesson = await _db.Lessons.FindAsync(lessonId);
        if (lesson == null) return NotFound(new { success = false, message = "Lesson not found." });

        object content;
        try
        {
            content = JsonSerializer.Deserialize<JsonElement>(lesson.Content);
        }
        catch (JsonException)
        {
            content = lesson.Content;
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                lesson.Id,
                lesson.Title,
                lesson.CourseId,
                content
            }
        });
    }

    // POST /api/courses/lessons/:lessonId/score
    [HttpPost("lessons/{lessonId}/score")]
    [Authorize]
    public async Task<IActionResult> SubmitLessonScore(string lessonId, [FromBody] SubmitScoreRequest req)
    {
        if (req.Score < 0 || req.TotalQuestions <= 0)
            return BadRequest(new { success = false, message = "Thieu du lieu diem so hoac tong so cau hoi." });

        var newScore = new Score
        {
            UserId = GetUserId(),
            LessonId = lessonId,
            ScoreValue = req.Score,
            TotalQuestions = req.TotalQuestions,
            CompletionTime = req.CompletionTime ?? 0
        };
        _db.Scores.Add(newScore);
        await _db.SaveChangesAsync();
        return StatusCode(201, new { success = true, message = "Đã lưu kết quả học tập thành công!", data = newScore });
    }

    // POST /api/courses  - Admin only
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title) ||
            string.IsNullOrWhiteSpace(req.Description) ||
            string.IsNullOrWhiteSpace(req.Language))
        {
            return BadRequest(new { success = false, message = "Vui long cung cap du Tieu de, Mo ta va Ngon ngu." });
        }

        var course = new Course { Title = req.Title, Description = req.Description, Language = req.Language };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();
        return StatusCode(201, new { success = true, message = "Tạo khóa học thành công", data = course });
    }

    // POST /api/courses/lessons/ai-generate  - Admin only
    [HttpPost("lessons/ai-generate")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateLessonWithExercises([FromBody] CreateLessonRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CourseId) ||
            string.IsNullOrWhiteSpace(req.Title) ||
            req.Content == null)
        {
            return BadRequest(new { success = false, message = "Thieu thong tin: courseId, title hoac content (bai tap)." });
        }

        var content = req.Content is JsonElement je ? je.GetRawText() : JsonSerializer.Serialize(req.Content);
        var lesson = new Lesson { Title = req.Title, CourseId = req.CourseId, Content = content };
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();
        return StatusCode(201, new { success = true, message = "Đã lưu bài tập AI vào khóa học thành công!", data = lesson });
    }

    // GET /api/courses/admin/analytics  - Admin only
    [HttpGet("admin/analytics")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var totalCourses = await _db.Courses.CountAsync();
        var totalLessons = await _db.Lessons.CountAsync();
        var totalUsers = await _db.Users.CountAsync(u => u.Role == "STUDENT");

        var recentScores = await _db.Scores
            .OrderByDescending(s => s.CreatedAt).Take(10)
            .Include(s => s.User).Include(s => s.Lesson).ThenInclude(l => l.Course)
            .Select(s => new {
                s.Id, s.ScoreValue, s.TotalQuestions, s.CreatedAt,
                user = new { s.User.Name, s.User.Email },
                lesson = new { s.Lesson.Title, course = new { s.Lesson.Course.Title } } })
            .ToListAsync();

        var allScores = await _db.Scores.Select(s => new { s.ScoreValue, s.TotalQuestions }).ToListAsync();
        double avgScore = allScores.Count > 0
            ? Math.Round(allScores.Average(s => (double)s.ScoreValue / s.TotalQuestions) * 100)
            : 0;

        return Ok(new { success = true, data = new { totalCourses, totalLessons, totalUsers, avgScore, recentScores } });
    }

    // GET /api/courses/admin/growth-stats  - Admin only
    [HttpGet("admin/growth-stats")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetGrowthData()
    {
        var students = await _db.Users.Where(u => u.Role == "STUDENT").Select(u => u.CreatedAt).ToListAsync();
        var stats = Enumerable.Range(0, 7)
            .Select(i => DateTime.UtcNow.Date.AddDays(-i))
            .ToDictionary(d => d.ToString("yyyy-MM-dd"), _ => 0);

        foreach (var d in students)
        {
            var key = d.ToString("yyyy-MM-dd");
            if (stats.ContainsKey(key)) stats[key]++;
        }

        var result = stats.OrderBy(k => k.Key)
            .Select(k => new { date = k.Key, count = k.Value });
        return Ok(new { success = true, data = result });
    }
}
