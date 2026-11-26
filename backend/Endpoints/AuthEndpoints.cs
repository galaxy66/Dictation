using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Dictation.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Dictation.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app, string jwtKey, string issuer, string audience)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async Task<IResult> (
            RegisterRequest request,
            UserManager<IdentityUser> userManager,
            CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = new IdentityUser { UserName = request.Email, Email = request.Email };
            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return TypedResults.ValidationProblem(result.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(grouping => grouping.Key, grouping => grouping.Select(error => error.Description).ToArray()));
            }

            var roleResult = await userManager.AddToRoleAsync(user, Roles.User);
            if (!roleResult.Succeeded)
            {
                var deleteResult = await userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not roll back the new account: {string.Join("; ", deleteResult.Errors.Select(error => error.Description))}");
                }

                throw new InvalidOperationException(
                    $"Could not assign the user role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }

            return TypedResults.Ok(new RegistrationResponse(user.Email!));
        })
            .WithName("RegisterUser")
            .WithSummary("Register a user account")
            .Produces<RegistrationResponse>()
            .ProducesValidationProblem();

        group.MapPost("/login", async Task<IResult> (
            LoginRequest request,
            UserManager<IdentityUser> userManager,
            CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                return TypedResults.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: expiresAt.UtcDateTime,
                signingCredentials: credentials);

            return TypedResults.Ok(new LoginResponse(
                new JwtSecurityTokenHandler().WriteToken(token),
                expiresAt,
                user.Email!,
                roles));
        })
            .WithName("Login")
            .WithSummary("Log in and receive a bearer token")
            .Produces<LoginResponse>()
            .Produces(StatusCodes.Status401Unauthorized);
    }
}

/// <summary>Credentials used to register a user account.</summary>
public sealed record RegisterRequest
{
    /// <summary>The user's email address.</summary>
    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }

    /// <summary>The user's password, with at least eight characters.</summary>
    [Required, MinLength(8), StringLength(128)]
    public required string Password { get; init; }
}

/// <summary>Credentials used to log in.</summary>
public sealed record LoginRequest
{
    /// <summary>The user's email address.</summary>
    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }

    /// <summary>The user's password.</summary>
    [Required]
    public required string Password { get; init; }
}

/// <summary>Confirmation that a user account was created.</summary>
public sealed record RegistrationResponse(string Email);

/// <summary>A JWT bearer token and its expiration time.</summary>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string Email, IList<string> Roles);
