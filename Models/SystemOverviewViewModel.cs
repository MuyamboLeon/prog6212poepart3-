// Models/SystemOverviewViewModel.cs
using System.Collections.Generic;

namespace The_CMCS.Models
{
    public class SystemOverviewViewModel
    {
        // User Statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int RecentUsers { get; set; } // Last 30 days

        // Claim Statistics
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int PendingClaims { get; set; }
        public int RejectedClaims { get; set; }
        public int RecentClaims { get; set; } // Last 30 days

        // Financial Statistics
        public decimal TotalAmount { get; set; }
        public decimal AverageClaimAmount { get; set; }
        public decimal HighestClaimAmount { get; set; }
        public int RecentApprovals { get; set; } // Last 30 days

        // Distributions
        public List<RoleStat> UsersByRole { get; set; } = new List<RoleStat>();
        public List<DepartmentStat> ClaimsByDepartment { get; set; } = new List<DepartmentStat>();
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new List<MonthlyTrend>();

        // System Health
        public string SystemUptime { get; set; }
        public int ActiveSessions { get; set; }
        public string StorageUsage { get; set; }
    }

    public class RoleStat
    {
        public string Role { get; set; }
        public int Count { get; set; }
        public int ActiveCount { get; set; }
    }

    public class DepartmentStat
    {
        public string Department { get; set; }
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int PendingClaims { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class MonthlyTrend
    {
        public string Period { get; set; }
        public int ClaimsCount { get; set; }
        public decimal ApprovedAmount { get; set; }
    }
}