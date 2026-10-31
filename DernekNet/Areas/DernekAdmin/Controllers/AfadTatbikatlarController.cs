
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
    public class AfadTatbikatlarController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public AfadTatbikatlarController(
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

        // TATBİKAT LİSTESİ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var tatbikatlar = await _db.AfadTatbikatlar
                .AsNoTracking()
                .Include(x => x.Katilimlar)
                .Where(x => x.DernekId == dernekId.Value)
                .OrderByDescending(x => x.Tarih)
                .ToListAsync();

            return View(tatbikatlar);
        }

        // YENİ TATBİKAT FORMU
        [HttpGet]
        public async Task<IActionResult> Olustur()
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            return View("Form", new AfadTatbikat
            {
                Tarih = DateTime.Now,
                AktifMi = true
            });
        }

        // TATBİKAT KAYDET
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
                TempData["AfadTatbikatHata"] =
                    "Geçerli bir başlık ve tarih giriniz.";

                return RedirectToAction(nameof(Olustur));
            }

            _db.AfadTatbikatlar.Add(new AfadTatbikat
            {
                DernekId = dernekId.Value,
                Baslik = baslik.Trim(),
                Aciklama = aciklama?.Trim(),
                Tarih = tarih,
                AktifMi = aktifMi
            });

            await _db.SaveChangesAsync();

            TempData["AfadTatbikatMesaj"] =
                "Tatbikat oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        // TATBİKAT DÜZENLEME FORMU
        [HttpGet]
        public async Task<IActionResult> Duzenle(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var tatbikat = await _db.AfadTatbikatlar
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (tatbikat == null) return NotFound();

            return View("Form", tatbikat);
        }

        // TATBİKAT GÜNCELLE
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

            var tatbikat = await _db.AfadTatbikatlar
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (tatbikat == null) return NotFound();

            if (string.IsNullOrWhiteSpace(baslik) ||
                baslik.Trim().Length > 200 ||
                tarih == default)
            {
                TempData["AfadTatbikatHata"] =
                    "Geçerli bir başlık ve tarih giriniz.";

                return RedirectToAction(
                    nameof(Duzenle), new { id });
            }

            tatbikat.Baslik = baslik.Trim();
            tatbikat.Aciklama = aciklama?.Trim();
            tatbikat.Tarih = tarih;
            tatbikat.AktifMi = aktifMi;

            await _db.SaveChangesAsync();

            TempData["AfadTatbikatMesaj"] =
                "Tatbikat güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        // TATBİKAT KATILIMLARI
        [HttpGet]
        public async Task<IActionResult> Katilimlar(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var tatbikat = await _db.AfadTatbikatlar
                .AsNoTracking()
                .Include(x => x.Katilimlar)
                .ThenInclude(x => x.Gonullu)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (tatbikat == null) return NotFound();

            ViewBag.Gonulluler = await _db.AfadGonulluler
                .AsNoTracking()
                .Where(x =>
                    x.DernekId == dernekId.Value &&
                    x.OnaylandiMi &&
                    x.AktifMi &&
                    !x.TatbikatKatilimlari.Any(k =>
                        k.AfadTatbikatId == id))
                .OrderBy(x => x.AdSoyad)
                .ToListAsync();

            return View(tatbikat);
        }

        // GÖNÜLLÜYÜ TATBİKATA ATA
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Ata(
            int id,
            int gonulluId,
            string? gorevi)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            bool tatbikatVar = await _db.AfadTatbikatlar
                .AnyAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value &&
                    x.AktifMi);

            if (!tatbikatVar) return NotFound();

            bool gonulluVar = await _db.AfadGonulluler
                .AnyAsync(x =>
                    x.Id == gonulluId &&
                    x.DernekId == dernekId.Value &&
                    x.OnaylandiMi &&
                    x.AktifMi);

            if (!gonulluVar)
                return BadRequest(
                    "Onaylı ve aktif gönüllü bulunamadı.");

            if (!string.IsNullOrWhiteSpace(gorevi) &&
                gorevi.Trim().Length > 150)
                return BadRequest(
                    "Tatbikat görevi en fazla 150 karakter olabilir.");

            bool mevcut = await _db.AfadTatbikatKatilimlari
                .AnyAsync(x =>
                    x.AfadTatbikatId == id &&
                    x.AfadGonulluId == gonulluId);

            if (mevcut)
            {
                TempData["AfadTatbikatMesaj"] =
                    "Bu gönüllü zaten tatbikata atanmış.";

                return RedirectToAction(
                    nameof(Katilimlar), new { id });
            }

            _db.AfadTatbikatKatilimlari.Add(
                new AfadTatbikatKatilim
                {
                    AfadTatbikatId = id,
                    AfadGonulluId = gonulluId,
                    Gorevi = gorevi?.Trim(),
                    KatildiMi = false
                });

            await _db.SaveChangesAsync();

            TempData["AfadTatbikatMesaj"] =
                "Gönüllü tatbikata atandı.";

            return RedirectToAction(
                nameof(Katilimlar), new { id });
        }

        // KATILIM VE GÖREV GÜNCELLE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> KatilimGuncelle(
            int id,
            int katilimId,
            bool katildiMi,
            string? gorevi)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var katilim = await _db.AfadTatbikatKatilimlari
                .FirstOrDefaultAsync(x =>
                    x.Id == katilimId &&
                    x.AfadTatbikatId == id &&
                    x.Tatbikat.DernekId == dernekId.Value &&
                    x.Gonullu.DernekId == dernekId.Value);

            if (katilim == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(gorevi) &&
                gorevi.Trim().Length > 150)
                return BadRequest(
                    "Tatbikat görevi en fazla 150 karakter olabilir.");

            katilim.KatildiMi = katildiMi;
            katilim.Gorevi = gorevi?.Trim();

            await _db.SaveChangesAsync();

            TempData["AfadTatbikatMesaj"] =
                "Tatbikat katılımı güncellendi.";

            return RedirectToAction(
                nameof(Katilimlar), new { id });
        }
    }
}
