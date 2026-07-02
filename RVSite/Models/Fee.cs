using System.ComponentModel.DataAnnotations;

namespace RVSite.Models
{
    public class Fee
    {
        [Key]
        public int FeeID { get; set; }

        [Required]
        [StringLength(50)]
        public string NameCode { get; set; }

        [Required]
        public int Amount { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }
    }
}