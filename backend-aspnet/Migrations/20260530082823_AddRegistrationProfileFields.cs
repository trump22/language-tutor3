using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace languagetutor.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "User",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "dateOfBirth",
                table: "User",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phoneNumber",
                table: "User",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address",
                table: "User");

            migrationBuilder.DropColumn(
                name: "dateOfBirth",
                table: "User");

            migrationBuilder.DropColumn(
                name: "phoneNumber",
                table: "User");
        }
    }
}
