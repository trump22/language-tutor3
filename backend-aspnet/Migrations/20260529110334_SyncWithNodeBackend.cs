using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace languagetutor.Migrations
{
    /// <inheritdoc />
    public partial class SyncWithNodeBackend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AITeacher",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    systemPrompt = table.Column<string>(type: "text", nullable: false),
                    supportLanguage = table.Column<string>(type: "text", nullable: false),
                    avatarUrl = table.Column<string>(type: "text", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AITeacher", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Course",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Course", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Exam",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    duration = table.Column<int>(type: "integer", nullable: false),
                    isPublished = table.Column<bool>(type: "boolean", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exam", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    languagePreference = table.Column<string>(type: "text", nullable: true),
                    skillLevel = table.Column<string>(type: "text", nullable: true),
                    learningGoal = table.Column<string>(type: "text", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Lesson",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    courseId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lesson", x => x.id);
                    table.ForeignKey(
                        name: "FK_Lesson_Course_courseId",
                        column: x => x.courseId,
                        principalTable: "Course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamPart",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    examId = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    instruction = table.Column<string>(type: "text", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamPart", x => x.id);
                    table.ForeignKey(
                        name: "FK_ExamPart_Exam_examId",
                        column: x => x.examId,
                        principalTable: "Exam",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessage",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    personaName = table.Column<string>(type: "text", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => x.id);
                    table.ForeignKey(
                        name: "FK_ChatMessage_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PronunciationRecord",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    referenceText = table.Column<string>(type: "text", nullable: false),
                    overallScore = table.Column<float>(type: "real", nullable: false),
                    accuracyScore = table.Column<float>(type: "real", nullable: false),
                    fluencyScore = table.Column<float>(type: "real", nullable: false),
                    completenessScore = table.Column<float>(type: "real", nullable: false),
                    prosodyScore = table.Column<float>(type: "real", nullable: true),
                    wordDetails = table.Column<string>(type: "jsonb", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PronunciationRecord", x => x.id);
                    table.ForeignKey(
                        name: "FK_PronunciationRecord_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestAttempt",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userId = table.Column<int>(type: "integer", nullable: false),
                    examId = table.Column<int>(type: "integer", nullable: false),
                    startTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    endTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    totalScore = table.Column<int>(type: "integer", nullable: true),
                    listeningScore = table.Column<int>(type: "integer", nullable: true),
                    readingScore = table.Column<int>(type: "integer", nullable: true),
                    speakingScore = table.Column<int>(type: "integer", nullable: true),
                    writingScore = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestAttempt", x => x.id);
                    table.ForeignKey(
                        name: "FK_TestAttempt_Exam_examId",
                        column: x => x.examId,
                        principalTable: "Exam",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestAttempt_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Score",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    totalQuestions = table.Column<int>(type: "integer", nullable: false),
                    completionTime = table.Column<int>(type: "integer", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<int>(type: "integer", nullable: false),
                    lessonId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Score", x => x.id);
                    table.ForeignKey(
                        name: "FK_Score_Lesson_lessonId",
                        column: x => x.lessonId,
                        principalTable: "Lesson",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Score_User_userId",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionGroup",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    partId = table.Column<int>(type: "integer", nullable: false),
                    passage = table.Column<string>(type: "text", nullable: true),
                    imageUrl = table.Column<string>(type: "text", nullable: true),
                    audioUrl = table.Column<string>(type: "text", nullable: true),
                    transcript = table.Column<string>(type: "text", nullable: true),
                    ssmlScript = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionGroup", x => x.id);
                    table.ForeignKey(
                        name: "FK_QuestionGroup_ExamPart_partId",
                        column: x => x.partId,
                        principalTable: "ExamPart",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Question",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    groupId = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "text", nullable: true),
                    options = table.Column<string>(type: "jsonb", nullable: true),
                    correctAnswer = table.Column<string>(type: "text", nullable: true),
                    explanation = table.Column<string>(type: "text", nullable: true),
                    evalCriteria = table.Column<string>(type: "text", nullable: true),
                    referenceText = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Question", x => x.id);
                    table.ForeignKey(
                        name: "FK_Question_QuestionGroup_groupId",
                        column: x => x.groupId,
                        principalTable: "QuestionGroup",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAnswer",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attemptId = table.Column<int>(type: "integer", nullable: false),
                    questionId = table.Column<int>(type: "integer", nullable: false),
                    selectedOption = table.Column<string>(type: "text", nullable: true),
                    audioUrl = table.Column<string>(type: "text", nullable: true),
                    textAnswer = table.Column<string>(type: "text", nullable: true),
                    isCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    score = table.Column<float>(type: "real", nullable: true),
                    aiFeedback = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAnswer", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserAnswer_Question_questionId",
                        column: x => x.questionId,
                        principalTable: "Question",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAnswer_TestAttempt_attemptId",
                        column: x => x.attemptId,
                        principalTable: "TestAttempt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AITeacher_supportLanguage",
                table: "AITeacher",
                column: "supportLanguage");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_userId_language_personaName",
                table: "ChatMessage",
                columns: new[] { "userId", "language", "personaName" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamPart_examId",
                table: "ExamPart",
                column: "examId");

            migrationBuilder.CreateIndex(
                name: "IX_Lesson_courseId",
                table: "Lesson",
                column: "courseId");

            migrationBuilder.CreateIndex(
                name: "IX_PronunciationRecord_userId",
                table: "PronunciationRecord",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Question_groupId",
                table: "Question",
                column: "groupId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroup_partId",
                table: "QuestionGroup",
                column: "partId");

            migrationBuilder.CreateIndex(
                name: "IX_Score_lessonId",
                table: "Score",
                column: "lessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Score_userId",
                table: "Score",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempt_examId",
                table: "TestAttempt",
                column: "examId");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempt_userId",
                table: "TestAttempt",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_User_email",
                table: "User",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_role",
                table: "User",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnswer_attemptId",
                table: "UserAnswer",
                column: "attemptId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnswer_questionId",
                table: "UserAnswer",
                column: "questionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AITeacher");

            migrationBuilder.DropTable(
                name: "ChatMessage");

            migrationBuilder.DropTable(
                name: "PronunciationRecord");

            migrationBuilder.DropTable(
                name: "Score");

            migrationBuilder.DropTable(
                name: "UserAnswer");

            migrationBuilder.DropTable(
                name: "Lesson");

            migrationBuilder.DropTable(
                name: "Question");

            migrationBuilder.DropTable(
                name: "TestAttempt");

            migrationBuilder.DropTable(
                name: "Course");

            migrationBuilder.DropTable(
                name: "QuestionGroup");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "ExamPart");

            migrationBuilder.DropTable(
                name: "Exam");
        }
    }
}
