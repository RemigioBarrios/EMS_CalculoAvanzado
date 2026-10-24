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

            // 2. Formula elements: render LaTeX math formulas as high-resolution images + selectable LaTeX layer for OCR/PDF extractors
            if (visual is FormulaControl formula)
            {
                var transform = formula.TransformToAncestor(pageBorder);
                Point pt = transform.Transform(new Point(0, 0));
                double w = formula.ActualWidth;
                double h = formula.ActualHeight;

                if (w > 0 && h > 0)
                {
                    if (!string.IsNullOrWhiteSpace(formula.Formula))
                    {
                        var latexFont = CreateFont("Times New Roman", 4.0, XFontStyleEx.Regular);
                        var invisibleBrush = new XSolidBrush(XColor.FromArgb(2, 255, 255, 255));
                        gfx.DrawString($" ${formula.Formula.Trim()}$ ", latexFont, invisibleBrush,
                            new XRect(pt.X * 0.75, pt.Y * 0.75, Math.Max(10, w * 0.75), Math.Max(6, h * 0.75)),
                            XStringFormats.TopLeft);
                    }
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

        public static string ExportAssistantMarkdownToPdf(string markdown, string documentTitle, string subtitle, string targetPath)
        {
            var doc = new PdfDocument();
            doc.Info.Title = documentTitle;
            doc.Info.Author = "Elida Math Studio - Asistente IA";
            doc.Info.Subject = subtitle;

            double pageWidth = XUnit.FromInch(8.5).Point;   // 612 pt
            double pageHeight = XUnit.FromInch(11.0).Point; // 792 pt
            double margin = 45.0;
            double contentWidth = pageWidth - (margin * 2);
            double bottomLimit = pageHeight - 50.0;

            PdfPage currentPage = doc.AddPage();
            currentPage.Width = XUnit.FromPoint(pageWidth);
            currentPage.Height = XUnit.FromPoint(pageHeight);
            XGraphics gfx = XGraphics.FromPdfPage(currentPage);
            int pageNum = 1;

            var fTitle = CreateFont("Times New Roman", 16, XFontStyleEx.Bold);
            var fSubtitle = CreateFont("Times New Roman", 11, XFontStyleEx.Italic);
            var fH1 = CreateFont("Times New Roman", 14.5, XFontStyleEx.Bold);
            var fH2 = CreateFont("Times New Roman", 12.5, XFontStyleEx.Bold);
            var fH3 = CreateFont("Times New Roman", 11.2, XFontStyleEx.Bold);
            var fBody = CreateFont("Times New Roman", 10.5, XFontStyleEx.Regular);
            var fFormulaFallback = CreateFont("Courier New", 10.0, XFontStyleEx.Italic);
            var fFooter = CreateFont("Times New Roman", 8.5, XFontStyleEx.Regular);

            var bPrimary = new XSolidBrush(XColor.FromArgb(255, 17, 24, 39));
            var bAccent = new XSolidBrush(XColor.FromArgb(255, 2, 132, 199));
            var bSubAccent = new XSolidBrush(XColor.FromArgb(255, 14, 116, 144));
            var bMath = new XSolidBrush(XColor.FromArgb(255, 185, 28, 28));
            var bMuted = new XSolidBrush(XColor.FromArgb(255, 107, 114, 128));

            void DrawPageFooter(XGraphics g, int pNum)
            {
                g.DrawLine(new XPen(XColor.FromArgb(255, 209, 213, 219), 0.7), margin, pageHeight - 36, pageWidth - margin, pageHeight - 36);
                g.DrawString($"{documentTitle} — Página {pNum}", fFooter, bMuted, new XRect(margin, pageHeight - 28, contentWidth, 16), XStringFormats.TopRight);
            }

            void EnsureSpace(double neededHeight, ref double y)
            {
                if (y + neededHeight > bottomLimit)
                {
                    DrawPageFooter(gfx, pageNum);
                    gfx.Dispose();
                    currentPage = doc.AddPage();
                    currentPage.Width = XUnit.FromPoint(pageWidth);
                    currentPage.Height = XUnit.FromPoint(pageHeight);
                    gfx = XGraphics.FromPdfPage(currentPage);
                    pageNum++;
                    y = margin;
                }
            }

            double curY = margin;

            // Header Banner
            gfx.DrawString(documentTitle, fTitle, bPrimary, new XRect(margin, curY, contentWidth, 22), XStringFormats.TopLeft);
            curY += 22;
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                gfx.DrawString(subtitle, fSubtitle, bAccent, new XRect(margin, curY, contentWidth, 16), XStringFormats.TopLeft);
                curY += 18;
            }
            gfx.DrawLine(new XPen(XColor.FromArgb(255, 2, 132, 199), 1.2), margin, curY, pageWidth - margin, curY);
            curY += 14;

            var lines = (markdown ?? string.Empty).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            bool inBlockMath = false;
            var mathLines = new List<string>();

            void DrawBlockFormula(string latex, ref double yPos)
            {
                latex = latex.Trim().Trim('$').Trim();
                if (string.IsNullOrWhiteSpace(latex)) return;

                try
                {
                    var fc = new FormulaControl
                    {
                        Formula = latex,
                        Scale = 16.0,
                        Foreground = new SolidColorBrush(Color.FromRgb(159, 18, 57))
                    };
                    fc.Measure(new Size(contentWidth, 220));
                    fc.Arrange(new Rect(0, 0, fc.DesiredSize.Width, fc.DesiredSize.Height));
                    fc.UpdateLayout();

                    double wPt = Math.Max(20, fc.ActualWidth * 0.75);
                    double hPt = Math.Max(14, fc.ActualHeight * 0.75);
                    EnsureSpace(hPt + 12, ref yPos);

                    double xPos = margin + Math.Max(0, (contentWidth - wPt) / 2.0);
                    RenderFrameworkElementAsImage(fc, gfx, xPos, yPos + 2, wPt, hPt);
                    yPos += hPt + 10;
                    return;
                }
                catch
                {
                    EnsureSpace(20, ref yPos);
                    DrawWrappedParagraph(gfx, latex, fFormulaFallback, bMath, margin + 12, ref yPos, contentWidth - 24, bottomLimit, () =>
                    {
                        DrawPageFooter(gfx, pageNum);
                        gfx.Dispose();
                        currentPage = doc.AddPage();
                        currentPage.Width = XUnit.FromPoint(pageWidth);
                        currentPage.Height = XUnit.FromPoint(pageHeight);
                        gfx = XGraphics.FromPdfPage(currentPage);
                        pageNum++;
                        return margin;
                    });
                    yPos += 4;
                }
            }

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line.StartsWith("$$") && line.EndsWith("$$") && line.Length > 3)
                {
                    string fText = line.Substring(2, line.Length - 4).Trim();
                    DrawBlockFormula(fText, ref curY);
                    continue;
                }

                if (line == "$$")
                {
                    if (!inBlockMath)
                    {
                        inBlockMath = true;
                        mathLines.Clear();
                    }
                    else
                    {
                        inBlockMath = false;
                        DrawBlockFormula(string.Join(" ", mathLines), ref curY);
                    }
                    continue;
                }

                if (inBlockMath)
                {
                    mathLines.Add(line);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    curY += 5;
                    continue;
                }

                if (line == "---")
                {
                    EnsureSpace(14, ref curY);
                    curY += 4;
                    gfx.DrawLine(new XPen(XColor.FromArgb(255, 209, 213, 219), 0.8), margin, curY, pageWidth - margin, curY);
                    curY += 8;
                    continue;
                }

                string cleanLine = line.Replace("**", "").Replace("`", "");

                if (line.StartsWith("# "))
                {
                    EnsureSpace(26, ref curY);
                    curY += 6;
                    DrawWrappedParagraph(gfx, cleanLine.Substring(2), fH1, bPrimary, margin, ref curY, contentWidth, bottomLimit, null);
                    curY += 4;
                }
                else if (line.StartsWith("## "))
                {
                    EnsureSpace(22, ref curY);
                    curY += 5;
                    DrawWrappedParagraph(gfx, cleanLine.Substring(3), fH2, bAccent, margin, ref curY, contentWidth, bottomLimit, null);
                    curY += 3;
                }
                else if (line.StartsWith("### ") || line.StartsWith("#### "))
                {
                    int skip = line.StartsWith("#### ") ? 5 : 4;
                    EnsureSpace(18, ref curY);
                    curY += 4;
                    DrawWrappedParagraph(gfx, cleanLine.Substring(skip), fH3, bSubAccent, margin, ref curY, contentWidth, bottomLimit, null);
                    curY += 2;
                }
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    EnsureSpace(16, ref curY);
                    string bulletText = "• " + cleanLine.Substring(2);
                    DrawWrappedParagraph(gfx, bulletText, fBody, bPrimary, margin + 10, ref curY, contentWidth - 10, bottomLimit, null);
                    curY += 2;
                }
                else
                {
                    EnsureSpace(16, ref curY);
                    DrawWrappedParagraph(gfx, cleanLine, fBody, bPrimary, margin, ref curY, contentWidth, bottomLimit, null);
                    curY += 3;
                }
            }

            DrawPageFooter(gfx, pageNum);
            gfx.Dispose();

            string? dir = System.IO.Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            doc.Save(targetPath);
            return targetPath;
        }

        private static void DrawWrappedParagraph(
            XGraphics gfx,
            string text,
            XFont font,
            XBrush brush,
            double x,
            ref double y,
            double maxWidth,
            double bottomLimit,
            Func<double>? onNewPage)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            double lineHeight = Math.Max(font.GetHeight(), font.Size * 1.32);
            var words = text.Split(' ');
            string currentLine = string.Empty;

            foreach (var word in words)
            {
                string test = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                if (gfx.MeasureString(test, font).Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    gfx.DrawString(currentLine, font, brush, new XRect(x, y, maxWidth, lineHeight), XStringFormats.TopLeft);
                    y += lineHeight;
                    if (y + lineHeight > bottomLimit && onNewPage != null)
                    {
                        y = onNewPage();
                    }
                    currentLine = word;
                }
                else
                {
                    currentLine = test;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                gfx.DrawString(currentLine, font, brush, new XRect(x, y, maxWidth, lineHeight), XStringFormats.TopLeft);
                y += lineHeight;
            }
        }
    }
}
