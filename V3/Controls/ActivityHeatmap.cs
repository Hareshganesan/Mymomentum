using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MyTodo.Controls
{
    /// <summary>
    /// Custom LeetCode/GitHub-style consistency heatmap control.
    /// High-performance frozen brush rendering with Terminal Emerald &amp; Cyan cyber gradient.
    /// </summary>
    public class ActivityHeatmap : UserControl
    {
        public static readonly DependencyProperty ActivityDataProperty =
            DependencyProperty.Register(nameof(ActivityData), typeof(Dictionary<string, double>), typeof(ActivityHeatmap),
                new PropertyMetadata(null, OnDataChanged));

        public Dictionary<string, double>? ActivityData
        {
            get => (Dictionary<string, double>?)GetValue(ActivityDataProperty);
            set => SetValue(ActivityDataProperty, value);
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ActivityHeatmap heatmap)
            {
                heatmap.RenderGrid();
            }
        }

        private readonly Grid _containerGrid = new();

        // Cached frozen brushes for 60+ FPS zero-overhead rendering
        private static readonly SolidColorBrush FutureDateBrush;
        private static readonly SolidColorBrush EmptyDayBrush;
        private static readonly SolidColorBrush Level1Brush; // Light Emerald
        private static readonly SolidColorBrush Level2Brush; // Medium Emerald
        private static readonly SolidColorBrush Level3Brush; // Cyber Cyan / High Momentum

        static ActivityHeatmap()
        {
            FutureDateBrush = new SolidColorBrush(Color.FromArgb(15, 148, 163, 184)); // Dim slate
            FutureDateBrush.Freeze();

            EmptyDayBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)); // Translucent cell
            EmptyDayBrush.Freeze();

            Level1Brush = new SolidColorBrush(Color.FromArgb(85, 255, 255, 255)); // Frosted White L1
            Level1Brush.Freeze();

            Level2Brush = new SolidColorBrush(Color.FromArgb(165, 255, 255, 255)); // Frosted White L2
            Level2Brush.Freeze();

            Level3Brush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)); // Pure White L3
            Level3Brush.Freeze();
        }

        public ActivityHeatmap()
        {
            Content = _containerGrid;
            Loaded += (s, e) => RenderGrid();
        }

        public void Refresh()
        {
            RenderGrid();
        }

        private void RenderGrid()
        {
            _containerGrid.Children.Clear();
            _containerGrid.ColumnDefinitions.Clear();
            _containerGrid.RowDefinitions.Clear();

            // 7 rows for days of week (0 = Monday, 6 = Sunday)
            for (int r = 0; r < 7; r++)
            {
                _containerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            }

            // We show 16 weeks (112 days)
            const int weeksToShow = 16;
            for (int c = 0; c < weeksToShow; c++)
            {
                _containerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            }

            var today = DateTime.Today;
            int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            var startMonday = today.AddDays(-daysSinceMonday).AddDays(-(weeksToShow - 1) * 7);

            var data = ActivityData ?? new Dictionary<string, double>();

            for (int w = 0; w < weeksToShow; w++)
            {
                for (int d = 0; d < 7; d++)
                {
                    var cellDate = startMonday.AddDays(w * 7 + d);
                    var dateKey = cellDate.ToString("yyyy-MM-dd");

                    var rect = new Border
                    {
                        Width = 10,
                        Height = 10,
                        CornerRadius = new CornerRadius(2.5),
                        Margin = new Thickness(2),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (cellDate > today)
                    {
                        rect.Background = FutureDateBrush;
                        rect.ToolTip = $"{cellDate:MMM dd, yyyy} (Future)";
                    }
                    else
                    {
                        double score = 0;
                        data.TryGetValue(dateKey, out score);

                        if (score <= 0)
                        {
                            rect.Background = EmptyDayBrush;
                            rect.ToolTip = $"{cellDate:MMM dd, yyyy}: No recorded practice";
                        }
                        else if (score < 40)
                        {
                            rect.Background = Level1Brush;
                            rect.ToolTip = $"{cellDate:MMM dd, yyyy}: 1-2 problems / reviews";
                        }
                        else if (score < 80)
                        {
                            rect.Background = Level2Brush;
                            rect.ToolTip = $"{cellDate:MMM dd, yyyy}: Solid practice session (3-4 items)";
                        }
                        else
                        {
                            rect.Background = Level3Brush;
                            rect.ToolTip = $"{cellDate:MMM dd, yyyy}: Peak activity day";
                        }
                    }

                    Grid.SetRow(rect, d);
                    Grid.SetColumn(rect, w);
                    _containerGrid.Children.Add(rect);
                }
            }
        }
    }
}
