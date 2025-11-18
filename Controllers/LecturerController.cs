using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using The_CMCS.Models;
using The_CMCS.Services;
using Microsoft.AspNetCore.Hosting;

namespace The_CMCS.Controllers
{
    public class LecturerController : Controller
    {
        private readonly IClaimsService _claimsService;
        private readonly IWebHostEnvironment _environment;

        public LecturerController(IClaimsService claimsService, IWebHostEnvironment environment)
        {
            _claimsService = claimsService;
            _environment = environment;
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
                HourlyRate = currentUser.HourlyRate,
                Department = currentUser.Department,
                Month = DateTime.Now.ToString("yyyy-MM") // Default to current month
            };
            ViewBag.CurrentUser = currentUser;
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(The_CMCS.Models.Claim claim, List<IFormFile> supportingDocuments)
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Lecturer")
                return RedirectToAction("Login", "Home");

            Console.WriteLine("=== CREATE CLAIM STARTED ===");

            // Clear the model state completely and rebuild validation
            ModelState.Clear();

            // Manually validate required fields
            if (string.IsNullOrEmpty(claim.Month))
            {
                ModelState.AddModelError("Month", "Month is required.");
            }

            if (claim.HoursWorked <= 0)
            {
                ModelState.AddModelError("HoursWorked", "Hours worked must be greater than 0.");
            }

            if (claim.HoursWorked > 180)
            {
                ModelState.AddModelError("HoursWorked", "Hours worked cannot exceed 180 hours per month.");
            }

            if (string.IsNullOrEmpty(claim.Description))
            {
                ModelState.AddModelError("Description", "Description is required.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine($"Validation errors: {string.Join(", ", errors)}");
                ViewBag.CurrentUser = currentUser;
                return View(claim);
            }

            try
            {
                Console.WriteLine("Model is valid, processing claim...");

                // Generate claim ID
                claim.Id = GenerateClaimId();
                claim.LecturerId = currentUser.Id;
                claim.LecturerName = currentUser.Name;
                claim.Status = "Pending";
                claim.SubmittedDate = DateTime.Now;
                claim.HourlyRate = currentUser.HourlyRate;
                claim.Department = currentUser.Department;

                // Initialize workflow fields
                claim.CoordinatorApproved = false;
                claim.ManagerApproved = false;
                claim.CoordinatorApprovedBy = null;
                claim.ManagerApprovedBy = null;
                claim.CoordinatorApprovedDate = null;
                claim.ManagerApprovedDate = null;
                claim.ReviewedBy = null;
                claim.ReviewedDate = null;
                claim.RejectionReason = null;

                // Calculate total amount
                claim.TotalAmount = claim.HoursWorked * claim.HourlyRate;

                Console.WriteLine($"Claim Details:");
                Console.WriteLine($"ID: {claim.Id}");
                Console.WriteLine($"Lecturer: {claim.LecturerName}");
                Console.WriteLine($"Month: {claim.Month}");
                Console.WriteLine($"Hours: {claim.HoursWorked}");
                Console.WriteLine($"Rate: {claim.HourlyRate}");
                Console.WriteLine($"Total: {claim.TotalAmount}");

                // Handle file uploads
                if (supportingDocuments != null && supportingDocuments.Count > 0)
                {
                    Console.WriteLine($"Processing {supportingDocuments.Count} files");
                    claim.Documents = new List<SupportingDocument>();

                    foreach (var file in supportingDocuments)
                    {
                        if (file.Length > 0)
                        {
                            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var document = new SupportingDocument
                            {
                                Id = "DOC-" + Guid.NewGuid().ToString(),
                                ClaimId = claim.Id,
                                FileName = file.FileName,
                                FileType = Path.GetExtension(file.FileName),
                                FileData = await GetFileBytes(file),
                                UploadedDate = DateTime.Now,
                                FilePath = uniqueFileName
                            };

                            claim.Documents.Add(document);
                            Console.WriteLine($"Uploaded document: {file.FileName}");
                        }
                    }
                }

                // Save claim using the correct service method
                var result = _claimsService.AddClaim(claim);
                if (result)
                {
                    Console.WriteLine("Claim created successfully!");
                    TempData["SuccessMessage"] = "Claim submitted successfully! It will be reviewed by the coordinator and manager.";
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    Console.WriteLine("Failed to create claim in service");
                    TempData["ErrorMessage"] = "Failed to submit claim. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Error submitting claim: {ex.Message}";
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

            // Clear model state and manually validate
            ModelState.Clear();

            if (string.IsNullOrEmpty(updatedClaim.Month))
            {
                ModelState.AddModelError("Month", "Month is required.");
            }

            if (updatedClaim.HoursWorked <= 0)
            {
                ModelState.AddModelError("HoursWorked", "Hours worked must be greater than 0.");
            }

            if (updatedClaim.HoursWorked > 180)
            {
                ModelState.AddModelError("HoursWorked", "Hours worked cannot exceed 180 hours per month.");
            }

            if (string.IsNullOrEmpty(updatedClaim.Description))
            {
                ModelState.AddModelError("Description", "Description is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CurrentUser = currentUser;
                return View(updatedClaim);
            }

            // Update claim details (preserve HR-set values)
            existingClaim.Month = updatedClaim.Month;
            existingClaim.HoursWorked = updatedClaim.HoursWorked;
            existingClaim.Description = updatedClaim.Description;
            existingClaim.TotalAmount = existingClaim.HoursWorked * existingClaim.HourlyRate;

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

            ViewBag.CurrentUser = currentUser;
            return View(updatedClaim);
        }

        private User? GetCurrentUser()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(username))
                return _claimsService.GetUserByUsername(username);

            return null;
        }

        // Auto-calculation endpoint for real-time calculation
        [HttpPost]
        public JsonResult AutoCalculate(decimal hoursWorked, decimal hourlyRate)
        {
            try
            {
                var totalAmount = hoursWorked * hourlyRate;
                var isValid = hoursWorked <= 180 && hoursWorked > 0;

                return Json(new
                {
                    success = true,
                    totalAmount = totalAmount,
                    isValid = isValid,
                    validationMessage = isValid ? "All validations passed" : "Hours must be between 1 and 180"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private async Task<byte[]> GetFileBytes(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private string GenerateClaimId()
        {
            var claims = _claimsService.GetAllClaims();
            var nextId = claims.Count + 1;
            return $"CLM-{DateTime.Now:yyyyMMdd}-{nextId.ToString().PadLeft(4, '0')}";
        }
        // GET: Download Document
        public IActionResult DownloadDocument(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var document = _claimsService.GetDocumentById(id);
            if (document == null)
                return NotFound();

            var currentUser = GetCurrentUser();
            var claim = _claimsService.GetClaimById(document.ClaimId);

            // Check if the current user owns this claim or is authorized to view it
            if (claim.LecturerId != currentUser?.Id && currentUser?.Role != "Coordinator" && currentUser?.Role != "Manager")
                return Forbid();

            if (document.FileData == null || document.FileData.Length == 0)
                return NotFound();

            return File(document.FileData, "application/octet-stream", document.FileName);
        }
    }
}