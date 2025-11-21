using System;
using System.ComponentModel.DataAnnotations;

namespace The_CMCS.Models
{
    public class Claim
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string LecturerId { get; set; }

        [Required]
        public string LecturerName { get; set; }

        [Required]
        public string Department { get; set; }

        [Required]
        public string Month { get; set; }

        [Required]
        [Range(0.1, 180, ErrorMessage = "Hours worked must be between 0.1 and 180")]
        public decimal HoursWorked { get; set; }

        [Required]
        [Range(0.1, double.MaxValue, ErrorMessage = "Hourly rate must be greater than 0")]
        public decimal HourlyRate { get; set; }

        // Make TotalAmount settable for automation
        public decimal TotalAmount { get; set; }

        public string Description { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        // Automation fields
        public bool AutoCalculated { get; set; }
        public bool ValidationPassed { get; set; }
        public string ValidationErrors { get; set; }

        // Coordinator approval
        public bool CoordinatorApproved { get; set; }
        public string CoordinatorApprovedBy { get; set; }
        public DateTime? CoordinatorApprovedDate { get; set; }

        // Manager approval
        public bool ManagerApproved { get; set; }
        public string ManagerApprovedBy { get; set; }
        public DateTime? ManagerApprovedDate { get; set; }

        // Review tracking
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedDate { get; set; }

        public string RejectionReason { get; set; }

        public List<SupportingDocument> Documents { get; set; } = new List<SupportingDocument>();
    }

    public class ClaimValidationRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RuleName { get; set; }
        public string Description { get; set; }
        public decimal MaxHours { get; set; } = 180;
        public decimal MinHourlyRate { get; set; } = 0;
        public decimal MaxHourlyRate { get; set; } = 1000;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int MinHours { get; internal set; }
    }

    public class AutomatedReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ReportType { get; set; } // Monthly, Department, Custom
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; }
        public byte[] ReportData { get; set; }
        public string Parameters { get; set; } // JSON string for report parameters
    }
}