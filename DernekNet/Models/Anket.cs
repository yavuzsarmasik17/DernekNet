
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class Anket
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Anket sorusu zorunludur.")]
        [StringLength(300)]
        public string Soru { get; set; } = string.Empty;

        public string? Aciklama { get; set; }

        public DateTime BaslangicTarihi { get; set; }
            = DateTime.UtcNow;

        public DateTime BitisTarihi { get; set; }

        public bool AktifMi { get; set; } = true;

        [Required]
        public int DernekId { get; set; }

        public Dernek? Dernek { get; set; }

        public ICollection<AnketSecenek> Secenekler { get; set; }
            = new List<AnketSecenek>();

        public ICollection<AnketOy> Oylar { get; set; }
            = new List<AnketOy>();
    }
}
