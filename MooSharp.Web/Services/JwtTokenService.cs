using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MooSharp.Web.Services;

public class JwtTokenService(IConfiguration config, TimeProvider clock)
{
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public string GenerateToken(string username)
    {
        var jwtSettings = config.GetSection("Jwt");
        var keyString = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing");

        // signing key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            // effectively the "User ID"
            new(JwtRegisteredClaimNames.Sub, username),

            // standard name claim
            new(JwtRegisteredClaimNames.Name, username),

            // token id
            new(JwtRegisteredClaimNames.Jti,
                Guid
                    .NewGuid()
                    .ToString())
        };

        var configuredExpiry = jwtSettings.GetValue<TimeSpan>("Expiration");
        
        var expiration = clock
            .GetUtcNow()
            .Add(configuredExpiry)
            .DateTime;

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new(claims),
            Expires = expiration,
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = creds
        };

        return _tokenHandler.CreateToken(descriptor);
    }
}