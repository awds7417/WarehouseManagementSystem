using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required]
        [StringLength(200)]
        public string PasswordHash { get; set; }

        [StringLength(50)]
        public string DisplayName { get; set; }

        public bool IsAdmin { get; set; }
    }
}
