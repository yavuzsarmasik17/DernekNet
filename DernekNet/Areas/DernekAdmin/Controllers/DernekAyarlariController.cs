
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DernekNet.Areas.DernekAdmin.Controllers
{
    public class DernekAyarlariModel
    {
        public string DernekAdi { get; set; } = "";

        public string? LogoUrl { get; set; }

        [StringLength(30)]
        public string? Telefon { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string? Email { get; set; }

        public string? Adres { get; set; }

        public string? Aciklama { get; set; }
    }

    [Area("DernekAdmin")]
    [Authorize(Roles = "DernekAdmin")]
    public class DernekAyarlariController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public DernekAyarlariController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        private async Task<Dernek?> YetkiliDernegiGetir()
        {
            var kullanici = await _userManager.GetUserAsync(User);

            if (kullanici == null ||
                !kullanici.EmailConfirmed ||
                !kullanici.DernekId.HasValue)
                return null;

            return await _context.Dernekler
                .FirstOrDefaultAsync(x =>
                    x.Id == kullanici.DernekId.Value &&
                    x.AktifMi);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernek = await YetkiliDernegiGetir();

            if (dernek == null)
                return Forbid();

            return View(new DernekAyarlariModel
            {
                DernekAdi = dernek.DernekAdi,
                LogoUrl = dernek.LogoUrl,
                Telefon = dernek.Telefon,
                Email = dernek.Email,
                Adres = dernek.Adres,
                Aciklama = dernek.Aciklama
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            DernekAyarlariModel model,
            IFormFile? logoDosyasi)
        {
            var dernek = await YetkiliDernegiGetir();

            if (dernek == null)
                return Forbid();

            // Bu alanlar kullanıcı tarafından değiştirilemez.
            model.DernekAdi = dernek.DernekAdi;
            model.LogoUrl = dernek.LogoUrl;

            if (logoDosyasi != null && logoDosyasi.Length > 0)
            {
                if (logoDosyasi.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "logoDosyasi",
                        "Logo en fazla 2 MB olabilir.");
                }

                var uzanti = Path.GetExtension(
                    logoDosyasi.FileName).ToLowerInvariant();

                if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }
                    .Contains(uzanti))
                {
                    ModelState.AddModelError(
                        "logoDosyasi",
                        "Yalnızca JPG, PNG veya WEBP yükleyebilirsiniz.");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            string? yeniLogoUrl = null;
            string? yeniLogoYolu = null;

            try
            {
                if (logoDosyasi != null && logoDosyasi.Length > 0)
                {
                    var uzanti = Path.GetExtension(
                        logoDosyasi.FileName).ToLowerInvariant();

                    string klasor = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "dernek-logolari");

                    Directory.CreateDirectory(klasor);

                    string dosyaAdi =
                        Guid.NewGuid().ToString("N") + uzanti;

                    yeniLogoYolu = Path.Combine(klasor, dosyaAdi);

                    using (var stream = new FileStream(
                        yeniLogoYolu, FileMode.CreateNew))
                    {
                        await logoDosyasi.CopyToAsync(stream);
                    }

                    yeniLogoUrl =
                        "/uploads/dernek-logolari/" + dosyaAdi;
                }

                dernek.Telefon = model.Telefon;
                dernek.Email = model.Email;
                dernek.Adres = model.Adres;
                dernek.Aciklama = model.Aciklama;

                if (yeniLogoUrl != null)
                    dernek.LogoUrl = yeniLogoUrl;

                await _context.SaveChangesAsync();

                TempData["Basarili"] =
                    "Dernek bilgileri başarıyla güncellendi.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                if (yeniLogoYolu != null &&
                    System.IO.File.Exists(yeniLogoYolu))
                {
                    System.IO.File.Delete(yeniLogoYolu);
                }

                ModelState.AddModelError(
                    "",
                    "Kaydetme sırasında bir hata oluştu.");

                return View(model);
            }
        }
    }
}
