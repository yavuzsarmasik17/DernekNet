using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DernekNet.Controllers
{
    public class AfadHesapController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;

        public AfadHesapController(ApplicationDbContext db, UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn)
        {
            _db = db; _users = users; _signIn = signIn;
        }

        private Task<bool> DernekAktif(int id) =>
            _db.Dernekler.AnyAsync(d => d.Id == id && d.AktifMi);

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> Basvuru(int dernekId)
        {
            if (!await DernekAktif(dernekId)) return NotFound();
            ViewBag.DernekAdi = await _db.Dernekler.Where(x => x.Id == dernekId)
                .Select(x => x.DernekAdi).FirstAsync();
            return View(new AfadBasvuruForm { DernekId = dernekId });
        }

        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Basvuru(AfadBasvuruForm form)
        {
            if (!await DernekAktif(form.DernekId)) return NotFound();
            ViewBag.DernekAdi = await _db.Dernekler.Where(x => x.Id == form.DernekId)
                .Select(x => x.DernekAdi).FirstAsync();
            if (!ModelState.IsValid) return View(form);

            form.Email = form.Email.Trim();
            form.AdSoyad = form.AdSoyad.Trim();
            if (await _users.FindByEmailAsync(form.Email) != null)
            {
                ModelState.AddModelError(nameof(form.Email),
                    "Bu e-posta adresi zaten kayıtlı. Mevcut hesabınızla başvuru için yöneticinizle iletişime geçin.");
                return View(form);
            }

            var user = new ApplicationUser
            {
                UserName = form.Email,
                Email = form.Email,
                PhoneNumber = form.Telefon?.Trim(),
                EmailConfirmed = false,
                DernekId = null // Dernek yöneticisi hesabı değildir.
            };
            var created = await _users.CreateAsync(user, form.Sifre);
            if (!created.Succeeded)
            {
                foreach (var error in created.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(form);
            }

            try
            {
                _db.AfadGonulluler.Add(new AfadGonullu
                {
                    DernekId = form.DernekId,
                    KullaniciId = user.Id,
                    AdSoyad = form.AdSoyad,
                    Telefon = form.Telefon?.Trim(),
                    OnaylandiMi = false,
                    AktifMi = true,
                    KayitTarihi = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Başarısız başvuruda sahipsiz Identity hesabı bırakma.
                _db.ChangeTracker.Clear();
                await _users.DeleteAsync(user);
                ModelState.AddModelError("", "Başvuru kaydedilemedi. Lütfen yeniden deneyin.");
                return View(form);
            }

            TempData["AfadMesaj"] = "Başvurunuz alındı. Dernek yöneticisinin onayını bekleyin.";
            return RedirectToAction(nameof(Giris), new { dernekId = form.DernekId });
        }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> Giris(int dernekId)
        {
            if (!await DernekAktif(dernekId)) return NotFound();
            ViewBag.DernekAdi = await _db.Dernekler.Where(x => x.Id == dernekId)
                .Select(x => x.DernekAdi).FirstAsync();
            return View(new AfadGirisForm { DernekId = dernekId });
        }

        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Giris(AfadGirisForm form)
        {
            if (!await DernekAktif(form.DernekId)) return NotFound();
            ViewBag.DernekAdi = await _db.Dernekler.Where(x => x.Id == form.DernekId)
                .Select(x => x.DernekAdi).FirstAsync();
            if (!ModelState.IsValid) return View(form);

            var user = await _users.FindByEmailAsync(form.Email.Trim());
            // Genel hata: başka derneğe kayıtlı hesabın varlığını açıklama.
            if (user == null)
            {
                ModelState.AddModelError("", "Giriş bilgileri geçersiz veya başvurunuz henüz onaylanmadı.");
                return View(form);
            }
            var volunteer = await _db.AfadGonulluler.AsNoTracking().FirstOrDefaultAsync(x =>
                x.KullaniciId == user.Id && x.DernekId == form.DernekId &&
                x.OnaylandiMi && x.AktifMi && x.Dernek.AktifMi);
            if (volunteer == null || !user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Giriş bilgileri geçersiz veya başvurunuz henüz onaylanmadı.");
                return View(form);
            }
            var result = await _signIn.PasswordSignInAsync(user, form.Sifre,
                isPersistent: false, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Giriş bilgileri geçersiz veya hesap geçici olarak kilitli.");
                return View(form);
            }
            return RedirectToAction(nameof(Panel), new { dernekId = form.DernekId });
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> Panel(int dernekId)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            var volunteer = await _db.AfadGonulluler.AsNoTracking()
                .Include(x => x.Dernek)
                .FirstOrDefaultAsync(x => x.KullaniciId == user.Id &&
                    x.DernekId == dernekId && x.Dernek.AktifMi &&
                    x.OnaylandiMi && x.AktifMi);
            if (volunteer == null || !user.EmailConfirmed) return Forbid();

            ViewBag.Atamalar = await _db.AfadGorevAtamalari.AsNoTracking()
                .Include(x => x.Gorev)
                .Where(x => x.AfadGonulluId == volunteer.Id &&
                    x.Gorev.DernekId == dernekId && x.Gorev.AktifMi)
                .OrderByDescending(x => x.AtamaTarihi).ToListAsync();
            ViewBag.Duyurular = await _db.AfadDuyurular.AsNoTracking()
                .Where(x => x.DernekId == dernekId && x.AktifMi &&
                    x.YayinTarihi <= DateTime.UtcNow)
                .OrderByDescending(x => x.YayinTarihi).Take(20).ToListAsync();
            ViewBag.Egitimler = await _db.AfadEgitimKatilimlari.AsNoTracking()
                .Include(x => x.Egitim)
                .Where(x => x.AfadGonulluId == volunteer.Id &&
                    x.Egitim.DernekId == dernekId && x.Egitim.AktifMi)
                .ToListAsync();
            ViewBag.Tatbikatlar = await _db.AfadTatbikatKatilimlari.AsNoTracking()
                .Include(x => x.Tatbikat)
                .Where(x => x.AfadGonulluId == volunteer.Id &&
                    x.Tatbikat.DernekId == dernekId && x.Tatbikat.AktifMi)
                .ToListAsync();

            ViewBag.Yetkinliklerim = await _db.AfadGonulluYetkinlikleri
                .AsNoTracking()
                .Include(x => x.Yetkinlik)
                .Where(x =>
                    x.AfadGonulluId == volunteer.Id &&
                    x.Yetkinlik.DernekId == dernekId)
                .OrderBy(x => x.Yetkinlik.Ad)
                .ToListAsync();

            ViewBag.EkipUyeliklerim = await _db.AfadEkipUyeleri
                .Include(x => x.Ekip)
                    .ThenInclude(e => e.Uyeler.Where(u =>
                        u.AktifMi && u.Gonullu.AktifMi &&
                        u.Gonullu.OnaylandiMi &&
                        u.Gonullu.DernekId == dernekId))
                    .ThenInclude(u => u.Gonullu)
                .Where(x => x.AfadGonulluId == volunteer.Id &&
                    x.AktifMi && x.Ekip.AktifMi &&
                    x.Ekip.DernekId == dernekId)
                .OrderBy(x => x.Ekip.Ad)
                .ToListAsync();

            return View(volunteer);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cikis(int dernekId)
        {
            // Ortak Identity çerezi kullanıldığı için tüm oturumu kapatır.
            await _signIn.SignOutAsync();
            return RedirectToAction("Index", "Afad", new { dernekId });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GorevDurumGuncelle(
            int dernekId, int atamaId, string yeniDurum)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();

            var volunteer = await _db.AfadGonulluler.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.KullaniciId == user.Id &&
                    x.DernekId == dernekId &&
                    x.Dernek.AktifMi &&
                    x.OnaylandiMi &&
                    x.AktifMi);

            if (volunteer == null || !user.EmailConfirmed)
                return Forbid();

            var atama = await _db.AfadGorevAtamalari
                .FirstOrDefaultAsync(x =>
                    x.Id == atamaId &&
                    x.AfadGonulluId == volunteer.Id &&
                    x.Gorev.DernekId == dernekId &&
                    x.Gorev.AktifMi);

            if (atama == null) return NotFound();

            if (atama.Durum == "Atandi" &&
                yeniDurum == "KabulEdildi")
            {
                atama.Durum = "KabulEdildi";
            }
            else if (atama.Durum == "KabulEdildi" &&
                     yeniDurum == "Tamamlandi")
            {
                atama.Durum = "Tamamlandi";
            }
            else
            {
                return BadRequest(
                    "Bu görev durumu değişikliği geçerli değil.");
            }

            await _db.SaveChangesAsync();

            TempData["AfadMesaj"] = "Görev durumunuz güncellendi.";

            return RedirectToAction(nameof(Panel),
                new { dernekId });
        }

    }
}
