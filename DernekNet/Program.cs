
using DernekNet.Data;
using DernekNet.Filters;
using DernekNet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SQL Server ba�lant�s�
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection bulunamad�.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity ve rol sistemi
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// DernekAdmin g�venlik filtresi
builder.Services.AddScoped<DernekAdminGuvenlikFiltresi>();

// B�t�n MVC Controller i�lemlerinde filtreyi �al��t�r
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<DernekAdminGuvenlikFiltresi>();
});

var app = builder.Build();

// Hata y�netimi
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Area y�nlendirmesi
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Normal MVC y�nlendirmesi
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// �lk PlatformAdmin rol�n� olu�tur
// ve mevcut kullan�c�ya ata.
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        const string adminRole = "PlatformAdmin";

        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole(adminRole));

            if (!roleResult.Succeeded)
            {
                throw new Exception(
                    "PlatformAdmin rol� olu�turulamad�: " +
                    string.Join(", ",
                        roleResult.Errors.Select(x => x.Description)));
            }
        }

        // Mevcut PlatformAdmin hesab�
        const string adminEmail = "yavuzsarmasik1@gmail.com";

        var adminUser = await userManager
            .FindByEmailAsync(adminEmail);

        if (adminUser != null &&
            adminUser.EmailConfirmed &&
            !await userManager.IsInRoleAsync(adminUser, adminRole))
        {
            var result = await userManager.AddToRoleAsync(
                adminUser, adminRole);

            if (!result.Succeeded)
            {
                throw new Exception(
                    "Y�netici rol� atanamad�: " +
                    string.Join(", ",
                        result.Errors.Select(x => x.Description)));
            }
        }
    }
}

app.Run();
