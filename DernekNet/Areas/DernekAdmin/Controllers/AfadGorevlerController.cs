using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DernekNet.Areas.DernekAdmin.Controllers
{
    [Area("DernekAdmin")]
    [Authorize(Roles = "DernekAdmin")]
    public class AfadGorevlerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        public AfadGorevlerController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        private async Task<int?> DernekIdGetir()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null || !user.EmailConfirmed || !user.DernekId.HasValue) return null;
            return await _db.Dernekler.AsNoTracking()
                .AnyAsync(x => x.Id == user.DernekId.Value && x.AktifMi)
                ? user.DernekId.Value : (int?)null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            var gorevler = await _db.AfadGorevler.AsNoTracking()
                .Where(x => x.DernekId == dernekId.Value)
                .Include(x => x.Atamalar)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            return View(gorevler);
        }

        [HttpGet]
        public async Task<IActionResult> Olustur()
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            return View("Form", new AfadGorevFormVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Olustur(AfadGorevFormVm model)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            TarihKontrol(model);
            if (!ModelState.IsValid) return View("Form", model);
            _db.AfadGorevler.Add(new AfadGorev
            {
                DernekId = dernekId.Value,
                Baslik = model.Baslik.Trim(),
                Aciklama = model.Aciklama?.Trim(),
                BaslangicTarihi = model.BaslangicTarihi,
                BitisTarihi = model.BitisTarihi,
                AktifMi = model.AktifMi
            });
            await _db.SaveChangesAsync();
            TempData["AfadGorevMesaj"] = "Görev oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Duzenle(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            var gorev = await _db.AfadGorevler.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (gorev == null) return NotFound();
            return View("Form", new AfadGorevFormVm
            {
                Id = gorev.Id,
                Baslik = gorev.Baslik,
                Aciklama = gorev.Aciklama,
                BaslangicTarihi = gorev.BaslangicTarihi,
                BitisTarihi = gorev.BitisTarihi,
                AktifMi = gorev.AktifMi
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Duzenle(int id, AfadGorevFormVm model)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            if (id != model.Id) return BadRequest();
            var gorev = await _db.AfadGorevler
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (gorev == null) return NotFound();
            TarihKontrol(model);
            if (!ModelState.IsValid) return View("Form", model);
            gorev.Baslik = model.Baslik.Trim();
            gorev.Aciklama = model.Aciklama?.Trim();
            gorev.BaslangicTarihi = model.BaslangicTarihi;
            gorev.BitisTarihi = model.BitisTarihi;
            gorev.AktifMi = model.AktifMi;
            await _db.SaveChangesAsync();
            TempData["AfadGorevMesaj"] = "Görev güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        private void TarihKontrol(AfadGorevFormVm model)
        {
            if (model.BaslangicTarihi.HasValue && model.BitisTarihi.HasValue &&
                model.BitisTarihi.Value < model.BaslangicTarihi.Value)
                ModelState.AddModelError(nameof(model.BitisTarihi), "Bitiş tarihi başlangıçtan önce olamaz.");
        }

        [HttpGet]
        public async Task<IActionResult> Atamalar(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            var gorev = await _db.AfadGorevler.AsNoTracking()
                .Include(x => x.Atamalar).ThenInclude(x => x.Gonullu)
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (gorev == null) return NotFound();
            ViewBag.Gonulluler = await _db.AfadGonulluler.AsNoTracking()
                .Where(x => x.DernekId == dernekId.Value && x.OnaylandiMi && x.AktifMi &&
                    !x.GorevAtamalari.Any(a => a.AfadGorevId == id))
                .OrderBy(x => x.AdSoyad).ToListAsync();
            return View(gorev);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Ata(int id, int gonulluId)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            bool gorevVar = await _db.AfadGorevler.AnyAsync(x =>
                x.Id == id && x.DernekId == dernekId.Value && x.AktifMi);
            if (!gorevVar) return NotFound();
            bool gonulluVar = await _db.AfadGonulluler.AnyAsync(x =>
                x.Id == gonulluId && x.DernekId == dernekId.Value && x.AktifMi && x.OnaylandiMi);
            if (!gonulluVar) return BadRequest("Bu dernekte onaylı ve aktif gönüllü bulunamadı.");
            bool mevcut = await _db.AfadGorevAtamalari.AnyAsync(x =>
                x.AfadGorevId == id && x.AfadGonulluId == gonulluId);
            if (mevcut)
            {
                TempData["AfadGorevMesaj"] = "Bu gönüllü zaten göreve atanmış.";
                return RedirectToAction(nameof(Atamalar), new { id });
            }
            _db.AfadGorevAtamalari.Add(new AfadGorevAtama
            {
                AfadGorevId = id,
                AfadGonulluId = gonulluId,
                AtamaTarihi = DateTime.UtcNow,
                Durum = "Atandi"
            });
            await _db.SaveChangesAsync();
            TempData["AfadGorevMesaj"] = "Gönüllü göreve atandı.";
            return RedirectToAction(nameof(Atamalar), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DurumGuncelle(int id, int atamaId, string durum)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            if (durum != "Atandi" && durum != "KabulEdildi" && durum != "Tamamlandi" && durum != "IptalEdildi")
                return BadRequest("Geçersiz durum.");
            var atama = await _db.AfadGorevAtamalari
                .FirstOrDefaultAsync(x => x.Id == atamaId && x.AfadGorevId == id &&
                    x.Gorev.DernekId == dernekId.Value && x.Gonullu.DernekId == dernekId.Value);
            if (atama == null) return NotFound();
            atama.Durum = durum;
            await _db.SaveChangesAsync();
            TempData["AfadGorevMesaj"] = "Görev durumu güncellendi.";
            return RedirectToAction(nameof(Atamalar), new { id });
        }
    }
}
