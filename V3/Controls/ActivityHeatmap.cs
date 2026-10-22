using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyTodo.Controls
{
    /// <summary>
    /// Custom LeetCode/GitHub-style consistency heatmap control.
    /// Renders 7 rows (days of week) across weeks with gold gradient intensity.
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
            // Find current week's Sunday as end boundary
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
                        CornerRadius = new CornerRadius(2),
                        Margin = new Thickness(2),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (cellDate > today)
                    {
                        // Future date: very dim outline
                        rect.Background = new SolidColorBrush(Color.FromArgb(10, 212, 175, 55));
                        rect.ToolTip = $"{cellDate:MMM dd, yyyy} (Future)";
                    }
                    else
                    {
                        double score = 0;
                        data.TryGetValue(dateKey, out score);

                        if (score <= 0)
                        {
                            rect.Background = new SolidColorBrush(Color.FromArgb(25, 212, 175, 55));
                            rect.ToolTip = $"{cellDate:MMM dd, yyyy}: No recorded placement activity";
                        }
                        else if (score < 40)
                        {
                            rect.Background = new SolidColorBrush(Color.FromArgb(90, 212, 175, 55));
                            rect.ToolTip = $"{cellDate:MMM dd, yyyy}: 1-2 problems / reviews";
                        }
                        else if (score < 80)
                        {
                            rect.Background = new SolidColorBrush(Color.FromArgb(180, 212, 175, 55));
                            rect.ToolTip = $"{cellDate:MMM dd, yyyy}: Solid practice (3-4 items)";
                        }
                        else
                        {
                            rect.Background = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Bright Gold
                            rect.ToolTip = $"{cellDate:MMM dd, yyyy}: High Momentum Day! 🔥";
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
