
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
    public class DuyurularController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        private const int MaksimumDosyaSayisi = 10;
        private const long MaksimumFotografBoyutu = 5 * 1024 * 1024;
        private const long MaksimumVideoBoyutu = 50 * 1024 * 1024;

        public DuyurularController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        private async Task<int?> DernekIdGetir()
        {
            var kullanici = await _userManager.GetUserAsync(User);

            if (kullanici == null ||
                !kullanici.EmailConfirmed ||
                !kullanici.DernekId.HasValue)
                return null;

            bool aktif = await _context.Dernekler.AnyAsync(x =>
                x.Id == kullanici.DernekId.Value && x.AktifMi);

            return aktif ? kullanici.DernekId : null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int? dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var duyurular = await _context.Duyurular
                .AsNoTracking()
                .Where(x => x.DernekId == dernekId.Value)
                .OrderByDescending(x => x.YayinTarihi)
                .ToListAsync();

            return View(duyurular);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await DernekYetkisiVarMi()) return Forbid();

            return View(new Duyuru { AktifMi = true });
        }

        private async Task<bool> DernekYetkisiVarMi()
        {
            return (await DernekIdGetir()).HasValue;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Baslik,Icerik,AktifMi")] Duyuru model,
            List<IFormFile>? medyaDosyalari)
        {
            int? dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            ModelState.Remove(nameof(Duyuru.DernekId));

            MedyalariDogrula(medyaDosyalari);

            if (!ModelState.IsValid)
                return View(model);

            var duyuru = new Duyuru
            {
                DernekId = dernekId.Value,
                Baslik = model.Baslik.Trim(),
                Icerik = model.Icerik.Trim(),
                AktifMi = model.AktifMi,
                YayinTarihi = DateTime.UtcNow
            };

            _context.Duyurular.Add(duyuru);
            await _context.SaveChangesAsync();

            try
            {
                await MedyalariKaydet(
                    duyuru, dernekId.Value, medyaDosyalari);

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                ModelState.AddModelError("",
                    "Medya dosyaları kaydedilemedi. Lütfen tekrar deneyin.");

                return View(model);
            }

            TempData["Basarili"] = "Duyuru başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            int? dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var duyuru = await _context.Duyurular
                .AsNoTracking()
                .Include(x => x.Medyalar)
                .FirstOrDefaultAsync(x =>
                    x.Id == id && x.DernekId == dernekId.Value);

            if (duyuru == null) return NotFound();

            return View(duyuru);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Baslik,Icerik,AktifMi")] Duyuru model,
            List<IFormFile>? medyaDosyalari)
        {
            int? dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            if (id != model.Id) return BadRequest();

            var duyuru = await _context.Duyurular
                .Include(x => x.Medyalar)
                .FirstOrDefaultAsync(x =>
                    x.Id == id && x.DernekId == dernekId.Value);

            if (duyuru == null) return NotFound();

            ModelState.Remove(nameof(Duyuru.DernekId));
            MedyalariDogrula(medyaDosyalari);

            if (!ModelState.IsValid)
                return View(duyuru);

            duyuru.Baslik = model.Baslik.Trim();
            duyuru.Icerik = model.Icerik.Trim();
            duyuru.AktifMi = model.AktifMi;

            try
            {
                await MedyalariKaydet(
                    duyuru, dernekId.Value, medyaDosyalari);

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                ModelState.AddModelError("",
                    "Dosyalar kaydedilemedi. Lütfen tekrar deneyin.");

                return View(duyuru);
            }

            TempData["Basarili"] = "Duyuru başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // Dernek yöneticisi yalnızca kendi derneğinin duyurusunu silebilir.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int? dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var duyuru = await _context.Duyurular
                .Include(x => x.Medyalar)
                .FirstOrDefaultAsync(x =>
                    x.Id == id && x.DernekId == dernekId.Value);

            if (duyuru == null) return NotFound();

            // Önce dosya adreslerini sakla, ardından ilişkili kayıtları sil.
            var dosyaYollari = duyuru.Medyalar
                .Select(x => x.DosyaYolu)
                .ToList();

            _context.DuyuruMedyalari.RemoveRange(duyuru.Medyalar);
            _context.Duyurular.Remove(duyuru);
            await _context.SaveChangesAsync();

            // Veritabanı silindikten sonra yalnızca bu duyurunun
            // kendi yükleme klasöründeki dosyaları temizle.
            string onEk = $"/uploads/duyurular/{dernekId.Value}/{id}/";
            bool dosyaHatasi = false;

            foreach (var yol in dosyaYollari)
            {
                if (string.IsNullOrWhiteSpace(yol) ||
                    !yol.StartsWith(onEk, StringComparison.Ordinal))
                    continue;

                string dosyaAdi = yol.Substring(onEk.Length);
                if (string.IsNullOrWhiteSpace(dosyaAdi) ||
                    Path.GetFileName(dosyaAdi) != dosyaAdi)
                    continue;

                string fizikselYol = Path.Combine(
                    _environment.WebRootPath,
                    "uploads", "duyurular",
                    dernekId.Value.ToString(),
                    id.ToString(), dosyaAdi);

                try
                {
                    if (System.IO.File.Exists(fizikselYol))
                        System.IO.File.Delete(fizikselYol);
                }
                catch (IOException)
                {
                    dosyaHatasi = true;
                }
                catch (UnauthorizedAccessException)
                {
                    dosyaHatasi = true;
                }
            }

            TempData["Basarili"] = dosyaHatasi
                ? "Duyuru silindi; bazı medya dosyaları sunucudan temizlenemedi."
                : "Duyuru ve ilişkili medya kayıtları başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MedyaSil(int id)
        {
            int? dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var medya = await _context.DuyuruMedyalari
                .Include(x => x.Duyuru)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Duyuru != null &&
                    x.Duyuru.DernekId == dernekId.Value);

            if (medya == null) return NotFound();

            int duyuruId = medya.DuyuruId;
            string dosyaYolu = medya.DosyaYolu;

            _context.DuyuruMedyalari.Remove(medya);
            await _context.SaveChangesAsync();

            // Yalnızca sistemin duyuru yükleme klasöründeki
            // dosyaları silmeye izin ver.
            string beklenenOnEk =
                $"/uploads/duyurular/{dernekId.Value}/{duyuruId}/";

            if (dosyaYolu.StartsWith(
                    beklenenOnEk, StringComparison.Ordinal) &&
                Path.GetFileName(dosyaYolu) ==
                dosyaYolu.Substring(beklenenOnEk.Length))
            {
                string fizikselYol = Path.Combine(
                    _environment.WebRootPath,
                    "uploads", "duyurular",
                    dernekId.Value.ToString(),
                    duyuruId.ToString(),
                    Path.GetFileName(dosyaYolu));

                if (System.IO.File.Exists(fizikselYol))
                    System.IO.File.Delete(fizikselYol);
            }

            TempData["Basarili"] = "Medya dosyası silindi.";

            return RedirectToAction(nameof(Edit),
                new { id = duyuruId });
        }

        private void MedyalariDogrula(
            List<IFormFile>? dosyalar)
        {
            if (dosyalar == null || dosyalar.Count == 0)
                return;

            if (dosyalar.Count > MaksimumDosyaSayisi)
            {
                ModelState.AddModelError("medyaDosyalari",
                    "Tek seferde en fazla 10 dosya yükleyebilirsiniz.");
                return;
            }

            foreach (var dosya in dosyalar)
            {
                if (dosya.Length == 0)
                {
                    ModelState.AddModelError("medyaDosyalari",
                        "Boş dosya yüklenemez.");
                    continue;
                }

                string uzanti = Path.GetExtension(
                    dosya.FileName).ToLowerInvariant();

                bool fotograf = uzanti == ".jpg" ||
                                uzanti == ".jpeg" ||
                                uzanti == ".png" ||
                                uzanti == ".webp";

                bool video = uzanti == ".mp4" ||
                             uzanti == ".webm";

                if (!fotograf && !video)
                {
                    ModelState.AddModelError("medyaDosyalari",
                        "Yalnızca JPG, PNG, WEBP, MP4 ve WEBM yüklenebilir.");
                    continue;
                }

                long limit = fotograf
                    ? MaksimumFotografBoyutu
                    : MaksimumVideoBoyutu;

                if (dosya.Length > limit)
                {
                    ModelState.AddModelError("medyaDosyalari",
                        $"{dosya.FileName} izin verilen boyutu aşıyor.");
                }
            }
        }

        private async Task MedyalariKaydet(
            Duyuru duyuru,
            int dernekId,
            List<IFormFile>? dosyalar)
        {
            if (dosyalar == null || dosyalar.Count == 0)
                return;

            string klasor = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "duyurular",
                dernekId.ToString(),
                duyuru.Id.ToString());

            Directory.CreateDirectory(klasor);

            foreach (var dosya in dosyalar)
            {
                string uzanti = Path.GetExtension(
                    dosya.FileName).ToLowerInvariant();

                bool video = uzanti == ".mp4" ||
                             uzanti == ".webm";

                string dosyaAdi =
                    $"{Guid.NewGuid():N}{uzanti}";

                string fizikselYol =
                    Path.Combine(klasor, dosyaAdi);

                using (var stream = new FileStream(
                    fizikselYol, FileMode.CreateNew))
                {
                    await dosya.CopyToAsync(stream);
                }

                string url =
                    $"/uploads/duyurular/{dernekId}/{duyuru.Id}/{dosyaAdi}";

                _context.DuyuruMedyalari.Add(new DuyuruMedya
                {
                    DuyuruId = duyuru.Id,
                    DosyaYolu = url,
                    MedyaTipi = video ? "Video" : "Fotoğraf",
                    YuklenmeTarihi = DateTime.UtcNow
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var duyuru = await _context.Duyurular
                .Include(x => x.Medyalar)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (duyuru == null)
                return NotFound();

            // Önce ilişkili medya kayıtlarını sil.
            var medyaDosyalari = duyuru.Medyalar
                .Select(x => x.DosyaYolu)
                .ToList();

            _context.DuyuruMedyalari.RemoveRange(duyuru.Medyalar);
            _context.Duyurular.Remove(duyuru);

            await _context.SaveChangesAsync();

            // Veritabanı silme işlemi başarılı olduktan sonra
            // duyuruya ait yükleme klasörünü temizle.
            string webRoot = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            string klasor = Path.Combine(
                webRoot,
                "uploads",
                "duyurular",
                dernekId.Value.ToString(),
                id.ToString());

            if (Directory.Exists(klasor))
            {
                try
                {
                    Directory.Delete(klasor, recursive: true);
                }
                catch (IOException)
                {
                    // Dosya silme hatası, duyurunun silinmesini geri almaz.
                }
                catch (UnauthorizedAccessException)
                {
                    // Klasör izinleri ayrıca kontrol edilebilir.
                }
            }

            TempData["Basarili"] = "Duyuru başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }

    }
}
