using Microsoft.EntityFrameworkCore;
using RVSite.Models;

namespace RVSite.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Site> Sites { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<Fee> Fees { get; set; }
        public DbSet<SiteType> SiteTypes { get; set; }
        public DbSet<SiteTypePrice> SiteTypePrices { get; set; }
        public DbSet<SitePhoto> SitePhoto { get; set; }
        public DbSet<MaintenanceTask> MaintenanceTasks { get; set; }
    }
}