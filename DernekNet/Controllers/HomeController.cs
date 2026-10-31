
using DernekNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DernekNet.Controllers
{
    [AllowAnonymous]
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
            var dernekler = await _context.Dernekler
                .AsNoTracking()
                .Where(x => x.AktifMi)
                .OrderBy(x => x.DernekAdi)
                .ToListAsync();

            return View(dernekler);
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
