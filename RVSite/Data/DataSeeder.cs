using RVSite.Models;
using Microsoft.EntityFrameworkCore;


namespace RVSite.Data
{
    public class DataSeeder
    {
        public static void Seed(AppDbContext db)
        {
            //db.Database.Migrate();

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

            if (!db.SiteTypes.Any())
            {
                db.SiteTypes.AddRange(new[]
                {
                    new SiteType { Name = "RV" },
                    new SiteType { Name = "Tent" },
                    new SiteType { Name = "Cabin" }
                });

                db.SaveChanges();
            }

            var rvType = db.SiteTypes.First(st => st.Name == "RV");
            var tentType = db.SiteTypes.First(st => st.Name == "Tent");

            if (!db.SiteTypePrices.Any())
            {
                db.SiteTypePrices.AddRange(new[]
                {
                    new SiteTypePrice
                    {
                        SiteTypeID = rvType.SiteTypeID,
                        StartDate = new DateTime(2024, 1, 1),
                        EndDate = new DateTime(2024, 5, 31),
                        Price = 40m
                    },
                    new SiteTypePrice
                    {
                        SiteTypeID = rvType.SiteTypeID,
                        StartDate = new DateTime(2024, 6, 1),
                        EndDate = null, // current price
                        Price = 45m
                    }
                });

                db.SaveChanges();
            }

            if (!db.Sites.Any())
            {
                db.Sites.AddRange(new[]
                {
                    new Site { SiteNumber = "A1", SiteTypeID = rvType.SiteTypeID, SiteType = rvType, SiteStatus = SiteStatus.Available.ToString(), MaxRVLength = 40, BaseRate = 45m },
                    new Site { SiteNumber = "A2", SiteTypeID = rvType.SiteTypeID, SiteType = rvType, SiteStatus = SiteStatus.Available.ToString(), MaxRVLength = 40, BaseRate = 45m },
                    new Site { SiteNumber = "B1", SiteTypeID = tentType.SiteTypeID, SiteType = tentType, SiteStatus = SiteStatus.Available.ToString(), MaxRVLength = 0, BaseRate = 25m }
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
