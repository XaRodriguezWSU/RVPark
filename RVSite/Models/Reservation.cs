using System.ComponentModel.DataAnnotations;

namespace RVSite.Models

public class Reservation
{
	public Reservation()
	{
        [Key]
        public int ReservationID { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        [Required]
        public int SiteID { get; set; }

        [ForeignKey("SiteID")]
        public Site? Site { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Range(1, 20)]
        public int NumberOfAdults { get; set; }

        [Range(0, 20)]
        public int NumberOfChildren { get; set; }

        [Range(0, 20)]
        public int NumberOfPets { get; set; }

        [StringLength(500)]
        public string? SpecialRequests { get; set; }

        [Required]
        [StringLength(30)]
        public string ReservationStatus { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalCost { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal BalanceDue { get; set; }

        public DateTime ReservationDate { get; set; } = DateTime.Now;
    }
}
