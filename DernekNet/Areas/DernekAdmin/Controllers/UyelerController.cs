
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
    public class UyelerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UyelerController(
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

            bool aktif = await _context.Dernekler
                .AnyAsync(x =>
                    x.Id == kullanici.DernekId.Value &&
                    x.AktifMi);

            return aktif ? kullanici.DernekId : null;
        }

        public async Task<IActionResult> Index(string? ara)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var sorgu = _context.Uyeler
                .AsNoTracking()
                .Where(x => x.DernekId == dernekId.Value);

            if (!string.IsNullOrWhiteSpace(ara))
            {
                string kelime = ara.Trim();

                sorgu = sorgu.Where(x =>
                    x.Ad.Contains(kelime) ||
                    x.Soyad.Contains(kelime) ||
                    (x.Telefon != null &&
                     x.Telefon.Contains(kelime)));
            }

            var uyeler = await sorgu
                .OrderBy(x => x.Soyad)
                .ThenBy(x => x.Ad)
                .ToListAsync();

            ViewBag.Ara = ara;

            return View(uyeler);
        }


        [HttpGet]
        public async Task<IActionResult> Fotograf(int id)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var uye = await _context.Uyeler
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (uye == null ||
                string.IsNullOrWhiteSpace(uye.FotografUrl))
                return NotFound();

            // Fotoğraflar wwwroot dışında saklanacak.
            string klasor = Path.Combine(
                Directory.GetCurrentDirectory(),
                "PrivateUploads",
                "Uyeler");

            string dosyaAdi = Path.GetFileName(uye.FotografUrl);
            string dosyaYolu = Path.Combine(klasor, dosyaAdi);

            if (!System.IO.File.Exists(dosyaYolu))
                return NotFound();

            string uzanti = Path.GetExtension(dosyaAdi).ToLowerInvariant();

            string contentType = uzanti switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => ""
            };

            if (string.IsNullOrEmpty(contentType))
                return NotFound();

            return PhysicalFile(dosyaYolu, contentType);
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            return View(new Uye
            {
                UyelikTarihi = DateTime.Today,
                AktifMi = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Uye model,
            IFormFile? fotograf)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            // DernekId kullanıcıdan alınmaz.
            // Oturum açan yöneticiden belirlenir.
            ModelState.Remove(nameof(Uye.DernekId));
            ModelState.Remove(nameof(Uye.Dernek));
            ModelState.Remove(nameof(Uye.FotografUrl));

            if (fotograf != null && fotograf.Length > 0)
            {
                const long maksimumBoyut = 2 * 1024 * 1024;

                if (fotograf.Length > maksimumBoyut)
                {
                    ModelState.AddModelError(
                        "fotograf",
                        "Fotoğraf en fazla 2 MB olabilir.");
                }
                else
                {
                    string uzanti = Path.GetExtension(
                        fotograf.FileName).ToLowerInvariant();

                    string[] izinliUzantilar =
                    {
                ".jpg", ".jpeg", ".png", ".webp"
            };

                    if (!izinliUzantilar.Contains(uzanti))
                    {
                        ModelState.AddModelError(
                            "fotograf",
                            "Yalnızca JPG, PNG veya WEBP yükleyebilirsiniz.");
                    }
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            string? kaydedilenDosya = null;

            try
            {
                if (fotograf != null && fotograf.Length > 0)
                {
                    // Dosyanın gerçek formatını kontrol et.
                    byte[] imza = new byte[12];

                    using (var stream = fotograf.OpenReadStream())
                    {
                        int okunan = await stream.ReadAsync(
                            imza.AsMemory(0, imza.Length));

                        if (okunan < 12)
                        {
                            ModelState.AddModelError(
                                "fotograf",
                                "Geçersiz fotoğraf dosyası.");

                            return View(model);
                        }
                    }

                    bool jpg =
                        imza[0] == 0xFF &&
                        imza[1] == 0xD8 &&
                        imza[2] == 0xFF;

                    bool png =
                        imza[0] == 0x89 &&
                        imza[1] == 0x50 &&
                        imza[2] == 0x4E &&
                        imza[3] == 0x47 &&
                        imza[4] == 0x0D &&
                        imza[5] == 0x0A &&
                        imza[6] == 0x1A &&
                        imza[7] == 0x0A;

                    bool webp =
                        imza[0] == 0x52 &&
                        imza[1] == 0x49 &&
                        imza[2] == 0x46 &&
                        imza[3] == 0x46 &&
                        imza[8] == 0x57 &&
                        imza[9] == 0x45 &&
                        imza[10] == 0x42 &&
                        imza[11] == 0x50;

                    string uzanti = Path.GetExtension(
                        fotograf.FileName).ToLowerInvariant();

                    bool gecerli =
                        (jpg && (uzanti == ".jpg" ||
                                 uzanti == ".jpeg")) ||
                        (png && uzanti == ".png") ||
                        (webp && uzanti == ".webp");

                    if (!gecerli)
                    {
                        ModelState.AddModelError(
                            "fotograf",
                            "Dosya uzantısı ve fotoğraf formatı uyuşmuyor.");

                        return View(model);
                    }

                    string klasor = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "PrivateUploads",
                        "Uyeler");

                    Directory.CreateDirectory(klasor);

                    string dosyaAdi =
                        Guid.NewGuid().ToString("N") + uzanti;

                    string dosyaYolu = Path.Combine(
                        klasor, dosyaAdi);

                    using (var stream = new FileStream(
                        dosyaYolu,
                        FileMode.CreateNew))
                    {
                        await fotograf.CopyToAsync(stream);
                    }

                    kaydedilenDosya = dosyaYolu;
                    model.FotografUrl = dosyaAdi;
                }

                // Formdan gelen nesneyi doğrudan kaydetmiyoruz.
                // Yalnızca izin verdiğimiz alanları aktarıyoruz.
                var yeniUye = new Uye
                {
                    DernekId = dernekId.Value,
                    Ad = model.Ad.Trim(),
                    Soyad = model.Soyad.Trim(),
                    Telefon = model.Telefon?.Trim(),
                    Email = model.Email?.Trim(),
                    UyelikTarihi = model.UyelikTarihi,
                    AktifMi = model.AktifMi,
                    Aciklama = model.Aciklama?.Trim(),
                    FotografUrl = model.FotografUrl
                };

                _context.Uyeler.Add(yeniUye);
                await _context.SaveChangesAsync();

                TempData["Basarili"] =
                    "Üye başarıyla kaydedildi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                // Veritabanına kayıt yapılamadıysa
                // yüklenen dosyayı temizle.
                if (kaydedilenDosya != null &&
                    System.IO.File.Exists(kaydedilenDosya))
                {
                    System.IO.File.Delete(kaydedilenDosya);
                }

                ModelState.AddModelError(
                    "",
                    "Üye kaydedilirken hata oluştu.");

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var uye = await _context.Uyeler
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (uye == null)
                return NotFound();

            return View(uye);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var uye = await _context.Uyeler
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (uye == null)
                return NotFound();

            return View(uye);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Uye model,
            IFormFile? fotograf,
            bool fotografKaldir = false)
        {
            int? dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            if (id != model.Id)
                return BadRequest();

            var uye = await _context.Uyeler
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (uye == null)
                return NotFound();

            ModelState.Remove(nameof(Uye.DernekId));
            ModelState.Remove(nameof(Uye.Dernek));
            ModelState.Remove(nameof(Uye.FotografUrl));

            if (fotograf != null && fotograf.Length > 0)
            {
                if (fotograf.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "fotograf",
                        "Fotoğraf en fazla 2 MB olabilir.");
                }

                string uzanti = Path.GetExtension(
                    fotograf.FileName).ToLowerInvariant();

                if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }
                    .Contains(uzanti))
                {
                    ModelState.AddModelError(
                        "fotograf",
                        "Yalnızca JPG, PNG ve WEBP yükleyebilirsiniz.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.FotografUrl = uye.FotografUrl;
                return View(model);
            }

            string? yeniDosyaYolu = null;
            string? eskiDosyaAdi = uye.FotografUrl;

            try
            {
                string klasor = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "PrivateUploads",
                    "Uyeler");

                if (fotograf != null && fotograf.Length > 0)
                {
                    byte[] imza = new byte[12];

                    using (var stream = fotograf.OpenReadStream())
                    {
                        int okunan = await stream.ReadAsync(
                            imza.AsMemory());

                        if (okunan < 12)
                        {
                            ModelState.AddModelError(
                                "fotograf",
                                "Geçersiz fotoğraf dosyası.");

                            model.FotografUrl = uye.FotografUrl;
                            return View(model);
                        }
                    }

                    bool jpg =
                        imza[0] == 0xFF &&
                        imza[1] == 0xD8 &&
                        imza[2] == 0xFF;

                    bool png =
                        imza[0] == 0x89 &&
                        imza[1] == 0x50 &&
                        imza[2] == 0x4E &&
                        imza[3] == 0x47 &&
                        imza[4] == 0x0D &&
                        imza[5] == 0x0A &&
                        imza[6] == 0x1A &&
                        imza[7] == 0x0A;

                    bool webp =
                        imza[0] == 0x52 &&
                        imza[1] == 0x49 &&
                        imza[2] == 0x46 &&
                        imza[3] == 0x46 &&
                        imza[8] == 0x57 &&
                        imza[9] == 0x45 &&
                        imza[10] == 0x42 &&
                        imza[11] == 0x50;

                    string uzanti = Path.GetExtension(
                        fotograf.FileName).ToLowerInvariant();

                    bool gecerli =
                        (jpg && (uzanti == ".jpg" ||
                                 uzanti == ".jpeg")) ||
                        (png && uzanti == ".png") ||
                        (webp && uzanti == ".webp");

                    if (!gecerli)
                    {
                        ModelState.AddModelError(
                            "fotograf",
                            "Fotoğraf formatı geçersiz.");

                        model.FotografUrl = uye.FotografUrl;
                        return View(model);
                    }

                    Directory.CreateDirectory(klasor);

                    string dosyaAdi =
                        Guid.NewGuid().ToString("N") + uzanti;

                    yeniDosyaYolu = Path.Combine(klasor, dosyaAdi);

                    using (var stream = new FileStream(
                        yeniDosyaYolu,
                        FileMode.CreateNew))
                    {
                        await fotograf.CopyToAsync(stream);
                    }

                    uye.FotografUrl = dosyaAdi;
                }
                else if (fotografKaldir)
                {
                    uye.FotografUrl = null;
                }

                uye.Ad = model.Ad.Trim();
                uye.Soyad = model.Soyad.Trim();
                uye.Telefon = model.Telefon?.Trim();
                uye.Email = model.Email?.Trim();
                uye.UyelikTarihi = model.UyelikTarihi;
                uye.AktifMi = model.AktifMi;
                uye.Aciklama = model.Aciklama?.Trim();

                await _context.SaveChangesAsync();

                // Yeni kayıt başarılıysa eski fotoğrafı temizle.
                if ((fotografKaldir ||
                     (fotograf != null && fotograf.Length > 0)) &&
                    !string.IsNullOrWhiteSpace(eskiDosyaAdi) &&
                    eskiDosyaAdi != uye.FotografUrl)
                {
                    string eskiDosyaYolu = Path.Combine(
                        klasor,
                        Path.GetFileName(eskiDosyaAdi));

                    try
                    {
                        if (System.IO.File.Exists(eskiDosyaYolu))
                            System.IO.File.Delete(eskiDosyaYolu);
                    }
                    catch
                    {
                        // Eski dosya temizlenemese bile
                        // başarılı veritabanı işlemini geri alma.
                    }
                }

                TempData["Basarili"] =
                    "Üye bilgileri başarıyla güncellendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                if (yeniDosyaYolu != null)
                {
                    try
                    {
                        if (System.IO.File.Exists(yeniDosyaYolu))
                            System.IO.File.Delete(yeniDosyaYolu);
                    }
                    catch
                    {
                        // Temizleme hatası asıl hatayı gizlemesin.
                    }
                }

                ModelState.AddModelError(
                    "",
                    "Üye güncellenirken hata oluştu.");

                model.FotografUrl = eskiDosyaAdi;
                return View(model);
            }
        }



    }
}
