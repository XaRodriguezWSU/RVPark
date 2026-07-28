using RVSite.Models;
using Microsoft.EntityFrameworkCore;

namespace RVSite.Data
{
    public class DataSeeder
    {
        public static void Seed(AppDbContext db, IWebHostEnvironment env)
        {
            // ---------------------------
            // 1. ROLES
            // ---------------------------
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

            // ---------------------------
            // 2. SITE TYPES
            // ---------------------------
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

            // ---------------------------
            // 3. SITE TYPE PRICES
            // ---------------------------
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

            // ---------------------------
            // 4. SITES (2–3 per type)
            // ---------------------------
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

            // ---------------------------
            // 5. USERS (Admin, Employee, Customer)
            // ---------------------------
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
                        PasswordHash = "admin123", // plain text for demo
                        MilitaryID = "A001",
                        BaseName = "Hill AFB",
                        Rank = "E-6",
                        RoleID = adminRole.RoleID,
                        EmailConfirmed = true,
                        IsLocked = false
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
                        RoleID = staffRole.RoleID,
                        EmailConfirmed = true,
                        IsLocked = false
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
                        RoleID = customerRole.RoleID,
                        EmailConfirmed = true,
                        IsLocked = false
                    }
                });

                db.SaveChanges();
            }

            var demoCustomer = db.Users.First(u => u.Email == "customer@rvpark.com");
            var allSites = db.Sites.ToList();

            // ---------------------------
            // 6. RESERVATIONS (12 varied)
            // ---------------------------
            if (!db.Reservations.Any())
            {
                var reservations = new List<Reservation>();
                var today = DateTime.Today;

                for (int i = 0; i < 12; i++)
                {
                    var site = allSites[i % allSites.Count];
                    var checkIn = today.AddDays(i - 6);   // mix past & future
                    var checkOut = checkIn.AddDays(3);

                    reservations.Add(new Reservation
                    {
                        UserID = demoCustomer.UserID,
                        SiteID = site.SiteID,
                        CheckInDate = checkIn,
                        CheckOutDate = checkOut,
                        ReservationDate = checkIn.AddDays(-10),
                        NumberOfAdults = 2,
                        NumberOfChildren = i % 3,
                        NumberOfPets = i % 2,
                        ReservationStatus = (ReservationStatus)(i % 4), // rotates statuses
                        TotalCost = site.BaseRate * 3,
                        BalanceDue = site.BaseRate * 3,
                        SpecialRequests = (i % 2 == 0) ? "Near restroom" : "Quiet area"
                    });
                }

                db.Reservations.AddRange(reservations);
                db.SaveChanges();
            }

            //// ---------------------------
            //// 7. SITE PHOTOS (seed references to existing files)
            //// ---------------------------
            //if (!db.SitePhoto.Any())
            //{
            //    var sites = db.Sites.ToList();
            //    var now = DateTime.Now;

            //    foreach (var site in sites)
            //    {
            //        // Build the physical folder path
            //        var folderPath = Path.Combine(env.WebRootPath, "storage", "photos", site.SiteID.ToString());

            //        if (!Directory.Exists(folderPath))
            //            continue; // No photos for this site

            //        // Get all JPG files in the folder
            //        var files = Directory.GetFiles(folderPath, "*.jpg");

            //        int sortOrder = 1;

            //        foreach (var file in files)
            //        {
            //            var fileName = Path.GetFileName(file);

            //            db.SitePhoto.Add(new SitePhoto
            //            {
            //                SiteID = site.SiteID,
            //                Caption = $"{site.SiteNumber} - Photo {sortOrder}",
            //                FilePath = $"/storage/photos/{site.SiteID}/{fileName}",
            //                SortOrder = sortOrder,
            //                UploadedAt = now
            //            });

            //            sortOrder++;
            //        }
            //    }

            //    db.SaveChanges();
            //}
        }
    }
}