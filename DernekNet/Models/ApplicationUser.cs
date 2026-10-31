
using Microsoft.AspNetCore.Identity;

namespace DernekNet.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Kullanıcının yönettiği dernek
        public int? DernekId { get; set; }

        // Kullanıcının bağlı olduğu dernek
        public Dernek? Dernek { get; set; }
    }
}
