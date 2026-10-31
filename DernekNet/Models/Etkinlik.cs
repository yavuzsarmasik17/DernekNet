
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class Etkinlik
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Etkinlik adı zorunludur.")]
        [StringLength(200)]
        public string EtkinlikAdi { get; set; } = string.Empty;

        public string? Aciklama { get; set; }

        [Required]
        public DateTime EtkinlikTarihi { get; set; }

        public string? Konum { get; set; }

        public bool AktifMi { get; set; } = true;

        [Required]
        public int DernekId { get; set; }

        public Dernek? Dernek { get; set; }

        // Etkinliğe ait fotoğraflar ve videolar
        public ICollection<EtkinlikMedya> Medyalar { get; set; }
            = new List<EtkinlikMedya>();
    }
}
