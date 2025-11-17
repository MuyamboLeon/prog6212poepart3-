using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using The_CMCS.Models;
using The_CMCS.Services;

namespace The_CMCS.Controllers
{
    public class ProgrammeCoordinatorController : Controller
    {
        private readonly IClaimsService _claimsService;

        public ProgrammeCoordinatorController(IClaimsService claimsService)
        {
            _claimsService = claimsService;
        }

        public IActionResult Dashboard()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Coordinator")
                return RedirectToAction("Login", "Home");

            // Get ALL claims regardless of department
            var allClaims = _claimsService.GetAllClaims();

            // Get pending claims for coordinator review (not coordinator approved yet)
            var pendingCoordinatorReview = allClaims.Where(c => c.Status == "Pending" && !c.CoordinatorApproved).ToList();

            // Get claims pending manager review (coordinator approved but waiting for manager)
            var pendingManagerReview = allClaims.Where(c => c.Status == "Pending" && c.CoordinatorApproved).ToList();

            ViewBag.PendingCount = pendingCoordinatorReview.Count;
            ViewBag.PendingManagerReviewCount = pendingManagerReview.Count;
            ViewBag.ApprovedCount = allClaims.Count(c => c.Status == "Approved");
            ViewBag.RejectedCount = allClaims.Count(c => c.Status == "Rejected");
            ViewBag.TotalApprovedAmount = allClaims.Where(c => c.Status == "Approved").Sum(c => c.TotalAmount);

            ViewBag.PendingClaims = pendingCoordinatorReview;
            ViewBag.AllClaims = allClaims;

            ViewBag.PendingReviewCount = pendingCoordinatorReview.Count;
            ViewBag.ApprovedTodayCount = allClaims.Count(c =>
                c.CoordinatorApprovedDate?.Date == DateTime.Today);
            ViewBag.TotalLecturersCount = allClaims.Select(c => c.LecturerId).Distinct().Count();
            ViewBag.TotalClaimsCount = allClaims.Count;
            ViewBag.CurrentUserDepartment = currentUser.Department;

            return View(allClaims);
        }

        public IActionResult ReviewClaims()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Coordinator")
                return RedirectToAction("Login", "Home");

            // Get claims pending coordinator review (not coordinator approved yet)
            var pendingClaims = _claimsService.GetAllClaims()
                .Where(c => c.Status == "Pending" && !c.CoordinatorApproved)
                .OrderByDescending(c => c.SubmittedDate)
                .ToList();

            return View(pendingClaims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveClaim(string id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Coordinator")
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Invalid claim ID.";
                return RedirectToAction("ReviewClaims");
            }

            var result = _claimsService.ApproveByCoordinator(id, currentUser.Name);
            if (result)
            {
                TempData["SuccessMessage"] = "Claim approved by coordinator! It will now be reviewed by the manager.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to approve claim.";
            }

            return RedirectToAction("ReviewClaims");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectClaim(string id, string rejectionReason)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Coordinator")
                return RedirectToAction("Login", "Home");

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

            var result = _claimsService.RejectByCoordinator(id, currentUser.Name, rejectionReason.Trim());
            if (result)
            {
                TempData["SuccessMessage"] = "Claim rejected successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reject claim.";
            }

            return RedirectToAction("ReviewClaims");
        }

        public IActionResult DepartmentHistory()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Coordinator")
                return RedirectToAction("Login", "Home");

            // Get ALL claims for history
            var allClaims = _claimsService.GetAllClaims();
            return View(allClaims.OrderByDescending(c => c.SubmittedDate).ToList());
        }

        public IActionResult ClaimDetails(string id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Coordinator")
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var claim = _claimsService.GetClaimById(id);
            if (claim == null)
                return NotFound();

            var documents = _claimsService.GetDocumentsByClaimId(id);
            claim.Documents = documents ?? new List<SupportingDocument>();

            return View(claim);
        }

        public IActionResult DownloadDocument(string id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Coordinator")
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var document = _claimsService.GetDocumentById(id);
            if (document == null)
                return NotFound();

            var claim = _claimsService.GetClaimById(document.ClaimId);
            if (claim == null)
                return NotFound();

            if (document.FileData != null && document.FileData.Length > 0)
            {
                var contentType = document.FileType ?? "application/octet-stream";
                return File(document.FileData, contentType, document.FileName);
            }
            else if (!string.IsNullOrEmpty(document.FilePath) && System.IO.File.Exists(document.FilePath))
            {
                var fileBytes = System.IO.File.ReadAllBytes(document.FilePath);
                var contentType = document.FileType ?? "application/octet-stream";
                return File(fileBytes, contentType, document.FileName);
            }
            else
            {
                return NotFound();
            }
        }

        private User? GetCurrentUser()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(username))
            {
                var user = _claimsService.GetUserByUsername(username);
                return user;
            }

            return null;
        }
    }
}