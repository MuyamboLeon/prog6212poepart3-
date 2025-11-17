using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using The_CMCS.Models;
using The_CMCS.Services;

namespace The_CMCS.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly IClaimsService _claimsService;
        private readonly IWebHostEnvironment _environment;

        public ClaimsController(IClaimsService claimsService, IWebHostEnvironment environment)
        {
            _claimsService = claimsService;
            _environment = environment;
        }

        public IActionResult Index()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Home");

            var userClaims = _claimsService.GetClaimsByUser(currentUser.Id);
            return View(userClaims);
        }

        public IActionResult Create()
        {
            var currentUser = GetCurrentUser();
            if (currentUser?.Role != "Lecturer")
            {
                var redirectController = currentUser?.Role == "Coordinator" ? "ProgrammeCoordinator" : "AcademicManagers";
                return RedirectToAction("Dashboard", redirectController);
            }

            var claim = new Models.Claim
            {
                HourlyRate = 320,
                Department = currentUser.Department
            };
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.Claim claim, List<IFormFile> supportingDocuments)
        {
            Console.WriteLine("Create POST method called");

            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                Console.WriteLine("User not authenticated");
                return RedirectToAction("Login", "Home");
            }

            // Remove model state errors for fields that will be set programmatically
            ModelState.Remove("Id");
            ModelState.Remove("LecturerId");
            ModelState.Remove("LecturerName");
            ModelState.Remove("Status");
            ModelState.Remove("SubmittedDate");
            ModelState.Remove("HourlyRate");
            ModelState.Remove("CoordinatorApproved");
            ModelState.Remove("CoordinatorApprovedBy");
            ModelState.Remove("CoordinatorApprovedDate");
            ModelState.Remove("ManagerApproved");
            ModelState.Remove("ManagerApprovedBy");
            ModelState.Remove("ManagerApprovedDate");
            ModelState.Remove("ReviewedBy");
            ModelState.Remove("ReviewedDate");
            ModelState.Remove("RejectionReason");
            ModelState.Remove("Documents");

            Console.WriteLine($"ModelState IsValid after removal: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is still invalid after removing server-set fields");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
                return View(claim);
            }

            try
            {
                Console.WriteLine("Starting claim creation...");

                // Generate claim ID
                claim.Id = GenerateClaimId();
                claim.LecturerId = currentUser.Id;
                claim.LecturerName = currentUser.Name;
                claim.Status = "Pending";
                claim.SubmittedDate = DateTime.Now;
                claim.HourlyRate = 320;

                // Initialize workflow fields
                claim.CoordinatorApproved = false;
                claim.ManagerApproved = false;

                // Calculate total amount
                claim.TotalAmount = claim.HoursWorked * claim.HourlyRate;

                Console.WriteLine($"Claim ID: {claim.Id}");
                Console.WriteLine($"Month: {claim.Month}");
                Console.WriteLine($"Department: {claim.Department}");
                Console.WriteLine($"Hours: {claim.HoursWorked}");
                Console.WriteLine($"Hourly Rate: {claim.HourlyRate}");
                Console.WriteLine($"Total Amount: {claim.TotalAmount}");
                Console.WriteLine($"Description: {claim.Description}");
                Console.WriteLine($"Lecturer: {claim.LecturerName}");

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
                        }
                    }
                }

                var result = _claimsService.AddClaim(claim);
                if (result)
                {
                    Console.WriteLine("Claim added successfully!");
                    TempData["SuccessMessage"] = "Claim submitted successfully! It will be reviewed by the coordinator.";
                    return RedirectToAction("Index");
                }
                else
                {
                    Console.WriteLine("Failed to add claim");
                    ModelState.AddModelError("", "Failed to submit claim. Please try again.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }

            return View(claim);
        }

        public IActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var claim = _claimsService.GetClaimById(id);
            if (claim == null)
                return NotFound();

            var documents = _claimsService.GetDocumentsByClaimId(id);
            claim.Documents = documents;

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Home");

            var claim = _claimsService.GetClaimById(id);
            if (claim == null || claim.LecturerId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "Claim not found or you don't have permission to delete it.";
                return RedirectToAction("Index");
            }

            if (claim.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending claims can be deleted.";
                return RedirectToAction("Index");
            }

            var result = _claimsService.DeleteClaim(id);
            if (result)
            {
                TempData["SuccessMessage"] = "Claim deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete claim.";
            }

            return RedirectToAction("Index");
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

        private User GetCurrentUser()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(username))
                return _claimsService.GetUserByUsername(username);

            return null;
        }
    }
}