using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using The_CMCS.Models;
using The_CMCS.Services;

namespace The_CMCS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IClaimsService _claimsService;

        public HomeController(ILogger<HomeController> logger, IClaimsService claimsService)
        {
            _logger = logger;
            _claimsService = claimsService;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
            {
                var user = _claimsService.GetUserByUsername(User.Identity.Name);
                if (user != null && !string.IsNullOrEmpty(user.Role))
                {
                    return RedirectToAction("Dashboard", GetDashboardController(user.Role));
                }
            }
            return RedirectToAction("Login");
        }

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
            {
                var user = _claimsService.GetUserByUsername(User.Identity.Name);
                if (user != null && !string.IsNullOrEmpty(user.Role))
                {
                    return RedirectToAction("Dashboard", GetDashboardController(user.Role));
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string role)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                ModelState.AddModelError(string.Empty, "Please fill in all fields.");
                return View();
            }

            var user = _claimsService.GetUserByUsername(username);

            if (user != null && user.Password == password && user.Role == role && user.IsActive)
            {
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id ?? ""),
                    new System.Security.Claims.Claim(ClaimTypes.Name, user.Username ?? ""),
                    new System.Security.Claims.Claim(ClaimTypes.Role, user.Role ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Dashboard", GetDashboardController(user.Role ?? ""));
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt or account is inactive.");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetDashboardController(string role)
        {
            return role switch
            {
                "Lecturer" => "Lecturer",
                "Coordinator" => "ProgrammeCoordinator",
                "Manager" => "AcademicManagers",
                "HR" => "HR",
                _ => "Home"
            };
        }
    }
}