
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DernekNet.Models
{
    public class YonetimKurulu
    {
        public int Id { get; set; }

        [Required]
        public int DernekId { get; set; }

        public Dernek? Dernek { get; set; }

        [Required]
        public int UyeId { get; set; }

        [ForeignKey(nameof(UyeId))]
        public Uye? Uye { get; set; }

        [Required]
        [StringLength(50)]
        public string KurulTipi { get; set; } = "Yönetim Kurulu";

        [Required(ErrorMessage = "Görev seçiniz.")]
        [StringLength(100)]
        public string Gorev { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime GorevBaslangicTarihi { get; set; }
            = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime? GorevBitisTarihi { get; set; }

        public bool AktifMi { get; set; } = true;

        public int SiraNo { get; set; } = 0;

        [StringLength(500)]
        public string? Aciklama { get; set; }
    }
}
