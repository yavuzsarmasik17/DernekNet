
using System;
using System.Collections.Generic;

namespace DernekNet.Models
{
    public class PlatformDernekKartViewModel
    {
        public int Id { get; set; }

        public string DernekAdi { get; set; } = "";

        public string? LogoUrl { get; set; }

        public bool AktifMi { get; set; }

        public DateTime KayitTarihi { get; set; }

        public int ToplamUye { get; set; }

        public int AktifUye { get; set; }

        public int AktifKurulGorevi { get; set; }
    }

    public class PlatformDashboardViewModel
    {
        public int ToplamDernek { get; set; }

        public int AktifDernek { get; set; }

        public int PasifDernek { get; set; }

        public int ToplamUye { get; set; }

        public int AktifUye { get; set; }

        public int AktifKurulGorevi { get; set; }

        public List<PlatformDernekKartViewModel> Dernekler
        { get; set; } = new();
    }
}
