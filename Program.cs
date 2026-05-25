using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;
using GasStationMS.Models;
using GasStationMS.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. Տվյալների բազայի կոնֆիգուրացիա
//    Ավտոմատ ընտրում է PostgreSQL կամ SQL Server provider-ը՝
//    կախված connection string-ի ձևաչափից։
//    - Render/Heroku-ն տալիս է DATABASE_URL՝ postgres:// ձևաչափով
//    - Локал-ում օգտագործվում է SQL Server LocalDB
// =========================================================
string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;
bool usePostgres;

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Render-ը տալիս է postgres://user:pass@host:port/dbname ձևաչափով
    // EF Core-ը պահանջում է Npgsql-ի key=value ձևաչափ — փոխակերպում ենք
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString =
        $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};" +
        $"Database={uri.AbsolutePath.TrimStart('/')};" +
        $"Username={userInfo[0]};Password={userInfo[1]};" +
        $"SSL Mode=Require;Trust Server Certificate=true";
    usePostgres = true;
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
    // Եթե connection string-ը պարունակում է "Host=" — ապա PostgreSQL է
    usePostgres = connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase);
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (usePostgres)
        options.UseNpgsql(connectionString);
    else
        options.UseSqlServer(connectionString);
});

// =========================================================
// 2. Identity
// =========================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// =========================================================
// 3. Cookie configuration
// =========================================================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.Name = "GasStationMS.Auth";
    options.Cookie.HttpOnly = true;
});

// =========================================================
// 4. Business services
// =========================================================
builder.Services.AddScoped<IFuelInventoryService, FuelInventoryService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddHttpContextAccessor();

// =========================================================
// 5. MVC
// =========================================================
builder.Services.AddControllersWithViews();

var app = builder.Build();

// =========================================================
// 6. Հոստինգի պորտի կոնֆիգուրացիա
//    Render/Heroku-ն տրամադրում է PORT environment variable
// =========================================================
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Clear();
    app.Urls.Add($"http://0.0.0.0:{port}");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS-ը միացված է production-ում
    app.UseHsts();
}

// HTTPS redirect-ը միայն локал-ում, քանի որ Render-ը ինքն է
// ապահովում HTTPS-ը reverse-proxy-ով
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =========================================================
// 7. Տվյալների բազայի migration և seed
//    Render-ում ավտոմատ կիրառում է migration-ները ամեն deploy-ի ժամանակ
// =========================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // EnsureCreated՝ ստեղծում է սխեման ուղղակիորեն մոդելից։
        // Demo-ի համար պարզ է — չի պահանջում Migrations folder և
        // աշխատում է թե՛ SQL Server, թե՛ PostgreSQL provider-ով։
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Չհաջողվեց migrate/seed-ել տվյալների բազան");
    }
}

app.Run();
