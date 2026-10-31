using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadHaber
    {
        public int Id { get; set; }
        public int DernekId { get; set; }
        public Dernek Dernek { get; set; } = null!;
        [Required, MaxLength(200)] public string Baslik { get; set; } = "";
        [Required] public string Icerik { get; set; } = "";
        public DateTime YayinTarihi { get; set; } = DateTime.UtcNow;
        public bool AktifMi { get; set; } = true;
    }
}
