
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
    public class AfadYetkinliklerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public AfadYetkinliklerController(
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

            bool aktif = await _db.Dernekler.AsNoTracking()
                .AnyAsync(x =>
                    x.Id == user.DernekId.Value &&
                    x.AktifMi);

            return aktif ? user.DernekId : null;
        }

        // YETKİNLİK LİSTESİ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var liste = await _db.AfadYetkinlikler
                .AsNoTracking()
                .Include(x => x.Gonulluler)
                .Where(x => x.DernekId == dernekId.Value)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            return View(liste);
        }

        // YENİ YETKİNLİK
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Olustur(
            string ad, string? aciklama)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            if (string.IsNullOrWhiteSpace(ad) ||
                ad.Trim().Length > 150)
            {
                TempData["AfadYetkinlikHata"] =
                    "Yetkinlik adı 1-150 karakter olmalıdır.";
                return RedirectToAction(nameof(Index));
            }

            string temizAd = ad.Trim();

            bool mevcut = await _db.AfadYetkinlikler
                .AnyAsync(x =>
                    x.DernekId == dernekId.Value &&
                    x.Ad == temizAd);

            if (mevcut)
            {
                TempData["AfadYetkinlikHata"] =
                    "Bu yetkinlik zaten tanımlanmış.";
                return RedirectToAction(nameof(Index));
            }

            _db.AfadYetkinlikler.Add(new AfadYetkinlik
            {
                DernekId = dernekId.Value,
                Ad = temizAd,
                Aciklama = aciklama?.Trim()
            });

            await _db.SaveChangesAsync();

            TempData["AfadYetkinlikMesaj"] =
                "Yetkinlik oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        // GÖNÜLLÜLERİN YETKİNLİKLERİ
        [HttpGet]
        public async Task<IActionResult> Gonulluler()
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var gonulluler = await _db.AfadGonulluler
                .AsNoTracking()
                .Include(x => x.Yetkinlikleri)
                    .ThenInclude(x => x.Yetkinlik)
                .Where(x => x.DernekId == dernekId.Value)
                .OrderBy(x => x.AdSoyad)
                .ToListAsync();

            return View(gonulluler);
        }

        // GÖNÜLLÜ YETKİNLİK DETAYI
        [HttpGet]
        public async Task<IActionResult> Detay(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var gonullu = await _db.AfadGonulluler
                .AsNoTracking()
                .Include(x => x.Yetkinlikleri)
                    .ThenInclude(x => x.Yetkinlik)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (gonullu == null) return NotFound();

            ViewBag.Yetkinlikler = await _db.AfadYetkinlikler
                .AsNoTracking()
                .Where(x =>
                    x.DernekId == dernekId.Value &&
                    !x.Gonulluler.Any(g =>
                        g.AfadGonulluId == id))
                .OrderBy(x => x.Ad)
                .ToListAsync();

            return View(gonullu);
        }

        // GÖNÜLLÜYE YETKİNLİK ATA
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Ata(
            int id,
            int yetkinlikId,
            string? belgeNo,
            DateTime? belgeTarihi,
            DateTime? gecerlilikBitisi)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            bool gonulluVar = await _db.AfadGonulluler
                .AnyAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value &&
                    x.OnaylandiMi &&
                    x.AktifMi);

            if (!gonulluVar)
                return BadRequest(
                    "Onaylı ve aktif gönüllü bulunamadı.");

            bool yetkinlikVar = await _db.AfadYetkinlikler
                .AnyAsync(x =>
                    x.Id == yetkinlikId &&
                    x.DernekId == dernekId.Value);

            if (!yetkinlikVar)
                return BadRequest("Geçersiz yetkinlik.");

            if (!string.IsNullOrWhiteSpace(belgeNo) &&
                belgeNo.Trim().Length > 200)
                return BadRequest(
                    "Belge numarası en fazla 200 karakter olabilir.");

            if (belgeTarihi.HasValue &&
                gecerlilikBitisi.HasValue &&
                gecerlilikBitisi < belgeTarihi)
                return BadRequest(
                    "Geçerlilik bitişi belge tarihinden önce olamaz.");

            bool mevcut = await _db.AfadGonulluYetkinlikleri
                .AnyAsync(x =>
                    x.AfadGonulluId == id &&
                    x.AfadYetkinlikId == yetkinlikId);

            if (mevcut)
            {
                TempData["AfadYetkinlikHata"] =
                    "Bu yetkinlik gönüllüye zaten atanmış.";
                return RedirectToAction(nameof(Detay), new { id });
            }

            _db.AfadGonulluYetkinlikleri.Add(
                new AfadGonulluYetkinlik
                {
                    AfadGonulluId = id,
                    AfadYetkinlikId = yetkinlikId,
                    BelgeNo = belgeNo?.Trim(),
                    BelgeTarihi = belgeTarihi,
                    GecerlilikBitisi = gecerlilikBitisi
                });

            await _db.SaveChangesAsync();

            TempData["AfadYetkinlikMesaj"] =
                "Yetkinlik gönüllüye atandı.";

            return RedirectToAction(nameof(Detay), new { id });
        }

        // BELGE BİLGİLERİNİ GÜNCELLE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(
            int id,
            int kayitId,
            string? belgeNo,
            DateTime? belgeTarihi,
            DateTime? gecerlilikBitisi)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var kayit = await _db.AfadGonulluYetkinlikleri
                .FirstOrDefaultAsync(x =>
                    x.Id == kayitId &&
                    x.AfadGonulluId == id &&
                    x.Gonullu.DernekId == dernekId.Value &&
                    x.Yetkinlik.DernekId == dernekId.Value);

            if (kayit == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(belgeNo) &&
                belgeNo.Trim().Length > 200)
                return BadRequest(
                    "Belge numarası en fazla 200 karakter olabilir.");

            if (belgeTarihi.HasValue &&
                gecerlilikBitisi.HasValue &&
                gecerlilikBitisi < belgeTarihi)
                return BadRequest(
                    "Geçerlilik bitişi belge tarihinden önce olamaz.");

            kayit.BelgeNo = belgeNo?.Trim();
            kayit.BelgeTarihi = belgeTarihi;
            kayit.GecerlilikBitisi = gecerlilikBitisi;

            await _db.SaveChangesAsync();

            TempData["AfadYetkinlikMesaj"] =
                "Belge bilgileri güncellendi.";

            return RedirectToAction(nameof(Detay), new { id });
        }

        // YETKİNLİK ATAMASINI KALDIR
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Kaldir(
            int id, int kayitId)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var kayit = await _db.AfadGonulluYetkinlikleri
                .FirstOrDefaultAsync(x =>
                    x.Id == kayitId &&
                    x.AfadGonulluId == id &&
                    x.Gonullu.DernekId == dernekId.Value &&
                    x.Yetkinlik.DernekId == dernekId.Value);

            if (kayit == null) return NotFound();

            _db.AfadGonulluYetkinlikleri.Remove(kayit);
            await _db.SaveChangesAsync();

            TempData["AfadYetkinlikMesaj"] =
                "Yetkinlik ataması kaldırıldı.";

            return RedirectToAction(nameof(Detay), new { id });
        }
    }
}
