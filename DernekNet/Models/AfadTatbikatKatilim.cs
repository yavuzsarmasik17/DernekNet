using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadTatbikatKatilim
    {
        public int Id { get; set; }
        public int AfadTatbikatId { get; set; }
        public AfadTatbikat Tatbikat { get; set; } = null!;
        public int AfadGonulluId { get; set; }
        public AfadGonullu Gonullu { get; set; } = null!;
        [MaxLength(150)] public string? Gorevi { get; set; }
        public bool KatildiMi { get; set; }
    }
}
