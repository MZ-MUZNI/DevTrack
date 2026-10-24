using System.Text;
using DevTrack.Application.Interfaces;
using DevTrack.Application.Services;
using DevTrack.Core.Interfaces;
using DevTrack.Infrastructure.Data;
using DevTrack.Infrastructure.Identity;
using DevTrack.Infrastructure.AI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DevTrack.Api.Authorization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();

var domainConnection = builder.Configuration.GetConnectionString("DevTrackConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DevTrackConnection is not configured.");
var identityConnection = builder.Configuration.GetConnectionString("IdentityConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:IdentityConnection is not configured.");
var jwt = builder.Configuration.GetRequiredSection("Jwt");
var jwtKey = jwt["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be a non-empty, cryptographically random key of at least 32 bytes.");
var businessTimeZoneId = builder.Configuration["BusinessTimeZone"] ?? "UTC";
var ollamaBaseUri = ValidateOllamaBaseUri(builder.Configuration["Ollama:BaseUrl"]);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(_ => TimeZoneInfo.FindSystemTimeZoneById(businessTimeZoneId));
builder.Services.AddDbContext<DevTrackDbContext>(options => options.UseSqlServer(domainConnection));
builder.Services.AddDbContext<DevTrackIdentityDbContext>(options => options.UseSqlServer(identityConnection));

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<DevTrackIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwt["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ProjectAccess", policy => policy.Requirements.Add(new ProjectOwnerRequirement()));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});
builder.Services.AddSingleton<IAuthorizationHandler, ProjectOwnerAuthorizationHandler>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DevTrackDbContext>("domain-database")
    .AddDbContextCheck<DevTrackIdentityDbContext>("identity-database");

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ISprintRepository, SprintRepository>();
builder.Services.AddScoped<IWorkItemRepository, WorkItemRepository>();
builder.Services.AddScoped<IWorkItemService, WorkItemService>();
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddHttpClient<ITaskPlanningService, OllamaTaskPlanningService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>().Value;
    client.BaseAddress = ollamaBaseUri;
    client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
});

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

using (var scope = app.Services.CreateScope())
{
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in new[] { "Admin", "TeamMember" })
    {
        if (!await roles.RoleExistsAsync(roleName)) await roles.CreateAsync(new IdentityRole(roleName));
    }
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'; base-uri 'none'");
    await next();
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready").RequireAuthorization("AdminOnly");
app.MapControllers();
app.Run();

static Uri ValidateOllamaBaseUri(string? configuredValue)
{
    if (!Uri.TryCreate(configuredValue, UriKind.Absolute, out var uri) ||
        !string.IsNullOrEmpty(uri.UserInfo) ||
        (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
    {
        throw new InvalidOperationException("Ollama:BaseUrl must be HTTPS, or HTTP on a loopback address.");
    }

    return new Uri(uri.ToString().TrimEnd('/') + "/");
}

public partial class Program;
