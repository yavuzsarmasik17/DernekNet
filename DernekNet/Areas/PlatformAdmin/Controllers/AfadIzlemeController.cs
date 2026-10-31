
using System.Linq;
using System.Threading.Tasks;
using DernekNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DernekNet.Areas.PlatformAdmin.Controllers
{
    [Area("PlatformAdmin")]
    [Authorize(Roles = "PlatformAdmin")]
    public class AfadIzlemeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AfadIzlemeController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // AFAD İZLEME MERKEZİ - DERNEK LİSTESİ
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernekler = await _db.Dernekler
                .AsNoTracking()
                .OrderBy(x => x.DernekAdi)
                .Select(x => new
                {
                    x.Id,
                    x.DernekAdi,
                    x.AktifMi,

                    GonulluSayisi = _db.AfadGonulluler
                        .Count(g => g.DernekId == x.Id),

                    EkipSayisi = _db.AfadEkipler
                        .Count(e => e.DernekId == x.Id),

                    GorevSayisi = _db.AfadGorevler
                        .Count(g => g.DernekId == x.Id),

                    EgitimSayisi = _db.AfadEgitimler
                        .Count(e => e.DernekId == x.Id),

                    TatbikatSayisi = _db.AfadTatbikatlar
                        .Count(t => t.DernekId == x.Id)
                })
                .ToListAsync();

            ViewBag.Dernekler = dernekler;

            ViewBag.ToplamGonullu = await _db.AfadGonulluler
                .CountAsync();

            ViewBag.ToplamEkip = await _db.AfadEkipler
                .CountAsync();

            ViewBag.ToplamGorev = await _db.AfadGorevler
                .CountAsync();

            return View();
        }

        // ==========================================
        // AFAD İZLEME MERKEZİ - DERNEK DETAYI
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Detay(int id)
        {
            // DERNEK BİLGİLERİ

            var dernek = await _db.Dernekler
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dernek == null)
            {
                return NotFound();
            }

            // ======================================
            // AFAD GÖNÜLLÜLERİ
            // ======================================

            ViewBag.Gonulluler = await _db.AfadGonulluler
                .AsNoTracking()
                .Where(x => x.DernekId == id)
                .OrderBy(x => x.AdSoyad)
                .ToListAsync();

            // ======================================
            // AFAD EKİPLERİ VE EKİP ÜYELERİ
            // ======================================

            ViewBag.Ekipler = await _db.AfadEkipler
                .AsNoTracking()
                .Include(x => x.Uyeler)
                    .ThenInclude(x => x.Gonullu)
                .Where(x => x.DernekId == id)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            // ======================================
            // AFAD GÖREVLERİ VE GÖREV ATAMALARI
            // ======================================

            ViewBag.Gorevler = await _db.AfadGorevler
                .AsNoTracking()
                .Include(x => x.Atamalar)
                    .ThenInclude(x => x.Gonullu)
                .Where(x => x.DernekId == id)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            // ======================================
            // AFAD EĞİTİMLERİ VE KATILIMLARI
            // ======================================

            ViewBag.Egitimler = await _db.AfadEgitimler
                .AsNoTracking()
                .Include(x => x.Katilimlar)
                    .ThenInclude(x => x.Gonullu)
                .Where(x => x.DernekId == id)
                .OrderByDescending(x => x.Tarih)
                .ToListAsync();

            // ======================================
            // AFAD TATBİKATLARI VE KATILIMLARI
            // ======================================

            ViewBag.Tatbikatlar = await _db.AfadTatbikatlar
                .AsNoTracking()
                .Include(x => x.Katilimlar)
                    .ThenInclude(x => x.Gonullu)
                .Where(x => x.DernekId == id)
                .OrderByDescending(x => x.Tarih)
                .ToListAsync();

            // ======================================
            // AFAD YETKİNLİKLERİ VE GÖNÜLLÜLER
            // ======================================

            ViewBag.Yetkinlikler = await _db.AfadYetkinlikler
                .AsNoTracking()
                .Include(x => x.Gonulluler)
                    .ThenInclude(x => x.Gonullu)
                .Where(x => x.DernekId == id)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            // SAYFAYA DERNEK MODELİNİ GÖNDER

            return View(dernek);
        }
    }
}
