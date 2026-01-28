namespace JournalApp.Models
{
    public class JournalEntry
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Category { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties (not stored in DB directly)
        public List<Mood> Moods { get; set; } = new();
        public List<Tag> Tags { get; set; } = new();
    }
}
