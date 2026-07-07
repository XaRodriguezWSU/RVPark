using System.ComponentModel.DataAnnotations;

namespace RVSite.Models
{
    public class SiteType
    {
        public int SiteTypeID { get; set; }

        [Required]
        public string Name { get; set; }   // RV, Tent, Cabin, etc.

        public ICollection<SiteTypePrice> Prices { get; set; } = new List<SiteTypePrice>();
        public ICollection<Site> Sites { get; set; } = new List<Site>();
    }
}