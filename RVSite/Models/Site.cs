using System.ComponentModel.DataAnnotations;

namespace RVSite.Models
{
    public class Site
    {
        [Key]
        public int SiteID { get; set; }

        [Required, StringLength(20)]
        public string SiteNumber { get; set; }

        [Required, StringLength(50)]
        public int SiteTypeID { get; set; }
        public SiteType SiteType { get; set; }

        [Required, StringLength(50)]
        public string SiteStatus { get; set; }

        public int MaxRVLength { get; set; }

        [Required, Range(0, 9999)]
        public decimal BaseRate { get; set; }
    }
}
