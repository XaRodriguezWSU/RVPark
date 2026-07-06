using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RVSite.Models
{
    public class Fee
    {
        [Key]
        public int FeeID { get; set; }

        [Required]
        public int ReservationID { get; set; }

        [ForeignKey("ReservationID")]
        public Reservation? Reservation { get; set; }

        [Required]
        [StringLength(50)]
        public string NameCode { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 9999.99)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }
    }
}