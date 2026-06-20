using Microsoft.EntityFrameworkCore;
using languagetutor.Models;

namespace languagetutor.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Score> Scores => Set<Score>();
    public DbSet<AITeacher> AITeachers => Set<AITeacher>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<PronunciationRecord> PronunciationRecords => Set<PronunciationRecord>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamPart> ExamParts => Set<ExamPart>();
    public DbSet<QuestionGroup> QuestionGroups => Set<QuestionGroup>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<UserAnswer> UserAnswers => Set<UserAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        MapPrismaNames(modelBuilder);
        ConfigureIndexes(modelBuilder);
        ConfigureRelations(modelBuilder);
    }

    private static void MapPrismaNames(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.PhoneNumber).HasColumnName("phoneNumber");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.DateOfBirth).HasColumnName("dateOfBirth").HasColumnType("date");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.LanguagePreference).HasColumnName("languagePreference");
            entity.Property(e => e.SkillLevel).HasColumnName("skillLevel");
            entity.Property(e => e.LearningGoal).HasColumnName("learningGoal");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Course");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Language).HasColumnName("language");
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("Lesson");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Content).HasColumnName("content").HasColumnType("jsonb");
            entity.Property(e => e.CourseId).HasColumnName("courseId");
        });

        modelBuilder.Entity<Score>(entity =>
        {
            entity.ToTable("Score");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ScoreValue).HasColumnName("score");
            entity.Property(e => e.TotalQuestions).HasColumnName("totalQuestions");
            entity.Property(e => e.CompletionTime).HasColumnName("completionTime");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.LessonId).HasColumnName("lessonId");
        });

        modelBuilder.Entity<AITeacher>(entity =>
        {
            entity.ToTable("AITeacher");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.SystemPrompt).HasColumnName("systemPrompt");
            entity.Property(e => e.SupportLanguage).HasColumnName("supportLanguage");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatarUrl");
            entity.Property(e => e.Temperature).HasColumnName("temperature");
            entity.Property(e => e.MaxTokens).HasColumnName("maxTokens");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessage");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Text).HasColumnName("text");
            entity.Property(e => e.Language).HasColumnName("language");
            entity.Property(e => e.PersonaName).HasColumnName("personaName");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UserId).HasColumnName("userId");
        });

        modelBuilder.Entity<PronunciationRecord>(entity =>
        {
            entity.ToTable("PronunciationRecord");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReferenceText).HasColumnName("referenceText");
            entity.Property(e => e.OverallScore).HasColumnName("overallScore");
            entity.Property(e => e.AccuracyScore).HasColumnName("accuracyScore");
            entity.Property(e => e.FluencyScore).HasColumnName("fluencyScore");
            entity.Property(e => e.CompletenessScore).HasColumnName("completenessScore");
            entity.Property(e => e.ProsodyScore).HasColumnName("prosodyScore");
            entity.Property(e => e.WordDetails).HasColumnName("wordDetails").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UserId).HasColumnName("userId");
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.ToTable("Exam");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.IsPublished).HasColumnName("isPublished");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
        });

        modelBuilder.Entity<ExamPart>(entity =>
        {
            entity.ToTable("ExamPart");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExamId).HasColumnName("examId");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Instruction).HasColumnName("instruction");
            entity.Property(e => e.Order).HasColumnName("order");
        });

        modelBuilder.Entity<QuestionGroup>(entity =>
        {
            entity.ToTable("QuestionGroup");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PartId).HasColumnName("partId");
            entity.Property(e => e.Passage).HasColumnName("passage");
            entity.Property(e => e.ImageUrl).HasColumnName("imageUrl");
            entity.Property(e => e.AudioUrl).HasColumnName("audioUrl");
            entity.Property(e => e.Transcript).HasColumnName("transcript");
            entity.Property(e => e.SsmlScript).HasColumnName("ssmlScript");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("Question");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("groupId");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Text).HasColumnName("text");
            entity.Property(e => e.Options).HasColumnName("options").HasColumnType("jsonb");
            entity.Property(e => e.CorrectAnswer).HasColumnName("correctAnswer");
            entity.Property(e => e.Explanation).HasColumnName("explanation");
            entity.Property(e => e.EvalCriteria).HasColumnName("evalCriteria");
            entity.Property(e => e.ReferenceText).HasColumnName("referenceText");
        });

        modelBuilder.Entity<TestAttempt>(entity =>
        {
            entity.ToTable("TestAttempt");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.ExamId).HasColumnName("examId");
            entity.Property(e => e.StartTime).HasColumnName("startTime");
            entity.Property(e => e.EndTime).HasColumnName("endTime");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TotalScore).HasColumnName("totalScore");
            entity.Property(e => e.ListeningScore).HasColumnName("listeningScore");
            entity.Property(e => e.ReadingScore).HasColumnName("readingScore");
            entity.Property(e => e.SpeakingScore).HasColumnName("speakingScore");
            entity.Property(e => e.WritingScore).HasColumnName("writingScore");
        });

        modelBuilder.Entity<UserAnswer>(entity =>
        {
            entity.ToTable("UserAnswer");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AttemptId).HasColumnName("attemptId");
            entity.Property(e => e.QuestionId).HasColumnName("questionId");
            entity.Property(e => e.SelectedOption).HasColumnName("selectedOption");
            entity.Property(e => e.AudioUrl).HasColumnName("audioUrl");
            entity.Property(e => e.TextAnswer).HasColumnName("textAnswer");
            entity.Property(e => e.IsCorrect).HasColumnName("isCorrect");
            entity.Property(e => e.ScoreValue).HasColumnName("score");
            entity.Property(e => e.AiFeedback).HasColumnName("aiFeedback").HasColumnType("jsonb");
        });
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Role);
        modelBuilder.Entity<AITeacher>().HasIndex(a => a.SupportLanguage);
        modelBuilder.Entity<Score>().HasIndex(s => s.UserId);
        modelBuilder.Entity<Score>().HasIndex(s => s.LessonId);
        modelBuilder.Entity<ChatMessage>().HasIndex(c => new { c.UserId, c.Language, c.PersonaName });
        modelBuilder.Entity<PronunciationRecord>().HasIndex(p => p.UserId);
    }

    private static void ConfigureRelations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Course).WithMany(c => c.Lessons)
            .HasForeignKey(l => l.CourseId);
        modelBuilder.Entity<Score>()
            .HasOne(s => s.User).WithMany(u => u.Scores)
            .HasForeignKey(s => s.UserId);
        modelBuilder.Entity<Score>()
            .HasOne(s => s.Lesson).WithMany(l => l.Scores)
            .HasForeignKey(s => s.LessonId);
        modelBuilder.Entity<ChatMessage>()
            .HasOne(c => c.User).WithMany(u => u.ChatMessages)
            .HasForeignKey(c => c.UserId);
        modelBuilder.Entity<PronunciationRecord>()
            .HasOne(p => p.User).WithMany(u => u.PronunciationRecords)
            .HasForeignKey(p => p.UserId);
        modelBuilder.Entity<ExamPart>()
            .HasOne(ep => ep.Exam).WithMany(e => e.Parts)
            .HasForeignKey(ep => ep.ExamId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<QuestionGroup>()
            .HasOne(qg => qg.Part).WithMany(ep => ep.Groups)
            .HasForeignKey(qg => qg.PartId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Question>()
            .HasOne(q => q.Group).WithMany(qg => qg.Questions)
            .HasForeignKey(q => q.GroupId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TestAttempt>()
            .HasOne(ta => ta.Exam).WithMany(e => e.Attempts)
            .HasForeignKey(ta => ta.ExamId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TestAttempt>()
            .HasOne(ta => ta.User).WithMany(u => u.TestAttempts)
            .HasForeignKey(ta => ta.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserAnswer>()
            .HasOne(ua => ua.Attempt).WithMany(ta => ta.Answers)
            .HasForeignKey(ua => ua.AttemptId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserAnswer>()
            .HasOne(ua => ua.Question).WithMany(q => q.UserAnswers)
            .HasForeignKey(ua => ua.QuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}
