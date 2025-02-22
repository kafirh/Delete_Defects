using Microsoft.EntityFrameworkCore;
using DeleteDefect.Data;
using DeleteDefect.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Konfigurasi Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(30)); // 4 jam 30 menit
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

// Konfigurasi Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// Konfigurasi Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Konfigurasi MVC
builder.Services.AddControllersWithViews();

// Konfigurasi SignalR
builder.Services.AddSignalR();

// Registrasi DefectTableListener sebagai Singleton
builder.Services.AddSingleton<DefectTableListener>(provider =>
{
    var hubContext = provider.GetRequiredService<IHubContext<DefectHub>>();
    return new DefectTableListener(hubContext, connectionString);
});

var app = builder.Build();

// Middleware dan konfigurasi request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Global Exception Handling Middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An unhandled exception occurred.");
        context.Response.Redirect("/Home/Error");
    }
});

// Konfigurasi SignalR
app.MapHub<DefectHub>("/defectHub");

// Konfigurasi Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mulai DefectTableListener setelah aplikasi dijalankan
var defectTableListener = app.Services.GetRequiredService<DefectTableListener>();
defectTableListener.StartMonitoring();

app.Run();
