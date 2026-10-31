using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadBasvuruForm
    {
        public int DernekId { get; set; }
        [Required(ErrorMessage = "Ad soyad zorunludur."), StringLength(120)]
        public string AdSoyad { get; set; } = "";
        [Phone, StringLength(30)] public string? Telefon { get; set; }
        [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password), MinLength(8)] public string Sifre { get; set; } = "";
        [Required, Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor."), DataType(DataType.Password)]
        public string SifreTekrar { get; set; } = "";

    }
}
