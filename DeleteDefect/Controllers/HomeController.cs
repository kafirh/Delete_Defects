using System.Diagnostics;
using DeleteDefect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using DeleteDefect.Data;

namespace DeleteDefect.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly List<string> adminNIKs = new List<string> { "7098", "5714", "7217" };

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: Halaman Login
        [HttpGet]
        public IActionResult Index()
        {
            // Cek apakah user sudah login
            if (HttpContext.Session.GetString("UserNIK") != null)
            {
                return RedirectToAction("Index", "Defect"); // Redirect ke halaman utama jika sudah login
            }
            return View();
        }

        // Login Logic
        public IActionResult Login(string Username, string Password)
        {
            var user = GetUserByUsername(Username);

            if (user != null)
            {
                if (BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
                {
                    // Simpan informasi user ke session
                    HttpContext.Session.SetString("UserNIK", user.NIK);
                    HttpContext.Session.SetString("UserName", user.Name);

                    bool isAdmin = adminNIKs.Contains(user.NIK);
                    HttpContext.Session.SetString("IsAdmin", isAdmin.ToString());

                    return RedirectToAction("Index", "Defect");
                }

                ViewData["ErrorMessage"] = "Invalid NIK or password.";
            }
            else
            {
                ViewData["ErrorMessage"] = "User not found.";
            }

            return View("Index");
        }

        // Fungsi untuk mendapatkan user berdasarkan Username (NIK)
        public InspectorModel GetUserByUsername(string username)
        {
            return _context.AspNetUsers
                .Where(u => u.NIK == username)
                .Select(u => new InspectorModel
                {
                    NIK = u.NIK,
                    Name = u.Name,
                    PasswordHash = u.PasswordHash
                })
                .FirstOrDefault();
        }

        // Logout Logic
        [HttpPost]
        public IActionResult Logout()
        {
            // Hapus semua data session
            HttpContext.Session.Clear();

            return RedirectToAction("Index");
        }

        // Error Handling
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
