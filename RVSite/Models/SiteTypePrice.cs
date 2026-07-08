using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RVSite.Models
{
    public class SiteTypePrice
    {
        public int SiteTypePriceID { get; set; }

        [Required]
        public int SiteTypeID { get; set; }
        public SiteType? SiteType { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }   // NULL = current price

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
    }
}
