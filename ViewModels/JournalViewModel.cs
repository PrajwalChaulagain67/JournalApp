using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using JournalApp.Models;
using JournalApp.Services;
using Microsoft.Win32;

namespace JournalApp.ViewModels
{
    public class JournalViewModel : INotifyPropertyChanged
    {
        private readonly JournalService _journalService;
        private JournalEntry? _currentEntry;
        private string _content = string.Empty;
        private MoodType _selectedPrimaryMood;
        private ObservableCollection<MoodType> _selectedSecondaryMoods = new();
        private ObservableCollection<string> _tags = new();
        private string _newTag = string.Empty;
        private string _selectedCategory = "None";
        private DateTime _selectedDate = DateTime.Today;
        private string _searchTerm = string.Empty;
        private ObservableCollection<JournalEntry> _filteredEntries = new();

        public JournalViewModel(JournalService journalService)
        {
            _journalService = journalService;

            Categories = new ObservableCollection<string>(new[]
            {
                "None",
                "Work", "Career", "Studies", "Family", "Friends", "Relationships",
                "Health", "Fitness", "Personal Growth", "Self-care", "Hobbies", "Travel", "Nature",
                "Finance", "Spirituality", "Birthday", "Holiday", "Vacation", "Celebration",
                "Exercise", "Reading", "Writing", "Cooking", "Meditation", "Yoga", "Music",
                "Shopping", "Parenting", "Projects", "Planning", "Reflection"
            });

            PrebuiltTags = new ObservableCollection<string>(Categories.Where(c => c != "None"));

            LoadEntryForDate(DateTime.Today);
            LoadAllEntries();
        }

        // Notifies host (MainWindow) so Dashboard can refresh after saves/deletes.
        public event EventHandler? EntriesChanged;

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }

        public MoodType SelectedPrimaryMood
        {
            get => _selectedPrimaryMood;
            set
            {
                _selectedPrimaryMood = value;
                OnPropertyChanged();

                // Ensure primary mood cannot also be in secondary moods
                if (SelectedSecondaryMoods.Contains(value))
                {
                    SelectedSecondaryMoods.Remove(value);
                }
            }
        }

