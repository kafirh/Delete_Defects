using System.Globalization;
using System.Text;
using DeleteDefect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeleteDefect.Data;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;

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
            if (HttpContext.Session.GetString("UserNIK") == null)
            {
                return RedirectToAction("Index", "Home"); // Redirect ke login jika belum login
            }
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
            if (HttpContext.Session.GetString("UserNIK") == null)
            {
                return RedirectToAction("Index", "Home"); // Redirect ke login jika belum login
            }
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

public async Task<IActionResult> ExportToExcel(DateTime? selectedDate)
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

        // Jika tidak ada data, beri pesan
        if (!defects.Any())
        {
            ViewBag.ErrorMessage = "Tidak ada data untuk tanggal tersebut.";
            return await Selected(selectedDate);
        }

        // Buat workbook Excel
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Defect Data");

            // Header Excel
            worksheet.Cell(1, 1).Value = "No";
            worksheet.Cell(1, 2).Value = "Tanggal";
            worksheet.Cell(1, 3).Value = "Waktu";
            worksheet.Cell(1, 4).Value = "ModelCode";
            worksheet.Cell(1, 5).Value = "SerialNumber";
            worksheet.Cell(1, 6).Value = "DefectName";
            worksheet.Cell(1, 7).Value = "InspectorName";
            worksheet.Cell(1, 8).Value = "ModelNumber";
            worksheet.Cell(1, 9).Value = "LocationName";

            // Format header
            var headerRange = worksheet.Range("A1:I1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            int rowIndex = 2;
            int index = 1;

            foreach (var defect in defects)
            {
                worksheet.Cell(rowIndex, 1).Value = index; // No
                worksheet.Cell(rowIndex, 2).Value = defect.DateTime.ToString("dd MMM yy", CultureInfo.InvariantCulture); // Tanggal
                worksheet.Cell(rowIndex, 3).Value = defect.DateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture); // Waktu
                worksheet.Cell(rowIndex, 4).Value = defect.ModelCode; // ModelCode
                worksheet.Cell(rowIndex, 5).Value = defect.SerialNumber; // SerialNumber
                worksheet.Cell(rowIndex, 6).Value = defect.Defect?.DefectName; // DefectName
                worksheet.Cell(rowIndex, 7).Value = defect.Inspector?.Name; // InspectorName
                worksheet.Cell(rowIndex, 8).Value = defect.ModelNumber; // ModelNumber
                worksheet.Cell(rowIndex, 9).Value = defect.Location?.LocationName; // LocationName

                rowIndex++;
                index++;
            }

            // Autosize semua kolom
            worksheet.Columns().AdjustToContents();

            // Konversi workbook ke byte array
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var fileBytes = stream.ToArray();
                var fileName = $"Defects_{selectedDate.Value:yyyy-MM-dd}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }


}
}
