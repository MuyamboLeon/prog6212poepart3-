using The_CMCS.Models;
using System.Globalization;

namespace The_CMCS.Services
{
    public class ClaimsService : IClaimsService
    {
        private static List<Claim> _claims = new List<Claim>();
        private static List<User> _users = new List<User>();
        private static List<SupportingDocument> _documents = new List<SupportingDocument>();
        private static List<Report> _reports = new List<Report>();

        static ClaimsService()
        {
            InitializeSampleData();
        }

        private static void InitializeSampleData()
        {
            // Initialize sample users with HR
            _users.AddRange(new List<User>
            {
                new User {
                    Id = "hr1",
                    Username = "hr",
                    Password = "password",
                    Name = "HR Manager",
                    Role = "HR",
                    Department = "Human Resources",
                    Email = "hr@university.ac.za",
                    PhoneNumber = "+27 11 123 4567",
                    HourlyRate = 0,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                },
                new User {
                    Id = "lecturer1",
                    Username = "Mr.Leon",
                    Password = "password",
                    Name = "Dr. Leon",
                    Role = "Lecturer",
                    Department = "Computer Science",
                    Email = "leon@university.ac.za",
                    PhoneNumber = "+27 11 123 4568",
                    HourlyRate = 320,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                },
                new User {
                    Id = "lecturer2",
                    Username = "Ms Lerato",
                    Password = "password",
                    Name = "Prof. Lerato",
                    Role = "Lecturer",
                    Department = "Mathematics",
                    Email = "lerato@university.ac.za",
                    PhoneNumber = "+27 11 123 4569",
                    HourlyRate = 350,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                },
                new User {
                    Id = "lecturer3",
                    Username = "Thato Mollo",
                    Password = "password",
                    Name = "Dr. Thato Mollo",
                    Role = "Lecturer",
                    Department = "Engineering",
                    Email = "thato@university.ac.za",
                    PhoneNumber = "+27 11 123 4570",
                    HourlyRate = 380,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                },
                new User {
                    Id = "coordinator1",
                    Username = "coordinator",
                    Password = "password",
                    Name = "Mr. Coordinator",
                    Role = "Coordinator",
                    Department = "Computer Science",
                    Email = "coordinator@university.ac.za",
                    PhoneNumber = "+27 11 123 4571",
                    HourlyRate = 0,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                },
                new User {
                    Id = "coordinator2",
                    Username = "math.coordinator",
                    Password = "password",
                    Name = "Ms. Mathematics Coordinator",
                    Role = "Coordinator",
                    Department = "Mathematics",
                    Email = "math.coordinator@university.ac.za",
                    PhoneNumber = "+27 11 123 4572",
                    HourlyRate = 0,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                },
                new User {
                    Id = "manager1",
                    Username = "manager",
                    Password = "password",
                    Name = "Ms. Manager",
                    Role = "Manager",
                    Department = "All",
                    Email = "manager@university.ac.za",
                    PhoneNumber = "+27 11 123 4573",
                    HourlyRate = 0,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                }
            });

            // Initialize some sample claims for testing
            _claims.AddRange(new List<Claim>
            {
                new Claim {
                    Id = "CLM-001",
                    LecturerId = "lecturer1",
                    LecturerName = "Dr. Leon",
                    Month = "January",
                    Department = "Computer Science",
                    HoursWorked = 40,
                    HourlyRate = 320,
                    Description = "Lecture hours for January",
                    Status = "Approved",
                    SubmittedDate = DateTime.Now.AddDays(-30),
                    CoordinatorApproved = true,
                    CoordinatorApprovedBy = "Mr. Coordinator",
                    CoordinatorApprovedDate = DateTime.Now.AddDays(-25),
                    ManagerApproved = true,
                    ManagerApprovedBy = "Ms. Manager",
                    ManagerApprovedDate = DateTime.Now.AddDays(-20)
                },
                new Claim {
                    Id = "CLM-002",
                    LecturerId = "lecturer2",
                    LecturerName = "Prof. Lerato",
                    Month = "January",
                    Department = "Mathematics",
                    HoursWorked = 35,
                    HourlyRate = 350,
                    Description = "Tutorial sessions",
                    Status = "Pending",
                    SubmittedDate = DateTime.Now.AddDays(-15),
                    CoordinatorApproved = true,
                    CoordinatorApprovedBy = "Ms. Mathematics Coordinator",
                    CoordinatorApprovedDate = DateTime.Now.AddDays(-10),
                    ManagerApproved = false
                },
                new Claim {
                    Id = "CLM-003",
                    LecturerId = "lecturer3",
                    LecturerName = "Dr. Thato Mollo",
                    Month = "February",
                    Department = "Engineering",
                    HoursWorked = 45,
                    HourlyRate = 380,
                    Description = "Laboratory supervision",
                    Status = "Pending",
                    SubmittedDate = DateTime.Now.AddDays(-5),
                    CoordinatorApproved = false,
                    ManagerApproved = false
                }
            });
        }

