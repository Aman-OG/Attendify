using System.Windows;
using System.Globalization;
using System.Windows.Data;

namespace Attendify.Converters
{
    public class TimeSpanTo12HourConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                DateTime today = DateTime.Today;
                DateTime dateTime = today.Add(timeSpan);
                return dateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            }
            if (value is string timeString && TimeSpan.TryParse(timeString, out TimeSpan ts))
            {
                DateTime today = DateTime.Today;
                DateTime dateTime = today.Add(ts);
                return dateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                if (DateTime.TryParse(str, out DateTime dt))
                {
                    return dt.TimeOfDay;
                }
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
