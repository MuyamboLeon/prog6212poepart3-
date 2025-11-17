using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using The_CMCS.Models;
using The_CMCS.Services;
using Microsoft.Extensions.Logging;

namespace The_CMCS.Controllers
{
    public class HRController : Controller
    {
        private readonly IClaimsService _claimsService;
        private readonly ILogger<HRController> _logger;

        public HRController(IClaimsService claimsService, ILogger<HRController> logger)
        {
            _claimsService = claimsService ?? throw new ArgumentNullException(nameof(claimsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult Dashboard()
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser?.Role != "HR")
                {
                    _logger.LogWarning("Unauthorized access attempt to HR Dashboard by user: {User}", currentUser?.Name);
                    return RedirectToAction("Login", "Home");
                }

                var allUsers = _claimsService.GetAllUsers();
                var allClaims = _claimsService.GetAllClaims();

                ViewBag.TotalUsers = allUsers.Count;
                ViewBag.TotalLecturers = allUsers.Count(u => u.Role == "Lecturer");
                ViewBag.TotalCoordinators = allUsers.Count(u => u.Role == "Coordinator");
                ViewBag.TotalManagers = allUsers.Count(u => u.Role == "Manager");
                ViewBag.TotalClaims = allClaims.Count;
                ViewBag.ApprovedClaims = allClaims.Count(c => c.Status == "Approved");
                ViewBag.PendingClaims = allClaims.Count(c => c.Status == "Pending");
                ViewBag.TotalAmount = allClaims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount);

                return View(allUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in HR Dashboard action");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return RedirectToAction("Error", "Home");
            }
        }

        // User Management
        public IActionResult ManageUsers()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            var allUsers = _claimsService.GetAllUsers();
            return View(allUsers);
        }

        public IActionResult CreateUser()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(User user)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                // Generate username if not provided
                if (string.IsNullOrEmpty(user.Username))
                {
                    user.Username = GenerateUsername(user.Name);
                }

                // Set default password if not provided
                if (string.IsNullOrEmpty(user.Password))
                {
                    user.Password = "Welcome123"; // Default password
                }

