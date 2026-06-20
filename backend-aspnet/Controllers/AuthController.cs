using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using languagetutor.Data;
using languagetutor.DTOs;
using languagetutor.Models;

namespace languagetutor.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Password) ||
            string.IsNullOrWhiteSpace(req.Name) ||
            string.IsNullOrWhiteSpace(req.PhoneNumber) ||
            string.IsNullOrWhiteSpace(req.Address) ||
            req.DateOfBirth == null)
        {
            return BadRequest(new { message = "Vui lòng nhập đầy đủ họ tên, email, số điện thoại, địa chỉ, ngày sinh và mật khẩu." });
        }

        var email = req.Email.Trim();
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
            return BadRequest(new { message = "Email đã tồn tại." });

        var user = new User
        {
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Name = req.Name.Trim(),
            Role = "STUDENT",
            PhoneNumber = req.PhoneNumber.Trim(),
            Address = req.Address.Trim(),
            DateOfBirth = req.DateOfBirth,
            LanguagePreference = req.LanguagePreference ?? "en",
            SkillLevel = req.SkillLevel ?? "beginner",
            LearningGoal = req.LearningGoal ?? "general"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateToken(user);
        return StatusCode(201, new { success = true, message = "Tạo tài khoản thành công!", token, user = ToDto(user) });
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Vui lòng nhập email và mật khẩu." });

        var normalizedEmail = req.Email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.Password))
            return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });

        var token = GenerateToken(user);
        return Ok(new { token, user = ToDto(user) });
    }

    private string GenerateToken(User user)
    {
        var jwtSecret = _config["Jwt:Key"] ?? _config["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
            throw new InvalidOperationException("JWT key is not configured. Set Jwt:Key or Jwt:Secret.");

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim("userId", user.Id.ToString()),
            new Claim("id", user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(1),
            claims: claims,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.Email, u.Name, u.Role,
        u.PhoneNumber, u.Address, u.DateOfBirth, u.Gender,
        u.LanguagePreference, u.SkillLevel, u.LearningGoal, u.CreatedAt);
}
