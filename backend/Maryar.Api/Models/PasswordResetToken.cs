namespace Maryar.Api.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public string UserId { get; set; }   // ← string, não int
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
    }
}
