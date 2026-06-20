using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace languagetutor.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    [Required] public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "STUDENT";
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? LanguagePreference { get; set; }
    public string? SkillLevel { get; set; }
    public string? LearningGoal { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Score> Scores { get; set; } = new List<Score>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    public ICollection<PronunciationRecord> PronunciationRecords { get; set; } = new List<PronunciationRecord>();
    public ICollection<TestAttempt> TestAttempts { get; set; } = new List<TestAttempt>();
}

public class Course
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string Description { get; set; } = string.Empty;
    [Required] public string Language { get; set; } = string.Empty;

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

public class Lesson
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Title { get; set; } = string.Empty;
    [Column(TypeName = "jsonb")] public string Content { get; set; } = "[]";

    public string CourseId { get; set; } = string.Empty;
    public Course Course { get; set; } = null!;
    public ICollection<Score> Scores { get; set; } = new List<Score>();
}

public class Score
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    public int ScoreValue { get; set; }
    public int TotalQuestions { get; set; }
    public int? CompletionTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string LessonId { get; set; } = string.Empty;
    public Lesson Lesson { get; set; } = null!;
}

public class AITeacher
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string SystemPrompt { get; set; } = string.Empty;
    [Required] public string SupportLanguage { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1024;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ChatMessage
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Role { get; set; } = string.Empty; // "user" | "model"
    [Required] public string Text { get; set; } = string.Empty;
    [Required] public string Language { get; set; } = string.Empty;
    [Required] public string PersonaName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class PronunciationRecord
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ReferenceText { get; set; } = string.Empty;
    public float OverallScore { get; set; }
    public float AccuracyScore { get; set; }
    public float FluencyScore { get; set; }
    public float CompletenessScore { get; set; }
    public float? ProsodyScore { get; set; }
    [Column(TypeName = "jsonb")] public string WordDetails { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class Exam
{
    [Key] public int Id { get; set; }
    [Required] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public string Type { get; set; } = string.Empty;
    public int Duration { get; set; }
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ExamPart> Parts { get; set; } = new List<ExamPart>();
    public ICollection<TestAttempt> Attempts { get; set; } = new List<TestAttempt>();
}

public class ExamPart
{
    [Key] public int Id { get; set; }
    public int ExamId { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string? Instruction { get; set; }
    public int Order { get; set; }

    public Exam Exam { get; set; } = null!;
    public ICollection<QuestionGroup> Groups { get; set; } = new List<QuestionGroup>();
}

public class QuestionGroup
{
    [Key] public int Id { get; set; }
    public int PartId { get; set; }
    public string? Passage { get; set; }
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? Transcript { get; set; }
    public string? SsmlScript { get; set; }

    public ExamPart Part { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

public class Question
{
    [Key] public int Id { get; set; }
    public int GroupId { get; set; }
    [Required] public string Type { get; set; } = string.Empty; // "MCQ" | "SPEAKING" | "WRITING"
    public string? Text { get; set; }
    [Column(TypeName = "jsonb")] public string? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public string? Explanation { get; set; }
    public string? EvalCriteria { get; set; }
    public string? ReferenceText { get; set; }

    public QuestionGroup Group { get; set; } = null!;
    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}

public class TestAttempt
{
    [Key] public int Id { get; set; }
    public int UserId { get; set; }
    public int ExamId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "IN_PROGRESS";
    public int? TotalScore { get; set; }
    public int? ListeningScore { get; set; }
    public int? ReadingScore { get; set; }
    public int? SpeakingScore { get; set; }
    public int? WritingScore { get; set; }

    public Exam Exam { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<UserAnswer> Answers { get; set; } = new List<UserAnswer>();
}

public class UserAnswer
{
    [Key] public int Id { get; set; }
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public string? SelectedOption { get; set; }
    public string? AudioUrl { get; set; }
    public string? TextAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public float? ScoreValue { get; set; }
    [Column(TypeName = "jsonb")] public string? AiFeedback { get; set; }

    public TestAttempt Attempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
