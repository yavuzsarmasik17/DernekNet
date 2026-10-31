
using System;
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
    public class AfadEkiplerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public AfadEkiplerController(
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
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == user.DernekId.Value &&
                    x.AktifMi);

            return aktif ? user.DernekId : null;
        }

        // EKİP LİSTESİ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var ekipler = await _db.AfadEkipler
                .AsNoTracking()
                .Include(x => x.Uyeler)
                    .ThenInclude(x => x.Gonullu)
                .Where(x => x.DernekId == dernekId.Value)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            return View(ekipler);
        }

        // YENİ EKİP OLUŞTUR
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Olustur(
            string ad, string? aciklama)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            ad = ad?.Trim() ?? "";

            if (ad.Length < 2 || ad.Length > 150)
            {
                TempData["EkipHata"] =
                    "Ekip adı 2-150 karakter olmalıdır.";
                return RedirectToAction(nameof(Index));
            }

            if (aciklama?.Length > 1000)
            {
                TempData["EkipHata"] =
                    "Açıklama en fazla 1000 karakter olabilir.";
                return RedirectToAction(nameof(Index));
            }

            bool mevcut = await _db.AfadEkipler
                .AnyAsync(x =>
                    x.DernekId == dernekId.Value &&
                    x.Ad == ad);

            if (mevcut)
            {
                TempData["EkipHata"] =
                    "Bu isimde bir ekip zaten bulunuyor.";
                return RedirectToAction(nameof(Index));
            }

            _db.AfadEkipler.Add(new AfadEkip
            {
                DernekId = dernekId.Value,
                Ad = ad,
                Aciklama = aciklama?.Trim(),
                AktifMi = true
            });

            await _db.SaveChangesAsync();

            TempData["EkipMesaj"] = "Ekip oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        // EKİP DETAYI
        [HttpGet]
        public async Task<IActionResult> Detay(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var ekip = await _db.AfadEkipler
                .AsNoTracking()
                .Include(x => x.Uyeler)
                    .ThenInclude(x => x.Gonullu)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (ekip == null) return NotFound();

            ViewBag.Gonulluler = await _db.AfadGonulluler
                .AsNoTracking()
                .Where(x =>
                    x.DernekId == dernekId.Value &&
                    x.AktifMi &&
                    x.OnaylandiMi &&
                    !x.EkipUyelikleri.Any(u =>
                        u.AfadEkipId == id))
                .OrderBy(x => x.AdSoyad)
                .ToListAsync();

            return View(ekip);
        }

        // EKİBE GÖNÜLLÜ EKLE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UyeEkle(
            int id, int gonulluId)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            bool ekipVar = await _db.AfadEkipler
                .AnyAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value &&
                    x.AktifMi);

            bool gonulluVar = await _db.AfadGonulluler
                .AnyAsync(x =>
                    x.Id == gonulluId &&
                    x.DernekId == dernekId.Value &&
                    x.AktifMi &&
                    x.OnaylandiMi);

            if (!ekipVar || !gonulluVar)
                return BadRequest(
                    "Aktif ekip veya onaylı gönüllü bulunamadı.");

            bool mevcut = await _db.AfadEkipUyeleri
                .AnyAsync(x =>
                    x.AfadEkipId == id &&
                    x.AfadGonulluId == gonulluId);

            if (mevcut)
            {
                TempData["EkipHata"] =
                    "Bu gönüllü zaten ekipte kayıtlı.";
                return RedirectToAction(nameof(Detay), new { id });
            }

            _db.AfadEkipUyeleri.Add(new AfadEkipUye
            {
                AfadEkipId = id,
                AfadGonulluId = gonulluId,
                AktifMi = true,
                EkipLideriMi = false
            });

            await _db.SaveChangesAsync();

            TempData["EkipMesaj"] =
                "Gönüllü ekibe eklendi.";

            return RedirectToAction(nameof(Detay), new { id });
        }

        // EKİP LİDERİ BELİRLE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LiderYap(
            int id, int uyeId)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var ekip = await _db.AfadEkipler
                .Include(x => x.Uyeler)
                    .ThenInclude(x => x.Gonullu)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value &&
                    x.AktifMi);

            if (ekip == null) return NotFound();

            var secilen = ekip.Uyeler
                .FirstOrDefault(x =>
                    x.Id == uyeId &&
                    x.AktifMi &&
                    x.Gonullu.DernekId == dernekId.Value &&
                    x.Gonullu.AktifMi &&
                    x.Gonullu.OnaylandiMi);

            if (secilen == null)
                return BadRequest(
                    "Aktif ekip üyesi bulunamadı.");

            // Önce eski liderleri kaldır.
            foreach (var uye in ekip.Uyeler)
                uye.EkipLideriMi = false;

            // Sonra yeni lideri ata.
            secilen.EkipLideriMi = true;

            await _db.SaveChangesAsync();

            TempData["EkipMesaj"] =
                "Ekip lideri güncellendi.";

            return RedirectToAction(nameof(Detay), new { id });
        }

        // EKİP ÜYESİNİ PASİFE AL
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UyePasifYap(
            int id, int uyeId)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var uye = await _db.AfadEkipUyeleri
                .FirstOrDefaultAsync(x =>
                    x.Id == uyeId &&
                    x.AfadEkipId == id &&
                    x.Ekip.DernekId == dernekId.Value);

            if (uye == null) return NotFound();

            uye.AktifMi = false;
            uye.EkipLideriMi = false;

            await _db.SaveChangesAsync();

            TempData["EkipMesaj"] =
                "Ekip üyesi pasife alındı.";

            return RedirectToAction(nameof(Detay), new { id });
        }

        // PASİF ÜYEYİ YENİDEN AKTİF ET
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UyeAktifYap(
            int id, int uyeId)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var uye = await _db.AfadEkipUyeleri
                .Include(x => x.Gonullu)
                .FirstOrDefaultAsync(x =>
                    x.Id == uyeId &&
                    x.AfadEkipId == id &&
                    x.Ekip.DernekId == dernekId.Value &&
                    x.Ekip.AktifMi);

            if (uye == null) return NotFound();

            if (!uye.Gonullu.AktifMi ||
                !uye.Gonullu.OnaylandiMi ||
                uye.Gonullu.DernekId != dernekId.Value)
                return BadRequest(
                    "Bu gönüllü aktif veya onaylı değil.");

            uye.AktifMi = true;
            uye.EkipLideriMi = false;

            await _db.SaveChangesAsync();

            TempData["EkipMesaj"] =
                "Ekip üyesi yeniden aktifleştirildi.";

            return RedirectToAction(nameof(Detay), new { id });
        }
    }
}
