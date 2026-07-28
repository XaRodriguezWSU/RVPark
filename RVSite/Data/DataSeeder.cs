using RVSite.Models;
using Microsoft.EntityFrameworkCore;

namespace RVSite.Data
{
    public class DataSeeder
    {
        public static void Seed(AppDbContext db)
        {
            // Ensure roles exist
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

            var adminRole = db.Role.First(r => r.Type == RoleType.Admin);
            var staffRole = db.Role.First(r => r.Type == RoleType.Staff);
            var customerRole = db.Role.First(r => r.Type == RoleType.Customer);

            // Seed Site Types
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
            var cabinType = db.SiteTypes.First(st => st.Name == "Cabin");

            // Seed Site Type Prices
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
                        EndDate = null,
                        Price = 45m
                    },
                    new SiteTypePrice
                    {
                        SiteTypeID = tentType.SiteTypeID,
                        StartDate = new DateTime(2024, 1, 1),
                        EndDate = null,
                        Price = 25m
                    },
                    new SiteTypePrice
                    {
                        SiteTypeID = cabinType.SiteTypeID,
                        StartDate = new DateTime(2024, 1, 1),
                        EndDate = null,
                        Price = 80m
                    }
                });

                db.SaveChanges();
            }

            // Seed Sites (2–3 per type)
            if (!db.Sites.Any())
            {
                db.Sites.AddRange(new[]
                {
                    // RV Sites
                    new Site { SiteNumber = "RV-01", SiteTypeID = rvType.SiteTypeID, SiteStatus = "Available", MaxRVLength = 40, BaseRate = 45m },
                    new Site { SiteNumber = "RV-02", SiteTypeID = rvType.SiteTypeID, SiteStatus = "Available", MaxRVLength = 35, BaseRate = 45m },
                    new Site { SiteNumber = "RV-03", SiteTypeID = rvType.SiteTypeID, SiteStatus = "Available", MaxRVLength = 50, BaseRate = 45m },

                    // Tent Sites
                    new Site { SiteNumber = "T-01", SiteTypeID = tentType.SiteTypeID, SiteStatus = "Available", MaxRVLength = 0, BaseRate = 25m },
                    new Site { SiteNumber = "T-02", SiteTypeID = tentType.SiteTypeID, SiteStatus = "Available", MaxRVLength = 0, BaseRate = 25m },

                    // Cabin Sites
                    new Site { SiteNumber = "C-01", SiteTypeID = cabinType.SiteTypeID, SiteStatus = "Available", MaxRVLength = 0, BaseRate = 80m },
                    new Site { SiteNumber = "C-02", SiteTypeID = cabinType.SiteTypeID, SiteStatus = "Available", MaxRVLength = 0, BaseRate = 80m }
                });

                db.SaveChanges();
            }

            // Seed Users
            if (!db.Users.Any())
            {
                db.Users.AddRange(new[]
                {
                    new User
                    {
                        FirstName = "Admin",
                        LastName = "User",
                        Email = "admin@rvpark.com",
                        PhoneNumber = "555-0001",
                        PasswordHash = "admin123",
                        MilitaryID = "A001",
                        BaseName = "Hill AFB",
                        Rank = "E-6",
                        RoleID = adminRole.RoleID
                    },
                    new User
                    {
                        FirstName = "Employee",
                        LastName = "User",
                        Email = "employee@rvpark.com",
                        PhoneNumber = "555-0002",
                        PasswordHash = "staff123",
                        MilitaryID = "S001",
                        BaseName = "Hill AFB",
                        Rank = "E-4",
                        RoleID = staffRole.RoleID
                    },
                    new User
                    {
                        FirstName = "Demo",
                        LastName = "Customer",
                        Email = "customer@rvpark.com",
                        PhoneNumber = "555-0003",
                        PasswordHash = "cust123",
                        MilitaryID = "C001",
                        BaseName = "Hill AFB",
                        Rank = "E-3",
                        RoleID = customerRole.RoleID
                    }
                });

                db.SaveChanges();
            }

            var demoCustomer = db.Users.First(u => u.Email == "customer@rvpark.com");
            var allSites = db.Sites.ToList();

            // Seed 12 Reservations
            if (!db.Reservations.Any())
            {
                var reservations = new List<Reservation>();
                var today = DateTime.Today;

                // Create varied reservations
                for (int i = 0; i < 12; i++)
                {
                    var site = allSites[i % allSites.Count];
                    var checkIn = today.AddDays(i - 6);   // some past, some future
                    var checkOut = checkIn.AddDays(3);

                    reservations.Add(new Reservation
                    {
                        UserID = demoCustomer.UserID,
                        SiteID = site.SiteID,
                        CheckInDate = checkIn,
                        CheckOutDate = checkOut,
                        NumberOfAdults = 2,
                        NumberOfChildren = i % 3,
                        NumberOfPets = i % 2,
                        ReservationStatus = (ReservationStatus)(i % 4), // rotates through statuses
                        TotalCost = site.BaseRate * 3
                    });
                }

                db.Reservations.AddRange(reservations);
                db.SaveChanges();
            }
        }
    }
}