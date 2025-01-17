using System.Linq;
using System.Threading.Tasks;
using DeleteDefect.Data;
using DeleteDefect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeleteDefect.Controllers
{
    public class DefectNamesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DefectNamesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Index dengan fitur Search
        public async Task<IActionResult> Index(string searchQuery)
        {
            if (HttpContext.Session.GetString("UserNIK") == null)
            {
                return RedirectToAction("Index", "Home"); // Redirect ke login jika belum login
            }
            var defectNamesQuery = _context.Defect_Names
                .Include(d => d.Char)
                .OrderByDescending(d => d.Priority)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                defectNamesQuery = defectNamesQuery.Where(d => d.DefectName.Contains(searchQuery));
            }

            var defectNames = await defectNamesQuery.ToListAsync();
            ViewData["SearchQuery"] = searchQuery;

            return View(defectNames);
        }

        // GET: Form (Digunakan untuk Add dan Edit)
        public async Task<IActionResult> Form(int? id)
        {
            if (HttpContext.Session.GetString("UserNIK") == null)
            {
                return RedirectToAction("Index", "Home"); // Redirect ke login jika belum login
            }
            var characters = await _context.Characters.ToListAsync();
            ViewBag.Characters = new SelectList(characters, "id", "Character");

            if (id == null)
            {
                return View(new DefectNameModel());
            }

            var defect = await _context.Defect_Names
                .Include(d => d.Char)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (defect == null)
            {
                return NotFound();
            }

            return View(defect);
        }

        // POST: Save (Handle Add dan Edit)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(DefectNameModel model)
        {
            ViewBag.Characters = new SelectList(await _context.Characters.ToListAsync(), "id", "Character");

            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            var character = await _context.Characters.FirstOrDefaultAsync(c => c.id == model.ChartId);
            if (character == null)
            {
                ModelState.AddModelError("ChartId", "Karakter yang dipilih tidak valid.");
                return View("Form", model);
            }

            if (model.Id == 0)
            {
                _context.Defect_Names.Add(model);
            }
            else
            {
                try
                {
                    _context.Defect_Names.Update(model);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DefectExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Delete Defect
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var defect = await _context.Defect_Names
                .FirstOrDefaultAsync(d => d.Id == id);

            if (defect == null)
            {
                return NotFound();
            }

            try
            {
                _context.Defect_Names.Remove(defect);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Gagal menghapus data. Pastikan data tidak digunakan di tempat lain.");
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper: Cek apakah Defect ada
        private bool DefectExists(int id)
        {
            return _context.Defect_Names.Any(e => e.Id == id);
        }
    }
}
