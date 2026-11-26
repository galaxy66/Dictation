using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dictation.Api.Data;
using Dictation.Api.Endpoints;
using Dictation.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Dictation.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Dictation.Client";

builder.Services.AddDbContext<DictationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<DictationDbContext>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IContentService, ContentService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DictationDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BootstrapAdminAsync(scope.ServiceProvider, app.Configuration);
}

app.MapAuthEndpoints(jwtKey, jwtIssuer, jwtAudience);
app.MapContentEndpoints();
app.Run();

static async Task BootstrapAdminAsync(IServiceProvider services, IConfiguration configuration)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { Roles.Admin, Roles.User })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create the {role} role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }
        }
    }

    var email = configuration["BootstrapAdmin:Email"];
    var password = configuration["BootstrapAdmin:Password"];
    if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException(
            "BootstrapAdmin:Email and BootstrapAdmin:Password must both be configured.");
    }

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var admin = await userManager.FindByEmailAsync(email);
    if (admin is null)
    {
        admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var userResult = await userManager.CreateAsync(admin, password);
        if (!userResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create the bootstrap admin: {string.Join("; ", userResult.Errors.Select(error => error.Description))}");
        }
    }

    if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
    {
        var assignmentResult = await userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!assignmentResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not assign the admin role: {string.Join("; ", assignmentResult.Errors.Select(error => error.Description))}");
        }
    }
}
