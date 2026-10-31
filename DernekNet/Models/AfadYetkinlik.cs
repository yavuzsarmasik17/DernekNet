using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadYetkinlik
    {
        public int Id { get; set; }
        public int DernekId { get; set; }
        public Dernek Dernek { get; set; } = null!;
        [Required, MaxLength(150)] public string Ad { get; set; } = "";
        public string? Aciklama { get; set; }
        public ICollection<AfadGonulluYetkinlik> Gonulluler { get; set; } = new List<AfadGonulluYetkinlik>();

    }
}
