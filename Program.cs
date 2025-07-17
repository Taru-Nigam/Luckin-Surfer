using GameCraft.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using GameCraft.DbInitializers; // <--- ADD THIS USING DIRECTIVE
using Microsoft.Extensions.Logging; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<GameCraftDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GameCraftDb")));

// *** IMPORTANT: ADD AUTHENTICATION SERVICES HERE ***
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Employee/Login"; // This is the URL where unauthorized users will be redirected
        options.AccessDeniedPath = "/Employee/AccessDenied"; // Optional: A page for authenticated but unauthorized users
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // How long the authentication cookie lasts
        options.SlidingExpiration = true; // Reset cookie expiration on each request
    });

builder.Services.AddAuthorization(); // This was already there, keep it

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options => // Add options for session, especially IsEssential
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Crucial for session to work reliably
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// --- START: Database Seeding Logic ---
// This block should run once when the application starts
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<GameCraftDbContext>();
        // Call your custom initializer to seed data
        await DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
// --- END: Database Seeding Logic ---


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Assuming MapStaticAssets is a custom extension method for static files/assets
// If it's not custom, ensure it's placed correctly relative to UseStaticFiles
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets(); // If WithStaticAssets is also a custom extension

app.Run();
