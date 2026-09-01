using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Read_It.Data;
using Read_It.Models;

var builder = WebApplication.CreateBuilder(args);

// Render dynamic port binding
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Redirect legacy /Identity/Account endpoints to custom MVC /Account endpoints
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/Identity/Account/Login", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Account/Login");
        return;
    }
    if (path.StartsWith("/Identity/Account/Register", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Account/Register");
        return;
    }
    if (path.StartsWith("/Identity/Account/Logout", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Account/Logout");
        return;
    }
    if (path.StartsWith("/Identity/Account/ForgotPassword", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Account/ForgotPassword");
        return;
    }
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

Read_It.DbSeeder.Seed(app);

app.Run();