        public ObservableCollection<MoodType> SelectedSecondaryMoods
        {
            get => _selectedSecondaryMoods;
            set
            {
                _selectedSecondaryMoods = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Tags
        {
            get => _tags;
            set
            {
                _tags = value;
                OnPropertyChanged();
            }
        }

        public string NewTag
        {
            get => _newTag;
            set
            {
                _newTag = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Categories { get; }
        public ObservableCollection<string> PrebuiltTags { get; }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                LoadEntryForDate(value);
            }
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                SearchEntries();
            }
        }

        public ObservableCollection<JournalEntry> FilteredEntries
        {
            get => _filteredEntries;
            set
            {
                _filteredEntries = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand => new RelayCommand(_ => SaveEntry());
        public ICommand DeleteCommand => new RelayCommand(_ => DeleteEntry());
        public ICommand AddTagCommand => new RelayCommand(_ => AddTag());
        public ICommand RemoveTagCommand => new RelayCommand(RemoveTag);
        public ICommand ExportPdfCommand => new RelayCommand(_ => ExportToPdf());
        public ICommand ToggleSecondaryMoodCommand => new RelayCommand(ToggleSecondaryMood);
        public ICommand AddPrebuiltTagCommand => new RelayCommand(AddPrebuiltTag);
        public ICommand SetPrimaryMoodCommand => new RelayCommand(SetPrimaryMood);

        private void LoadEntryForDate(DateTime date)
        {
            _currentEntry = _journalService.GetEntryByDate(date);
            
            if (_currentEntry != null)
            {
                Content = _currentEntry.Content;
                if (!string.IsNullOrWhiteSpace(_currentEntry.Category) && !Categories.Contains(_currentEntry.Category))
                {
                    Categories.Add(_currentEntry.Category);
                }
                SelectedCategory = string.IsNullOrWhiteSpace(_currentEntry.Category) ? "None" : _currentEntry.Category!;
                var primaryMood = _currentEntry.Moods.FirstOrDefault(m => m.IsPrimary);
                SelectedPrimaryMood = primaryMood?.Type ?? MoodType.Calm;
                SelectedSecondaryMoods = new ObservableCollection<MoodType>(
                    _currentEntry.Moods.Where(m => !m.IsPrimary).Select(m => m.Type).Take(2));
                Tags = new ObservableCollection<string>(_currentEntry.Tags.Select(t => t.Name));
            }
            else
            {
                Content = string.Empty;
                SelectedCategory = "None";
                SelectedPrimaryMood = MoodType.Calm;
                SelectedSecondaryMoods.Clear();
                Tags.Clear();
            }
        }

        private void SaveEntry()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Content))
                {
                    var result = MessageBox.Show(
                        "The journal entry is empty. Do you want to save it anyway?",
                        "Empty Entry",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                var entry = new JournalEntry
                {
                    Date = SelectedDate.Date,
                    Content = Content ?? string.Empty,
                    Category = string.Equals(SelectedCategory, "None", StringComparison.OrdinalIgnoreCase) ? null : SelectedCategory,
                    UpdatedAt = DateTime.Now
                };

                // Add primary mood
                entry.Moods.Add(new Mood
                {
                    Type = SelectedPrimaryMood,
                    IsPrimary = true,
                    JournalEntryId = 0
                });

                // Add secondary moods
                if (SelectedSecondaryMoods != null)
                {
                    foreach (var moodType in SelectedSecondaryMoods.Take(2)) // requirement: max 2 secondary moods
                    {
                        entry.Moods.Add(new Mood
                        {
                            Type = moodType,
                            IsPrimary = false,
                            JournalEntryId = 0
                        });
                    }
                }

                // Add tags
                if (Tags != null)
                {
                    foreach (var tagName in Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tagName))
                        {
                            entry.Tags.Add(new Tag
                            {
                                Name = tagName,
                                JournalEntryId = 0
                            });
                        }
                    }
                }

                _journalService.SaveEntry(entry);
                LoadEntryForDate(SelectedDate);
                LoadAllEntries();
                
                MessageBox.Show("Journal entry saved successfully!", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                EntriesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving entry: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteEntry()
        {
            if (_currentEntry != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the journal entry for {SelectedDate:yyyy-MM-dd}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _journalService.DeleteEntry(SelectedDate);
                        LoadEntryForDate(SelectedDate);
                        LoadAllEntries();
                        MessageBox.Show("Journal entry deleted successfully!", "Delete Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                        EntriesChanged?.Invoke(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting entry: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("No entry to delete for this date.", "Delete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddTag()
        {
            var tag = (NewTag ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(tag) && !Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            {
                Tags.Add(tag);
                var tagName = tag;
                NewTag = string.Empty;
                // Success feedback is visual (tag appears), no popup needed
            }
            else if (!string.IsNullOrWhiteSpace(tag) && Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This tag already exists.", "Tag", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveTag(object? tag)
        {
            if (tag is string tagName)
            {
                Tags.Remove(tagName);
            }
        }

        private void ToggleSecondaryMood(object? mood)
        {
            if (mood is MoodType moodType)
            {
                if (moodType == SelectedPrimaryMood)
                {
                    MessageBox.Show("Primary mood cannot be selected as a secondary mood.", "Mood", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (SelectedSecondaryMoods.Contains(moodType))
                {
                    SelectedSecondaryMoods.Remove(moodType);
                }
                else
                {
                    // requirement: max 2 secondary moods
                    if (SelectedSecondaryMoods.Count >= 2)
                    {
                        MessageBox.Show("You can select up to 2 secondary moods.", "Mood Limit", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    SelectedSecondaryMoods.Add(moodType);
                }
            }
        }

        private void SetPrimaryMood(object? mood)
        {
            if (mood is MoodType moodType)
            {
                SelectedPrimaryMood = moodType;
            }
        }

        private void AddPrebuiltTag(object? tagObj)
        {
            if (tagObj is not string tag || string.IsNullOrWhiteSpace(tag))
                return;

            var trimmed = tag.Trim();
            if (!Tags.Any(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                Tags.Add(trimmed);
            }
        }

        private void SearchEntries()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                LoadAllEntries();
            }
            else
            {
                var results = _journalService.SearchEntries(SearchTerm);
                FilteredEntries = new ObservableCollection<JournalEntry>(results);
            }
        }

        private void LoadAllEntries()
        {
            var entries = _journalService.GetAllEntries();
            FilteredEntries = new ObservableCollection<JournalEntry>(entries);
        }

        private void ExportToPdf()
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"Journal_Export_{DateTime.Now:yyyyMMdd}.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var entries = _journalService.GetAllEntries();
                    if (entries == null || !entries.Any())
                    {
                        MessageBox.Show("No journal entries to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    JournalApp.Helpers.PdfExporter.ExportToPdf(entries, saveDialog.FileName);
                    MessageBox.Show($"Journal exported successfully!\n\nSaved to: {saveDialog.FileName}", 
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting PDF: {ex.Message}\n\nPlease ensure you have write permissions and PDF fonts are available.", 
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);
    }
}
