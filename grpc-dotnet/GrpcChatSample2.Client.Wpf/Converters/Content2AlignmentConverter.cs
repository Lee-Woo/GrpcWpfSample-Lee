using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GrpcChatSample2.Client.Wpf.Converters
{
    public class Content2AlignmentConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2) return HorizontalAlignment.Left;

            var content = values[0].ToString() ?? string.Empty;
            var fromName = values[1].ToString() ?? string.Empty;
            if (content.Contains(fromName + ":")) return HorizontalAlignment.Left;

            return HorizontalAlignment.Right;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class Content2TextAlignmentConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2) return TextAlignment.Left;

            var content = values[0].ToString() ?? string.Empty;
            var fromName = values[1].ToString() ?? string.Empty;
            if (content.Contains(fromName + ":")) return TextAlignment.Left;

            return TextAlignment.Right;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class Content2ForegroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2) 
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));

            var content = values[0].ToString() ?? string.Empty;
            var fromName = values[1].ToString() ?? string.Empty;
            if (content.Contains(fromName + ":")) 
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#254EDB"));

            if(content.Contains("System:") || content.Contains("Server:"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3DAA22"));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
