
using System.Threading.Tasks;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DernekNet.Areas.Identity.Pages.Account
{
    [Authorize]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public LogoutModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Çıkış yapmadan önce kullanıcı bilgilerini al.
            var kullanici = await _userManager.GetUserAsync(User);

            int? dernekId = null;

            if (kullanici != null &&
                kullanici.DernekId.HasValue &&
                await _userManager.IsInRoleAsync(
                    kullanici, "DernekAdmin") &&
                !await _userManager.IsInRoleAsync(
                    kullanici, "PlatformAdmin"))
            {
                dernekId = kullanici.DernekId.Value;
            }

            // Oturumu kapat.
            await _signInManager.SignOutAsync();

            // Dernek yöneticisini kendi derneğinin sitesine gönder.
            if (dernekId.HasValue)
            {
                return LocalRedirect(
                    "/Dernek/Index/" + dernekId.Value);
            }

            // Platform yöneticisini platform girişine gönder.
            return LocalRedirect("/Identity/Account/Login");
        }
    }
}
