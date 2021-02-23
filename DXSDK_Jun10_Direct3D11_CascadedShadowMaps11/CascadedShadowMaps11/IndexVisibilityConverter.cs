using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CascadedShadowMaps11
{
    class IndexVisibilityConverter : IValueConverter
    {
        public static readonly IndexVisibilityConverter Default = new IndexVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int count)
            {
                return Visibility.Visible;
            }

            if (parameter is not int index)
            {
                return Visibility.Visible;
            }

            if (index < count)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
