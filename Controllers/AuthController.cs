using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using JobTracker.Api.Models;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMongoCollection<User> _users;
    private readonly IConfiguration _config;

    public AuthController(IMongoClient client, IConfiguration config)
    {
        var database = client.GetDatabase(config["MongoDb:DatabaseName"]);
        _users = database.GetCollection<User>("Users");
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User userDto)
    {
        var existingUser = await _users.Find(u => u.Email == userDto.Email).FirstOrDefaultAsync();
        if (existingUser != null)
            return BadRequest("Email already exists.");

        var user = new User
        {
            Username = userDto.Username,
            Email = userDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.PasswordHash)
        };

        await _users.InsertOneAsync(user);
        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] User loginDto)
    {
        var user = await _users.Find(u => u.Email == loginDto.Email).FirstOrDefaultAsync();
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.PasswordHash, user.PasswordHash))
            return Unauthorized("Invalid email or password");

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpireMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
