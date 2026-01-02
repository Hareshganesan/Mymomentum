using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace MyTodo.Controls
{
    /// <summary>
    /// Premium stat card with glass effect and gold accent.
    /// </summary>
    public class StatCard : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(StatCard),
                new PropertyMetadata("Stat", OnPropertyChanged));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(StatCard),
                new PropertyMetadata("0", OnPropertyChanged));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(StatCard),
                new PropertyMetadata("📊", OnPropertyChanged));

        public static readonly DependencyProperty AccentColorProperty =
            DependencyProperty.Register(nameof(AccentColor), typeof(Color), typeof(StatCard),
                new PropertyMetadata(Color.FromRgb(212, 175, 55), OnPropertyChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public Color AccentColor
        {
            get => (Color)GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }

        #endregion

        private Border _container = null!;
        private TextBlock _iconBlock = null!;
        private TextBlock _valueBlock = null!;
        private TextBlock _titleBlock = null!;
        private Rectangle _accentLine = null!;

        // Gold colors
        private static readonly Color GoldPrimary = Color.FromRgb(212, 175, 55);
        private static readonly Color GoldLight = Color.FromRgb(255, 215, 0);
        private static readonly Color DarkBg = Color.FromRgb(16, 16, 22);
        private static readonly Color DarkSurface = Color.FromRgb(20, 20, 28);

        public StatCard()
        {
            InitializeVisual();
        }

        private void InitializeVisual()
        {
            // Main container with glass effect
            _container = new Border
            {
                Background = new SolidColorBrush(DarkSurface),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 212, 175, 55)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(20, 16, 20, 16),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // Subtle shadow
            _container.Effect = new DropShadowEffect
            {
                BlurRadius = 20,
                ShadowDepth = 5,
                Direction = 270,
                Color = Colors.Black,
                Opacity = 0.3
            };

            var mainStack = new StackPanel();

            // Top row: Icon + Value
            var topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Icon with glow background
            var iconContainer = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(30, 212, 175, 55)),
                Margin = new Thickness(0, 0, 12, 0)
            };

            _iconBlock = new TextBlock
            {
                Text = Icon,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconContainer.Child = _iconBlock;
            Grid.SetColumn(iconContainer, 0);

            // Value (large, gold)
            _valueBlock = new TextBlock
            {
                Text = Value,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new LinearGradientBrush(GoldPrimary, GoldLight, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(_valueBlock, 1);

            topRow.Children.Add(iconContainer);
            topRow.Children.Add(_valueBlock);

            // Title (subtle)
            _titleBlock = new TextBlock
            {
                Text = Title,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 150)),
                Margin = new Thickness(0, 12, 0, 0)
            };

            // Accent line at bottom
            _accentLine = new Rectangle
            {
                Height = 3,
                Fill = new LinearGradientBrush(
                    Color.FromArgb(0, 212, 175, 55),
                    GoldPrimary,
                    new Point(0, 0.5),
                    new Point(1, 0.5)),
                RadiusX = 1.5,
                RadiusY = 1.5,
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            mainStack.Children.Add(topRow);
            mainStack.Children.Add(_titleBlock);
            mainStack.Children.Add(_accentLine);

            _container.Child = mainStack;
            Content = _container;

            // Add hover animation
            SetupHoverAnimation();
        }

        private void SetupHoverAnimation()
        {
            _container.MouseEnter += (s, e) =>
            {
                var borderAnim = new ColorAnimation(
                    Color.FromArgb(80, 212, 175, 55),
                    TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                
                var brush = new SolidColorBrush(Color.FromArgb(40, 212, 175, 55));
                _container.BorderBrush = brush;
                brush.BeginAnimation(SolidColorBrush.ColorProperty, borderAnim);

                // Scale up effect
                var transform = new ScaleTransform(1, 1);
                _container.RenderTransform = transform;
                _container.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleAnim = new DoubleAnimation(1.02, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

                // Enhance shadow
                if (_container.Effect is DropShadowEffect shadow)
                {
                    var shadowAnim = new DoubleAnimation(30, TimeSpan.FromMilliseconds(200));
                    shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, shadowAnim);
                    
                    var opacityAnim = new DoubleAnimation(0.5, TimeSpan.FromMilliseconds(200));
                    shadow.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
                }
            };

            _container.MouseLeave += (s, e) =>
            {
                var borderAnim = new ColorAnimation(
                    Color.FromArgb(40, 212, 175, 55),
                    TimeSpan.FromMilliseconds(200));
                
                if (_container.BorderBrush is SolidColorBrush brush)
                {
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, borderAnim);
                }

                if (_container.RenderTransform is ScaleTransform transform)
                {
                    var scaleAnim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(200));
                    transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                    transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                }

                if (_container.Effect is DropShadowEffect shadow)
                {
                    var shadowAnim = new DoubleAnimation(20, TimeSpan.FromMilliseconds(200));
                    shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, shadowAnim);
                    
                    var opacityAnim = new DoubleAnimation(0.3, TimeSpan.FromMilliseconds(200));
                    shadow.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
                }
            };
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StatCard card)
            {
                card.UpdateVisual();
            }
        }

        private void UpdateVisual()
        {
            if (_iconBlock != null) _iconBlock.Text = Icon;
            if (_valueBlock != null) _valueBlock.Text = Value;
            if (_titleBlock != null) _titleBlock.Text = Title;
        }
    }
}
