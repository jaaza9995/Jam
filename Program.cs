
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Jam.DAL;
using Jam.DAL.StoryDAL;
using Jam.DAL.SceneDAL;
using Jam.DAL.AnswerOptionDAL;
using Jam.DAL.PlayingSessionDAL;
using Jam.Models;
using Serilog;
using Serilog.Events;
using Jam.DAL.ApplicationUserDAL;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("StoryDbContextConnection") ?? throw new InvalidOperationException("Connection string 'StoryDbContextConnection' not found.");

// MVC
builder.Services.AddControllersWithViews();


// DbContext
builder.Services.AddDbContext<StoryDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration["ConnectionStrings:StoryDbContextConnection"]);
});


// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    //Password setting
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(60);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<StoryDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
});


// Repositories 
builder.Services.AddScoped<IAnswerOptionRepository, AnswerOptionRepository>();
builder.Services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
builder.Services.AddScoped<IPlayingSessionRepository, PlayingSessionRepository>();
builder.Services.AddScoped<ISceneRepository, SceneRepository>();
builder.Services.AddScoped<IStoryRepository, StoryRepository>();


builder.Services.AddRazorPages();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Jam.StoryCreation.Session";
    options.IdleTimeout = TimeSpan.FromHours(4); // large timespan for creation mode
    options.Cookie.IsEssential = true;
});


// Serilog
var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File($"Logs/app_{DateTime.Now:yyyyMMdd_HHmmss}.log");

loggerConfiguration.Filter.ByExcluding(e => e.Properties.TryGetValue("SourceContext", out var value) &&
    e.Level == LogEventLevel.Information &&
    e.MessageTemplate.Text.Contains("Executed DbCommand"));

var logger = loggerConfiguration.CreateLogger();
builder.Logging.AddSerilog(logger);

var app = builder.Build();


// Developer setup
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DBInit.SeedAsync(app);
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapDefaultControllerRoute();
app.MapRazorPages();


// Run (seeding is async, so the entire file is also async)
await app.RunAsync();
