
using System;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class DuyuruMedya
    {
        public int Id { get; set; }

        [Required]
        public int DuyuruId { get; set; }

        public Duyuru? Duyuru { get; set; }

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