                var result = _claimsService.AddUser(user);
                if (result)
                {
                    TempData["SuccessMessage"] = $"User {user.Name} created successfully!";
                    return RedirectToAction("ManageUsers");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create user. Please try again.";
                }
            }

            return View(user);
        }

        public IActionResult EditUser(string id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = _claimsService.GetUserById(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(User user)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            if (ModelState.IsValid)
            {
                var result = _claimsService.UpdateUser(user);
                if (result)
                {
                    TempData["SuccessMessage"] = $"User {user.Name} updated successfully!";
                    return RedirectToAction("ManageUsers");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update user. Please try again.";
                }
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(string id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Invalid user ID.";
                return RedirectToAction("ManageUsers");
            }

            // Prevent HR from deleting themselves
            if (id == currentUser.Id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction("ManageUsers");
            }

            var result = _claimsService.DeleteUser(id);
            if (result)
            {
                TempData["SuccessMessage"] = "User deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            return RedirectToAction("ManageUsers");
        }

        // Reports and Invoices
        public IActionResult Reports()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            var allClaims = _claimsService.GetAllClaims();
            var approvedClaims = allClaims.Where(c => c.Status == "Approved").ToList();

            ViewBag.TotalApprovedClaims = approvedClaims.Count;
            ViewBag.TotalAmount = approvedClaims.Sum(c => c.TotalAmount);
            ViewBag.Departments = allClaims.Select(c => c.Department).Distinct().ToList();
            ViewBag.Months = allClaims.Select(c => c.Month).Distinct().ToList();

            return View(approvedClaims);
        }

        [HttpPost]
        public IActionResult GenerateMonthlyReport(string month, int year)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            try
            {
                var reports = _claimsService.GenerateMonthlyReport(month, year);
                if (reports.Any())
                {
                    TempData["SuccessMessage"] = $"Monthly report for {month} {year} generated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "No data available for the selected period.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly report");
                TempData["ErrorMessage"] = "An error occurred while generating the report.";
            }

            return RedirectToAction("Reports");
        }

        [HttpPost]
        public IActionResult GenerateDepartmentReport(string department)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            try
            {
                var reports = _claimsService.GenerateDepartmentReport(department);
                if (reports.Any())
                {
                    TempData["SuccessMessage"] = $"Department report for {department} generated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "No data available for the selected department.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating department report");
                TempData["ErrorMessage"] = "An error occurred while generating the report.";
            }

            return RedirectToAction("Reports");
        }

        public IActionResult DownloadInvoice(string claimId)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(claimId))
                return NotFound();

            var claim = _claimsService.GetClaimById(claimId);
            if (claim == null)
                return NotFound();

            try
            {
                var pdfData = _claimsService.GenerateInvoicePdf(claim);
                if (pdfData != null && pdfData.Length > 0)
                {
                    var fileName = $"Invoice_{claim.Id}_{DateTime.Now:yyyyMMddHHmmss}.txt"; // Using .txt for simulation
                    return File(pdfData, "text/plain", fileName);
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to generate invoice.";
                    return RedirectToAction("Reports");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice for claim {ClaimId}", claimId);
                TempData["ErrorMessage"] = "An error occurred while generating the invoice.";
                return RedirectToAction("Reports");
            }
        }

        public IActionResult BulkInvoices()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            var approvedClaims = _claimsService.GetAllClaims().Where(c => c.Status == "Approved").ToList();
            return View(approvedClaims);
        }

        [HttpPost]
        public IActionResult DownloadBulkInvoices(List<string> claimIds)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            if (claimIds == null || !claimIds.Any())
            {
                TempData["ErrorMessage"] = "No claims selected for download.";
                return RedirectToAction("BulkInvoices");
            }

            try
            {
                var invoicesContent = new List<string>();
                foreach (var claimId in claimIds)
                {
                    var claim = _claimsService.GetClaimById(claimId);
                    if (claim != null)
                    {
                        var pdfData = _claimsService.GenerateInvoicePdf(claim);
                        invoicesContent.Add($"--- Invoice for {claim.Id} ---");
                        invoicesContent.Add(System.Text.Encoding.UTF8.GetString(pdfData));
                        invoicesContent.Add("");
                    }
                }

                var combinedContent = string.Join(Environment.NewLine, invoicesContent);
                var fileName = $"Bulk_Invoices_{DateTime.Now:yyyyMMddHHmmss}.txt";
                return File(System.Text.Encoding.UTF8.GetBytes(combinedContent), "text/plain", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating bulk invoices");
                TempData["ErrorMessage"] = "An error occurred while generating bulk invoices.";
                return RedirectToAction("BulkInvoices");
            }
        }

        // System Overview
        public IActionResult SystemOverview()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "HR")
                return RedirectToAction("Login", "Home");

            var allUsers = _claimsService.GetAllUsers();
            var allClaims = _claimsService.GetAllClaims();

            var overview = new
            {
                TotalUsers = allUsers.Count,
                ActiveUsers = allUsers.Count(u => u.IsActive),
                TotalClaims = allClaims.Count,
                ApprovedClaims = allClaims.Count(c => c.Status == "Approved"),
                PendingClaims = allClaims.Count(c => c.Status == "Pending"),
                RejectedClaims = allClaims.Count(c => c.Status == "Rejected"),
                TotalAmount = allClaims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount),
                UsersByRole = allUsers.GroupBy(u => u.Role).ToDictionary(g => g.Key, g => g.Count()),
                ClaimsByDepartment = allClaims.GroupBy(c => c.Department).ToDictionary(g => g.Key, g => g.Count()),
                MonthlyBreakdown = allClaims.GroupBy(c => c.Month).ToDictionary(g => g.Key, g => g.Count())
            };

            return View(overview);
        }

        private string GenerateUsername(string fullName)
        {
            var names = fullName.Split(' ');
            var firstName = names[0].ToLower();
            var lastName = names.Length > 1 ? names[^1].ToLower() : "";

            var baseUsername = $"{firstName}.{lastName}";
            var username = baseUsername;
            var counter = 1;

            // Ensure unique username
            while (_claimsService.GetAllUsers().Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username;
        }

        private User GetCurrentUser()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(username))
                    return _claimsService.GetUserByUsername(username);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting current user");
                return null;
            }
        }
    }
}