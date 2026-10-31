
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DernekNet.Areas.DernekAdmin.Controllers
{
    [Area("DernekAdmin")]
    [Authorize(Roles = "DernekAdmin")]
    public class YonetimKuruluController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly string[] KurulTipleri =
        {
            "Yönetim Kurulu",
            "Denetim Kurulu",
            "Disiplin Kurulu"
        };

        public YonetimKuruluController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int?> DernekIdGetir()
        {
            var kullanici = await _userManager.GetUserAsync(User);

            if (kullanici == null ||
                !kullanici.EmailConfirmed ||
                !kullanici.DernekId.HasValue)
                return null;

            bool aktifDernek = await _context.Dernekler
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == kullanici.DernekId.Value &&
                    x.AktifMi);

            return aktifDernek ? kullanici.DernekId : null;
        }

        private static string KurulTipiniDogrula(string? kurul)
        {
            return KurulTipleri.Contains(kurul)
                ? kurul!
                : "Yönetim Kurulu";
        }

        private async Task UyeleriYukle(int dernekId)
        {
            var uyeler = await _context.Uyeler
                .AsNoTracking()
                .Where(x => x.DernekId == dernekId && x.AktifMi)
                .OrderBy(x => x.Ad)
                .ThenBy(x => x.Soyad)
                .Select(x => new
                {
                    x.Id,
                    x.Ad,
                    x.Soyad
                })
                .ToListAsync();

            ViewBag.Uyeler = uyeler.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Ad + " " + x.Soyad
            }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? kurul)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            string seciliKurul = KurulTipiniDogrula(kurul);

            var liste = await _context.YonetimKuruluUyeleri
                .AsNoTracking()
                .Include(x => x.Uye)
                .Where(x =>
                    x.DernekId == dernekId.Value &&
                    x.KurulTipi == seciliKurul)
                .OrderBy(x => x.SiraNo)
                .ThenBy(x => x.Id)
                .ToListAsync();

            ViewBag.SeciliKurul = seciliKurul;

            return View(liste);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string? kurul)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            await UyeleriYukle(dernekId.Value);

            return View(new YonetimKurulu
            {
                KurulTipi = KurulTipiniDogrula(kurul),
                GorevBaslangicTarihi = DateTime.Today,
                AktifMi = true,
                SiraNo = 1
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("UyeId,KurulTipi,Gorev,GorevBaslangicTarihi,GorevBitisTarihi,AktifMi,SiraNo,Aciklama")]
            YonetimKurulu model)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            model.DernekId = dernekId.Value;

            ModelState.Remove(nameof(YonetimKurulu.DernekId));
            ModelState.Remove(nameof(YonetimKurulu.Dernek));
            ModelState.Remove(nameof(YonetimKurulu.Uye));

            if (!KurulTipleri.Contains(model.KurulTipi))
            {
                ModelState.AddModelError(
                    nameof(model.KurulTipi),
                    "Geçerli bir kurul seçiniz.");
            }

            bool uyeGecerli = await _context.Uyeler
                .AnyAsync(x =>
                    x.Id == model.UyeId &&
                    x.DernekId == dernekId.Value &&
                    x.AktifMi);

            if (!uyeGecerli)
            {
                ModelState.AddModelError(
                    nameof(model.UyeId),
                    "Derneğin aktif üyelerinden birini seçiniz.");
            }

            if (model.GorevBitisTarihi.HasValue &&
                model.GorevBitisTarihi.Value.Date <
                model.GorevBaslangicTarihi.Date)
            {
                ModelState.AddModelError(
                    nameof(model.GorevBitisTarihi),
                    "Bitiş tarihi başlangıçtan önce olamaz.");
            }

            if (model.AktifMi && uyeGecerli &&
                KurulTipleri.Contains(model.KurulTipi))
            {
                bool zatenGorevli = await _context
                    .YonetimKuruluUyeleri
                    .AnyAsync(x =>
                        x.DernekId == dernekId.Value &&
                        x.UyeId == model.UyeId &&
                        x.KurulTipi == model.KurulTipi &&
                        x.AktifMi);

                if (zatenGorevli)
                {
                    ModelState.AddModelError(
                        nameof(model.UyeId),
                        "Bu üyenin seçilen kurulda zaten aktif görevi var.");
                }
            }

            if (!ModelState.IsValid)
            {
                await UyeleriYukle(dernekId.Value);
                return View(model);
            }

            _context.YonetimKuruluUyeleri.Add(model);
            await _context.SaveChangesAsync();

            TempData["Basarili"] =
                "Üyeye kurul görevi başarıyla verildi.";

            return RedirectToAction(nameof(Index),
                new { kurul = model.KurulTipi });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoreviBitir(int id)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var kayit = await _context.YonetimKuruluUyeleri
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (kayit == null)
                return NotFound();

            if (kayit.AktifMi)
            {
                kayit.AktifMi = false;
                kayit.GorevBitisTarihi = DateTime.Today;

                await _context.SaveChangesAsync();
            }

            TempData["Basarili"] =
                "Kurul görevi sona erdirildi. Normal üyelik devam ediyor.";

            return RedirectToAction(nameof(Index),
                new { kurul = kayit.KurulTipi });
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var kayit = await _context.YonetimKuruluUyeleri
                .AsNoTracking()
                .Include(x => x.Uye)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (kayit == null)
                return NotFound();

            await UyeleriYukle(dernekId.Value);

            return View(kayit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("UyeId,KurulTipi,Gorev,GorevBaslangicTarihi,GorevBitisTarihi,AktifMi,SiraNo,Aciklama")]
    YonetimKurulu model)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var kayit = await _context.YonetimKuruluUyeleri
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (kayit == null)
                return NotFound();

            // Kurul üyesinin kimliği değiştirilmez.
            // Başka bir üye için yeni görev kaydı oluşturulur.
            ModelState.Remove(nameof(YonetimKurulu.UyeId));
            ModelState.Remove(nameof(YonetimKurulu.DernekId));
            ModelState.Remove(nameof(YonetimKurulu.Dernek));
            ModelState.Remove(nameof(YonetimKurulu.Uye));

            if (!KurulTipleri.Contains(model.KurulTipi))
            {
                ModelState.AddModelError(
                    nameof(model.KurulTipi),
                    "Geçerli bir kurul seçiniz.");
            }

            if (model.GorevBitisTarihi.HasValue &&
                model.GorevBitisTarihi.Value.Date <
                model.GorevBaslangicTarihi.Date)
            {
                ModelState.AddModelError(
                    nameof(model.GorevBitisTarihi),
                    "Bitiş tarihi başlangıçtan önce olamaz.");
            }

            if (model.AktifMi)
            {
                bool zatenGorevli = await _context.YonetimKuruluUyeleri
                    .AnyAsync(x =>
                        x.Id != id &&
                        x.DernekId == dernekId.Value &&
                        x.UyeId == kayit.UyeId &&
                        x.KurulTipi == model.KurulTipi &&
                        x.AktifMi);

                if (zatenGorevli)
                {
                    ModelState.AddModelError(
                        nameof(model.KurulTipi),
                        "Bu üyenin seçilen kurulda zaten aktif görevi var.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.Id = id;
                model.UyeId = kayit.UyeId;
                await UyeleriYukle(dernekId.Value);
                return View(model);
            }

            kayit.KurulTipi = model.KurulTipi;
            kayit.Gorev = model.Gorev;
            kayit.GorevBaslangicTarihi = model.GorevBaslangicTarihi;
            kayit.GorevBitisTarihi = model.GorevBitisTarihi;
            kayit.AktifMi = model.AktifMi;
            kayit.SiraNo = model.SiraNo;
            kayit.Aciklama = model.Aciklama;

            await _context.SaveChangesAsync();

            TempData["Basarili"] =
                "Kurul görevi başarıyla güncellendi.";

            return RedirectToAction(nameof(Index),
                new { kurul = kayit.KurulTipi });
        }

    }
}