        // Claim Methods
        public List<Claim> GetAllClaims() => _claims.OrderByDescending(c => c.SubmittedDate).ToList();

        public List<Claim> GetClaimsByUser(string userId) =>
            _claims.Where(c => c.LecturerId == userId).OrderByDescending(c => c.SubmittedDate).ToList();

        public List<Claim> GetPendingClaims()
        {
            // For coordinators: claims that haven't been reviewed by coordinator
            // For managers: claims that are coordinator-approved but pending manager review
            var currentUser = GetCurrentUser();
            if (currentUser?.Role == "Coordinator")
            {
                return GetAllClaims().Where(c => c.Status == "Pending" && !c.CoordinatorApproved).ToList();
            }
            else if (currentUser?.Role == "Manager")
            {
                return GetClaimsPendingManagerReview();
            }

            // Default: return all pending claims
            return _claims.Where(c => c.Status == "Pending").OrderByDescending(c => c.SubmittedDate).ToList();
        }

        public List<Claim> GetClaimsByDepartment(string department)
        {
            if (department == "All")
                return _claims.OrderByDescending(c => c.SubmittedDate).ToList();

            return _claims.Where(c => c.Department == department).OrderByDescending(c => c.SubmittedDate).ToList();
        }

        public Claim? GetClaimById(string claimId) =>
            _claims.FirstOrDefault(c => c.Id == claimId);

