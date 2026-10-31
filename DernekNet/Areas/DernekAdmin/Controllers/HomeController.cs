
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
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var kullanici = await _userManager.GetUserAsync(User);

            if (kullanici == null)
                return Challenge();

            // Hesap onayı kaldırılmışsa erişimi engelle.
            if (!kullanici.EmailConfirmed)
                return Forbid();

            // Dernek atanmamışsa erişimi engelle.
            if (!kullanici.DernekId.HasValue)
                return Forbid();

            int dernekId = kullanici.DernekId.Value;

            // Yalnızca kullanıcının kendi derneği.
            var dernek = await _context.Dernekler
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == dernekId &&
                    x.AktifMi);

            if (dernek == null)
                return Forbid();

            ViewBag.DuyuruSayisi = await _context.Duyurular
                .CountAsync(x => x.DernekId == dernekId);

            ViewBag.EtkinlikSayisi = await _context.Etkinlikler
                .CountAsync(x => x.DernekId == dernekId);

            return View(dernek);
        }
    }
}
