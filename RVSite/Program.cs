using Microsoft.EntityFrameworkCore;
using RVSite.Models;
using SiteDemo.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("ProductionConnection")));
}


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}


// Quick data seeding for demonstration purposes
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Sites.Any())
    {
        db.Sites.AddRange(
            new Site { SiteNumber = "A1", SiteType = "Tent", SiteStatus = "Available", MaxRVLength = 0, BaseRate = 25.00m },
            new Site { SiteNumber = "B2", SiteType = "RV", SiteStatus = "Occupied", MaxRVLength = 40, BaseRate = 45.00m },
            new Site { SiteNumber = "C3", SiteType = "Cabin", SiteStatus = "Available", MaxRVLength = 0, BaseRate = 80.00m }
        );
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
