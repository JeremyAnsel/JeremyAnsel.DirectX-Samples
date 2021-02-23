using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace CascadedShadowMaps11
{
    class ArrayIndexConverter : IMultiValueConverter
    {
        public static readonly ArrayIndexConverter Default = new ArrayIndexConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return null;
            }

            if (values[0] is not Array items)
            {
                return null;
            }

            if (values[1] is not int length)
            {
                return items;
            }

            if (parameter is not int extra)
            {
                extra = 0;
            }

            object[] array = items
                .Cast<object>()
                .Take(length + extra)
                .ToArray();

            return array;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
