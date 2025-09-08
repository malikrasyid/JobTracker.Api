using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using dotenv.net;
using MongoDB.Driver;
using JobTracker.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Load .env file
DotEnv.Load();

// 2. Read environment variables
var mongoUri = Environment.GetEnvironmentVariable("MONGO_URI");
var mongoDb = Environment.GetEnvironmentVariable("MONGO_DBNAME");
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "JobTrackerAPI";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "JobTrackerUsers";

// 3. Register MongoDB settings from .env
builder.Services.Configure<MongoDbSettings>(options =>
{
    options.ConnectionString = mongoUri ?? throw new Exception("MONGO_URI missing in .env");
    options.DatabaseName = mongoDb ?? throw new Exception("MONGO_DBNAME missing in .env");
});
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(mongoUri)
);

// 4. Configure JWT Authentication with .env values
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret ?? throw new Exception("JWT_SECRET missing in .env"))
            )
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
