
using DernekNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DernekNet.Areas.PlatformAdmin.Controllers
{
    [Area("PlatformAdmin")]
    [Authorize(Roles = "PlatformAdmin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Bütün dernekler: aktif ve pasif
            var dernekler = await _context.Dernekler
                .AsNoTracking()
                .OrderByDescending(x => x.KayitTarihi)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            // Dernek başına üye sayıları
            var uyeSayilari = await _context.Uyeler
                .AsNoTracking()
                .GroupBy(x => x.DernekId)
                .Select(g => new
                {
                    DernekId = g.Key,
                    Toplam = g.Count(),
                    Aktif = g.Count(x => x.AktifMi)
                })
                .ToDictionaryAsync(
                    x => x.DernekId,
                    x => new { x.Toplam, x.Aktif });

            // Dernek başına aktif kurul görevi sayıları
            var kurulSayilari = await _context.YonetimKuruluUyeleri
                .AsNoTracking()
                .Where(x => x.AktifMi &&
                    x.Uye != null &&
                    x.Uye.AktifMi)
                .GroupBy(x => x.DernekId)
                .Select(g => new
                {
                    DernekId = g.Key,
                    Toplam = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.DernekId,
                    x => x.Toplam);

            var kartlar = dernekler.Select(d =>
                new DernekNet.Models.PlatformDernekKartViewModel
                {
                    Id = d.Id,
                    DernekAdi = d.DernekAdi,
                    LogoUrl = d.LogoUrl,
                    AktifMi = d.AktifMi,
                    KayitTarihi = d.KayitTarihi,
                    ToplamUye = uyeSayilari.ContainsKey(d.Id)
                        ? uyeSayilari[d.Id].Toplam : 0,
                    AktifUye = uyeSayilari.ContainsKey(d.Id)
                        ? uyeSayilari[d.Id].Aktif : 0,
                    AktifKurulGorevi = kurulSayilari.ContainsKey(d.Id)
                        ? kurulSayilari[d.Id] : 0
                }).ToList();

            var model = new DernekNet.Models.PlatformDashboardViewModel
            {
                ToplamDernek = kartlar.Count,
                AktifDernek = kartlar.Count(x => x.AktifMi),
                PasifDernek = kartlar.Count(x => !x.AktifMi),
                ToplamUye = kartlar.Sum(x => x.ToplamUye),
                AktifUye = kartlar.Sum(x => x.AktifUye),
                AktifKurulGorevi = kartlar.Sum(x => x.AktifKurulGorevi),
                Dernekler = kartlar
            };

            return View(model);
        }
    }
}
