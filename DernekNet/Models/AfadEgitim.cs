using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadEgitim
    {
        public int Id { get; set; }
        public int DernekId { get; set; }
        public Dernek Dernek { get; set; } = null!;
        [Required, MaxLength(200)] public string Baslik { get; set; } = "";
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; }
        public bool AktifMi { get; set; } = true;
        public ICollection<AfadEgitimKatilim> Katilimlar { get; set; } = new List<AfadEgitimKatilim>();

    }
}
