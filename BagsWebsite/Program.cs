using BagsWebsite.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Services Registration (Hamesha builder.Build() se PEHLE) ---

builder.Services.AddControllersWithViews();

// DbContext setup (Direct builder.Configuration use karein)
builder.Services.AddDbContext<BagDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("dbcs")));

// Session Service (Yahan register karna lazmi hai)
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
// Ab application build karein
var app = builder.Build();

// --- 2. Middleware Pipeline (Hamesha builder.Build() se BAAD) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session Middleware (Routing ke baad aur Auth se pehle)
app.UseSession();

app.UseAuthorization();

// --- 3. Routing Configuration ---

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Start page Login rakha hai

app.Run();