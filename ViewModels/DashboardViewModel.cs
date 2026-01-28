using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using JournalApp.Models;
using JournalApp.Services;

namespace JournalApp.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly AnalyticsService _analyticsService;
        private readonly JournalService _journalService;
        private int _streak;
        private int _totalEntries;
        private Dictionary<MoodType, int> _moodDistribution = new();
        private int _moodDistributionMax = 1;
        private ObservableCollection<string> _topTags = new();
        private DateTime? _firstEntryDate;
        private DateTime? _lastEntryDate;
        private string? _selectedTag;
        private ObservableCollection<JournalEntry> _selectedTagEntries = new();

        public DashboardViewModel(AnalyticsService analyticsService, JournalService journalService)
        {
            _analyticsService = analyticsService;
            _journalService = journalService;
            RefreshCommand = new RelayCommand(_ => Refresh());
            SelectTagCommand = new RelayCommand(SelectTag);
            Refresh();
        }

        public int Streak
        {
            get => _streak;
            set
            {
                _streak = value;
                OnPropertyChanged();
            }
        }

        public int TotalEntries
        {
            get => _totalEntries;
            set
            {
                _totalEntries = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<MoodType, int> MoodDistribution
        {
            get => _moodDistribution;
            set
            {
                _moodDistribution = value;
                OnPropertyChanged();
            }
        }

        public int MoodDistributionMax
        {
            get => _moodDistributionMax;
            set
            {
                _moodDistributionMax = value < 1 ? 1 : value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> TopTags
        {
            get => _topTags;
            set
            {
                _topTags = value;
                OnPropertyChanged();
            }
        }

        public string? SelectedTag
        {
            get => _selectedTag;
            set
            {
                _selectedTag = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedTag));
                OnPropertyChanged(nameof(ShowTagHint));
                OnPropertyChanged(nameof(ShowNoEntriesForSelectedTag));
            }
        }

        public bool HasSelectedTag => !string.IsNullOrWhiteSpace(SelectedTag);
        public bool ShowTagHint => !HasSelectedTag;

        public ObservableCollection<JournalEntry> SelectedTagEntries
        {
            get => _selectedTagEntries;
            set
            {
                _selectedTagEntries = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedTagEntries));
                OnPropertyChanged(nameof(ShowNoEntriesForSelectedTag));
            }
        }

        public bool HasSelectedTagEntries => SelectedTagEntries != null && SelectedTagEntries.Any();
        public bool ShowNoEntriesForSelectedTag => HasSelectedTag && !HasSelectedTagEntries;

        public DateTime? FirstEntryDate
        {
            get => _firstEntryDate;
            set
            {
                _firstEntryDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime? LastEntryDate
        {
            get => _lastEntryDate;
            set
            {
                _lastEntryDate = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SelectTagCommand { get; }

        public void Refresh()
        {
            try
            {
                // Clear selected tag to show fresh dashboard
                SelectedTag = null;
                SelectedTagEntries = new ObservableCollection<JournalEntry>();

                // Refresh all dashboard data
                Streak = _analyticsService.GetStreak();
                TotalEntries = _analyticsService.GetTotalEntries();
                MoodDistribution = _analyticsService.GetMoodDistribution();
                MoodDistributionMax = MoodDistribution != null && MoodDistribution.Any()
                    ? MoodDistribution.Values.Max()
                    : 1;
                TopTags = new ObservableCollection<string>(_analyticsService.GetMostUsedTags());
                FirstEntryDate = _analyticsService.GetFirstEntryDate();
                LastEntryDate = _analyticsService.GetLastEntryDate();

                // Show success message
                MessageBox.Show("Dashboard refreshed successfully!", 
                    "Refresh Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing dashboard: {ex.Message}", 
                    "Refresh Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectTag(object? tagObj)
        {
            if (tagObj is not string tagName || string.IsNullOrWhiteSpace(tagName))
                return;

            try
            {
                SelectedTag = tagName;
                var entries = _journalService
                    .FilterByTag(tagName)
                    .OrderByDescending(e => e.Date)
                    .ToList();

                SelectedTagEntries = new ObservableCollection<JournalEntry>(entries);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading entries for tag '{tagName}': {ex.Message}",
                    "Tag Filter Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
