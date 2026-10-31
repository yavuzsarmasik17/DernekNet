
using System;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class Dernek
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string DernekAdi { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? LogoUrl { get; set; }

        [MaxLength(20)]
        public string AnaRenk { get; set; } = "#2563EB";

        [MaxLength(20)]
        public string IkinciRenk { get; set; } = "#FFFFFF";

        [MaxLength(500)]
        public string? Aciklama { get; set; }

        [MaxLength(250)]
        public string? Adres { get; set; }

        [MaxLength(20)]
        public string? Telefon { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;
    }
}
