using RVSite.Models;
using Microsoft.EntityFrameworkCore;


namespace RVSite.Data
{
    public class DataSeeder
    {
        public static void Seed(AppDbContext db)
        {
            db.Database.Migrate();

            if (!db.Role.Any())
            {
                db.Role.AddRange(new[]
                {
                    new Role { Type = RoleType.Admin },
                    new Role { Type = RoleType.Staff },
                    new Role { Type = RoleType.Customer }
                });

                db.SaveChanges();
            }

            var customerRole = db.Role.FirstOrDefault(r => r.Type == RoleType.Customer);

            if (!db.Sites.Any())
            {
                db.Sites.AddRange(new[]
                {
                    new Site { SiteNumber = "A1", SiteType = "RV", SiteStatus = SiteStatus.Available.ToString(), MaxRVLength = 40, BaseRate = 45m },
                    new Site { SiteNumber = "A2", SiteType = "RV", SiteStatus = SiteStatus.Available.ToString(), MaxRVLength = 40, BaseRate = 45m },
                    new Site { SiteNumber = "B1", SiteType = "Tent", SiteStatus = SiteStatus.Available.ToString(), MaxRVLength = 0, BaseRate = 25m }
                });

                db.SaveChanges();
            }

            if (!db.Users.Any())
            {
                db.Users.AddRange(new[]
                {
                    new User { FirstName = "John", LastName = "Doe", Email = "john@example.com", PhoneNumber = "555-5678", PasswordHash = "devHash1", RoleID = customerRole.RoleID },
                    new User { FirstName = "Sarah", LastName = "Smith", Email = "sarah@example.com", PhoneNumber = "555-1234", PasswordHash = "devHash2", RoleID = customerRole.RoleID }
                });

                db.SaveChanges();
            }

            if (!db.Reservations.Any())
            {
                var user1 = db.Users.First();
                var site1 = db.Sites.First();

                db.Reservations.AddRange(new[]
                {
                    new Reservation
                    {
                        UserID = user1.UserID,
                        SiteID = site1.SiteID,

                        CheckInDate = DateTime.Today.AddDays(1),
                        CheckOutDate = DateTime.Today.AddDays(4),

                        NumberOfAdults = 2,
                        NumberOfChildren = 0,
                        NumberOfPets = 0,

                        ReservationStatus = ReservationStatus.Confirmed,

                        TotalCost = site1.BaseRate * 3,
                    }
                });

                db.SaveChanges();
            }

            db.SaveChanges();
        }
    }
}
