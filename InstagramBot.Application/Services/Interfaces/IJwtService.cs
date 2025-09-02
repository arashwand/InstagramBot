using InstagramBot.Core.Entities;
using System.Security.Claims;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        ClaimsPrincipal ValidateToken(string token);
    }
}
