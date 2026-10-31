
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class Duyuru
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Duyuru başlığı zorunludur.")]
        [StringLength(200)]
        public string Baslik { get; set; } = string.Empty;

        [Required(ErrorMessage = "Duyuru içeriği zorunludur.")]
        public string Icerik { get; set; } = string.Empty;

        public DateTime YayinTarihi { get; set; } = DateTime.UtcNow;

        public bool AktifMi { get; set; } = true;

        [Required]
        public int DernekId { get; set; }

        public Dernek? Dernek { get; set; }

        // Duyuruya ait fotoğraflar ve videolar
        public ICollection<DuyuruMedya> Medyalar { get; set; }
            = new List<DuyuruMedya>();
    }
}
