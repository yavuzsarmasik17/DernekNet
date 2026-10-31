using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadGorevFormVm
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Görev başlığı zorunludur.")]
        [StringLength(200)]
        [Display(Name = "Görev başlığı")]
        public string Baslik { get; set; } = "";
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }
        [Display(Name = "Başlangıç tarihi")]
        [DataType(DataType.DateTime)]
        public DateTime? BaslangicTarihi { get; set; }
        [Display(Name = "Bitiş tarihi")]
        [DataType(DataType.DateTime)]
        public DateTime? BitisTarihi { get; set; }
        [Display(Name = "Aktif")]
        public bool AktifMi { get; set; } = true;
    }
}
