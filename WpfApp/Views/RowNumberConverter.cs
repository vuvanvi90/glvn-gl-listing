using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfApp.Views
{
    /// <summary>
    /// Converter để tự động generate số thứ tự (STT) cho DataGrid
    /// </summary>
    public class RowNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataGridRow row)
            {
                // Get the index of the row (0-based)
                var index = row.GetIndex();
                
                // Return 1-based index
                return (index + 1).ToString();
            }
            
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}