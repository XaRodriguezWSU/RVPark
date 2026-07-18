using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Policy;

namespace RVSite.Models
{
    public class MaintenanceTask
    {
        [Key]
        public int MaintenanceTaskID { get; set; }

        [Required]
        public int SiteID { get; set; }

        [ForeignKey(nameof(SiteID))]
        public Site? Site { get; set; }

        [Required]
        public int CreatedByUserID { get; set; }

        [ForeignKey(nameof(CreatedByUserID))]
        public User? CreatedByUser { get; set; }

        public int? ClosedByUserID { get; set; }

        [ForeignKey(nameof(ClosedByUserID))]
        public User? ClosedByUser { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public MaintenanceTaskStatus Status { get; set; }
            = MaintenanceTaskStatus.Open;

        [Required]
        public MaintenanceTaskPriority Priority { get; set; }
            = MaintenanceTaskPriority.Regular;

        [Required]
        public MaintenanceTaskType TaskType { get; set; }
            = MaintenanceTaskType.Other;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }

        [StringLength(1000)]
        public string? CompletionNotes { get; set; }
    }
}