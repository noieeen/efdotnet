using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EFDotnet.Helpers;
using EFDotnet.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace EFDotnet.Controllers;

public class AuthController : ControllerBase
{
    private readonly JwtTokenHelper _jwtTokenHelper;

    // In-memory store for simplicity
    private static Dictionary<string, string> _refreshTokens = new();

    public AuthController(JwtTokenHelper jwtTokenHelper)
    {
        _jwtTokenHelper = jwtTokenHelper;
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public ActionResult<AuthResponse> Login([FromBody] User user)
    {
        if (user.Username == "test" && user.Password == "password")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username)
            };

            var accessToken = _jwtTokenHelper.GenerateAccessToken(claims);
            var refreshToken = _jwtTokenHelper.GenerateRefreshToken();

            // Store refresh token (in-memory for now)
            _refreshTokens[user.Username] = refreshToken;

            return Ok(new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        return Unauthorized();
    }

    // POST: api/auth/refresh
    [HttpPost("refresh")]
    public ActionResult<AuthResponse> Refresh([FromBody] AuthResponse authResponse)
    {
        // Parse the expired JWT token to get the username
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(authResponse.AccessToken);

        var username = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        if (username == null || !_refreshTokens.ContainsKey(username) ||
            _refreshTokens[username] != authResponse.RefreshToken)
        {
            return Unauthorized();
        }

        var claims = jwtToken.Claims.ToList();
        var newAccessToken = _jwtTokenHelper.GenerateAccessToken(claims);
        var newRefreshToken = _jwtTokenHelper.GenerateRefreshToken();

        // Update the refresh token store
        _refreshTokens[username] = newAccessToken;

        return Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }
}