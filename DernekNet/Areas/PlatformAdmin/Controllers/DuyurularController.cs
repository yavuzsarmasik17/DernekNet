
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
    public class DuyurularController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DuyurularController(ApplicationDbContext context)
        {
            _context = context;
        }

        // TÜM DERNEKLERİN DUYURULARI - SALT OKUNUR
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var duyurular = await _context.Duyurular
                .AsNoTracking()
                .Include(x => x.Dernek)
                .OrderByDescending(x => x.YayinTarihi)
                .ToListAsync();

            return View(duyurular);
        }

        // ESKİ DUYURU EKLEME ADRESLERİNİ KAPAT
        [HttpGet]
        public IActionResult Create()
        {
            return Forbid();
        }

        [HttpPost]
        public IActionResult CreatePost()
        {
            return Forbid();
        }

        // ESKİ DUYURU DÜZENLEME ADRESLERİNİ KAPAT
        [HttpGet]
        public IActionResult Edit(int id)
        {
            return Forbid();
        }

        [HttpPost]
        [ActionName("Edit")]
        public IActionResult EditPost(int id)
        {
            return Forbid();
        }
    }
}
