using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using languagetutor.Data;
using languagetutor.DTOs;

namespace languagetutor.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AppDbContext _db;

    public UserController(AppDbContext db) => _db = db;

    private int GetUserId() =>
        int.Parse(User.FindFirst("userId")?.Value ?? User.FindFirst("id")?.Value ?? "0");

    // GET /api/users/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var user = await _db.Users.FindAsync(GetUserId());
        if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

        return Ok(new
        {
            success = true,
            data = new UserDto(
                user.Id, user.Email, user.Name, user.Role,
                user.PhoneNumber, user.Address, user.DateOfBirth, user.Gender,
                user.LanguagePreference, user.SkillLevel, user.LearningGoal, user.CreatedAt)
        });
    }

    // PUT /api/users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest req)
    {
        var user = await _db.Users.FindAsync(GetUserId());
        if (user == null) return NotFound();

        if (req.Name != null) user.Name = req.Name;
        if (req.PhoneNumber != null) user.PhoneNumber = req.PhoneNumber;
        if (req.Address != null) user.Address = req.Address;
        if (req.DateOfBirth != null) user.DateOfBirth = req.DateOfBirth;
        if (req.LanguagePreference != null) user.LanguagePreference = req.LanguagePreference;
        if (req.SkillLevel != null) user.SkillLevel = req.SkillLevel;
        if (req.LearningGoal != null) user.LearningGoal = req.LearningGoal;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new
        {
            success = true,
            message = "Cập nhật hồ sơ thành công",
            data = new
            {
                user.Id,
                user.Name,
                user.PhoneNumber,
                user.Address,
                user.DateOfBirth,
                user.LanguagePreference,
                user.SkillLevel,
                user.LearningGoal
            }
        });
    }
}
