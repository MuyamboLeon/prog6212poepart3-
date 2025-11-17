using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using The_CMCS.Models;
using The_CMCS.Services;
using Microsoft.Extensions.Logging;

namespace The_CMCS.Controllers
{
    public class AcademicManagersController : Controller
    {
        private readonly IClaimsService _claimsService;
        private readonly ILogger<AcademicManagersController> _logger;

        public AcademicManagersController(IClaimsService claimsService, ILogger<AcademicManagersController> logger)
        {
            _claimsService = claimsService ?? throw new ArgumentNullException(nameof(claimsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult Dashboard()
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser?.Role != "Manager")
                {
                    _logger.LogWarning("Unauthorized access attempt to Dashboard by user: {User}", currentUser?.Name);
                    return RedirectToAction("Login", "Home");
                }

                var allClaims = _claimsService.GetAllClaims();
                var pendingManagerReview = _claimsService.GetClaimsPendingManagerReview();

                ViewBag.PendingCount = pendingManagerReview.Count;
                ViewBag.ApprovedCount = allClaims.Count(c => c.Status == "Approved");
                ViewBag.RejectedCount = allClaims.Count(c => c.Status == "Rejected");
                ViewBag.TotalAmount = allClaims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount);
                ViewBag.RecentClaims = allClaims.OrderByDescending(c => c.SubmittedDate).Take(5).ToList();
                ViewBag.AllClaims = allClaims;
                ViewBag.DepartmentOverview = allClaims
                    .GroupBy(c => c.Department)
                    .ToDictionary(g => g.Key, g => g.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount));

                return View(allClaims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Dashboard action");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return RedirectToAction("Error", "Home");
            }
        }

        public IActionResult ReviewClaims()
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser?.Role != "Manager")
                {
                    _logger.LogWarning("Unauthorized access attempt to ReviewClaims by user: {User}", currentUser?.Name);
                    return RedirectToAction("Login", "Home");
                }

                var pendingClaims = _claimsService.GetClaimsPendingManagerReview();
                return View(pendingClaims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ReviewClaims action");
                TempData["ErrorMessage"] = "An error occurred while loading claims for review.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveClaim(string id)
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser?.Role != "Manager")
                {
                    _logger.LogWarning("Unauthorized access attempt to ApproveClaim by user: {User}", currentUser?.Name);
                    return RedirectToAction("Login", "Home");
                }

                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid claim ID.";
                    return RedirectToAction("ReviewClaims");
                }

                var result = _claimsService.ApproveByManager(id, currentUser.Name);
                if (result)
                {
                    _logger.LogInformation("Claim {ClaimId} approved by manager {ManagerName}", id, currentUser.Name);
                    TempData["SuccessMessage"] = "Claim approved successfully!";
                }
                else
                {
                    _logger.LogWarning("Failed to approve claim {ClaimId} by manager {ManagerName}", id, currentUser.Name);
                    TempData["ErrorMessage"] = "Failed to approve claim. The claim may have already been processed or not approved by coordinator.";
                }

                return RedirectToAction("ReviewClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while approving claim {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while approving the claim.";
                return RedirectToAction("ReviewClaims");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectClaim(string id, string rejectionReason)
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser?.Role != "Manager")
                {
                    _logger.LogWarning("Unauthorized access attempt to RejectClaim by user: {User}", currentUser?.Name);
                    return RedirectToAction("Login", "Home");
                }

                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid claim ID.";
                    return RedirectToAction("ReviewClaims");
                }

                if (string.IsNullOrEmpty(rejectionReason) || rejectionReason.Trim().Length == 0)
                {
                    TempData["ErrorMessage"] = "Rejection reason is required.";
                    return RedirectToAction("ReviewClaims");
                }

                var result = _claimsService.RejectByManager(id, currentUser.Name, rejectionReason.Trim());
                if (result)
                {
                    _logger.LogInformation("Claim {ClaimId} rejected by manager {ManagerName}", id, currentUser.Name);
                    TempData["SuccessMessage"] = "Claim rejected successfully!";
                }
                else
                {
                    _logger.LogWarning("Failed to reject claim {ClaimId} by manager {ManagerName}", id, currentUser.Name);
                    TempData["ErrorMessage"] = "Failed to reject claim. The claim may have already been processed or not approved by coordinator.";
                }

                return RedirectToAction("ReviewClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rejecting claim {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while rejecting the claim.";
                return RedirectToAction("ReviewClaims");
            }
        }

        public IActionResult AllClaims()
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser?.Role != "Manager")
                {
                    _logger.LogWarning("Unauthorized access attempt to AllClaims by user: {User}", currentUser?.Name);
                    return RedirectToAction("Login", "Home");
                }

                var allClaims = _claimsService.GetAllClaims();
                return View(allClaims.OrderByDescending(c => c.SubmittedDate).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in AllClaims action");
                TempData["ErrorMessage"] = "An error occurred while loading all claims.";
                return RedirectToAction("Dashboard");
            }
        }

        public IActionResult ClaimDetails(string id)
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser?.Role != "Manager")
                {
                    _logger.LogWarning("Unauthorized access attempt to ClaimDetails by user: {User}", currentUser?.Name);
                    return RedirectToAction("Login", "Home");
                }

                if (string.IsNullOrEmpty(id))
                    return NotFound();

                var claim = _claimsService.GetClaimById(id);
                if (claim == null)
                    return NotFound();

                var documents = _claimsService.GetDocumentsByClaimId(id);
                claim.Documents = documents;

                return View(claim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading claim details for claim {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading claim details.";
                return RedirectToAction("AllClaims");
            }
        }

        public IActionResult Reports()
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser?.Role != "Manager")
                {
                    _logger.LogWarning("Unauthorized access attempt to Reports by user: {User}", currentUser?.Name);
                    return RedirectToAction("Login", "Home");
                }

                var allClaims = _claimsService.GetAllClaims();

                var reportData = new
                {
                    TotalClaims = allClaims.Count,
                    ApprovedClaims = allClaims.Count(c => c.Status == "Approved"),
                    PendingClaims = allClaims.Count(c => c.Status == "Pending"),
                    RejectedClaims = allClaims.Count(c => c.Status == "Rejected"),
                    TotalAmount = allClaims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount),
                    ClaimsByDepartment = allClaims.GroupBy(c => c.Department)
                        .Select(g => new { Department = g.Key, Count = g.Count() }),
                    MonthlyData = allClaims.Where(c => c.Status == "Approved")
                        .GroupBy(c => c.Month)
                        .Select(g => new { Month = g.Key, Amount = g.Sum(c => c.TotalAmount) })
                        .OrderBy(x => x.Month)
                };

                return View(reportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Reports action");
                TempData["ErrorMessage"] = "An error occurred while generating reports.";
                return RedirectToAction("Dashboard");
            }
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