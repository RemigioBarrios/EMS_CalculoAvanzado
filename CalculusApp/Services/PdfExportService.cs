using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using CalculusApp.Controls;
using WpfMath.Controls;

namespace CalculusApp.Services
{
    public static class PdfExportService
    {
        static PdfExportService()
        {
            try
            {
                PdfSharp.PdfSharpLicense.SetAgplLicense();
            }
            catch { }

            try
            {
                if (PdfSharp.Fonts.GlobalFontSettings.FontResolver == null)
                {
                    PdfSharp.Fonts.GlobalFontSettings.FontResolver = new WindowsFontResolver();
                }
            }
            catch { }
        }

        public static string ExportToPdf(WorksheetView worksheetView, string? targetPath = null)
        {
            if (worksheetView == null) throw new ArgumentNullException(nameof(worksheetView));

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                string title = worksheetView.CurrentAssignment?.Heading?.Title ?? "Calculus";
                string safeName = string.Join("_", title.Split(System.IO.Path.GetInvalidFileNameChars()));
                targetPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }

            var doc = new PdfDocument();
            doc.Info.Title = worksheetView.CurrentAssignment?.Heading?.Title ?? "Calculus Worksheet";
            doc.Info.Author = "Infinite Calculus Studio";
            doc.Info.Subject = "Calculus Assignment & Worksheet";

            var pages = worksheetView.GetPageBorders();
            if (pages.Count == 0)
            {
                throw new InvalidOperationException("No worksheet pages available to export.");
            }

            int pageIdx = 0;
            var debugLogs = new System.Text.StringBuilder();
            debugLogs.AppendLine($"ExportToPdf called. Total page borders found: {pages.Count}");

            foreach (var pageBorder in pages)
            {
                pageIdx++;
                if (pageBorder.ActualWidth == 0 || pageBorder.ActualHeight == 0)
                {
                    pageBorder.Measure(new Size(816, 1056));
                    pageBorder.Arrange(new Rect(0, 0, 816, 1056));
                }
                pageBorder.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
                pageBorder.UpdateLayout();

                debugLogs.AppendLine($"--- Starting Page {pageIdx} ---");
                debugLogs.AppendLine($"PageBorder size: ActualWidth={pageBorder.ActualWidth}, ActualHeight={pageBorder.ActualHeight}, Width={pageBorder.Width}, Height={pageBorder.Height}, IsLoaded={pageBorder.IsLoaded}, IsVisible={pageBorder.IsVisible}");

                var pdfPage = doc.AddPage();
                pdfPage.Width = XUnit.FromInch(8.5);  // 612 pt
                pdfPage.Height = XUnit.FromInch(11.0); // 792 pt

                using var gfx = XGraphics.FromPdfPage(pdfPage);

                try
                {
                    // 1. Draw page background
                    gfx.DrawRectangle(XBrushes.White, 0, 0, pdfPage.Width.Point, pdfPage.Height.Point);

                    // 2. Hybrid traversal: real selectable text + crisp rendered formula and graph images
                    int nodeCount = 0;
                    RenderVisualNode(pageBorder, pageBorder, gfx, ref nodeCount, debugLogs);
                    debugLogs.AppendLine($"Page {pageIdx} rendered successfully with {nodeCount} visual nodes.");
                }
                catch (Exception ex)
                {
                    debugLogs.AppendLine($"Hybrid export exception on page {pageIdx}: {ex}");
                    // Fallback to high-resolution rasterization if hybrid pass encounters an issue
                    RenderPageAsRasterFallback(pageBorder, gfx, pdfPage.Width.Point, pdfPage.Height.Point);
                }
            }

            string? dir = System.IO.Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                File.WriteAllText("pdf_export_debug.log", debugLogs.ToString());
            }
            catch { }

