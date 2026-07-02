using System.ComponentModel.DataAnnotations;

namespace RVSite.Models
{
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required]
        public RoleType Type { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();

        public bool CanSubmitStatusChange()
        {
            return Type == RoleType.Admin || Type == RoleType.Staff;
        }

        public string GetRoleName()
        {
            return Type.ToString();
        }
    }
}