using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace JournalApp.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            this.Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            // View loaded
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Scroll to top when refresh is clicked
            if (DashboardScrollViewer != null)
            {
                DashboardScrollViewer.ScrollToTop();
            }
        }
    }

    public class PercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length >= 2)
            {
                int count = 0;
                int total = 0;

                if (values[0] is int countInt)
                    count = countInt;
                else if (values[0] != null)
                    int.TryParse(values[0].ToString(), out count);

                if (values[1] is int totalInt)
                    total = totalInt;
                else if (values[1] != null)
                    int.TryParse(values[1].ToString(), out total);

                if (total > 0)
                {
                    return Math.Min((double)count / total * 200, 200); // Max width of 200
                }
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
