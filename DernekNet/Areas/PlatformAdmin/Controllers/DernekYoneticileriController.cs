
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Areas.PlatformAdmin.Controllers
{
    [Area("PlatformAdmin")]
    [Authorize(Roles = "PlatformAdmin")]
    public class DernekYoneticileriController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DernekYoneticileriController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // YÖNETİCİ LİSTESİ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var yoneticiler = await _userManager
                .GetUsersInRoleAsync("DernekAdmin");

            var dernekler = await _context.Dernekler
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.DernekAdi);

            ViewBag.DernekAdlari = dernekler;

            return View(yoneticiler);
        }

        // YENİ YÖNETİCİ FORMU
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await DernekleriYukle();

            return View(new DernekYoneticiOlusturModel());
        }

        // YENİ YÖNETİCİ KAYDET
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            DernekYoneticiOlusturModel model)
        {
            if (!ModelState.IsValid)
            {
                await DernekleriYukle();
                return View(model);
            }

            bool dernekVarMi = await _context.Dernekler
                .AnyAsync(x => x.Id == model.DernekId && x.AktifMi);

            if (!dernekVarMi)
            {
                ModelState.AddModelError(
                    nameof(model.DernekId),
                    "Geçerli ve aktif bir dernek seçiniz.");

                await DernekleriYukle();
                return View(model);
            }

            var mevcutKullanici = await _userManager
                .FindByEmailAsync(model.Email);

            if (mevcutKullanici != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Bu e-posta adresiyle kayıtlı kullanıcı var.");

                await DernekleriYukle();
                return View(model);
            }

            const string rolAdi = "DernekAdmin";

            if (!await _roleManager.RoleExistsAsync(rolAdi))
            {
                var rolSonucu = await _roleManager.CreateAsync(
                    new IdentityRole(rolAdi));

                if (!rolSonucu.Succeeded)
                {
                    ModelState.AddModelError(
                        "",
                        string.Join(", ",
                            rolSonucu.Errors.Select(x => x.Description)));

                    await DernekleriYukle();
                    return View(model);
                }
            }

            var kullanici = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,

                // İlk oluşturulduğunda onay bekler.
                EmailConfirmed = false,

                DernekId = model.DernekId
            };

            var sonuc = await _userManager.CreateAsync(
                kullanici, model.Sifre);

            if (!sonuc.Succeeded)
            {
                foreach (var hata in sonuc.Errors)
                {
                    ModelState.AddModelError("", hata.Description);
                }

                await DernekleriYukle();
                return View(model);
            }

            var rolAtama = await _userManager
                .AddToRoleAsync(kullanici, rolAdi);

            if (!rolAtama.Succeeded)
            {
                await _userManager.DeleteAsync(kullanici);

                ModelState.AddModelError(
                    "",
                    "Yönetici rolü atanamadı.");

                await DernekleriYukle();
                return View(model);
            }

            TempData["Basarili"] =
                "Dernek yöneticisi oluşturuldu. " +
                "Giriş yapabilmesi için hesabı onaylayın.";

            return RedirectToAction(nameof(Index));
        }

        // HESAP ONAYLAMA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Onayla(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var kullanici = await _userManager.FindByIdAsync(id);

            if (kullanici == null)
                return NotFound();

            // Yalnızca DernekAdmin hesapları onaylanabilir.
            if (!await _userManager.IsInRoleAsync(
                kullanici, "DernekAdmin"))
            {
                return Forbid();
            }

            // Hesabın aktif bir derneğe bağlı olması gerekir.
            if (!kullanici.DernekId.HasValue)
                return BadRequest("Kullanıcının derneği yok.");

            bool dernekAktif = await _context.Dernekler
                .AnyAsync(x =>
                    x.Id == kullanici.DernekId.Value &&
                    x.AktifMi);

            if (!dernekAktif)
                return BadRequest("Dernek aktif değil.");

            if (kullanici.EmailConfirmed)
            {
                TempData["Basarili"] =
                    "Bu hesap zaten onaylanmış.";

                return RedirectToAction(nameof(Index));
            }

            // PlatformAdmin tarafından hesap onayı.
            kullanici.EmailConfirmed = true;

            var sonuc = await _userManager.UpdateAsync(kullanici);

            if (!sonuc.Succeeded)
            {
                TempData["Hata"] =
                    string.Join(", ",
                        sonuc.Errors.Select(x => x.Description));

                return RedirectToAction(nameof(Index));
            }

            TempData["Basarili"] =
                "Dernek yöneticisi hesabı onaylandı. " +
                "Kullanıcı artık giriş yapabilir.";

            return RedirectToAction(nameof(Index));
        }

        // HESAP ONAYINI KALDIR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnayiKaldir(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var kullanici = await _userManager.FindByIdAsync(id);

            if (kullanici == null)
                return NotFound();

            if (!await _userManager.IsInRoleAsync(
                kullanici, "DernekAdmin"))
            {
                return Forbid();
            }

            kullanici.EmailConfirmed = false;

            // Mevcut oturumları da geçersiz kıl.
            var sonuc = await _userManager.UpdateAsync(kullanici);

            if (!sonuc.Succeeded)
            {
                TempData["Hata"] =
                    string.Join(", ",
                        sonuc.Errors.Select(x => x.Description));

                return RedirectToAction(nameof(Index));
            }

            var securityResult = await _userManager
                .UpdateSecurityStampAsync(kullanici);

            if (!securityResult.Succeeded)
            {
                TempData["Hata"] =
                    "Onay kaldırıldı ancak oturum güvenliği " +
                    "güncellenemedi. Kontrol ediniz.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Basarili"] =
                "Yönetici hesabının onayı kaldırıldı.";

            return RedirectToAction(nameof(Index));
        }

        // AKTİF DERNEKLER
        private async Task DernekleriYukle()
        {
            var dernekler = await _context.Dernekler
                .AsNoTracking()
                .Where(x => x.AktifMi)
                .OrderBy(x => x.DernekAdi)
                .ToListAsync();

            ViewBag.Dernekler = new SelectList(
                dernekler,
                "Id",
                "DernekAdi");
        }
    }

    // YENİ YÖNETİCİ FORM MODELİ
    public class DernekYoneticiOlusturModel
    {
        [Required(ErrorMessage = "Dernek seçiniz.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Geçerli bir dernek seçiniz.")]
        public int DernekId { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli e-posta giriniz.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; } = "";

        [Required(ErrorMessage = "Şifre tekrarını giriniz.")]
        [Compare(nameof(Sifre),
            ErrorMessage = "Şifreler eşleşmiyor.")]
        [DataType(DataType.Password)]
        public string SifreTekrar { get; set; } = "";
    }
}
