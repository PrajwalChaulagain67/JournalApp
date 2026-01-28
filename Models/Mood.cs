namespace JournalApp.Models
{
    public enum MoodType
    {
        // IMPORTANT:
        // These explicit values preserve compatibility with existing DB rows (stored as integers).
        Happy = 0,
        Sad = 1,
        Angry = 2,
        Anxious = 3,
        Excited = 4,
        Calm = 5,
        Tired = 6,
        Energetic = 7,
        Confused = 8,
        Grateful = 9,
        Lonely = 10,
        Content = 11,

        // Added to match coursework requirements (do NOT renumber existing values above)
        Relaxed = 12,
        Confident = 13,
        Thoughtful = 14,
        Curious = 15,
        Nostalgic = 16,
        Bored = 17,
        Stressed = 18
    }

    public class Mood
    {
        public int Id { get; set; }
        public MoodType Type { get; set; }
        public bool IsPrimary { get; set; }
        public int JournalEntryId { get; set; }
    }
}
