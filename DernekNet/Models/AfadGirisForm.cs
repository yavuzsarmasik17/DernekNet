using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadGirisForm
    {
        public int DernekId { get; set; }
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)] public string Sifre { get; set; } = "";
    }
}
