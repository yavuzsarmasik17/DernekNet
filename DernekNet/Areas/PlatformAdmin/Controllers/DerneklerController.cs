
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DernekNet.Areas.PlatformAdmin.Controllers
{
    [Area("PlatformAdmin")]
    [Authorize(Roles = "PlatformAdmin")]
    public class DerneklerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] TemaRenkleri =
        {
            "#2563EB",
            "#059669",
            "#7C3AED",
            "#EA580C",
            "#BE185D",
            "#0891B2",
            "#4F46E5",
            "#B45309"
        };

        public DerneklerController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // TÜM DERNEKLER
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernekler = await _context.Dernekler
                .AsNoTracking()
                .OrderBy(x => x.DernekAdi)
                .ToListAsync();

            return View(dernekler);
        }

        // DERNEK DETAYLARI
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var dernek = await _context.Dernekler
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dernek == null)
                return NotFound();

            var uyeler = await _context.Uyeler
                .AsNoTracking()
                .Where(x => x.DernekId == id)
                .OrderBy(x => x.Ad)
                .ThenBy(x => x.Soyad)
                .ToListAsync();

            var kurullar = await _context.YonetimKuruluUyeleri
                .AsNoTracking()
                .Include(x => x.Uye)
                .Where(x => x.DernekId == id)
                .OrderBy(x => x.SiraNo)
                .ThenBy(x => x.Id)
                .ToListAsync();

            ViewBag.Uyeler = uyeler;
            ViewBag.Kurullar = kurullar;

            return View(dernek);
        }

        // YENİ DERNEK FORMU
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Dernek
            {
                AktifMi = true
            });
        }

        // YENİ DERNEK KAYDI + LOGO YÜKLEME
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("DernekAdi,Telefon,Email,Adres,Aciklama")]
            Dernek model,
            IFormFile? logoDosyasi)
        {
            model.AnaRenk = "#2563EB";
            model.IkinciRenk = "#FFFFFF";
            model.AktifMi = true;

            ModelState.Remove(nameof(Dernek.AnaRenk));
            ModelState.Remove(nameof(Dernek.IkinciRenk));

            // LogoUrl kullanıcı tarafından doğrudan girilmiyor.
            ModelState.Remove(nameof(Dernek.LogoUrl));

            if (!ModelState.IsValid)
                return View(model);

            // LOGO KONTROLLERİ
            if (logoDosyasi != null && logoDosyasi.Length > 0)
            {
                string uzanti = Path.GetExtension(
                    logoDosyasi.FileName).ToLowerInvariant();

                string[] izinliUzantilar =
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

                if (!izinliUzantilar.Contains(uzanti))
                {
                    ModelState.AddModelError(
                        "logoDosyasi",
                        "Logo JPG, PNG veya WEBP formatında olmalıdır.");

                    return View(model);
                }

                const long maksimumBoyut = 5 * 1024 * 1024;

                if (logoDosyasi.Length > maksimumBoyut)
                {
                    ModelState.AddModelError(
                        "logoDosyasi",
                        "Logo dosyası en fazla 5 MB olabilir.");

                    return View(model);
                }

                // Dosyanın gerçek görüntü türünü kontrol et.
                byte[] baslik = new byte[12];
                int okunan = 0;

                using (var stream = logoDosyasi.OpenReadStream())
                {
                    while (okunan < baslik.Length)
                    {
                        int adet = await stream.ReadAsync(
                            baslik,
                            okunan,
                            baslik.Length - okunan);

                        if (adet == 0)
                            break;

                        okunan += adet;
                    }
                }

                bool png =
                    okunan >= 8 &&
                    baslik[0] == 0x89 &&
                    baslik[1] == 0x50 &&
                    baslik[2] == 0x4E &&
                    baslik[3] == 0x47 &&
                    baslik[4] == 0x0D &&
                    baslik[5] == 0x0A &&
                    baslik[6] == 0x1A &&
                    baslik[7] == 0x0A;

                bool jpeg =
                    okunan >= 3 &&
                    baslik[0] == 0xFF &&
                    baslik[1] == 0xD8 &&
                    baslik[2] == 0xFF;

                bool webp =
                    okunan >= 12 &&
                    baslik[0] == 0x52 &&
                    baslik[1] == 0x49 &&
                    baslik[2] == 0x46 &&
                    baslik[3] == 0x46 &&
                    baslik[8] == 0x57 &&
                    baslik[9] == 0x45 &&
                    baslik[10] == 0x42 &&
                    baslik[11] == 0x50;

                bool gecerliResim =
                    (uzanti == ".png" && png) ||
                    ((uzanti == ".jpg" || uzanti == ".jpeg") && jpeg) ||
                    (uzanti == ".webp" && webp);

                if (!gecerliResim)
                {
                    ModelState.AddModelError(
                        "logoDosyasi",
                        "Seçilen dosya geçerli bir logo görüntüsü değil.");

                    return View(model);
                }

                // LOGOYU KAYDET
                string wwwroot = _environment.WebRootPath
                    ?? Path.Combine(
                        _environment.ContentRootPath,
                        "wwwroot");

                string klasor = Path.Combine(
                    wwwroot,
                    "uploads",
                    "dernekler");

                Directory.CreateDirectory(klasor);

                string dosyaAdi =
                    Guid.NewGuid().ToString("N") + uzanti;

                string tamYol = Path.Combine(
                    klasor,
                    dosyaAdi);

                using (var stream = new FileStream(
                    tamYol,
                    FileMode.CreateNew))
                {
                    await logoDosyasi.CopyToAsync(stream);
                }

                model.LogoUrl =
                    "/uploads/dernekler/" + dosyaAdi;
            }

            // DERNEĞİ VERİTABANINA KAYDET
            model.Id = 0;
            model.KayitTarihi = DateTime.UtcNow;

            _context.Dernekler.Add(model);
            await _context.SaveChangesAsync();

            // DERNEĞE OTOMATİK TEMA RENGİ ATA
            int renkIndex =
                (model.Id - 1) % TemaRenkleri.Length;

            model.AnaRenk = TemaRenkleri[renkIndex];
            model.IkinciRenk = "#FFFFFF";

            await _context.SaveChangesAsync();

            TempData["Basarili"] =
                "Yeni dernek başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        // DÜZENLEME KAPALI
        [HttpGet]
        public IActionResult Edit(int id)
        {
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Dernek model)
        {
            return Forbid();
        }

        // DERNEĞİ AKTİF / PASİF YAP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DurumDegistir(int id)
        {
            var dernek = await _context.Dernekler
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dernek == null)
                return NotFound();

            dernek.AktifMi = !dernek.AktifMi;

            await _context.SaveChangesAsync();

            TempData["Basarili"] = dernek.AktifMi
                ? "Dernek yeniden aktif hale getirildi."
                : "Dernek pasif hale getirildi.";

            return RedirectToAction(nameof(Index));
        }
    }
}
