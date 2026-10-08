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

var builder = WebApplication.CreateBuilder(args);

var domainConnection = builder.Configuration.GetConnectionString("DevTrackConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DevTrackConnection is not configured.");
var identityConnection = builder.Configuration.GetConnectionString("IdentityConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:IdentityConnection is not configured.");
var jwt = builder.Configuration.GetRequiredSection("Jwt");
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers();
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
builder.Services.AddAuthorization(options => options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ISprintRepository, SprintRepository>();
builder.Services.AddScoped<IWorkItemRepository, WorkItemRepository>();
builder.Services.AddScoped<IWorkItemService, WorkItemService>();
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddHttpClient<ITaskPlanningService, OllamaTaskPlanningService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
