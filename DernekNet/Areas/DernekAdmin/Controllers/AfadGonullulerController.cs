
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DernekNet.Areas.DernekAdmin.Controllers
{
    [Area("DernekAdmin")]
    [Authorize(Roles = "DernekAdmin")]
    public class AfadGonullulerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public AfadGonullulerController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        private async Task<int?> DernekIdGetir()
        {
            var user = await _users.GetUserAsync(User);

            if (user == null ||
                !user.EmailConfirmed ||
                !user.DernekId.HasValue)
            {
                return null;
            }

            return await _db.Dernekler.AnyAsync(x =>
                x.Id == user.DernekId.Value && x.AktifMi)
                ? user.DernekId
                : null;
        }

        // GÖNÜLLÜ LİSTESİ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var list = await _db.AfadGonulluler
                .AsNoTracking()
                .Include(x => x.Kullanici)
                .Where(x => x.DernekId == dernekId.Value)
                .OrderBy(x => x.OnaylandiMi)
                .ThenByDescending(x => x.KayitTarihi)
                .ToListAsync();

            return View(list);
        }

        // GÖNÜLLÜ ONAYLAMA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Onayla(int id)
        {
            var dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var gonullu = await _db.AfadGonulluler
                .Include(x => x.Kullanici)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (gonullu == null)
                return NotFound();

            if (!gonullu.AktifMi)
                return BadRequest("Pasif başvuru onaylanamaz.");

            if (gonullu.Kullanici == null)
                return NotFound();

            // GEÇİCİ:
            // SMTP kurulana kadar yönetici onayı
            // e-posta onayı yerine geçiyor.
            // Canlı kullanımdan önce gerçek
            // e-posta doğrulaması kurulmalıdır.
            gonullu.Kullanici.EmailConfirmed = true;

            gonullu.OnaylandiMi = true;

            await _db.SaveChangesAsync();

            TempData["AfadAdminMesaj"] =
                "Gönüllü başvurusu başarıyla onaylandı. " +
                "(E-posta sahipliği henüz doğrulanmadı.)";

            return RedirectToAction(nameof(Index));
        }

        // GÖNÜLLÜYÜ PASİFE AL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PasifeAl(int id)
        {
            var dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var gonullu = await _db.AfadGonulluler
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (gonullu == null)
                return NotFound();

            gonullu.AktifMi = false;
            gonullu.OnaylandiMi = false;

            await _db.SaveChangesAsync();

            TempData["AfadAdminMesaj"] =
                "Gönüllü pasife alındı.";

            return RedirectToAction(nameof(Index));
        }

        // GÖNÜLLÜYÜ TEKRAR AKTİF YAP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AktifeAl(int id)
        {
            var dernekId = await DernekIdGetir();

            if (!dernekId.HasValue)
                return Forbid();

            var gonullu = await _db.AfadGonulluler
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.DernekId == dernekId.Value);

            if (gonullu == null)
                return NotFound();

            if (gonullu.AktifMi)
            {
                TempData["AfadAdminMesaj"] =
                    "Bu gönüllü zaten aktif durumda.";

                return RedirectToAction(nameof(Index));
            }

            gonullu.AktifMi = true;

            // Yeniden aktif olan gönüllü,
            // yönetici onayı bekleyecek.
            gonullu.OnaylandiMi = false;

            await _db.SaveChangesAsync();

            TempData["AfadAdminMesaj"] =
                "Gönüllü yeniden aktif hale getirildi. " +
                "Başvuru yönetici onayı bekliyor.";

            return RedirectToAction(nameof(Index));
        }
    }
}
