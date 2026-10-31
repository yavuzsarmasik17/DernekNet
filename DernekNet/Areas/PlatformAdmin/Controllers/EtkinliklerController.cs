
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DernekNet.Areas.PlatformAdmin.Controllers
{
    [Area("PlatformAdmin")]
    [Authorize(Roles = "PlatformAdmin")]
    public class EtkinliklerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EtkinliklerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ETKİNLİK LİSTESİ
        public async Task<IActionResult> Index()
        {
            var etkinlikler = await _context.Etkinlikler
                .Include(x => x.Dernek)
                .AsNoTracking()
                .OrderByDescending(x => x.EtkinlikTarihi)
                .ToListAsync();

            return View(etkinlikler);
        }

        // DERNEK LİSTESİ
        private async Task DernekleriYukle(int? seciliId = null)
        {
            var dernekler = await _context.Dernekler
                .AsNoTracking()
                .Where(x => x.AktifMi)
                .OrderBy(x => x.DernekAdi)
                .ToListAsync();

            ViewBag.Dernekler = new SelectList(
                dernekler,
                "Id",
                "DernekAdi",
                seciliId
            );
        }

        // YENİ ETKİNLİK FORMU
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await DernekleriYukle();

            return View(new Etkinlik
            {
                AktifMi = true,
                EtkinlikTarihi = DateTime.Today.AddDays(1)
            });
        }

        // ETKİNLİK KAYDET
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Etkinlik model)
        {
            ModelState.Remove(nameof(Etkinlik.Dernek));

            if (!ModelState.IsValid)
            {
                await DernekleriYukle(model.DernekId);
                return View(model);
            }

            bool dernekVarMi = await _context.Dernekler
                .AnyAsync(x => x.Id == model.DernekId && x.AktifMi);

            if (!dernekVarMi)
            {
                ModelState.AddModelError(
                    nameof(Etkinlik.DernekId),
                    "Geçerli ve aktif bir dernek seçiniz."
                );

                await DernekleriYukle(model.DernekId);
                return View(model);
            }

            var etkinlik = new Etkinlik
            {
                DernekId = model.DernekId,
                EtkinlikAdi = model.EtkinlikAdi,
                Aciklama = model.Aciklama,
                Konum = model.Konum,
                EtkinlikTarihi = model.EtkinlikTarihi,
                AktifMi = model.AktifMi
            };

            _context.Etkinlikler.Add(etkinlik);
            await _context.SaveChangesAsync();

            TempData["Basarili"] =
                "Etkinlik başarıyla kaydedildi.";

            return RedirectToAction(nameof(Index));
        }

        // ETKİNLİK DÜZENLE
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var etkinlik = await _context.Etkinlikler
                .FindAsync(id);

            if (etkinlik == null)
                return NotFound();

            await DernekleriYukle(etkinlik.DernekId);

            return View(etkinlik);
        }

        // ETKİNLİK GÜNCELLE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Etkinlik model)
        {
            if (id != model.Id)
                return BadRequest();

            ModelState.Remove(nameof(Etkinlik.Dernek));

            if (!ModelState.IsValid)
            {
                await DernekleriYukle(model.DernekId);
                return View(model);
            }

            bool dernekVarMi = await _context.Dernekler
                .AnyAsync(x => x.Id == model.DernekId && x.AktifMi);

            if (!dernekVarMi)
            {
                ModelState.AddModelError(
                    nameof(Etkinlik.DernekId),
                    "Geçerli ve aktif bir dernek seçiniz."
                );

                await DernekleriYukle(model.DernekId);
                return View(model);
            }

            var etkinlik = await _context.Etkinlikler
                .FindAsync(id);

            if (etkinlik == null)
                return NotFound();

            etkinlik.DernekId = model.DernekId;
            etkinlik.EtkinlikAdi = model.EtkinlikAdi;
            etkinlik.Aciklama = model.Aciklama;
            etkinlik.Konum = model.Konum;
            etkinlik.EtkinlikTarihi = model.EtkinlikTarihi;
            etkinlik.AktifMi = model.AktifMi;

            await _context.SaveChangesAsync();

            TempData["Basarili"] =
                "Etkinlik başarıyla güncellendi.";

            return RedirectToAction(nameof(Index));
        }
    }
}
