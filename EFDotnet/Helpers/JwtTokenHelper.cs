using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EFDotnet.Helpers;

public class JwtTokenHelper
{
    private readonly IConfiguration _configuration;

    public JwtTokenHelper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 15 Minutes
        var adjExpires = DateTime.Now.AddMinutes(15);
        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: adjExpires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    // Extract the expiry time from JWT token
    public DateTime? GetTokenExpiry(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken? jwtToken = null;

        try
        {
            jwtToken = tokenHandler.ReadJwtToken(token);
        }
        catch (ArgumentException)
        {
            return null;
        }

        var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);
        if (expClaim == null)
        {
            return null; // not found
        }

        // Convert Unix timestamp to DateTime

        if (long.TryParse(expClaim.Value, out var expSeconds))
        {
            var expiryDateTime = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
            return expiryDateTime;
        }

        return null;
    }
}