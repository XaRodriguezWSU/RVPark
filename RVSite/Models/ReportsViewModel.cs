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

        // Report results
        public List<Reservation> Reservations { get; set; } = new();
    }
}