
using DernekNet.Data;
using DernekNet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DernekNet.Filters
{
    public class DernekAdminGuvenlikFiltresi : IAsyncAuthorizationFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DernekAdminGuvenlikFiltresi(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task OnAuthorizationAsync(
            AuthorizationFilterContext context)
        {
            // Yalnızca DernekAdmin alanını kontrol et.
            var area = context.RouteData.Values["area"]?.ToString();

            if (!string.Equals(
                area,
                "DernekAdmin",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            if (!context.HttpContext.User.IsInRole("DernekAdmin"))
            {
                context.Result = new ForbidResult();
                return;
            }

            var kullanici = await _userManager.GetUserAsync(
                context.HttpContext.User);

            if (kullanici == null ||
                !kullanici.EmailConfirmed ||
                !kullanici.DernekId.HasValue)
            {
                context.Result = new ForbidResult();
                return;
            }

            bool dernekAktif = await _context.Dernekler
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == kullanici.DernekId.Value &&
                    x.AktifMi);

            if (!dernekAktif)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
