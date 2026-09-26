using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevTrack.Api.Contracts;
using DevTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace DevTrack.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(UserManager<ApplicationUser> users, IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email, DisplayName = request.DisplayName?.Trim(), EmailConfirmed = true };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new ValidationProblemDetails(
                result.Errors.ToDictionary(error => error.Code, error => new[] { error.Description })));
        }

        await users.AddToRoleAsync(user, "TeamMember");
        return Created("", null);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password." });
        }

        var expiresAt = DateTime.UtcNow.AddHours(1);
        return Ok(new TokenResponse(CreateToken(user, await users.GetRolesAsync(user), expiresAt), expiresAt));
    }

    private string CreateToken(ApplicationUser user, IList<string> roles, DateTime expiresAt)
    {
        var jwt = configuration.GetRequiredSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.")));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? user.UserName ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: jwt["Issuer"], audience: jwt["Audience"], claims: claims, notBefore: DateTime.UtcNow,
            expires: expiresAt, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
    }
}
