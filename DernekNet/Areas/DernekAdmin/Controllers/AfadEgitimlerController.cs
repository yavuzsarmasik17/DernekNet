
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
    public class AfadEgitimlerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public AfadEgitimlerController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        private async Task<int?> DernekIdGetir()
        {
            var user = await _users.GetUserAsync(User);

            if (user == null ||
                !user.EmailConfirmed ||
                !user.DernekId.HasValue)
                return null;

            bool aktif = await _db.Dernekler
                .AnyAsync(x =>
                    x.Id == user.DernekId.Value &&
                    x.AktifMi);

            return aktif ? user.DernekId : null;
        }

        // EĞİTİM LİSTESİ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var egitimler = await _db.AfadEgitimler
                .AsNoTracking()
                .Include(x => x.Katilimlar)
                .Where(x => x.DernekId == dernekId.Value)
                .OrderByDescending(x => x.Tarih)
                .ToListAsync();

            return View(egitimler);
        }

        // EĞİTİM OLUŞTURMA SAYFASI
        [HttpGet]
        public async Task<IActionResult> Olustur()
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            return View("Form", new AfadEgitim
            {
                Tarih = DateTime.Now,
                AktifMi = true
            });
        }

        // EĞİTİM KAYDET
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Olustur(
            string baslik,
            string? aciklama,
            DateTime tarih,
            bool aktifMi)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            if (string.IsNullOrWhiteSpace(baslik) ||
                baslik.Trim().Length > 200 ||
                tarih == default)
            {
                TempData["AfadEgitimHata"] =
                    "Geçerli bir başlık ve tarih giriniz.";

                return RedirectToAction(nameof(Olustur));
            }

            var egitim = new AfadEgitim
            {
                DernekId = dernekId.Value,
                Baslik = baslik.Trim(),
                Aciklama = aciklama?.Trim(),
                Tarih = tarih,
                AktifMi = aktifMi
            };

            _db.AfadEgitimler.Add(egitim);
            await _db.SaveChangesAsync();

            TempData["AfadEgitimMesaj"] =
                "Eğitim başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        // EĞİTİM DÜZENLEME SAYFASI
        [HttpGet]
        public async Task<IActionResult> Duzenle(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var egitim = await _db.AfadEgitimler
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (egitim == null) return NotFound();

            return View("Form", egitim);
        }

        // EĞİTİM GÜNCELLE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Duzenle(
            int id,
            string baslik,
            string? aciklama,
            DateTime tarih,
            bool aktifMi)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var egitim = await _db.AfadEgitimler
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (egitim == null) return NotFound();

            if (string.IsNullOrWhiteSpace(baslik) ||
                baslik.Trim().Length > 200 ||
                tarih == default)
            {
                TempData["AfadEgitimHata"] =
                    "Geçerli bir başlık ve tarih giriniz.";

                return RedirectToAction(
                    nameof(Duzenle), new { id });
            }

            egitim.Baslik = baslik.Trim();
            egitim.Aciklama = aciklama?.Trim();
            egitim.Tarih = tarih;
            egitim.AktifMi = aktifMi;

            await _db.SaveChangesAsync();

            TempData["AfadEgitimMesaj"] =
                "Eğitim güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        // EĞİTİM KATILIMLARI
        [HttpGet]
        public async Task<IActionResult> Katilimlar(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var egitim = await _db.AfadEgitimler
                .AsNoTracking()
                .Include(x => x.Katilimlar)
                .ThenInclude(x => x.Gonullu)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (egitim == null) return NotFound();

            ViewBag.Gonulluler = await _db.AfadGonulluler
                .AsNoTracking()
                .Where(x =>
                    x.DernekId == dernekId.Value &&
                    x.OnaylandiMi &&
                    x.AktifMi &&
                    !x.EgitimKatilimlari.Any(k =>
                        k.AfadEgitimId == id))
                .OrderBy(x => x.AdSoyad)
                .ToListAsync();

            return View(egitim);
        }

        // GÖNÜLLÜYÜ EĞİTİME ATA
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Ata(
            int id, int gonulluId)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            bool egitimVar = await _db.AfadEgitimler
                .AnyAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value &&
                    x.AktifMi);

            if (!egitimVar) return NotFound();

            bool gonulluVar = await _db.AfadGonulluler
                .AnyAsync(x =>
                    x.Id == gonulluId &&
                    x.DernekId == dernekId.Value &&
                    x.OnaylandiMi &&
                    x.AktifMi);

            if (!gonulluVar)
                return BadRequest(
                    "Geçerli ve onaylı gönüllü bulunamadı.");

            bool mevcut = await _db.AfadEgitimKatilimlari
                .AnyAsync(x =>
                    x.AfadEgitimId == id &&
                    x.AfadGonulluId == gonulluId);

            if (mevcut)
            {
                TempData["AfadEgitimMesaj"] =
                    "Bu gönüllü zaten eğitime atanmış.";

                return RedirectToAction(
                    nameof(Katilimlar), new { id });
            }

            _db.AfadEgitimKatilimlari.Add(
                new AfadEgitimKatilim
                {
                    AfadEgitimId = id,
                    AfadGonulluId = gonulluId,
                    KatildiMi = false
                });

            await _db.SaveChangesAsync();

            TempData["AfadEgitimMesaj"] =
                "Gönüllü eğitime atandı.";

            return RedirectToAction(
                nameof(Katilimlar), new { id });
        }

        // KATILIM DURUMUNU GÜNCELLE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> KatilimGuncelle(
            int id, int katilimId, bool katildiMi)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var katilim = await _db.AfadEgitimKatilimlari
                .FirstOrDefaultAsync(x =>
                    x.Id == katilimId &&
                    x.AfadEgitimId == id &&
                    x.Egitim.DernekId == dernekId.Value &&
                    x.Gonullu.DernekId == dernekId.Value);

            if (katilim == null) return NotFound();

            katilim.KatildiMi = katildiMi;

            await _db.SaveChangesAsync();

            TempData["AfadEgitimMesaj"] =
                "Katılım durumu güncellendi.";

            return RedirectToAction(
                nameof(Katilimlar), new { id });
        }
    }
}
