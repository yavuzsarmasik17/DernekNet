
using System;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class Uye
    {
        public int Id { get; set; }

        [Required]
        public int DernekId { get; set; }

        public Dernek? Dernek { get; set; }

        [Required(ErrorMessage = "Ad zorunludur.")]
        [StringLength(100)]
        public string Ad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [StringLength(100)]
        public string Soyad { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Telefon { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli e-posta giriniz.")]
        [StringLength(150)]
        public string? Email { get; set; }

        public DateTime UyelikTarihi { get; set; }
            = DateTime.Today;

        public bool AktifMi { get; set; } = true;

        [StringLength(500)]
        public string? Aciklama { get; set; }

        [StringLength(300)]
        public string? FotografUrl { get; set; }
    }
}
