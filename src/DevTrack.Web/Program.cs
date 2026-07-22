using DevTrack.Application.Interfaces;
using DevTrack.Application.Services;
using DevTrack.Core.Interfaces;
using DevTrack.Infrastructure.Data;
using DevTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using DevTrack.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DevTrackDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DevTrackConnection")));

builder.Services.AddDbContext<DevTrackIdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<DevTrackIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddRazorPages(); // Identity UI is Razor Pages, not MVC — check this is registered

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddScoped<ISprintRepository, SprintRepository>();

builder.Services.AddScoped<IWorkItemRepository, WorkItemRepository>();

builder.Services.AddScoped<IWorkItemService, WorkItemService>();

builder.Services.AddScoped<IEmailSender, DevTrackEmailSender>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages(); // needed for the scaffolded Identity pages to be routable

app.Run();
