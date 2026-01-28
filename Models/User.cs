namespace JournalApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? PinHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
