
using System;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class EtkinlikMedya
    {
        public int Id { get; set; }

        [Required]
        public int EtkinlikId { get; set; }

        public Etkinlik? Etkinlik { get; set; }

        [Required]
        [StringLength(500)]
        public string DosyaYolu { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string MedyaTipi { get; set; } = string.Empty;

        public DateTime YuklenmeTarihi { get; set; }
            = DateTime.UtcNow;
    }
}
