using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DernekNet.Areas.DernekAdmin.Controllers
{
    [Area("DernekAdmin")]
    [Authorize(Roles = "DernekAdmin")]
    public class AfadHaberlerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        public AfadHaberlerController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        private async Task<int?> AktifDernekId()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null || !user.DernekId.HasValue || !user.EmailConfirmed)
                return null;
            bool aktif = await _db.Dernekler.AsNoTracking()
                .AnyAsync(d => d.Id == user.DernekId.Value && d.AktifMi);
            return aktif ? user.DernekId : null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernekId = await AktifDernekId();
            if (dernekId == null) return Forbid();
            var kayitlar = await _db.Set<AfadHaber>().AsNoTracking()
                .Where(x => x.DernekId == dernekId.Value)
                .OrderByDescending(x => x.YayinTarihi).ToListAsync();
            return View(kayitlar);
        }

        [HttpGet]
        public async Task<IActionResult> Ekle()
        {
            if (await AktifDernekId() == null) return Forbid();
            return View("Form", new AfadHaberForm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(AfadHaberForm model)
        {
            var dernekId = await AktifDernekId();
            if (dernekId == null) return Forbid();
            if (!ModelState.IsValid) return View("Form", model);
            var kayit = new AfadHaber
            {
                DernekId = dernekId.Value,
                Baslik = model.Baslik.Trim(),
                Icerik = model.Icerik.Trim(),
                AktifMi = model.AktifMi,
                YayinTarihi = DateTime.UtcNow,
            };
            _db.Set<AfadHaber>().Add(kayit);
            await _db.SaveChangesAsync();
            TempData["Mesaj"] = "Kayıt eklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Duzenle(int id)
        {
            var dernekId = await AktifDernekId();
            if (dernekId == null) return Forbid();
            var kayit = await _db.Set<AfadHaber>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (kayit == null) return NotFound();
            return View("Form", new AfadHaberForm
            {
                Id = kayit.Id,
                Baslik = kayit.Baslik,
                Icerik = kayit.Icerik,
                AktifMi = kayit.AktifMi,
                YayinTarihi = kayit.YayinTarihi,
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Duzenle(int id, AfadHaberForm model)
        {
            var dernekId = await AktifDernekId();
            if (dernekId == null) return Forbid();
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View("Form", model);
            var kayit = await _db.Set<AfadHaber>()
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (kayit == null) return NotFound();
            kayit.Baslik = model.Baslik.Trim();
            kayit.Icerik = model.Icerik.Trim();
            kayit.AktifMi = model.AktifMi;
            // İlk yayın tarihi korunur.
            await _db.SaveChangesAsync();
            TempData["Mesaj"] = "Kayıt güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DurumDegistir(int id)
        {
            var dernekId = await AktifDernekId();
            if (dernekId == null) return Forbid();
            var kayit = await _db.Set<AfadHaber>()
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (kayit == null) return NotFound();
            kayit.AktifMi = !kayit.AktifMi;
            await _db.SaveChangesAsync();
            TempData["Mesaj"] = kayit.AktifMi ? "Yayına alındı." : "Yayından kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class AfadHaberForm
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(200)]
        public string Baslik { get; set; } = "";
        [Required(ErrorMessage = "İçerik zorunludur.")]
        public string Icerik { get; set; } = "";
        public bool AktifMi { get; set; } = true;
        public DateTime YayinTarihi { get; set; } = DateTime.UtcNow;
    }
}
