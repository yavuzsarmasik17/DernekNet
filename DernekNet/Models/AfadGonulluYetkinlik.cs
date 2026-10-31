using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadGonulluYetkinlik
    {
        public int Id { get; set; }
        public int AfadGonulluId { get; set; }
        public AfadGonullu Gonullu { get; set; } = null!;
        public int AfadYetkinlikId { get; set; }
        public AfadYetkinlik Yetkinlik { get; set; } = null!;
        public DateTime? BelgeTarihi { get; set; }
        public DateTime? GecerlilikBitisi { get; set; }
        [MaxLength(200)] public string? BelgeNo { get; set; }
    }
}
