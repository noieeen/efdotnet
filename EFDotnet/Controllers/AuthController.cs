using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EFDotnet.Helpers;
using EFDotnet.Models;
using EFDotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace EFDotnet.Controllers;

public class AuthController : ControllerBase
{
    private readonly JwtTokenHelper _jwtTokenHelper;

    private readonly RedisCacheService _redisCacheService;
    private readonly AuthService _authService;

    public AuthController(JwtTokenHelper jwtTokenHelper, AuthService authService, RedisCacheService redisCacheService)
    {
        _jwtTokenHelper = jwtTokenHelper;
        _authService = authService;
        _redisCacheService = redisCacheService;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult> RegisterAsync([FromBody] UserRegisterReq req)
    {
        try
        {
            await _authService.RegisterAsync(req.username, req.email, req.password);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> LoginAsync([FromBody] UserLoginReq req)
    {
        var resp = await _authService.LoginAsync(req.username, req.password);
        if (resp != null) return Ok(resp);

        // Test
        // if (username == "test" && password == "password")
        // {
        // }

        return Unauthorized();
    }

    // POST: api/auth/refresh
    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult> Refresh()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Invalid token");
        }

        AuthResponse? resp = null;
        try
        {
            resp = await _authService.RefreshTokenAsync(token);
        }
        catch (Exception)
        {
            return BadRequest("Invalid generate refreshToken");
        }

        return Ok(resp);
        // // Parse the expired JWT token to get the username
        // var handler = new JwtSecurityTokenHandler();
        // var jwtToken = handler.ReadJwtToken(authResponse.AccessToken);
        //
        // var username = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        // if (username == null || !_refreshTokens.ContainsKey(username) ||
        //     _refreshTokens[username] != authResponse.RefreshToken)
        // {
        //     return Unauthorized();
        // }
        //
        // var claims = jwtToken.Claims.ToList();
        // var newAccessToken = _jwtTokenHelper.GenerateAccessToken(claims);
        // var newRefreshToken = _jwtTokenHelper.GenerateRefreshToken();
        //
        // // Update the refresh token store
        // _refreshTokens[username] = newAccessToken;
        //
        // return Ok(new AuthResponse
        // {
        //     AccessToken = newAccessToken,
        //     RefreshToken = newRefreshToken
        // });
    }

    // POST: api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> LogoutAsync()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Invalid token");
        }

        var expiry = _jwtTokenHelper.GetTokenExpiry(token);
        if (expiry.HasValue)
        {
            await _redisCacheService.SetAsync($"blacklisted_{token}", true, expiry.Value - DateTime.Now);
        }

        return NoContent();
    }
}