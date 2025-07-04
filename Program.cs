using GameCraft.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies; // <--- ADD THIS USING DIRECTIVE

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

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
