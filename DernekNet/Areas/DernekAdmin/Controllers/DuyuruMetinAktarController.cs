using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;

namespace DernekNet.Areas.DernekAdmin.Controllers
{
    [Area("DernekAdmin")]
    [Authorize(Roles = "DernekAdmin")]
    public class DuyuruMetinAktarController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly ApplicationDbContext _db;
        private const long Limit = 5 * 1024 * 1024;
        private const int MetinLimit = 100000;

        public DuyuruMetinAktarController(UserManager<ApplicationUser> users, ApplicationDbContext db)
        {
            _users = users;
            _db = db;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Oku(IFormFile? dosya)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null || !user.EmailConfirmed || !user.DernekId.HasValue ||
                !await _db.Dernekler.AsNoTracking().AnyAsync(x => x.Id == user.DernekId.Value && x.AktifMi))
                return Forbid();

            if (dosya == null || dosya.Length == 0 || dosya.Length > Limit)
                return BadRequest(new { hata = "En fazla 5 MB boyutunda dolu bir dosya seçin." });

            var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
            if (uzanti != ".txt" && uzanti != ".docx" && uzanti != ".pdf")
                return BadRequest(new { hata = "Yalnızca TXT, DOCX veya PDF dosyası seçin." });

            try
            {
                using var ms = new MemoryStream();
                await dosya.CopyToAsync(ms);
                ms.Position = 0;
                string metin;
                if (uzanti == ".txt")
                {
                    using var reader = new StreamReader(ms, new UTF8Encoding(false, true), true, 4096, leaveOpen: true);
                    metin = await reader.ReadToEndAsync();
                }
                else if (uzanti == ".docx")
                {
                    using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: true);
                    var entry = zip.GetEntry("word/document.xml");
                    if (entry == null || entry.Length > 10 * 1024 * 1024)
                        return BadRequest(new { hata = "Geçerli bir Word belgesi seçin." });
                    using var xmlStream = entry.Open();
                    var doc = XDocument.Load(xmlStream);
                    XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                    metin = string.Join("\n", doc.Descendants(w + "p")
                        .Select(par => string.Concat(par.Descendants(w + "t").Select(t => t.Value)))
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                }
                else
                {
                    using var pdf = PdfDocument.Open(ms);
                    if (pdf.NumberOfPages > 100)
                        return BadRequest(new { hata = "PDF en fazla 100 sayfa olabilir." });
                    var sb = new StringBuilder();
                    foreach (var page in pdf.GetPages())
                    {
                        sb.AppendLine(page.Text);
                        if (sb.Length > MetinLimit)
                            return BadRequest(new { hata = "Metin 100.000 karakter sınırını aşıyor." });
                    }
                    metin = sb.ToString();
                }
                metin = metin.Trim();
                if (string.IsNullOrWhiteSpace(metin))
                    return BadRequest(new { hata = "Dosyada okunabilir metin bulunamadı. Taranmış PDF için OCR gerekir." });
                if (metin.Length > MetinLimit)
                    return BadRequest(new { hata = "Metin 100.000 karakter sınırını aşıyor." });
                return Json(new { metin });
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is System.Xml.XmlException ||
                ex is DecoderFallbackException || ex is IOException || ex is ArgumentException ||
                ex is UnauthorizedAccessException)
            {
                return BadRequest(new { hata = "Dosya okunamadı. Geçerli ve şifresiz bir dosya seçin." });
            }
            catch (Exception)
            {
                return BadRequest(new { hata = "Belge işlenemedi. Şifresiz, metin içeren bir dosya deneyin." });
            }
        }
    }
}
