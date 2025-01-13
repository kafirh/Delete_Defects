using System.Globalization;
using System.Text;
using DeleteDefect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeleteDefect.Data;

namespace DeleteDefect.Controllers
{
    public class DefectController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DefectController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var selectedDate = DateTime.Now.Date;
            // Ambil status admin dari session
            bool isAdmin = HttpContext.Session.GetString("IsAdmin") == "True";

            // Menyimpan tanggal yang dipilih pada ViewData agar tetap muncul di form
            ViewData["SelectedDate"] = selectedDate.ToString("yyyy-MM-dd");

            // Mengambil Defect_Results dengan filter berdasarkan hari ini dan Join dengan tabel Locations
            var products = await _context.Defect_Results
                .Where(d => d.DateTime.Date == selectedDate)  // Filter berdasarkan tanggal hari ini
                .Include(d => d.Location) // Join dengan tabel Locations
                .Include(d => d.Defect)
                .Include(d => d.Inspector)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> Selected(DateTime? selectedDate)
        {
            // Simpan tanggal yang dipilih agar tetap muncul di form
            ViewData["SelectedDate"] = selectedDate?.ToString("yyyy-MM-dd");

            // Default ke tanggal hari ini jika tidak ada tanggal yang dipilih
            var dateToFilter = selectedDate ?? DateTime.Now.Date;

            // Filter data berdasarkan tanggal
            var defects = await _context.Defect_Results
                .Where(d => d.DateTime.Date == dateToFilter)
                .Include(d => d.Location)
                .Include(d => d.Defect)
                .Include(d => d.Inspector)
                .ToListAsync();

            return View("Index", defects);
        }

        [HttpPost]
        [ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id, string? selectedDate)
        {
            var product = _context.Defect_Results.FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            _context.Defect_Results.Remove(product);
            _context.SaveChanges();

            // Redirect ke halaman Selected jika ada tanggal yang dipilih
            if (!string.IsNullOrEmpty(selectedDate))
            {
                return RedirectToAction("Selected", new { selectedDate });
            }

            // Redirect ke Index jika tidak ada tanggal yang dipilih
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> ExportToCsv(DateTime? selectedDate)
        {
            if (!selectedDate.HasValue)
            {
                return BadRequest("Tanggal tidak valid.");
            }

            // Filter data berdasarkan tanggal yang dipilih
            var defects = await _context.Defect_Results
                .Where(d => d.DateTime.Date == selectedDate.Value.Date)
                .Include(d => d.Location)
                .Include(d => d.Defect)
                .Include(d => d.Inspector)
                .ToListAsync();

            // Jika tidak ada data, berikan pesan
            if (!defects.Any())
            {
                return NotFound("Tidak ada data pada tanggal yang dipilih.");
            }

            // Buat header CSV
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("\"No\",\"Id\",\"Tanggal\",\"Waktu\",\"ModelCode\",\"SerialNumber\",\"DefectName\",\"InspectorName\",\"ModelNumber\",\"LocationName\"");

            int index = 1;
            foreach (var defect in defects)
            {
                csvBuilder.AppendLine(string.Join(",",
                    $"\"{index}\"",
                    $"\"{defect.DateTime.ToString("dd MMM yy", CultureInfo.InvariantCulture)}\"", // Tanggal dalam format "13 Sep 24"
                    $"\"{defect.DateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}\"", // Waktu 24 jam dengan detik
                    $"\"{defect.ModelCode}\"",
                    $"\"{defect.SerialNumber}\"",
                    $"\"{defect.Defect?.DefectName}\"",
                    $"\"{defect.Inspector?.Name}\"",
                    $"\"{defect.ModelNumber}\"",
                    $"\"{defect.Location?.LocationName}\""
                ));
                index++; // Increment nomor
            }

            // Konversi ke byte array dan kembalikan sebagai file
            var fileBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var fileName = $"Defects_{selectedDate.Value:yyyy-MM-dd}.csv";

            return File(fileBytes, "text/csv", fileName);
        }

    }
}
