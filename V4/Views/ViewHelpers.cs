using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Momentum.Views
{
    /// <summary>One-off converters referenced from XAML with x:Static.</summary>
    public static class ViewHelpers
    {
        public static readonly IValueConverter BoolToBold = new Lambda(v => v is true ? FontWeights.SemiBold : FontWeights.Normal);
        public static readonly IValueConverter StarGlyph = new Lambda(v => v is true ? "" : "");
        public static readonly IValueConverter SolvedPrompt = new Lambda(v => v is true ? "Reviewed it again? Grade your recall" : "Solved it? Grade how it went");
        public static readonly IValueConverter RevealGlyph = new Lambda(v => v is true ? "" : "");

        internal sealed class Lambda : IValueConverter
        {
            private readonly Func<object?, object> _f;
            public Lambda(Func<object?, object> f) => _f = f;
            public object Convert(object? value, Type t, object? p, CultureInfo c) => _f(value);
            public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => Binding.DoNothing;
        }
    }

    public static class Fmt
    {
        /// <summary>0..100 → 0..1</summary>
        public static readonly IValueConverter Percent = new ViewHelpers.Lambda(v => v == null ? 0.0 : System.Convert.ToDouble(v, CultureInfo.InvariantCulture) / 100.0);
    }
}
