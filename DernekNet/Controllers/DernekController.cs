
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace DernekNet.Controllers
{
    [AllowAnonymous]
    public class DernekController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DernekController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            var dernek = await _context.Dernekler
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id && x.AktifMi);

            if (dernek == null)
                return NotFound();

            // DUYURULAR
            var duyurular = await _context.Duyurular
                .AsNoTracking()
                .Include(x => x.Medyalar)
                .Where(x =>
                    x.DernekId == id && x.AktifMi)
                .OrderByDescending(x => x.YayinTarihi)
                .ToListAsync();

            // ETKİNLİKLER
            var etkinlikler = await _context.Etkinlikler
                .AsNoTracking()
                .Include(x => x.Medyalar)
                .Where(x =>
                    x.DernekId == id && x.AktifMi)
                .OrderBy(x => x.EtkinlikTarihi)
                .ToListAsync();

            // ANKETLER
            // Yalnızca bu derneğin yayındaki anketleri.
            var simdi = DateTime.UtcNow;

            var anketler = await _context.Anketler
                .AsNoTracking()
                .Include(x => x.Secenekler)
                .Where(x =>
                    x.DernekId == id &&
                    x.AktifMi &&
                    x.BaslangicTarihi <= simdi &&
                    x.BitisTarihi > simdi)
                .OrderByDescending(x => x.BaslangicTarihi)
                .ToListAsync();

            var anketIdleri = anketler.Select(x => x.Id).ToList();

            var anketOySayilari = await _context.AnketOylari
                .AsNoTracking()
                .Where(x => anketIdleri.Contains(x.AnketId))
                .GroupBy(x => x.AnketSecenekId)
                .Select(g => new
                {
                    SecenekId = g.Key,
                    OySayisi = g.Count()
                })
                .ToDictionaryAsync(x => x.SecenekId, x => x.OySayisi);

            ViewBag.AnketOySayilari = anketOySayilari;

            // AKTİF KURULLAR
            var kurullar = await _context.YonetimKuruluUyeleri
                .AsNoTracking()
                .Include(x => x.Uye)
                .Where(x =>
                    x.DernekId == id &&
                    x.AktifMi &&
                    x.Uye != null &&
                    x.Uye.DernekId == id &&
                    x.Uye.AktifMi &&
                    (!x.GorevBitisTarihi.HasValue ||
                     x.GorevBitisTarihi.Value.Date >= DateTime.Today))
                .OrderBy(x => x.SiraNo)
                .ThenBy(x => x.Id)
                .ToListAsync();

            ViewBag.Duyurular = duyurular;
            ViewBag.Etkinlikler = etkinlikler;
            ViewBag.Anketler = anketler;

            ViewBag.YonetimKurulu = kurullar
                .Where(x => x.KurulTipi == "Yönetim Kurulu")
                .ToList();

            ViewBag.DenetimKurulu = kurullar
                .Where(x => x.KurulTipi == "Denetim Kurulu")
                .ToList();

            ViewBag.DisiplinKurulu = kurullar
                .Where(x => x.KurulTipi == "Disiplin Kurulu")
                .ToList();

            return View(dernek);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnketOyVer(
            int dernekId,
            int anketId,
            int secenekId)
        {
            var simdi = DateTime.UtcNow;

            // Dernek, anket ve seçeneğin birbirine ait olduğunu doğrula.
            var anketGecerli = await _context.Anketler
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == anketId &&
                    x.DernekId == dernekId &&
                    x.Dernek.AktifMi &&
                    x.AktifMi &&
                    x.BaslangicTarihi <= simdi &&
                    x.BitisTarihi > simdi &&
                    x.Secenekler.Any(s => s.Id == secenekId));

            if (!anketGecerli)
                return NotFound();

            const string cerezAdi = "DernekNetAnketZiyaretci";

            var ziyaretciAnahtari = Request.Cookies[cerezAdi];

            if (string.IsNullOrWhiteSpace(ziyaretciAnahtari) ||
                ziyaretciAnahtari.Length != 64 ||
                !ziyaretciAnahtari.All(Uri.IsHexDigit))
            {
                ziyaretciAnahtari = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(32));

                Response.Cookies.Append(
                    cerezAdi,
                    ziyaretciAnahtari,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        IsEssential = true,
                        Expires = DateTimeOffset.UtcNow.AddYears(1)
                    });
            }

            var anahtarVerisi = Encoding.UTF8.GetBytes(
                $"{anketId}:{ziyaretciAnahtari}");

            var ziyaretciHash = Convert.ToHexString(
                SHA256.HashData(anahtarVerisi));

            var dahaOnceOyVermis = await _context.AnketOylari
                .AnyAsync(x =>
                    x.AnketId == anketId &&
                    x.ZiyaretciAnahtariHash == ziyaretciHash);

            if (dahaOnceOyVermis)
            {
                TempData["AnketMesaj"] =
                    "Bu ankete daha önce oy kullandınız.";

                return RedirectToAction(
                    nameof(Index),
                    new { id = dernekId });
            }

            _context.AnketOylari.Add(new AnketOy
            {
                AnketId = anketId,
                AnketSecenekId = secenekId,
                ZiyaretciAnahtariHash = ziyaretciHash,
                OyTarihi = simdi
            });

            try
            {
                await _context.SaveChangesAsync();

                TempData["AnketMesaj"] =
                    "Oyunuz başarıyla kaydedildi.";
            }
            catch (DbUpdateException)
            {
                TempData["AnketMesaj"] =
                    "Oyunuz kaydedilemedi veya daha önce oy kullandınız.";
            }

            return RedirectToAction(
                nameof(Index),
                new { id = dernekId });
        }

        // Halka açık sayfada kurul üyesinin fotoğrafını gösterir.
        // Normal üye fotoğraflarını herkese açmaz.
        [HttpGet]
        public async Task<IActionResult> KurulFotograf(
            int dernekId,
            int uyeId)
        {
            bool yayinlanabilir = await _context
                .YonetimKuruluUyeleri
                .AsNoTracking()
                .AnyAsync(x =>
                    x.DernekId == dernekId &&
                    x.UyeId == uyeId &&
                    x.AktifMi &&
                    x.Dernek != null &&
                    x.Dernek.AktifMi &&
                    x.Uye != null &&
                    x.Uye.DernekId == dernekId &&
                    x.Uye.AktifMi &&
                    (!x.GorevBitisTarihi.HasValue ||
                     x.GorevBitisTarihi.Value >= DateTime.Today));

            if (!yayinlanabilir)
                return NotFound();

            var uye = await _context.Uyeler
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == uyeId &&
                    x.DernekId == dernekId);

            if (uye == null ||
                string.IsNullOrWhiteSpace(uye.FotografUrl))
                return NotFound();

            // Dosya adı dışında klasör yolu kabul edilmez.
            string dosyaAdi = Path.GetFileName(uye.FotografUrl);

            string yol = Path.Combine(
                _environment.ContentRootPath,
                "PrivateUploads",
                "Uyeler",
                dosyaAdi);

            if (!System.IO.File.Exists(yol))
                return NotFound();

            string uzanti = Path.GetExtension(dosyaAdi)
                .ToLowerInvariant();

            string icerikTipi = uzanti switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => ""
            };

            if (string.IsNullOrEmpty(icerikTipi))
                return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Cache-Control"] = "no-store";

            return PhysicalFile(yol, icerikTipi);
        }
    }
}
