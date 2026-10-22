using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyTodo.Controls
{
    /// <summary>
    /// Attached property providing 60/120+ FPS hardware-interpolated smooth inertial mouse wheel scrolling
    /// for WPF ScrollViewers. Replaces jerky default WPF stepped jumping with silky exponential decay glide.
    /// </summary>
    public static class SmoothScrollHelper
    {
        public static readonly DependencyProperty SmoothScrollProperty =
            DependencyProperty.RegisterAttached(
                "SmoothScroll",
                typeof(bool),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(false, OnSmoothScrollChanged));

        public static bool GetSmoothScroll(DependencyObject obj) => (bool)obj.GetValue(SmoothScrollProperty);
        public static void SetSmoothScroll(DependencyObject obj, bool value) => obj.SetValue(SmoothScrollProperty, value);

        private static readonly DependencyProperty ControllerProperty =
            DependencyProperty.RegisterAttached(
                "Controller",
                typeof(SmoothScrollController),
                typeof(SmoothScrollHelper),
                new PropertyMetadata(null));

        private static void OnSmoothScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer scrollViewer) return;

            if ((bool)e.NewValue)
            {
                var controller = new SmoothScrollController(scrollViewer);
                scrollViewer.SetValue(ControllerProperty, controller);
            }
            else
            {
                if (scrollViewer.GetValue(ControllerProperty) is SmoothScrollController controller)
                {
                    controller.Detach();
                    scrollViewer.ClearValue(ControllerProperty);
                }
            }
        }
    }

    internal class SmoothScrollController
    {
        private readonly ScrollViewer _scrollViewer;
        private double _targetVerticalOffset;
        private double _targetHorizontalOffset;
        private double _lastSetVerticalOffset;
        private double _lastSetHorizontalOffset;
        private bool _isRendering;
        private long _lastTimestamp;
        private const double ScrollDecayRate = 18.0; // Silky ~200ms settling time
        private const double NotchPixels = 120.0;    // Logical pixels per standard wheel notch

        public SmoothScrollController(ScrollViewer scrollViewer)
        {
            _scrollViewer = scrollViewer;
            _scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            _scrollViewer.ScrollChanged += OnScrollChanged;
            _scrollViewer.Unloaded += OnUnloaded;

            _targetVerticalOffset = _scrollViewer.VerticalOffset;
            _targetHorizontalOffset = _scrollViewer.HorizontalOffset;
            _lastSetVerticalOffset = _scrollViewer.VerticalOffset;
            _lastSetHorizontalOffset = _scrollViewer.HorizontalOffset;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Detect if horizontal scroll is requested (Shift pressed, or only horizontal scrollable)
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            bool canScrollV = _scrollViewer.ScrollableHeight > 0;
            bool canScrollH = _scrollViewer.ScrollableWidth > 0;

            if (isShift || (!canScrollV && canScrollH))
            {
                if (!canScrollH) return;
                e.Handled = true;

                if (!_isRendering)
                {
                    _targetHorizontalOffset = _scrollViewer.HorizontalOffset;
                }

                double deltaPixels = (e.Delta / 120.0) * NotchPixels;
                _targetHorizontalOffset = Math.Clamp(_targetHorizontalOffset - deltaPixels, 0, _scrollViewer.ScrollableWidth);
                StartRendering();
            }
            else
            {
                if (!canScrollV) return;
                e.Handled = true;

                if (!_isRendering)
                {
                    _targetVerticalOffset = _scrollViewer.VerticalOffset;
                }

                double deltaPixels = (e.Delta / 120.0) * NotchPixels;
                _targetVerticalOffset = Math.Clamp(_targetVerticalOffset - deltaPixels, 0, _scrollViewer.ScrollableHeight);
                StartRendering();
            }
        }

        private void StartRendering()
        {
            if (!_isRendering)
            {
                _isRendering = true;
                _lastTimestamp = Stopwatch.GetTimestamp();
                _lastSetVerticalOffset = _scrollViewer.VerticalOffset;
                _lastSetHorizontalOffset = _scrollViewer.HorizontalOffset;
                CompositionTarget.Rendering += OnRendering;
            }
        }

        private void StopRendering()
        {
            if (_isRendering)
            {
                _isRendering = false;
                CompositionTarget.Rendering -= OnRendering;
            }
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (!_isRendering) return;

            long now = Stopwatch.GetTimestamp();
            double dt = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
            _lastTimestamp = now;

            if (dt <= 0.0) return;
            if (dt > 0.05) dt = 0.05; // clamp to 50ms to prevent giant leaps on hitch

            double factor = 1.0 - Math.Exp(-ScrollDecayRate * dt);

            bool verticalDone = true;
            if (_scrollViewer.ScrollableHeight > 0)
            {
                double currentV = _scrollViewer.VerticalOffset;
                double diffV = _targetVerticalOffset - currentV;
                if (Math.Abs(diffV) < 0.5)
                {
                    _scrollViewer.ScrollToVerticalOffset(_targetVerticalOffset);
                    _lastSetVerticalOffset = _targetVerticalOffset;
                }
                else
                {
                    double nextV = currentV + (diffV * factor);
                    _scrollViewer.ScrollToVerticalOffset(nextV);
                    _lastSetVerticalOffset = nextV;
                    verticalDone = false;
                }
            }

            bool horizontalDone = true;
            if (_scrollViewer.ScrollableWidth > 0)
            {
                double currentH = _scrollViewer.HorizontalOffset;
                double diffH = _targetHorizontalOffset - currentH;
                if (Math.Abs(diffH) < 0.5)
                {
                    _scrollViewer.ScrollToHorizontalOffset(_targetHorizontalOffset);
                    _lastSetHorizontalOffset = _targetHorizontalOffset;
                }
                else
                {
                    double nextH = currentH + (diffH * factor);
                    _scrollViewer.ScrollToHorizontalOffset(nextH);
                    _lastSetHorizontalOffset = nextH;
                    horizontalDone = false;
                }
            }

            if (verticalDone && horizontalDone)
            {
                StopRendering();
            }
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // If the user drags the scrollbar thumb manually or page key pressed:
            if (_isRendering)
            {
                // Check if the change came from user interaction rather than our own interpolated update
                if (Math.Abs(_scrollViewer.VerticalOffset - _lastSetVerticalOffset) > 2.0 ||
                    Math.Abs(_scrollViewer.HorizontalOffset - _lastSetHorizontalOffset) > 2.0)
                {
                    _targetVerticalOffset = _scrollViewer.VerticalOffset;
                    _targetHorizontalOffset = _scrollViewer.HorizontalOffset;
                    StopRendering();
                }
            }
            else
            {
                _targetVerticalOffset = _scrollViewer.VerticalOffset;
                _targetHorizontalOffset = _scrollViewer.HorizontalOffset;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Detach();
        }

        public void Detach()
        {
            StopRendering();
            _scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
            _scrollViewer.ScrollChanged -= OnScrollChanged;
            _scrollViewer.Unloaded -= OnUnloaded;
        }
    }
}
