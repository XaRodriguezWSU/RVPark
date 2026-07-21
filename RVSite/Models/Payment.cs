using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RVSite.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        [Required]
        public int ReservationID { get; set; }
        [ForeignKey("ReservationID")]
        public Reservation? Reservation { get; set; }

        [Required]
        public decimal AmountPaid { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        public PaymentStatus Status { get; set; }

        [Required]
        public PaymentMethodType Method { get; set; }

        // Stripe session id, check number, or manual-entry auth code
        public string? TransactionReference { get; set; }

        // Employee who recorded an offline payment. Null for Stripe payments.
        public int? ProcessedByUserID { get; set; }
    }
}