using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using geoback.Data;
using geoback.DTOs;
using geoback.Models;
using BCrypt.Net;

namespace geoback.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly GeoDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(GeoDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { message = "User with this email already exists." });
        }

        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role ?? "User" // Default to User if not specified
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new AuthResponseDto
        {
            Token = CreateToken(user),
            User = MapToDto(user)
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return BadRequest("Invalid email or password.");
        }

        return Ok(new AuthResponseDto
        {
            Token = CreateToken(user),
            User = MapToDto(user)
        });
    }
    
    // Simple endpoint to get current user details from token claims
    // The frontend calls /auth/me to get user details
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        // For now, let's just mock this or implement strictly if the token is passed.
        // In a real app we'd use [Authorize] and User.Claims.
        // Given the task is to Login with REAL data, the Login endpoint is key.
        // We will implement full [Authorize] middleware in Program.cs next.
        // But for "Me" endpoint we need to read the token.
        
        // This is a placeholder as proper middleware setup is next step.
        return Unauthorized("Please implement middleware first");
    }

    private string CreateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? "SuperSecretKeyForDevelopmentOnly12345!"; // Fallback for dev

        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role
        };
    }
}
