using Microsoft.EntityFrameworkCore;
using RVSite.Models;

namespace SiteDemo.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Site> Sites { get; set; }
    }
}