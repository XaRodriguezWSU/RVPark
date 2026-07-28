using System;
using System.Collections.Generic;

namespace RVSite.Models
{
    /// <summary>
    /// Represents the data required to display admin/reservation reports 
    /// includes report filters, summary metrics, and reservation results
    /// </summary>
    public class ReportsViewModel
    {
        // Filter selections
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ReportType { get; set; } = "Reservations";

        // Dashboard summary metrics
        public int ArrivalsCount { get; set; }
        public int DeparturesCount { get; set; }
        public decimal RevenueTotal { get; set; }
        public decimal OccupancyRate { get; set; }
        public int TotalSites { get; set; }
        public int OccupiedSites { get; set; }

        // Payment summary metrics
        public int PaymentCount { get; set; }
        public decimal PendingPaymentTotal { get; set; }
        public int PendingPaymentCount { get; set; }
        public int FailedPaymentCount { get; set; }
        public decimal OutstandingBalanceTotal { get; set; }
        public decimal FeeTotal { get; set; }


        // Report results
        public List<Reservation> Reservations { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
        public List<SiteUsageReportRow> SiteUsageRows { get; set; } = new();
    }

    public class SiteUsageReportRow
    {
        public int SiteID { get; set; }
        public string SiteNumber { get; set; } = string.Empty;
        public string SiteTypeName { get; set; } = string.Empty;
        public int ReservationCount { get; set; }
        public int ReservedNights { get; set; }
        public decimal RevenueTotal { get; set; }
    }
}