using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SantiBot.Dashboard.Services;

public class JwtService
{
    private readonly string _key;

    public JwtService(IConfiguration config)
    {
        _key = config["Jwt:Key"] ?? "SantiBot-Dashboard-Secret-Key-Change-In-Production-Min32Chars!";
    }

    public string GenerateToken(ulong userId, string username, string avatarUrl)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim("avatar", avatarUrl ?? ""),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "SantiBot.Dashboard",
            audience: "SantiBot.Dashboard",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ulong? GetUserIdFromToken(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is null || !ulong.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }
}
