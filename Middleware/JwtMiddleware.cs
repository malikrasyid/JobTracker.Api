using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace JobTracker.Api.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
            {
                try
                {
                    // JWT validation is handled by Authentication middleware
                    // This middleware can add additional custom validation if needed
                    if (context.User.Identity?.IsAuthenticated == true)
                    {
                        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (string.IsNullOrEmpty(userId))
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsJsonAsync(new { message = "Invalid token: missing user ID claim" });
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Invalid token", details = ex.Message });
                    return;
                }
            }

            await _next(context);
        }
    }
}