using System.ComponentModel.DataAnnotations;

namespace RVSite.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        public int RoleID { get; set; }

        public Role? Role { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public bool EmailConfirmed { get; set; } = false;

        public string? EmailConfirmationToken { get; set; }


        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();


        public bool HasRole(string roleName)
        {
            return Role?.Type.ToString() == roleName;
        }

        public bool IsLocked { get; set; } = false;
    }
}