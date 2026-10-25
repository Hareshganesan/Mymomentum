using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Momentum.Content;
using Momentum.Models;

namespace Momentum.Core
{
    internal static class ConverterHelpers
    {
        public static bool Invert(object? parameter) => parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase);
        public static Visibility Vis(bool visible, object? parameter) => visible ^ Invert(parameter) ? Visibility.Visible : Visibility.Collapsed;
    }

    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c) => ConverterHelpers.Vis(value is true, parameter);
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => value is Visibility.Visible;
    }

    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c) => ConverterHelpers.Vis(value != null, parameter);
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => Binding.DoNothing;
    }

    /// <summary>Visible when a number is non-zero or a collection is non-empty.</summary>
    public sealed class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
        {
            bool any = value switch
            {
                int i => i != 0,
                double d => Math.Abs(d) > double.Epsilon,
                ICollection col => col.Count > 0,
                _ => false,
            };
            return ConverterHelpers.Vis(any, parameter);
        }
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => Binding.DoNothing;
    }

    public sealed class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c) => ConverterHelpers.Vis(!string.IsNullOrWhiteSpace(value as string), parameter);
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => Binding.DoNothing;
    }

    public sealed class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c) => value is not true;
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => value is not true;
    }

    /// <summary>Maps a difficulty (enum or text) to the theme's Easy/Medium/Hard brush.</summary>
    public sealed class DifficultyBrushConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
        {
            var key = value?.ToString() switch
            {
                nameof(Difficulty.Easy) => "EasyBrush",
                nameof(Difficulty.Hard) => "HardBrush",
                _ => "MediumBrush",
            };
            return Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
        }
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => Binding.DoNothing;
    }

    /// <summary>Picks a theme brush by name: ConverterParameter "Success|Danger" chooses by bool.</summary>
    public sealed class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
        {
            var parts = (parameter as string ?? "Success|TextFaint").Split('|');
            var key = value is true ? parts[0] : parts.ElementAtOrDefault(1) ?? "TextFaint";
            if (string.Equals(key, "Transparent", StringComparison.OrdinalIgnoreCase))
                return Brushes.Transparent;
            return Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
        }
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => Binding.DoNothing;
    }

    /// <summary>
    /// Two-way "value equals parameter" for radio buttons bound to enums, ints or strings.
    /// </summary>
    public sealed class EqualsConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c) =>
            string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo c)
        {
            if (value is not true || parameter == null) return Binding.DoNothing;
            var p = parameter.ToString()!;
            var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (type.IsEnum) return Enum.Parse(type, p);
            if (type == typeof(int)) return int.Parse(p, CultureInfo.InvariantCulture);
            return p;
        }
    }

    public sealed class NodeHighlightToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
        {
            if (value is NodeHighlight h)
            {
                return h switch
                {
                    NodeHighlight.Active => Application.Current.TryFindResource("Accent") as Brush ?? Brushes.Goldenrod,
                    NodeHighlight.Matched => Application.Current.TryFindResource("Success") as Brush ?? Brushes.SeaGreen,
                    NodeHighlight.Comparing => Application.Current.TryFindResource("MediumBrush") as Brush ?? Brushes.SkyBlue,
                    NodeHighlight.Visited => Application.Current.TryFindResource("BorderStrong") as Brush ?? Brushes.DimGray,
                    NodeHighlight.Discarded => Application.Current.TryFindResource("Border") as Brush ?? Brushes.DarkSlateGray,
                    _ => Application.Current.TryFindResource("Border") as Brush ?? Brushes.Gray
                };
            }
            return Application.Current.TryFindResource("Border") as Brush ?? Brushes.Gray;
        }
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => Binding.DoNothing;
    }

    public sealed class NodeHighlightToBgBrushConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
        {
            if (value is NodeHighlight h)
            {
                return h switch
                {
                    NodeHighlight.Active => new SolidColorBrush(Color.FromArgb(40, 232, 180, 88)),
                    NodeHighlight.Matched => new SolidColorBrush(Color.FromArgb(45, 112, 201, 154)),
                    NodeHighlight.Comparing => new SolidColorBrush(Color.FromArgb(35, 96, 165, 250)),
                    NodeHighlight.Visited => new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    NodeHighlight.Discarded => new SolidColorBrush(Color.FromArgb(10, 0, 0, 0)),
                    _ => Application.Current.TryFindResource("Surface2") as Brush ?? Brushes.Transparent
                };
            }
            return Application.Current.TryFindResource("Surface2") as Brush ?? Brushes.Transparent;
        }
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => Binding.DoNothing;
    }

    public sealed class NodeHighlightToFgBrushConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
        {
            if (value is NodeHighlight h)
            {
                return h switch
                {
                    NodeHighlight.Active => Application.Current.TryFindResource("Accent") as Brush ?? Brushes.Goldenrod,
                    NodeHighlight.Matched => Application.Current.TryFindResource("Success") as Brush ?? Brushes.SeaGreen,
                    NodeHighlight.Comparing => Application.Current.TryFindResource("MediumBrush") as Brush ?? Brushes.SkyBlue,
                    NodeHighlight.Discarded => Application.Current.TryFindResource("TextFaint") as Brush ?? Brushes.Gray,
                    _ => Application.Current.TryFindResource("Text") as Brush ?? Brushes.White
                };
            }
            return Application.Current.TryFindResource("Text") as Brush ?? Brushes.White;
        }
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => Binding.DoNothing;
    }

    public sealed class HeightPercentToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
        {
            if (value is int pct)
            {
                double clamped = Math.Clamp(pct, 10, 100);
                return 28.0 + (clamped / 100.0) * 52.0;
            }
            return 50.0;
        }
        public object ConvertBack(object value, Type t, object parameter, CultureInfo c) => Binding.DoNothing;
    }
}
