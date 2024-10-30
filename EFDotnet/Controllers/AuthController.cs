using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EFDotnet.Helpers;
using EFDotnet.Models;
using EFDotnet.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace EFDotnet.Controllers;

public class AuthController : ControllerBase
{
    private readonly JwtTokenHelper _jwtTokenHelper;

    // In-memory store for simplicity
    private static Dictionary<string, string> _refreshTokens = new();
    private readonly AuthService _authService;

    public AuthController(JwtTokenHelper jwtTokenHelper, AuthService authService)
    {
        _jwtTokenHelper = jwtTokenHelper;
        _authService = authService;
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> LoginAsync([FromBody] string username, string password)
    {
        var resp = await _authService.LoginAsync(username, password);
        if (resp != null) return Ok(resp);
        
        // Test
        // if (username == "test" && password == "password")
        // {
        // }

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