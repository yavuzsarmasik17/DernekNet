using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadGonullu
    {
        public int Id { get; set; }
        public int DernekId { get; set; }
        public Dernek Dernek { get; set; } = null!;
        [Required, MaxLength(450)] public string KullaniciId { get; set; } = "";
        public ApplicationUser Kullanici { get; set; } = null!;
        [Required, MaxLength(120)] public string AdSoyad { get; set; } = "";
        [MaxLength(30)] public string? Telefon { get; set; }
        public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;
        public bool OnaylandiMi { get; set; }
        public bool AktifMi { get; set; } = true;
        public ICollection<AfadGorevAtama> GorevAtamalari { get; set; } = new List<AfadGorevAtama>();
        public ICollection<AfadEgitimKatilim> EgitimKatilimlari { get; set; } = new List<AfadEgitimKatilim>();
        public ICollection<AfadTatbikatKatilim> TatbikatKatilimlari { get; set; } = new List<AfadTatbikatKatilim>();
        public ICollection<AfadGonulluYetkinlik> Yetkinlikleri { get; set; } = new List<AfadGonulluYetkinlik>();

        public ICollection<AfadEkipUye> EkipUyelikleri { get; set; }
            = new List<AfadEkipUye>();


    }
}
