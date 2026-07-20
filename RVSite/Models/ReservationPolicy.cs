using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RVSite.Models
{
    public class ReservationPolicy
    {
        [Key]
        public int ReservationPolicyID { get; set; }

        [Required]
        [Range(1, 365)]
        public int MaximumAdvanceBookingDays { get; set; } = 183;

        [NotMapped]
        [Display(Name = "Maximum Advance Booking (months)")]
        [Range(1, 24)]
        public int MaximumAdvanceBookingMonths
        {
            get
            {
                return (int)Math.Round(MaximumAdvanceBookingDays / 30.0);
            }
            set
            {
                MaximumAdvanceBookingDays = value * 30;
            }
        }

        [Required]
        [Range(1, 365)]
        public int PeakSeasonMaximumStayNights { get; set; } = 14;

        [Required]
        [Range(0, 365)]
        public int RequiredDaysAwayBeforeReturn { get; set; } = 14;

        [Required]
        [Range(0, 365)]
        public int LateCancellationWindowDays { get; set; } = 7;

        [StringLength(1000)]
        public string? GeneralPolicyNotes { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}

