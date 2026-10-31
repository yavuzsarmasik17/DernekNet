
using System;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadEkipUye
    {
        public int Id { get; set; }

        public int AfadEkipId { get; set; }
        public AfadEkip Ekip { get; set; } = null!;

        public int AfadGonulluId { get; set; }
        public AfadGonullu Gonullu { get; set; } = null!;

        public bool EkipLideriMi { get; set; } = false;

        public DateTime KatilimTarihi { get; set; } =
            DateTime.UtcNow;

        public bool AktifMi { get; set; } = true;
    }
}
