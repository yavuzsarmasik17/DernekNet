
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DernekNet.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
            = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DernekId { get; set; }

        public string? DernekAdi { get; private set; }
        public string? DernekLogoUrl { get; private set; }
        public string? DernekAciklama { get; private set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "E-posta adresi zorunludur.")]
            [EmailAddress(ErrorMessage = "Geçerli e-posta giriniz.")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Şifre zorunludur.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Display(Name = "Beni Hatırla")]
            public bool RememberMe { get; set; }
        }

        private async Task<bool> GirisGorunumunuYukleAsync()
        {
            ExternalLogins =
                (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .ToList();

            if (!DernekId.HasValue)
                return true;

            var dernek = await _db.Dernekler
                .AsNoTracking()
                .Where(d => d.Id == DernekId.Value && d.AktifMi)
                .Select(d => new
                {
                    d.DernekAdi,
                    d.LogoUrl,
                    d.Aciklama
                })
                .FirstOrDefaultAsync();

            if (dernek == null)
                return false;

            DernekAdi = dernek.DernekAdi;
            DernekLogoUrl = dernek.LogoUrl;
            DernekAciklama = dernek.Aciklama;

            return true;
        }

        public async Task<IActionResult> OnGetAsync(
            string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!string.IsNullOrWhiteSpace(ErrorMessage))
            {
                ModelState.AddModelError(
                    string.Empty,
                    ErrorMessage);
            }

            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            if (!await GirisGorunumunuYukleAsync())
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!await GirisGorunumunuYukleAsync())
                return NotFound();

            if (!ModelState.IsValid)
                return Page();

            var kullanici = await _userManager.FindByEmailAsync(
                Input.Email.Trim());

            if (kullanici == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "E-posta veya şifre hatalı.");

                return Page();
            }

            bool platformAdmin =
                await _userManager.IsInRoleAsync(
                    kullanici, "PlatformAdmin");

            bool dernekAdmin =
                await _userManager.IsInRoleAsync(
                    kullanici, "DernekAdmin");

            if (DernekId.HasValue)
            {
                if (!dernekAdmin ||
                    platformAdmin ||
                    !kullanici.EmailConfirmed ||
                    kullanici.DernekId != DernekId.Value)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Bu derneğin yönetim paneline giriş yetkiniz yok.");

                    return Page();
                }
            }
            else if (!platformAdmin)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Bu giriş yalnızca platform yöneticisine açıktır.");

                return Page();
            }

            var result =
                await _signInManager.PasswordSignInAsync(
                    kullanici,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Yetkili yönetici giriş yaptı.");

                return LocalRedirect(
                    DernekId.HasValue
                        ? "/DernekAdmin/Home/Index"
                        : "/PlatformAdmin/Home/Index");
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage(
                    "./LoginWith2fa",
                    new
                    {
                        ReturnUrl = returnUrl,
                        RememberMe = Input.RememberMe,
                        DernekId
                    });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning(
                    "Yönetici hesabı kilitlendi.");

                return RedirectToPage("./Lockout");
            }

            ModelState.AddModelError(
                string.Empty,
                "E-posta veya şifre hatalı ya da giriş başarısız.");

            return Page();
        }
    }
}
