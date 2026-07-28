using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RVSite.Models
{
    public class SpecialEventPolicy
    {
        [Key]
        public int SpecialEventPolicyID { get; set; }

        [Required]
        [StringLength(100)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public int? SiteTypeID { get; set; }

        [ForeignKey("SiteTypeID")]
        public SiteType? SiteType { get; set; }

        [Range(1, 365)]
        public int? MaximumStayNights { get; set; }

        [Range(0, 365)]
        public int? CancellationWindowDays { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}