using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EFDotnet.Helpers;
using EFDotnet.Models;
using EFDotnet.Repositories;

namespace EFDotnet.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtTokenHelper _jwtTokenHelper;

    public AuthService(IUserRepository userRepository, JwtTokenHelper jwtTokenHelper)
    {
        _userRepository = userRepository;
        _jwtTokenHelper = jwtTokenHelper;
    }

    public async Task RegisterAsync(string username, string email, string password)
    {
        CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

        var user = new User
        {
            Username = username,
            Email = username,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            IsDeleted = false
        };

        await _userRepository.AddUserAsync(user);
    }

    public async Task<AuthResponse?> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetUserByUsernameAsync(username);
        if (user == null || !VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var accessToken = _jwtTokenHelper.GenerateAccessToken(claims);
        var refreshToken = _jwtTokenHelper.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.Now.AddDays(7);
        await _userRepository.UpdateUserAsync(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string token)
    {
        var user = await _userRepository.GetUserByRefreshTokenAsync(token);
        if (user == null || user.RefreshTokenExpiry <= DateTime.Now)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var accessToken = _jwtTokenHelper.GenerateAccessToken(claims);
        var refreshToken = _jwtTokenHelper.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.Now.AddDays(7);
        await _userRepository.UpdateUserAsync(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }

    private bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
    {
        using (var hmac = new HMACSHA512(storedSalt))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(storedHash);
        }
    }
}