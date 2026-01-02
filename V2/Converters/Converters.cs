using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MyTodo.Converters
{
    /// <summary>
    /// Converts boolean to Visibility (True = Visible, False = Collapsed)
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts boolean to a tag string for button styling (True = "Completed", False = null)
    /// </summary>
    public class BoolToTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                return "Completed";
            }
            return null!;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == "Completed";
        }
    }

    /// <summary>
    /// Converts progress color string to actual Color for progress bars
    /// </summary>
    public class ProgressColorConverter : IMultiValueConverter
    {
        private static readonly Color GreenColor = Color.FromRgb(74, 222, 128);
        private static readonly Color YellowColor = Color.FromRgb(251, 191, 36);
        private static readonly Color RedColor = Color.FromRgb(239, 68, 68);
        private static readonly Color AccentColor = Color.FromRgb(108, 99, 255);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is string colorName)
            {
                return colorName switch
                {
                    "Green" => GreenColor,
                    "Yellow" => YellowColor,
                    "Red" => RedColor,
                    _ => AccentColor
                };
            }
            return AccentColor;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverts boolean value
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts progress percentage to status color brush
    /// </summary>
    public class ProgressToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                if (progress >= 70)
                    return new SolidColorBrush(Color.FromRgb(74, 222, 128));  // Green
                if (progress >= 30)
                    return new SolidColorBrush(Color.FromRgb(251, 191, 36)); // Yellow
                return new SolidColorBrush(Color.FromRgb(239, 68, 68));      // Red
            }
            return new SolidColorBrush(Color.FromRgb(108, 99, 255));         // Accent
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts progress percentage and container width to actual pixel width
    /// </summary>
    public class ProgressWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is double progress && values[1] is double containerWidth)
            {
                var clampedProgress = Math.Max(0, Math.Min(100, progress));
                return (clampedProgress / 100.0) * containerWidth;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to Visibility (True = Collapsed, False = Visible) - Inverse of BoolToVisibility
    /// </summary>
    public class BoolToVisibilityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return true;
        }
    }

    /// <summary>
    /// Converts non-empty string to Visible, empty/null to Collapsed
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
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
