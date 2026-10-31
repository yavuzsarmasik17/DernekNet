
using System;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AnketOy
    {
        public int Id { get; set; }

        [Required]
        public int AnketId { get; set; }

        public Anket? Anket { get; set; }

        [Required]
        public int AnketSecenekId { get; set; }

        public AnketSecenek? AnketSecenek { get; set; }

        // Tarayıcıya verilen rastgele kimliğin
        // SHA-256 ile oluşturulmuş özeti.
        [Required]
        [StringLength(64)]
        public string ZiyaretciAnahtariHash { get; set; }
            = string.Empty;

        public DateTime OyTarihi { get; set; }
            = DateTime.UtcNow;
    }
}
