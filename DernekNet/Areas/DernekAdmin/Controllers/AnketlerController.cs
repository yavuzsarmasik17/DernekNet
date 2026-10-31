using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;

namespace DernekNet.Areas.DernekAdmin.Controllers
{
    [Area("DernekAdmin")]
    [Authorize(Roles = "DernekAdmin")]
    public class AnketlerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnketlerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int?> DernekIdGetir()
        {
            var kullanici = await _userManager.GetUserAsync(User);
            if (kullanici == null || !kullanici.EmailConfirmed || !kullanici.DernekId.HasValue)
                return null;

            var aktif = await _context.Dernekler.AnyAsync(x =>
                x.Id == kullanici.DernekId.Value && x.AktifMi);
            return aktif ? kullanici.DernekId : null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var anketler = await _context.Anketler.AsNoTracking()
                .Include(x => x.Secenekler).Include(x => x.Oylar)
                .Where(x => x.DernekId == dernekId.Value)
                .OrderByDescending(x => x.Id).ToListAsync();
            return View(anketler);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!(await DernekIdGetir()).HasValue) return Forbid();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string soru, string? aciklama,
            DateTime baslangicTarihi, DateTime bitisTarihi, List<string> secenekler)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            soru = (soru ?? "").Trim();
            secenekler = (secenekler ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();

            if (string.IsNullOrWhiteSpace(soru) || soru.Length > 300)
                ModelState.AddModelError("", "Geçerli bir anket sorusu giriniz.");
            if (secenekler.Count < 2 || secenekler.Count > 10)
                ModelState.AddModelError("", "2 ile 10 arasında seçenek giriniz.");
            if (secenekler.Any(x => x.Length > 200))
                ModelState.AddModelError("", "Seçenekler 200 karakteri geçemez.");
            if (secenekler.Distinct(StringComparer.OrdinalIgnoreCase).Count() != secenekler.Count)
                ModelState.AddModelError("", "Aynı seçenek birden fazla eklenemez.");
            if (bitisTarihi <= baslangicTarihi)
                ModelState.AddModelError("", "Bitiş tarihi başlangıçtan sonra olmalıdır.");
            if (bitisTarihi <= DateTime.Now)
                ModelState.AddModelError("", "Bitiş tarihi gelecekte olmalıdır.");

            if (!ModelState.IsValid)
            {
                ViewBag.Soru = soru;
                ViewBag.Aciklama = aciklama;
                ViewBag.Baslangic = baslangicTarihi;
                ViewBag.Bitis = bitisTarihi;
                ViewBag.Secenekler = secenekler;
                return View();
            }

            var anket = new Anket
            {
                DernekId = dernekId.Value,
                Soru = soru,
                Aciklama = aciklama?.Trim(),
                BaslangicTarihi = baslangicTarihi.ToUniversalTime(),
                BitisTarihi = bitisTarihi.ToUniversalTime(),
                AktifMi = true,
                Secenekler = secenekler.Select((metin, index) => new AnketSecenek
                {
                    SecenekMetni = metin,
                    SiraNo = index + 1
                }).ToList()
            };
            _context.Anketler.Add(anket);
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Anket başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DurumDegistir(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            var anket = await _context.Anketler.FirstOrDefaultAsync(x =>
                x.Id == id && x.DernekId == dernekId.Value);
            if (anket == null) return NotFound();
            anket.AktifMi = !anket.AktifMi;
            await _context.SaveChangesAsync();
            TempData["Basarili"] = anket.AktifMi ? "Anket yayına alındı." : "Anket yayından kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Sonuclar(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            var anket = await _context.Anketler.AsNoTracking()
                .Include(x => x.Secenekler)
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (anket == null) return NotFound();
            var oySayilari = await _context.AnketOylari.AsNoTracking()
                .Where(x => x.AnketId == anket.Id)
                .GroupBy(x => x.AnketSecenekId)
                .Select(g => new { SecenekId = g.Key, OySayisi = g.Count() })
                .ToDictionaryAsync(x => x.SecenekId, x => x.OySayisi);
            ViewBag.OySayilari = oySayilari;
            return View(anket);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            var anket = await _context.Anketler.AsNoTracking()
                .Include(x => x.Secenekler)
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (anket == null) return NotFound();
            ViewBag.OyVar = await _context.AnketOylari.AnyAsync(x => x.AnketId == id);
            return View(anket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string soru, string? aciklama,
            DateTime baslangicTarihi, DateTime bitisTarihi, List<string>? secenekler)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            var anket = await _context.Anketler.Include(x => x.Secenekler)
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (anket == null) return NotFound();

            var oyVar = await _context.AnketOylari.AnyAsync(x => x.AnketId == id);
            soru = (soru ?? "").Trim();
            aciklama = aciklama?.Trim();
            secenekler = (secenekler ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()).ToList();

            if (string.IsNullOrWhiteSpace(soru) || soru.Length > 300)
                ModelState.AddModelError("", "Soru 1 ile 300 karakter arasında olmalıdır.");
            if (bitisTarihi <= baslangicTarihi)
                ModelState.AddModelError("", "Bitiş tarihi başlangıçtan sonra olmalıdır.");
            if (!oyVar)
            {
                if (secenekler.Count < 2 || secenekler.Count > 10)
                    ModelState.AddModelError("", "2 ile 10 arasında seçenek giriniz.");
                if (secenekler.Any(x => x.Length > 200))
                    ModelState.AddModelError("", "Seçenekler 200 karakteri geçemez.");
                if (secenekler.Distinct(StringComparer.OrdinalIgnoreCase).Count() != secenekler.Count)
                    ModelState.AddModelError("", "Seçenekler birbirinden farklı olmalıdır.");
            }
            else if (!string.Equals(soru, anket.Soru, StringComparison.Ordinal))
            {
                ModelState.AddModelError("", "Oy verilmiş anketin sorusu değiştirilemez.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.OyVar = oyVar;
                ViewBag.Soru = soru;
                ViewBag.Aciklama = aciklama;
                ViewBag.Baslangic = baslangicTarihi;
                ViewBag.Bitis = bitisTarihi;
                ViewBag.Secenekler = secenekler;
                return View(anket);
            }

            // Oy varsa soru ve seçenekler korunur; POST ile gönderilen seçenekler dikkate alınmaz.
            if (!oyVar)
            {
                anket.Soru = soru;
                _context.AnketSecenekleri.RemoveRange(anket.Secenekler);
                anket.Secenekler = secenekler.Select((metin, index) => new AnketSecenek
                {
                    AnketId = anket.Id,
                    SecenekMetni = metin,
                    SiraNo = index + 1
                }).ToList();
            }
            anket.Aciklama = aciklama;
            anket.BaslangicTarihi = baslangicTarihi.ToUniversalTime();
            anket.BitisTarihi = bitisTarihi.ToUniversalTime();
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Anket başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();
            var anket = await _context.Anketler.Include(x => x.Secenekler)
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (anket == null) return NotFound();
            if (await _context.AnketOylari.AnyAsync(x => x.AnketId == id))
            {
                TempData["Hata"] = "Oy kullanılmış anket silinemez. Yayından kaldırabilirsiniz.";
                return RedirectToAction(nameof(Index));
            }
            _context.AnketSecenekleri.RemoveRange(anket.Secenekler);
            _context.Anketler.Remove(anket);
            await _context.SaveChangesAsync();
            TempData["Basarili"] = "Anket silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExcelIndir(int id)
        {
            var dernekId = await DernekIdGetir();
            if (!dernekId.HasValue) return Forbid();

            var anket = await _context.Anketler
                .AsNoTracking()
                .Include(x => x.Secenekler)
                .FirstOrDefaultAsync(x => x.Id == id && x.DernekId == dernekId.Value);
            if (anket == null) return NotFound();

            var dernekAdi = await _context.Dernekler
                .AsNoTracking()
                .Where(x => x.Id == dernekId.Value)
                .Select(x => x.DernekAdi)
                .FirstAsync();

            var sayilar = await _context.AnketOylari
                .AsNoTracking()
                .Where(x => x.AnketId == id)
                .GroupBy(x => x.AnketSecenekId)
                .Select(g => new { SecenekId = g.Key, Adet = g.Count() })
                .ToDictionaryAsync(x => x.SecenekId, x => x.Adet);

            var toplam = sayilar.Values.Sum();
            using var kitap = new XLWorkbook();
            var sayfa = kitap.Worksheets.Add("Anket Sonuçları");

            sayfa.Cell("A1").Value = "DERNEKNET - ANKET SONUÇ RAPORU";
            sayfa.Range("A1:C1").Merge();
            sayfa.Range("A1:C1").Style.Fill.BackgroundColor = XLColor.FromHtml("#087F8C");
            sayfa.Range("A1:C1").Style.Font.FontColor = XLColor.White;
            sayfa.Range("A1:C1").Style.Font.Bold = true;
            sayfa.Range("A1:C1").Style.Font.FontSize = 15;
            sayfa.Row(1).Height = 30;

            sayfa.Cell("A3").Value = "Dernek";
            sayfa.Cell("B3").Value = dernekAdi;
            sayfa.Cell("A4").Value = "Anket";
            sayfa.Cell("B4").Value = anket.Soru;
            sayfa.Cell("A5").Value = "Açıklama";
            sayfa.Cell("B5").Value = anket.Aciklama ?? "";
            sayfa.Cell("A6").Value = "Başlangıç";
            sayfa.Cell("B6").Value = anket.BaslangicTarihi.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            sayfa.Cell("A7").Value = "Bitiş";
            sayfa.Cell("B7").Value = anket.BitisTarihi.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            sayfa.Cell("A8").Value = "Toplam oy";
            sayfa.Cell("B8").Value = toplam;
            sayfa.Range("A3:A8").Style.Font.Bold = true;

            sayfa.Cell("A10").Value = "Seçenek";
            sayfa.Cell("B10").Value = "Oy sayısı";
            sayfa.Cell("C10").Value = "Yüzde";
            var baslik = sayfa.Range("A10:C10");
            baslik.Style.Fill.BackgroundColor = XLColor.FromHtml("#DFF4F1");
            baslik.Style.Font.Bold = true;

            var satir = 11;
            foreach (var secenek in anket.Secenekler.OrderBy(x => x.SiraNo))
            {
                var oy = sayilar.TryGetValue(secenek.Id, out var adet) ? adet : 0;
                sayfa.Cell(satir, 1).Value = secenek.SecenekMetni;
                sayfa.Cell(satir, 2).Value = oy;
                sayfa.Cell(satir, 3).Value = toplam == 0 ? 0d : (double)oy / toplam;
                sayfa.Cell(satir, 3).Style.NumberFormat.Format = "0.0%";
                satir++;
            }

            sayfa.Columns("A:C").AdjustToContents();
            sayfa.Column("A").Width = Math.Min(Math.Max(sayfa.Column("A").Width, 24), 65);
            sayfa.Column("B").Width = Math.Min(Math.Max(sayfa.Column("B").Width, 20), 65);
            sayfa.Column("C").Width = 16;
            sayfa.SheetView.FreezeRows(10);

            using var bellek = new MemoryStream();
            kitap.SaveAs(bellek);
            return File(bellek.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Anket_Sonuclari_{anket.Id}.xlsx");
        }

    }
}
