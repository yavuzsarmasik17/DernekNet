using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadGorev
    {
        public int Id { get; set; }
        public int DernekId { get; set; }
        public Dernek Dernek { get; set; } = null!;
        [Required, MaxLength(200)] public string Baslik { get; set; } = "";
        public string? Aciklama { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public bool AktifMi { get; set; } = true;
        public ICollection<AfadGorevAtama> Atamalar { get; set; } = new List<AfadGorevAtama>();

    }
}
