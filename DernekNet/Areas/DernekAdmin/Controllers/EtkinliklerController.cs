
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DernekNet.Areas.DernekAdmin.Controllers
{
    [Area("DernekAdmin")]
    [Authorize(Roles = "DernekAdmin")]
    public class EtkinliklerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        private const int MaksimumDosya = 10;
        private const long MaksimumFotoBoyutu = 5 * 1024 * 1024;
        private const long MaksimumVideoBoyutu = 50 * 1024 * 1024;

        public EtkinliklerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // Yöneticinin bağlı olduğu aktif derneği bul.
        private async Task<int?> DernekIdGetir()
        {
            var kullanici = await _userManager.GetUserAsync(User);

            if (kullanici == null ||
                !kullanici.EmailConfirmed ||
                !kullanici.DernekId.HasValue)
            {
                return null;
            }

            bool dernekAktif = await _context.Dernekler
                .AnyAsync(x =>
                    x.Id == kullanici.DernekId.Value &&
                    x.AktifMi);

            if (!dernekAktif)
                return null;

            return kullanici.DernekId.Value;
        }

        // ETKİNLİK LİSTESİ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var etkinlikler = await _context.Etkinlikler
                .AsNoTracking()
                .Where(x => x.DernekId == dernekId.Value)
                .OrderByDescending(x => x.EtkinlikTarihi)
                .ToListAsync();

            return View(etkinlikler);
        }

        // YENİ ETKİNLİK FORMU
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            return View(new Etkinlik
            {
                AktifMi = true,
                EtkinlikTarihi = DateTime.Today.AddDays(1)
            });
        }

        // YENİ ETKİNLİK KAYDET
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("EtkinlikAdi,Aciklama,EtkinlikTarihi,Konum,AktifMi")]
            Etkinlik model,
            List<IFormFile>? medyaDosyalari)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            ModelState.Remove(nameof(Etkinlik.DernekId));

            var dosyalar = medyaDosyalari?
                .Where(x => x != null && x.Length > 0)
                .ToList() ?? new List<IFormFile>();

            if (!MedyalariDogrula(dosyalar))
                return View(model);

            if (!ModelState.IsValid)
                return View(model);

            var etkinlik = new Etkinlik
            {
                DernekId = dernekId.Value,
                EtkinlikAdi = model.EtkinlikAdi.Trim(),
                Aciklama = model.Aciklama,
                Konum = model.Konum,
                EtkinlikTarihi = model.EtkinlikTarihi,
                AktifMi = model.AktifMi
            };

            _context.Etkinlikler.Add(etkinlik);
            await _context.SaveChangesAsync();

            if (dosyalar.Any())
            {
                try
                {
                    await MedyalariKaydet(
                        etkinlik,
                        dernekId.Value,
                        dosyalar);
                }
                catch
                {
                    ModelState.AddModelError(
                        "",
                        "Etkinlik oluşturuldu ancak dosyalar yüklenemedi.");

                    return View(model);
                }
            }

            TempData["Basarili"] =
                "Etkinlik başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        // ETKİNLİK DÜZENLE
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var etkinlik = await _context.Etkinlikler
                .AsNoTracking()
                .Include(x => x.Medyalar)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (etkinlik == null)
                return NotFound();

            return View(etkinlik);
        }

        // ETKİNLİK GÜNCELLE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,EtkinlikAdi,Aciklama,EtkinlikTarihi,Konum,AktifMi")]
            Etkinlik model,
            List<IFormFile>? medyaDosyalari)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            if (id != model.Id)
                return BadRequest();

            ModelState.Remove(nameof(Etkinlik.DernekId));

            var dosyalar = medyaDosyalari?
                .Where(x => x != null && x.Length > 0)
                .ToList() ?? new List<IFormFile>();

            var etkinlik = await _context.Etkinlikler
                .Include(x => x.Medyalar)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (etkinlik == null)
                return NotFound();

            if (!MedyalariDogrula(dosyalar) ||
                !ModelState.IsValid)
            {
                model.Medyalar = etkinlik.Medyalar;
                return View(model);
            }

            etkinlik.EtkinlikAdi = model.EtkinlikAdi.Trim();
            etkinlik.Aciklama = model.Aciklama;
            etkinlik.Konum = model.Konum;
            etkinlik.EtkinlikTarihi = model.EtkinlikTarihi;
            etkinlik.AktifMi = model.AktifMi;

            await _context.SaveChangesAsync();

            if (dosyalar.Any())
            {
                try
                {
                    await MedyalariKaydet(
                        etkinlik,
                        dernekId.Value,
                        dosyalar);
                }
                catch
                {
                    TempData["Hata"] =
                        "Etkinlik güncellendi ancak bazı dosyalar yüklenemedi.";

                    return RedirectToAction(
                        nameof(Edit),
                        new { id });
                }
            }

            TempData["Basarili"] =
                "Etkinlik başarıyla güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        // ETKİNLİĞİ VE ONA AİT MEDYALARI KALICI SİL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            int? dernekId = await DernekIdGetir();
            if (!dernekId.HasValue)
                return Forbid();

            var etkinlik = await _context.Etkinlikler
                .Include(x => x.Medyalar)
                .FirstOrDefaultAsync(x =>
                    x.Id == id && x.DernekId == dernekId.Value);

            if (etkinlik == null)
                return NotFound();

            // İlişkili medya kayıtlarını önce sil; FK kısıtlarını ihlal etme.
            _context.EtkinlikMedyalari.RemoveRange(etkinlik.Medyalar);
            _context.Etkinlikler.Remove(etkinlik);
            await _context.SaveChangesAsync();

            // Yalnızca bu derneğin, bu etkinliğe özel klasörünü temizle.
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            string klasor = Path.Combine(webRoot, "uploads", "etkinlikler",
                dernekId.Value.ToString(), id.ToString());

            try
            {
                if (Directory.Exists(klasor))
                    Directory.Delete(klasor, recursive: true);
            }
            catch (IOException)
            {
                TempData["Hata"] = "Etkinlik silindi ancak medya klasörü temizlenemedi.";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Hata"] = "Etkinlik silindi ancak medya klasörü için erişim izni yok.";
            }

            TempData["Basarili"] = "Etkinlik başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        // FOTOĞRAF / VİDEO SİL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MedyaSil(int id)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var medya = await _context.EtkinlikMedyalari
                .Include(x => x.Etkinlik)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Etkinlik != null &&
                    x.Etkinlik.DernekId == dernekId.Value);

            if (medya == null)
                return NotFound();

            int etkinlikId = medya.EtkinlikId;

            string klasor = Path.GetFullPath(Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "etkinlikler",
                dernekId.Value.ToString(),
                etkinlikId.ToString()));

            string dosyaAdi = Path.GetFileName(medya.DosyaYolu);

            string fizikselYol = Path.GetFullPath(
                Path.Combine(klasor, dosyaAdi));

            if (Path.GetDirectoryName(fizikselYol) != klasor)
                return BadRequest();

            _context.EtkinlikMedyalari.Remove(medya);
            await _context.SaveChangesAsync();

            if (System.IO.File.Exists(fizikselYol))
                System.IO.File.Delete(fizikselYol);

            TempData["Basarili"] =
                "Etkinlik dosyası silindi.";

            return RedirectToAction(
                nameof(Edit),
                new { id = etkinlikId });
        }

        // DOSYALARI DOĞRULA
        private bool MedyalariDogrula(List<IFormFile> dosyalar)
        {
            if (dosyalar.Count > MaksimumDosya)
            {
                ModelState.AddModelError(
                    "",
                    "En fazla 10 dosya yükleyebilirsiniz.");
                return false;
            }

            foreach (var dosya in dosyalar)
            {
                string uzanti = Path.GetExtension(
                    dosya.FileName).ToLowerInvariant();

                bool videoMu =
                    uzanti == ".mp4" ||
                    uzanti == ".webm";

                bool fotografMu =
                    uzanti == ".jpg" ||
                    uzanti == ".jpeg" ||
                    uzanti == ".png" ||
                    uzanti == ".webp";

                if (!videoMu && !fotografMu)
                {
                    ModelState.AddModelError(
                        "",
                        "Yalnızca JPG, PNG, WEBP, MP4 ve WEBM yüklenebilir.");
                    return false;
                }

                long limit = videoMu
                    ? MaksimumVideoBoyutu
                    : MaksimumFotoBoyutu;

                if (dosya.Length <= 0 ||
                    dosya.Length > limit)
                {
                    ModelState.AddModelError(
                        "",
                        $"Dosya boyutu geçersiz: {dosya.FileName}");
                    return false;
                }
            }

            return true;
        }

        // MEDYALARI DİSKE VE VERİTABANINA KAYDET
        private async Task MedyalariKaydet(
            Etkinlik etkinlik,
            int dernekId,
            List<IFormFile> dosyalar)
        {
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");

            string klasor = Path.Combine(
                webRoot,
                "uploads",
                "etkinlikler",
                dernekId.ToString(),
                etkinlik.Id.ToString());

            Directory.CreateDirectory(klasor);

            foreach (var dosya in dosyalar)
            {
                string uzanti = Path.GetExtension(
                    dosya.FileName).ToLowerInvariant();

                string dosyaAdi =
                    Guid.NewGuid().ToString("N") + uzanti;

                string fizikselYol = Path.Combine(
                    klasor,
                    dosyaAdi);

                using (var stream = new FileStream(
                    fizikselYol,
                    FileMode.CreateNew))
                {
                    await dosya.CopyToAsync(stream);
                }

                string url =
                    $"/uploads/etkinlikler/{dernekId}/{etkinlik.Id}/{dosyaAdi}";

                _context.EtkinlikMedyalari.Add(
                    new EtkinlikMedya
                    {
                        EtkinlikId = etkinlik.Id,
                        DosyaYolu = url,
                        MedyaTipi =
                            uzanti == ".mp4" || uzanti == ".webm"
                                ? "Video"
                                : "Fotoğraf",
                        YuklenmeTarihi = DateTime.UtcNow
                    });
            }

            await _context.SaveChangesAsync();
        }
    }
}
