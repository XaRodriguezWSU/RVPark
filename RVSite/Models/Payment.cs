using System.ComponentModel.DataAnnotations;

namespace RVSite.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        [Required]
        public decimal AmountPaid { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        public PaymentStatus Status { get; set; }

        public string? PaymentMethod { get; set; }

    }
}