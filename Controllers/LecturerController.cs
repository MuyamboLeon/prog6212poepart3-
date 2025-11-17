using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using The_CMCS.Models;
using The_CMCS.Services;

namespace The_CMCS.Controllers
{
    public class LecturerController : Controller
    {
        private readonly IClaimsService _claimsService;

        public LecturerController(IClaimsService claimsService)
        {
            _claimsService = claimsService;
        }

        public IActionResult Dashboard()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Lecturer")
                return RedirectToAction("Login", "Home");

            var userClaims = _claimsService.GetClaimsByUser(currentUser.Id);

            ViewBag.PendingCount = userClaims.Count(c => c.Status == "Pending");
            ViewBag.ApprovedCount = userClaims.Count(c => c.Status == "Approved");
            ViewBag.RejectedCount = userClaims.Count(c => c.Status == "Rejected");
            ViewBag.RecentClaims = userClaims.OrderByDescending(c => c.SubmittedDate).Take(5).ToList();
            ViewBag.CurrentUser = currentUser;

            return View(userClaims);
        }

        public IActionResult ClaimHistory()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Lecturer")
                return RedirectToAction("Login", "Home");

            var userClaims = _claimsService.GetClaimsByUser(currentUser.Id);
            ViewBag.CurrentUser = currentUser;
            return View(userClaims.OrderByDescending(c => c.SubmittedDate).ToList());
        }

        // GET: Create Claim
        public IActionResult Create()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Lecturer")
                return RedirectToAction("Login", "Home");

            var claim = new The_CMCS.Models.Claim
            {
                HourlyRate = currentUser.HourlyRate, // Auto-populate from HR data
                Department = currentUser.Department
            };
            ViewBag.CurrentUser = currentUser;
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(The_CMCS.Models.Claim claim, List<IFormFile> supportingDocuments)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Lecturer")
                return RedirectToAction("Login", "Home");

            // Remove model state errors for fields that will be set programmatically
            ModelState.Remove("LecturerId");
            ModelState.Remove("LecturerName");
            ModelState.Remove("Status");
            ModelState.Remove("SubmittedDate");
            ModelState.Remove("HourlyRate");
            ModelState.Remove("Department");
            ModelState.Remove("TotalAmount");

            if (ModelState.IsValid)
            {
                try
                {
                    // Add lecturer information to the claim
                    claim.LecturerId = currentUser.Id;
                    claim.LecturerName = currentUser.Name;
                    claim.Status = "Pending";
                    claim.SubmittedDate = DateTime.Now;
                    claim.HourlyRate = currentUser.HourlyRate; // Use HR-set rate
                    claim.Department = currentUser.Department;

                    // TotalAmount is calculated automatically by the property getter

                    // Validation: Maximum hours per month
                    if (claim.HoursWorked > 180)
                    {
                        ModelState.AddModelError("HoursWorked", "Hours worked cannot exceed 180 hours per month.");
                        ViewBag.CurrentUser = currentUser;
                        return View(claim);
                    }

                    // Save claim and documents
                    var result = _claimsService.CreateClaim(claim, supportingDocuments);

                    if (result)
                    {
                        TempData["SuccessMessage"] = "Claim submitted successfully! It is now pending coordinator review.";
                        return RedirectToAction("Dashboard");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to submit claim. Please try again.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error submitting claim: {ex.Message}";
                }
            }

            ViewBag.CurrentUser = currentUser;
            return View(claim);
        }

        // GET: Claim Details
        public IActionResult Details(string id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Lecturer")
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var claim = _claimsService.GetClaimById(id);
            if (claim == null || claim.LecturerId != currentUser.Id)
                return NotFound();

            var documents = _claimsService.GetDocumentsByClaimId(id);
            claim.Documents = documents ?? new List<SupportingDocument>();

            ViewBag.CurrentUser = currentUser;
            return View(claim);
        }

        // GET: Edit Claim (only for pending claims)
        public IActionResult Edit(string id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Lecturer")
                return RedirectToAction("Login", "Home");

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var claim = _claimsService.GetClaimById(id);
            if (claim == null || claim.LecturerId != currentUser.Id || claim.Status != "Pending")
                return NotFound();

            ViewBag.CurrentUser = currentUser;
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, The_CMCS.Models.Claim updatedClaim)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Lecturer")
                return RedirectToAction("Login", "Home");

            if (id != updatedClaim.Id)
                return NotFound();

            var existingClaim = _claimsService.GetClaimById(id);
            if (existingClaim == null || existingClaim.LecturerId != currentUser.Id || existingClaim.Status != "Pending")
                return NotFound();

            // Remove model state errors for fields that cannot be edited
            ModelState.Remove("HourlyRate");
            ModelState.Remove("Department");
            ModelState.Remove("TotalAmount");

            if (ModelState.IsValid)
            {
                // Validation: Maximum hours per month
                if (updatedClaim.HoursWorked > 180)
                {
                    ModelState.AddModelError("HoursWorked", "Hours worked cannot exceed 180 hours per month.");
                    ViewBag.CurrentUser = currentUser;
                    return View(updatedClaim);
                }

                // Update claim details (preserve HR-set values)
                existingClaim.Month = updatedClaim.Month;
                existingClaim.HoursWorked = updatedClaim.HoursWorked;
                existingClaim.Description = updatedClaim.Description;

                // TotalAmount is calculated automatically by the property getter

                var result = _claimsService.UpdateClaim(existingClaim);
                if (result)
                {
                    TempData["SuccessMessage"] = "Claim updated successfully!";
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update claim. Please try again.";
                }
            }

            ViewBag.CurrentUser = currentUser;
            return View(updatedClaim);
        }

        private User GetCurrentUser()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(username))
                return _claimsService.GetUserByUsername(username);

            return null;
        }
    }
}