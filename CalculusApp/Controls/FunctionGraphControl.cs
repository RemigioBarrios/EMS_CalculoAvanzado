using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CalculusApp.Generators;

namespace CalculusApp.Controls
{
    public class FunctionGraphControl : FrameworkElement
    {
        public static readonly DependencyProperty IsBlankProperty =
            DependencyProperty.Register(nameof(IsBlank), typeof(bool), typeof(FunctionGraphControl),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FunctionExprProperty =
            DependencyProperty.Register(nameof(FunctionExpr), typeof(string), typeof(FunctionGraphControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty XMinProperty =
            DependencyProperty.Register(nameof(XMin), typeof(double), typeof(FunctionGraphControl),
                new FrameworkPropertyMetadata(-6.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty XMaxProperty =
            DependencyProperty.Register(nameof(XMax), typeof(double), typeof(FunctionGraphControl),
                new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty YMinProperty =
            DependencyProperty.Register(nameof(YMin), typeof(double), typeof(FunctionGraphControl),
                new FrameworkPropertyMetadata(-6.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty YMaxProperty =
            DependencyProperty.Register(nameof(YMax), typeof(double), typeof(FunctionGraphControl),
                new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsBlank
        {
            get => (bool)GetValue(IsBlankProperty);
            set => SetValue(IsBlankProperty, value);
        }

        public string FunctionExpr
        {
            get => (string)GetValue(FunctionExprProperty);
            set => SetValue(FunctionExprProperty, value);
        }

        public double XMin
        {
            get => (double)GetValue(XMinProperty);
            set => SetValue(XMinProperty, value);
        }

        public double XMax
        {
            get => (double)GetValue(XMaxProperty);
            set => SetValue(XMaxProperty, value);
        }

        public double YMin
        {
            get => (double)GetValue(YMinProperty);
            set => SetValue(YMinProperty, value);
        }

        public double YMax
        {
            get => (double)GetValue(YMaxProperty);
            set => SetValue(YMaxProperty, value);
        }

        public List<Point> Holes { get; set; } = new();
        public List<Point> Points { get; set; } = new();

        public FunctionGraphControl()
        {
            Width = 210;
            Height = 210;
            SnapsToDevicePixels = true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth > 0 ? ActualWidth : Width;
            double h = ActualHeight > 0 ? ActualHeight : Height;
            if (w <= 0 || h <= 0) return;

            // Strict [-6, 6] range enforcement
            double xMin = -6.0;
            double xMax = 6.0;
            double yMin = -6.0;
            double yMax = 6.0;

            // Inner padding to ensure tick labels (-6, 6) and arrows fit without clipping
            double pad = 14.0;
            var plotRect = new Rect(pad, pad, Math.Max(20, w - 2 * pad), Math.Max(20, h - 2 * pad));

            // Background & Border
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));
            dc.DrawRectangle(Brushes.White, new Pen(new SolidColorBrush(Color.FromRgb(185, 185, 195)), 1.0), plotRect);

            double ToScreenX(double x) => plotRect.Left + (x - xMin) / (xMax - xMin) * plotRect.Width;
            double ToScreenY(double y) => plotRect.Top + (yMax - y) / (yMax - yMin) * plotRect.Height;

            // 1. Grid lines for all integers from -6 to 6
            var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(225, 225, 232)), 0.8);
            gridPen.Freeze();

            for (int x = -6; x <= 6; x++)
            {
                if (x == 0) continue;
                double sx = ToScreenX(x);
                dc.DrawLine(gridPen, new Point(sx, plotRect.Top), new Point(sx, plotRect.Bottom));
            }

            for (int y = -6; y <= 6; y++)
            {
                if (y == 0) continue;
                double sy = ToScreenY(y);
                dc.DrawLine(gridPen, new Point(plotRect.Left, sy), new Point(plotRect.Right, sy));
            }

            // 2. Axes (x=0, y=0)
            var axisPen = new Pen(new SolidColorBrush(Color.FromRgb(50, 50, 58)), 1.3);
            axisPen.Freeze();

            double zeroX = ToScreenX(0);
            double zeroY = ToScreenY(0);

            // X Axis
            dc.DrawLine(axisPen, new Point(plotRect.Left, zeroY), new Point(plotRect.Right + 7, zeroY));
            // Y Axis
            dc.DrawLine(axisPen, new Point(zeroX, plotRect.Bottom), new Point(zeroX, plotRect.Top - 7));

            // Arrowheads on positive axes
            var arrowBrush = new SolidColorBrush(Color.FromRgb(50, 50, 58));
            arrowBrush.Freeze();

            // Right arrow (X)
            var arrowX = new PathGeometry();
            var figX = new PathFigure { StartPoint = new Point(plotRect.Right + 2, zeroY - 3), IsClosed = true, IsFilled = true };
            figX.Segments.Add(new LineSegment(new Point(plotRect.Right + 7, zeroY), true));
            figX.Segments.Add(new LineSegment(new Point(plotRect.Right + 2, zeroY + 3), true));
            arrowX.Figures.Add(figX);
            dc.DrawGeometry(arrowBrush, null, arrowX);

            // Top arrow (Y)
            var arrowY = new PathGeometry();
            var figY = new PathFigure { StartPoint = new Point(zeroX - 3, plotRect.Top - 2), IsClosed = true, IsFilled = true };
            figY.Segments.Add(new LineSegment(new Point(zeroX, plotRect.Top - 7), true));
            figY.Segments.Add(new LineSegment(new Point(zeroX + 3, plotRect.Top - 2), true));
            arrowY.Figures.Add(figY);
            dc.DrawGeometry(arrowBrush, null, arrowY);

            // Axis labels 'x' and 'y'
            var typeface = new Typeface(new FontFamily("Cambria, Times New Roman"), FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);
            var textX = new FormattedText("x", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10, arrowBrush, 1.0);
            dc.DrawText(textX, new Point(plotRect.Right - 4, zeroY + 3));

            var textY = new FormattedText("y", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10, arrowBrush, 1.0);
            dc.DrawText(textY, new Point(zeroX + 4, plotRect.Top - 12));

            // Numeric tick labels at -6, -4, -2, 2, 4, 6
            var numTypeface = new Typeface(new FontFamily("Segoe UI, Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var numBrush = new SolidColorBrush(Color.FromRgb(90, 90, 100));
            numBrush.Freeze();

            int[] tickValues = { -6, -4, -2, 2, 4, 6 };
            foreach (int v in tickValues)
            {
                double sx = ToScreenX(v);
                var ft = new FormattedText(v.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, numTypeface, 7.5, numBrush, 1.0);
                dc.DrawText(ft, new Point(sx - ft.Width / 2, zeroY + 2));
                dc.DrawLine(axisPen, new Point(sx, zeroY - 2), new Point(sx, zeroY + 2));

                double sy = ToScreenY(v);
                var fty = new FormattedText(v.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, numTypeface, 7.5, numBrush, 1.0);
                dc.DrawText(fty, new Point(zeroX - fty.Width - 3, sy - fty.Height / 2));
                dc.DrawLine(axisPen, new Point(zeroX - 2, sy), new Point(zeroX + 2, sy));
            }

            // 3. Plot curve (if not blank and expression provided)
            if (!IsBlank && !string.IsNullOrWhiteSpace(FunctionExpr))
            {
                dc.PushClip(new RectangleGeometry(plotRect));
                DrawFunctionCurve(dc, ToScreenX, ToScreenY, plotRect);
                dc.Pop();
            }

            // 4. Draw holes (open circles) and points (closed dots)
            DrawHolesAndPoints(dc, ToScreenX, ToScreenY, plotRect);
        }

        private void DrawFunctionCurve(DrawingContext dc, Func<double, double> toScreenX, Func<double, double> toScreenY, Rect plotRect)
        {
            var evaluator = MathExpressionEvaluator.Compile(FunctionExpr);
            if (evaluator == null) return;

            var curvePen = new Pen(new SolidColorBrush(Color.FromRgb(30, 80, 160)), 1.8);
            curvePen.Freeze();

            var asymptotePen = new Pen(new SolidColorBrush(Color.FromRgb(220, 80, 80)), 1.0);
            asymptotePen.DashStyle = DashStyles.Dash;
            asymptotePen.Freeze();

            int steps = 240;
            double xMin = -6.0;
            double xMax = 6.0;
            double step = (xMax - xMin) / steps;

            Point? lastPoint = null;
            double? lastYVal = null;

            for (int i = 0; i <= steps; i++)
            {
                double x = xMin + i * step;
                double y;
                try
                {
                    y = evaluator(x);
                }
                catch
                {
                    lastPoint = null;
                    lastYVal = null;
                    continue;
                }

                if (double.IsNaN(y) || double.IsInfinity(y))
                {
                    lastPoint = null;
                    lastYVal = null;
                    continue;
                }

                // Check for vertical asymptote jump
                if (lastYVal.HasValue && Math.Abs(y - lastYVal.Value) > 10.0 && Math.Sign(y) != Math.Sign(lastYVal.Value))
                {
                    double asympX = toScreenX(x - step / 2);
                    dc.DrawLine(asymptotePen, new Point(asympX, plotRect.Top), new Point(asympX, plotRect.Bottom));
                    lastPoint = null;
                    lastYVal = y;
                    continue;
                }

                double sx = toScreenX(x);
                double sy = toScreenY(y);
                var curPoint = new Point(sx, sy);

                if (lastPoint.HasValue)
                {
                    if ((y >= -15 && y <= 15) || (lastYVal.HasValue && lastYVal.Value >= -15 && lastYVal.Value <= 15))
                    {
                        dc.DrawLine(curvePen, lastPoint.Value, curPoint);
                    }
                }

                lastPoint = curPoint;
                lastYVal = y;
            }
        }

        private void DrawHolesAndPoints(DrawingContext dc, Func<double, double> toScreenX, Func<double, double> toScreenY, Rect plotRect)
        {
            var holePen = new Pen(new SolidColorBrush(Color.FromRgb(30, 80, 160)), 1.8);
            holePen.Freeze();
            var holeBg = Brushes.White;

            foreach (var pt in Holes)
            {
                if (pt.X >= -6 && pt.X <= 6 && pt.Y >= -6 && pt.Y <= 6)
                {
                    double sx = toScreenX(pt.X);
                    double sy = toScreenY(pt.Y);
                    dc.DrawEllipse(holeBg, holePen, new Point(sx, sy), 3.5, 3.5);
                }
            }

            var pointBrush = new SolidColorBrush(Color.FromRgb(30, 80, 160));
            pointBrush.Freeze();

            foreach (var pt in Points)
            {
                if (pt.X >= -6 && pt.X <= 6 && pt.Y >= -6 && pt.Y <= 6)
                {
                    double sx = toScreenX(pt.X);
                    double sy = toScreenY(pt.Y);
                    dc.DrawEllipse(pointBrush, null, new Point(sx, sy), 3.5, 3.5);
                }
            }
        }
    }
}
