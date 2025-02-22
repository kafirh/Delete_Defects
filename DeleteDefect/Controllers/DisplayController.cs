using DeleteDefect.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DeleteDefect.Controllers
{
    public class DisplayController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DisplayController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            DateTime selectedDate = DateTime.Now.Date; // Gunakan UTC untuk konsistensi waktu

            // Mengambil data defect hanya untuk hari ini
            var defects = await _context.Defect_Results
                .Where(d => d.DateTime.Date == selectedDate)
                .Include(d => d.Location)
                .Include(d => d.Defect)
                .Include(d => d.Inspector)
                .ToListAsync();

            return View("~/Views/Display/Index.cshtml", defects);
        }
    }
}
