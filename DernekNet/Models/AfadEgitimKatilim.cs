namespace DernekNet.Models
{
    public class AfadEgitimKatilim
    {
        public int Id { get; set; }
        public int AfadEgitimId { get; set; }
        public AfadEgitim Egitim { get; set; } = null!;
        public int AfadGonulluId { get; set; }
        public AfadGonullu Gonullu { get; set; } = null!;
        public bool KatildiMi { get; set; }
    }
}
