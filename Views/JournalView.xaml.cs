using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using JournalApp.Models;
using JournalApp.ViewModels;

namespace JournalApp.Views
{
    public partial class JournalView : UserControl
    {
        // Coursework mood categories
        public static List<MoodType> PositiveMoods { get; } = new()
        {
            MoodType.Happy, MoodType.Excited, MoodType.Relaxed, MoodType.Grateful, MoodType.Confident
        };

        public static List<MoodType> NeutralMoods { get; } = new()
        {
            MoodType.Calm, MoodType.Thoughtful, MoodType.Curious, MoodType.Nostalgic, MoodType.Bored
        };

        public static List<MoodType> NegativeMoods { get; } = new()
        {
            MoodType.Sad, MoodType.Angry, MoodType.Stressed, MoodType.Lonely, MoodType.Anxious
        };

        public JournalView()
        {
            InitializeComponent();
        }

        private void EntryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is JournalEntry entry && DataContext is JournalViewModel viewModel)
            {
                viewModel.SelectedDate = entry.Date;
            }
        }
    }

    public class MoodToBoolConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<MoodType> collection && parameter is MoodType mood)
            {
                return collection.Contains(mood);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Used to set IsChecked for primary mood radio buttons
    public class MoodEqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            if (values[0] is MoodType selected && values[1] is MoodType current)
                return selected == current;

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Used to set IsChecked for secondary mood checkboxes
    public class CollectionContainsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            if (values[0] is ObservableCollection<MoodType> collection && values[1] is MoodType mood)
                return collection.Contains(mood);

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
