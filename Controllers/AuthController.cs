
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using JobTracker.Api.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using dotenv.net;

namespace JobTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMongoCollection<User> _users;

        public AuthController(IMongoClient client)
        {
            DotEnv.Load();
            var mongoDb = Environment.GetEnvironmentVariable("MONGO_DBNAME");
            _users = client.GetDatabase(mongoDb).GetCollection<User>("Users");
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
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Try to find user by email or username
            var user = await _users.Find(u => 
                u.Email == loginDto.EmailOrUsername || 
                u.Username == loginDto.EmailOrUsername
            ).FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            var token = GenerateJwtToken(user);
            return Ok(new { 
                token,
                userId = user.Id,
                username = user.Username,
                email = user.Email
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
            var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
            var jwtExpireMinutes = Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES") ?? "60";

            // Validate secret length: HS256 requires a key of at least 128 bits (16 bytes).
            if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetBytes(jwtSecret).Length < 16)
            {
                // Throw a clear exception so the caller sees a descriptive message in logs.
                throw new InvalidOperationException("JWT_SECRET must be set and at least 16 characters (128 bits) long.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? throw new InvalidOperationException("User ID is required")),
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? throw new InvalidOperationException("User ID is required")),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("role", "User") // Default role for all users
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtExpireMinutes)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}