        public bool AddClaim(Claim claim)
        {
            try
            {
                // Generate ID if not provided
                if (string.IsNullOrEmpty(claim.Id))
                {
                    claim.Id = "CLM-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                }

                // Set submitted date if not provided
                if (claim.SubmittedDate == default)
                {
                    claim.SubmittedDate = DateTime.Now;
                }

                // Auto-calculate total amount
                claim.TotalAmount = claim.HoursWorked * claim.HourlyRate;

                _claims.Add(claim);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateClaim(string claimId, Claim updatedClaim)
        {
            var existingClaim = _claims.FirstOrDefault(c => c.Id == claimId);
            if (existingClaim != null)
            {
                _claims.Remove(existingClaim);
                _claims.Add(updatedClaim);
                return true;
            }
            return false;
        }

        public bool UpdateClaim(Claim existingClaim)
        {
            try
            {
                var claimInList = _claims.FirstOrDefault(c => c.Id == existingClaim.Id);
                if (claimInList != null)
                {
                    // Update the claim properties
                    claimInList.Month = existingClaim.Month;
                    claimInList.Department = existingClaim.Department;
                    claimInList.HoursWorked = existingClaim.HoursWorked;
                    claimInList.HourlyRate = existingClaim.HourlyRate;
                    claimInList.Description = existingClaim.Description;
                    // Auto-calculate total amount
                    claimInList.TotalAmount = existingClaim.HoursWorked * existingClaim.HourlyRate;

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating claim: {ex.Message}");
                return false;
            }
        }

        public bool DeleteClaim(string claimId)
        {
            var claim = _claims.FirstOrDefault(c => c.Id == claimId);
            if (claim != null)
            {
                _claims.Remove(claim);
                return true;
            }
            return false;
        }

        // Approval Workflow Methods
        public bool ApproveClaim(string claimId, string reviewedBy)
        {
            var claim = GetClaimById(claimId);
            if (claim == null)
                return false;

            // If coordinator hasn't approved yet, use coordinator approval
            if (!claim.CoordinatorApproved)
            {
                return ApproveByCoordinator(claimId, reviewedBy);
            }
            // Otherwise use manager approval
            else
            {
                return ApproveByManager(claimId, reviewedBy);
            }
        }

        public bool RejectClaim(string claimId, string reviewedBy, string reason)
        {
            var claim = GetClaimById(claimId);
            if (claim == null)
                return false;

            // If coordinator hasn't approved yet, use coordinator rejection
            if (!claim.CoordinatorApproved)
            {
                return RejectByCoordinator(claimId, reviewedBy, reason);
            }
            // Otherwise use manager rejection
            else
            {
                return RejectByManager(claimId, reviewedBy, reason);
            }
        }

        public bool ApproveByCoordinator(string claimId, string approvedBy)
        {
            var claim = GetClaimById(claimId);
            if (claim == null || claim.Status != "Pending")
                return false;

            claim.CoordinatorApproved = true;
            claim.CoordinatorApprovedBy = approvedBy;
            claim.CoordinatorApprovedDate = DateTime.Now;

            // Status remains "Pending" until manager reviews
            claim.Status = "Pending";

            return UpdateClaim(claim);
        }

        public bool RejectByCoordinator(string claimId, string rejectedBy, string rejectionReason)
        {
            var claim = GetClaimById(claimId);
            if (claim == null || claim.Status != "Pending")
                return false;

            claim.CoordinatorApproved = false;
            claim.CoordinatorApprovedBy = rejectedBy;
            claim.CoordinatorApprovedDate = DateTime.Now;
            claim.Status = "Rejected";
            claim.ReviewedBy = rejectedBy;
            claim.ReviewedDate = DateTime.Now;
            claim.RejectionReason = rejectionReason;

            return UpdateClaim(claim);
        }

        public bool ApproveByManager(string claimId, string approvedBy)
        {
            var claim = GetClaimById(claimId);
            if (claim == null || claim.Status != "Pending" || !claim.CoordinatorApproved)
                return false;

            claim.ManagerApproved = true;
            claim.ManagerApprovedBy = approvedBy;
            claim.ManagerApprovedDate = DateTime.Now;
            claim.Status = "Approved";
            claim.ReviewedBy = approvedBy;
            claim.ReviewedDate = DateTime.Now;

            return UpdateClaim(claim);
        }

        public bool RejectByManager(string claimId, string rejectedBy, string rejectionReason)
        {
            var claim = GetClaimById(claimId);
            if (claim == null || claim.Status != "Pending" || !claim.CoordinatorApproved)
                return false;

            claim.ManagerApproved = false;
            claim.ManagerApprovedBy = rejectedBy;
            claim.ManagerApprovedDate = DateTime.Now;
            claim.Status = "Rejected";
            claim.ReviewedBy = rejectedBy;
            claim.ReviewedDate = DateTime.Now;
            claim.RejectionReason = rejectionReason;

            return UpdateClaim(claim);
        }

        public List<Claim> GetClaimsPendingManagerReview()
        {
            return GetAllClaims()
                .Where(c => c.Status == "Pending" && c.CoordinatorApproved)
                .ToList();
        }

        // User Management Methods
        public List<User> GetAllUsers() => _users;

        public User? GetUserByUsername(string username) =>
            _users.FirstOrDefault(u => u.Username == username);

        public User? GetUserById(string userId) =>
            _users.FirstOrDefault(u => u.Id == userId);

        public bool AddUser(User user)
        {
            try
            {
                // Generate ID if not provided
                if (string.IsNullOrEmpty(user.Id))
                {
                    user.Id = Guid.NewGuid().ToString();
                }

                // Set created date
                user.CreatedDate = DateTime.Now;

                // Set default active status
                user.IsActive = true;

                _users.Add(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateUser(User user)
        {
            try
            {
                var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
                if (existingUser != null)
                {
                    existingUser.Username = user.Username;
                    existingUser.Name = user.Name;
                    existingUser.Role = user.Role;
                    existingUser.Department = user.Department;
                    existingUser.HourlyRate = user.HourlyRate;
                    existingUser.Email = user.Email;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.IsActive = user.IsActive;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteUser(string userId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                _users.Remove(user);
                return true;
            }
            return false;
        }

        // Document Methods
        public List<SupportingDocument> GetDocumentsByClaimId(string claimId) =>
            _documents.Where(d => d.ClaimId == claimId).ToList();

        public SupportingDocument? GetDocumentById(string documentId) =>
            _documents.FirstOrDefault(d => d.Id == documentId);

        public bool AddDocument(SupportingDocument document)
        {
            _documents.Add(document);
            return true;
        }

        // Enhanced Claim Creation with Auto-calculation and Validation
        public bool CreateClaim(Claim claim, List<IFormFile> supportingDocuments)
        {
            try
            {
                // Get lecturer details
                var lecturer = GetUserById(claim.LecturerId);
                if (lecturer == null)
                    return false;

                // Auto-populate lecturer details from HR data
                claim.LecturerName = lecturer.Name;
                claim.Department = lecturer.Department;
                claim.HourlyRate = lecturer.HourlyRate;

                // Validation: Maximum hours per month (180 hours)
                if (claim.HoursWorked > 180)
                {
                    throw new InvalidOperationException("Hours worked cannot exceed 180 hours per month.");
                }

                // Validation: Ensure positive values
                if (claim.HoursWorked <= 0)
                {
                    throw new InvalidOperationException("Hours worked must be greater than zero.");
                }

                // Auto-calculation of total amount
                claim.TotalAmount = claim.HoursWorked * claim.HourlyRate;

                // Generate unique ID for the claim
                if (string.IsNullOrEmpty(claim.Id))
                {
                    claim.Id = "CLM-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                }

                // Ensure submitted date is set
                if (claim.SubmittedDate == default)
                {
                    claim.SubmittedDate = DateTime.Now;
                }

                // Ensure status is set
                if (string.IsNullOrEmpty(claim.Status))
                {
                    claim.Status = "Pending";
                }

                // Initialize workflow properties
                claim.CoordinatorApproved = false;
                claim.ManagerApproved = false;

                // Add the claim to the list
                _claims.Add(claim);

                // Process supporting documents if any
                if (supportingDocuments != null && supportingDocuments.Count > 0)
                {
                    foreach (var file in supportingDocuments)
                    {
                        if (file != null && file.Length > 0)
                        {
                            var document = new SupportingDocument
                            {
                                Id = Guid.NewGuid().ToString(),
                                ClaimId = claim.Id,
                                FileName = file.FileName,
                                FileType = file.ContentType,
                                UploadDate = DateTime.Now
                            };

                            // Read file content into byte array
                            using (var memoryStream = new MemoryStream())
                            {
                                file.CopyTo(memoryStream);
                                document.FileData = memoryStream.ToArray();
                            }

                            _documents.Add(document);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating claim: {ex.Message}");
                return false;
            }
        }

        // Report Generation Methods
        public List<Report> GenerateMonthlyReport(string month, int year)
        {
            var claims = _claims.Where(c =>
                c.Month.Equals(month, StringComparison.OrdinalIgnoreCase) &&
                c.SubmittedDate.Year == year &&
                c.Status == "Approved").ToList();

            var report = new Report
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"Monthly Report - {month} {year}",
                Type = "Monthly",
                GeneratedBy = "System",
                GeneratedDate = DateTime.Now,
                Data = new Dictionary<string, object>
                {
                    ["TotalClaims"] = claims.Count,
                    ["TotalAmount"] = claims.Sum(c => c.TotalAmount),
                    ["DepartmentBreakdown"] = claims.GroupBy(c => c.Department)
                        .Select(g => new { Department = g.Key, Count = g.Count(), Amount = g.Sum(c => c.TotalAmount) }),
                    ["LecturerBreakdown"] = claims.GroupBy(c => c.LecturerName)
                        .Select(g => new { Lecturer = g.Key, Count = g.Count(), Amount = g.Sum(c => c.TotalAmount) })
                }
            };

            _reports.Add(report);
            return new List<Report> { report };
        }

        public List<Report> GenerateDepartmentReport(string department)
        {
            var claims = _claims.Where(c =>
                c.Department.Equals(department, StringComparison.OrdinalIgnoreCase) &&
                c.Status == "Approved").ToList();

            var report = new Report
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"Department Report - {department}",
                Type = "Department",
                GeneratedBy = "System",
                GeneratedDate = DateTime.Now,
                Data = new Dictionary<string, object>
                {
                    ["TotalClaims"] = claims.Count,
                    ["TotalAmount"] = claims.Sum(c => c.TotalAmount),
                    ["MonthlyBreakdown"] = claims.GroupBy(c => c.Month)
                        .Select(g => new { Month = g.Key, Count = g.Count(), Amount = g.Sum(c => c.TotalAmount) }),
                    ["LecturerPerformance"] = claims.GroupBy(c => c.LecturerName)
                        .Select(g => new { Lecturer = g.Key, TotalHours = g.Sum(c => c.HoursWorked), TotalAmount = g.Sum(c => c.TotalAmount) })
                }
            };

            _reports.Add(report);
            return new List<Report> { report };
        }

        public byte[] GenerateInvoicePdf(Claim claim)
        {
            // Enhanced PDF generation simulation with better formatting
            var invoiceContent = $@"
ACADEMIC CLAIMS MANAGEMENT SYSTEM
==================================
INVOICE
==================================
Invoice Number: {claim.Id}
Date Generated: {DateTime.Now:yyyy-MM-dd HH:mm}

LECTURER INFORMATION:
-------------------
Name: {claim.LecturerName}
Department: {claim.Department}

CLAIM DETAILS:
-------------
Month: {claim.Month}
Hours Worked: {claim.HoursWorked}
Hourly Rate: R{claim.HourlyRate:N2}
Total Amount: R{claim.TotalAmount:N2}

Status: {claim.Status}
Submitted: {claim.SubmittedDate:yyyy-MM-dd}

APPROVAL DETAILS:
----------------
Coordinator Approved: {(claim.CoordinatorApproved ? "Yes" : "No")}
{(claim.CoordinatorApproved ? $"By: {claim.CoordinatorApprovedBy} on {claim.CoordinatorApprovedDate:yyyy-MM-dd}" : "")}

Manager Approved: {(claim.ManagerApproved ? "Yes" : "No")}
{(claim.ManagerApproved ? $"By: {claim.ManagerApprovedBy} on {claim.ManagerApprovedDate:yyyy-MM-dd}" : "")}

Description: {claim.Description}

==================================
Thank you for using CMCS
==================================
";

            return System.Text.Encoding.UTF8.GetBytes(invoiceContent);
        }

        public List<Report> GetGeneratedReports()
        {
            return _reports.OrderByDescending(r => r.GeneratedDate).ToList();
        }

        // Bulk Operations for HR
        public List<byte[]> GenerateBulkInvoices(List<string> claimIds)
        {
            var invoices = new List<byte[]>();
            foreach (var claimId in claimIds)
            {
                var claim = GetClaimById(claimId);
                if (claim != null && claim.Status == "Approved")
                {
                    invoices.Add(GenerateInvoicePdf(claim));
                }
            }
            return invoices;
        }

        // System Analytics for HR Dashboard
        public Dictionary<string, object> GetSystemOverview()
        {
            var allClaims = GetAllClaims();
            var allUsers = GetAllUsers();

            return new Dictionary<string, object>
            {
                ["TotalUsers"] = allUsers.Count,
                ["ActiveUsers"] = allUsers.Count(u => u.IsActive),
                ["TotalClaims"] = allClaims.Count,
                ["ApprovedClaims"] = allClaims.Count(c => c.Status == "Approved"),
                ["PendingClaims"] = allClaims.Count(c => c.Status == "Pending"),
                ["RejectedClaims"] = allClaims.Count(c => c.Status == "Rejected"),
                ["TotalAmount"] = allClaims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount),
                ["UsersByRole"] = allUsers.GroupBy(u => u.Role).ToDictionary(g => g.Key, g => g.Count()),
                ["ClaimsByDepartment"] = allClaims.GroupBy(c => c.Department).ToDictionary(g => g.Key, g => g.Count()),
                ["MonthlyBreakdown"] = allClaims.GroupBy(c => c.Month).ToDictionary(g => g.Key, g => g.Count()),
                ["AverageClaimAmount"] = allClaims.Where(c => c.Status == "Approved").Average(c => c.TotalAmount),
                ["TopLecturers"] = allClaims.Where(c => c.Status == "Approved")
                    .GroupBy(c => c.LecturerName)
                    .Select(g => new { Lecturer = g.Key, TotalAmount = g.Sum(c => c.TotalAmount) })
                    .OrderByDescending(x => x.TotalAmount)
                    .Take(5)
            };
        }

        // Helper method for current user (to be implemented with proper authentication)
        private User? GetCurrentUser()
        {
            // This should be implemented based on your authentication system
            // For now, returning null as a placeholder - actual implementation
            // would get the current user from HttpContext or similar
            return null;
        }

        // Additional helper methods for validation
        public bool ValidateClaimHours(decimal hoursWorked)
        {
            return hoursWorked > 0 && hoursWorked <= 180;
        }

        public bool ValidateLecturerHourlyRate(string lecturerId, decimal hourlyRate)
        {
            var lecturer = GetUserById(lecturerId);
            return lecturer != null && lecturer.HourlyRate == hourlyRate;
        }

        // Method to get claims for specific time period
        public List<Claim> GetClaimsByPeriod(DateTime startDate, DateTime endDate)
        {
            return _claims.Where(c => c.SubmittedDate >= startDate && c.SubmittedDate <= endDate)
                         .OrderByDescending(c => c.SubmittedDate)
                         .ToList();
        }

        // Method to get department statistics
        public Dictionary<string, object> GetDepartmentStatistics(string department)
        {
            var departmentClaims = GetClaimsByDepartment(department);

            return new Dictionary<string, object>
            {
                ["TotalClaims"] = departmentClaims.Count,
                ["ApprovedClaims"] = departmentClaims.Count(c => c.Status == "Approved"),
                ["PendingClaims"] = departmentClaims.Count(c => c.Status == "Pending"),
                ["TotalAmount"] = departmentClaims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount),
                ["AverageHours"] = departmentClaims.Average(c => c.HoursWorked),
                ["LecturerCount"] = departmentClaims.Select(c => c.LecturerId).Distinct().Count()
            };
        }
    }
}