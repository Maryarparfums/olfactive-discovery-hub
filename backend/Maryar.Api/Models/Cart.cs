using System;

namespace Maryar.Api.Models
{
    public class Cart
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string AnonymousToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
