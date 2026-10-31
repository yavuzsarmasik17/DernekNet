
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DernekNet.Models
{
    public class AnketSecenek
    {
        public int Id { get; set; }

        [Required]
        public int AnketId { get; set; }

        public Anket? Anket { get; set; }

        [Required]
        [StringLength(200)]
        public string SecenekMetni { get; set; } = string.Empty;

        public int SiraNo { get; set; }

        public ICollection<AnketOy> Oylar { get; set; }
            = new List<AnketOy>();
    }
}
