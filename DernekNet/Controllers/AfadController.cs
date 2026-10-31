using DernekNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DernekNet.Controllers
{
    [AllowAnonymous]
    public class AfadController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AfadController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index(int dernekId)
        {
            var dernek = await _context.Dernekler.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dernekId && x.AktifMi);
            if (dernek == null) return NotFound();

            ViewBag.Haberler = await _context.AfadHaberler.AsNoTracking()
                .Where(x => x.DernekId == dernekId && x.AktifMi && x.YayinTarihi <= DateTime.UtcNow)
                .OrderByDescending(x => x.YayinTarihi).Take(12).ToListAsync();
            ViewBag.Duyurular = await _context.AfadDuyurular.AsNoTracking()
                .Where(x => x.DernekId == dernekId && x.AktifMi && !x.SadeceGonullulereMi && x.YayinTarihi <= DateTime.UtcNow)
                .OrderByDescending(x => x.YayinTarihi).Take(12).ToListAsync();
            ViewBag.Egitimler = await _context.AfadEgitimler.AsNoTracking()
                .Where(x => x.DernekId == dernekId && x.AktifMi && x.Tarih >= DateTime.UtcNow)
                .OrderBy(x => x.Tarih).Take(6).ToListAsync();
            ViewBag.Tatbikatlar = await _context.AfadTatbikatlar.AsNoTracking()
                .Where(x => x.DernekId == dernekId && x.AktifMi && x.Tarih >= DateTime.UtcNow)
                .OrderBy(x => x.Tarih).Take(6).ToListAsync();
            return View(dernek);
        }
    }
}
