using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadGorevAtama
    {
        public int Id { get; set; }
        public int AfadGorevId { get; set; }
        public AfadGorev Gorev { get; set; } = null!;
        public int AfadGonulluId { get; set; }
        public AfadGonullu Gonullu { get; set; } = null!;
        public DateTime AtamaTarihi { get; set; } = DateTime.UtcNow;
        [MaxLength(30)] public string Durum { get; set; } = "Atandi";
    }
}
