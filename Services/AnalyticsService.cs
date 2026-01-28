using JournalApp.Models;
using JournalApp.Services;

namespace JournalApp.Services
{
    public class AnalyticsService
    {
        private readonly JournalService _journalService;

        public AnalyticsService(JournalService journalService)
        {
            _journalService = journalService;
        }

        public int GetStreak()
        {
            var entries = _journalService.GetAllEntries();
            if (!entries.Any())
                return 0;

            var sortedDates = entries.Select(e => e.Date.Date).OrderByDescending(d => d).ToList();
            var today = DateTime.Today;
            var streak = 0;

            // Check if today has an entry
            if (sortedDates.Contains(today))
            {
                streak = 1;
            }
            else if (sortedDates.Contains(today.AddDays(-1)))
            {
                // If today doesn't have an entry, start from yesterday
                streak = 0;
                return 0;
            }
            else
            {
                return 0;
            }

            // Count consecutive days
            var currentDate = today.AddDays(-1);
            while (sortedDates.Contains(currentDate))
            {
                streak++;
                currentDate = currentDate.AddDays(-1);
            }

            return streak;
        }

        public int GetTotalEntries()
        {
            return _journalService.GetAllEntries().Count;
        }

        public Dictionary<MoodType, int> GetMoodDistribution()
        {
            var entries = _journalService.GetAllEntries();
            var distribution = new Dictionary<MoodType, int>();

            foreach (var entry in entries)
            {
                // Coursework requirement: Primary mood is used for analytics.
                var primaryMood = entry.Moods.FirstOrDefault(m => m != null && m.IsPrimary);
                if (primaryMood == null)
                    continue;

                if (!distribution.ContainsKey(primaryMood.Type))
                    distribution[primaryMood.Type] = 0;

                distribution[primaryMood.Type]++;
            }

            return distribution;
        }

        public List<string> GetMostUsedTags(int count = 10)
        {
            var entries = _journalService.GetAllEntries();
            var tagCounts = new Dictionary<string, int>();

            foreach (var entry in entries)
            {
                foreach (var tag in entry.Tags)
                {
                    if (!tagCounts.ContainsKey(tag.Name))
                        tagCounts[tag.Name] = 0;
                    tagCounts[tag.Name]++;
                }
            }

            return tagCounts.OrderByDescending(t => t.Value)
                .Take(count)
                .Select(t => t.Key)
                .ToList();
        }

        public DateTime? GetFirstEntryDate()
        {
            var entries = _journalService.GetAllEntries();
            return entries.OrderBy(e => e.Date).FirstOrDefault()?.Date;
        }

        public DateTime? GetLastEntryDate()
        {
            var entries = _journalService.GetAllEntries();
            return entries.OrderByDescending(e => e.Date).FirstOrDefault()?.Date;
        }
    }
}
