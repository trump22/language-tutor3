namespace languagetutor.DTOs;

// ==================== AUTH ====================
public record RegisterRequest(
    string Email, string Password, string Name,
    string? PhoneNumber, string? Address, DateOnly? DateOfBirth,
    string? LanguagePreference, string? SkillLevel,
    string? LearningGoal);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, UserDto User);

// ==================== USER ====================
public record UserDto(
    int Id, string Email, string Name, string Role,
    string? PhoneNumber, string? Address, DateOnly? DateOfBirth, string? Gender,
    string? LanguagePreference, string? SkillLevel,
    string? LearningGoal, DateTime CreatedAt);

public record UpdateProfileRequest(
    string? Name, string? PhoneNumber, string? Address, DateOnly? DateOfBirth,
    string? LanguagePreference,
    string? SkillLevel, string? LearningGoal);

// ==================== COURSE ====================
public record CreateCourseRequest(string Title, string Description, string Language);

public record CreateLessonRequest(string CourseId, string Title, object Content);

public record SubmitScoreRequest(int Score, int TotalQuestions, int? CompletionTime);

// ==================== AI ====================
public record ChatRequest(string Message, string Language, string? Level, string? PersonaName);

public record GeneratePracticeTextRequest(string Language, string? Level);

public record AnalyzeTextRequest(string Text, string Language);

public record GenerateExercisesRequest(string Language, string? Level, string? Topic);

public record PersonalizedGrammarPracticeRequest(string? Language, string? Level);

public record DraftListeningRequest(string RawScript, string? Level, int? Part);

public record SaveListeningRequest(
    int ExamPartId, string? AudioUrl,
    string? Transcript, string? Ssml, List<QuestionDto> Questions);

public record QuestionDto(
    string Text, Dictionary<string, string> Options,
    string CorrectAnswer, string? Explanation);

// ==================== ADMIN ====================
public record CreateAccountRequest(
    string Email, string Password, string Name, string Role,
    string? PhoneNumber, string? Address, DateOnly? DateOfBirth, string? Gender,
    string? LanguagePreference, string? SkillLevel, string? LearningGoal);

public record UpdateUserRoleRequest(string Role);

public record UpdateUserRequest(
    string? Name, string? Email, string? PhoneNumber,
    string? Address, DateOnly? DateOfBirth, string? Gender,
    string? Role, string? LanguagePreference, string? SkillLevel, string? LearningGoal);

public record GenerateExercisesAdminRequest(
    string Language, string Level, string Topic, string LessonId);

public record CreateAITeacherRequest(
    string Name, string SystemPrompt, string SupportLanguage,
    string? AvatarUrl, double? Temperature, int? MaxTokens);

public record UpdateAITeacherRequest(
    string Name, string SystemPrompt, string SupportLanguage,
    string? AvatarUrl, double? Temperature, int? MaxTokens);
