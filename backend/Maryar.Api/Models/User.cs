using System;

namespace Maryar.Api.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } // "customer" | "admin"
        public DateTime CreatedAt { get; set; }
    }
}
