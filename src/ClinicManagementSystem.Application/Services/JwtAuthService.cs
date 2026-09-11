using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagementSystem.Application.Services;

public interface IJwtAuthService
{
    string GenerateAccessToken(
        User user,
        List<string> roles);
}

public class JwtAuthService : IJwtAuthService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    public JwtAuthService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT Key is not configured.");

        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT Issuer is not configured.");

        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT Audience is not configured.");

        _accessTokenExpirationMinutes =
            configuration.GetValue<int>(
                "Jwt:AccessTokenExpirationMinutes");
    }

    public string GenerateAccessToken(
        User user,
        List<string> roles)
    {
        // 1. Tạo claims
        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new(
                "UserName",
                user.Username),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow
                    .ToUnixTimeSeconds()
                    .ToString(),
                ClaimValueTypes.Integer64)
        };

        // Email có thể null
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(
                new Claim(
                    JwtRegisteredClaimNames.Email,
                    user.Email));
        }

        // 2. Đưa các Role vào token
        foreach (var role in roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        // 3. Chuyển Secret Key thành byte[]
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_key));

        // 4. Tạo chữ ký cho token
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // 5. Tạo JWT
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _accessTokenExpirationMinutes),
            signingCredentials: credentials);

        // 6. Chuyển JWT thành chuỗi
        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}