using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace languagetutor.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTeacherGenerationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "maxTokens",
                table: "AITeacher",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "temperature",
                table: "AITeacher",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "maxTokens",
                table: "AITeacher");

            migrationBuilder.DropColumn(
                name: "temperature",
                table: "AITeacher");
        }
    }
}
