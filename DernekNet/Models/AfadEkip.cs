
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AfadEkip
    {
        public int Id { get; set; }

        public int DernekId { get; set; }
        public Dernek Dernek { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Ad { get; set; } = "";

        [MaxLength(1000)]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime OlusturmaTarihi { get; set; } =
            DateTime.UtcNow;

        public ICollection<AfadEkipUye> Uyeler { get; set; }
            = new List<AfadEkipUye>();
    }
}