            doc.Save(targetPath);
            return targetPath;
        }

        private static void RenderVisualNode(Visual visual, Border pageBorder, XGraphics gfx, ref int nodeCount, System.Text.StringBuilder debugLogs)
        {
            if (visual == null) return;
            nodeCount++;

            // 1. Text elements: render as native vector selectable text
            if (visual is TextBlock tb)
            {
                if (string.IsNullOrWhiteSpace(tb.Text)) return;

                var transform = tb.TransformToAncestor(pageBorder);
                Point pt = transform.Transform(new Point(0, 0));
                double x = pt.X * 0.75;
                double y = pt.Y * 0.75;
                double w = Math.Max(1, tb.ActualWidth * 0.75);
                double h = Math.Max(1, tb.ActualHeight * 0.75);

                double fontSize = Math.Max(6.5, tb.FontSize * 0.75);
                XFontStyleEx style = XFontStyleEx.Regular;
                bool isBold = tb.FontWeight >= FontWeights.SemiBold;
                bool isItalic = tb.FontStyle == FontStyles.Italic;
                if (isBold && isItalic) style = XFontStyleEx.BoldItalic;
                else if (isBold) style = XFontStyleEx.Bold;
                else if (isItalic) style = XFontStyleEx.Italic;

                var font = CreateFont("Times New Roman", fontSize, style);

                XBrush brush = XBrushes.Black;
                if (tb.Foreground is SolidColorBrush scb)
                {
                    brush = new XSolidBrush(XColor.FromArgb(scb.Color.A, scb.Color.R, scb.Color.G, scb.Color.B));
                }

                DrawText(gfx, tb.Text, font, brush, x, y, w, h, tb.TextAlignment);
                return;
            }

            // 2. Formula elements: render LaTeX math formulas as high-resolution images
            if (visual is FormulaControl formula)
            {
                var transform = formula.TransformToAncestor(pageBorder);
                Point pt = transform.Transform(new Point(0, 0));
                double w = formula.ActualWidth;
                double h = formula.ActualHeight;

                if (w > 0 && h > 0)
                {
                    RenderFrameworkElementAsImage(formula, gfx, pt.X * 0.75, pt.Y * 0.75, w * 0.75, h * 0.75);
                }
                return;
            }

            // 3. Graph elements: render function graphs as high-resolution images
            if (visual is FunctionGraphControl graph)
            {
                var transform = graph.TransformToAncestor(pageBorder);
                Point pt = transform.Transform(new Point(0, 0));
                double w = graph.ActualWidth;
                double h = graph.ActualHeight;

                if (w > 0 && h > 0)
                {
                    RenderFrameworkElementAsImage(graph, gfx, pt.X * 0.75, pt.Y * 0.75, w * 0.75, h * 0.75);
                }
                return;
            }

            // 4. Punch holes (ellipses)
            if (visual is Ellipse ellipse)
            {
                var transform = ellipse.TransformToAncestor(pageBorder);
                Point pt = transform.Transform(new Point(0, 0));
                double ew = ellipse.ActualWidth * 0.75;
                double eh = ellipse.ActualHeight * 0.75;
                if (ew > 0 && eh > 0)
                {
                    var pen = new XPen(XColor.FromArgb(90, 100, 100, 100), 1.0);
                    var fill = new XSolidBrush(XColor.FromArgb(30, 0, 0, 0));
                    gfx.DrawEllipse(pen, fill, pt.X * 0.75, pt.Y * 0.75, ew, eh);
                }
                return;
            }

            // 5. Lines
            if (visual is Line line)
            {
                var transform = line.TransformToAncestor(pageBorder);
                Point p1 = transform.Transform(new Point(line.X1, line.Y1));
                Point p2 = transform.Transform(new Point(line.X2, line.Y2));
                XColor strokeCol = XColors.Black;
                if (line.Stroke is SolidColorBrush scb)
                {
                    strokeCol = XColor.FromArgb(scb.Color.A, scb.Color.R, scb.Color.G, scb.Color.B);
                }
                var pen = new XPen(strokeCol, Math.Max(0.5, line.StrokeThickness * 0.75));
                gfx.DrawLine(pen, p1.X * 0.75, p1.Y * 0.75, p2.X * 0.75, p2.Y * 0.75);
                return;
            }

            // 6. Borders (e.g. answer boxes, table cells)
            if (visual is Border b && b != pageBorder)
            {
                var transform = b.TransformToAncestor(pageBorder);
                Point pt = transform.Transform(new Point(0, 0));
                double bw = b.ActualWidth * 0.75;
                double bh = b.ActualHeight * 0.75;

                if (bw > 0 && bh > 0)
                {
                    XBrush? bgBrush = null;
                    if (b.Background is SolidColorBrush bgScb && bgScb.Color.A > 0)
                    {
                        bgBrush = new XSolidBrush(XColor.FromArgb(bgScb.Color.A, bgScb.Color.R, bgScb.Color.G, bgScb.Color.B));
                    }

                    XPen? borderPen = null;
                    if (b.BorderBrush is SolidColorBrush borderScb && borderScb.Color.A > 0 && b.BorderThickness.Left > 0)
                    {
                        borderPen = new XPen(XColor.FromArgb(borderScb.Color.A, borderScb.Color.R, borderScb.Color.G, borderScb.Color.B), Math.Max(0.5, b.BorderThickness.Left * 0.75));
                    }

                    double rx = b.CornerRadius.TopLeft * 0.75;
                    double ry = b.CornerRadius.TopLeft * 0.75;

                    if (rx > 0 || ry > 0)
                    {
                        if (borderPen != null && bgBrush != null)
                            gfx.DrawRoundedRectangle(borderPen, bgBrush, pt.X * 0.75, pt.Y * 0.75, bw, bh, rx * 2, ry * 2);
                        else if (borderPen != null)
                            gfx.DrawRoundedRectangle(borderPen, pt.X * 0.75, pt.Y * 0.75, bw, bh, rx * 2, ry * 2);
                        else if (bgBrush != null)
                            gfx.DrawRoundedRectangle(bgBrush, pt.X * 0.75, pt.Y * 0.75, bw, bh, rx * 2, ry * 2);
                    }
                    else
                    {
                        if (borderPen != null && bgBrush != null)
                            gfx.DrawRectangle(borderPen, bgBrush, pt.X * 0.75, pt.Y * 0.75, bw, bh);
                        else if (borderPen != null)
                            gfx.DrawRectangle(borderPen, pt.X * 0.75, pt.Y * 0.75, bw, bh);
                        else if (bgBrush != null)
                            gfx.DrawRectangle(bgBrush, pt.X * 0.75, pt.Y * 0.75, bw, bh);
                    }
                }
            }

            // 7. Container elements: recurse through children
            int childCount = VisualTreeHelper.GetChildrenCount(visual);
            for (int i = 0; i < childCount; i++)
            {
                if (VisualTreeHelper.GetChild(visual, i) is Visual child)
                {
                    RenderVisualNode(child, pageBorder, gfx, ref nodeCount, debugLogs);
                }
            }
        }

        private static void RenderFrameworkElementAsImage(FrameworkElement element, XGraphics gfx, double pdfX, double pdfY, double pdfW, double pdfH)
        {
            try
            {
                double dpi = 300.0;
                double scale = dpi / 96.0;
                int pxW = (int)Math.Max(1, Math.Ceiling(element.ActualWidth * scale));
                int pxH = (int)Math.Max(1, Math.Ceiling(element.ActualHeight * scale));

                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    var vb = new VisualBrush(element)
                    {
                        Stretch = Stretch.Uniform,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top
                    };
                    dc.DrawRectangle(vb, null, new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                }

                var rtb = new RenderTargetBitmap(pxW, pxH, dpi, dpi, PixelFormats.Pbgra32);
                rtb.Render(dv);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using var ms = new MemoryStream();
                encoder.Save(ms);
                ms.Position = 0;

                using var xImg = XImage.FromStream(ms);
                gfx.DrawImage(xImg, pdfX, pdfY, pdfW, pdfH);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to render element as image: {ex.Message}");
            }
        }

        private static void RenderPageAsRasterFallback(Border pageBorder, XGraphics gfx, double pageWidthPt, double pageHeightPt)
        {
            double widthDips = pageBorder.ActualWidth > 0 ? pageBorder.ActualWidth : 816;
            double heightDips = pageBorder.ActualHeight > 0 ? pageBorder.ActualHeight : 1056;

            double dpi = 300.0;
            double scale = dpi / 96.0;
            int pixelWidth = (int)Math.Ceiling(widthDips * scale);
            int pixelHeight = (int)Math.Ceiling(heightDips * scale);

            // Use VisualBrush with explicit Viewbox to isolate the pageBorder from any parent stack offset
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var vb = new VisualBrush(pageBorder)
                {
                    Stretch = Stretch.None,
                    Viewbox = new Rect(0, 0, widthDips, heightDips),
                    ViewboxUnits = BrushMappingMode.Absolute,
                    Viewport = new Rect(0, 0, widthDips, heightDips),
                    ViewportUnits = BrushMappingMode.Absolute
                };
                dc.DrawRectangle(vb, null, new Rect(0, 0, widthDips, heightDips));
            }

            var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi, dpi, PixelFormats.Pbgra32);
            rtb.Render(dv);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;

            using var xImage = XImage.FromStream(ms);
            gfx.DrawImage(xImage, 0, 0, pageWidthPt, pageHeightPt);
        }

        private static void DrawText(XGraphics gfx, string text, XFont font, XBrush brush, double x, double y, double width, double height, TextAlignment alignment)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var rawLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            double lineHeight = Math.Max(font.GetHeight(), font.Size * 1.25);
            double currentY = y;

            XStringFormat format = XStringFormats.TopLeft;
            if (alignment == TextAlignment.Center)
            {
                format = XStringFormats.TopCenter;
            }
            else if (alignment == TextAlignment.Right)
            {
                format = XStringFormats.TopRight;
            }

            foreach (var rawLine in rawLines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    currentY += lineHeight;
                    continue;
                }

                // If entire line fits without wrapping
                if (width <= 15 || gfx.MeasureString(rawLine, font).Width <= width + 6)
                {
                    gfx.DrawString(rawLine, font, brush, new XRect(x, currentY, Math.Max(width, 10), lineHeight), format);
                    currentY += lineHeight;
                    continue;
                }

                // Word wrap
                var words = rawLine.Split(' ');
                string currentLine = "";

                foreach (var word in words)
                {
                    string testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                    double testWidth = gfx.MeasureString(testLine, font).Width;

                    if (testWidth > width && !string.IsNullOrEmpty(currentLine))
                    {
                        gfx.DrawString(currentLine, font, brush, new XRect(x, currentY, width, lineHeight), format);
                        currentY += lineHeight;
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }

                if (!string.IsNullOrEmpty(currentLine))
                {
                    gfx.DrawString(currentLine, font, brush, new XRect(x, currentY, width, lineHeight), format);
                    currentY += lineHeight;
                }
            }
        }

        private static XFont CreateFont(string familyName, double size, XFontStyleEx style)
        {
            try
            {
                return new XFont(familyName, size, style);
            }
            catch
            {
                try
                {
                    return new XFont("Times New Roman", size, style);
                }
                catch
                {
                    return new XFont("Arial", size, style);
                }
            }
        }
    }
}
