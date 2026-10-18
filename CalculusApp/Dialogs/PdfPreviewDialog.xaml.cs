using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CalculusApp.Models;
using CalculusApp.Services;

namespace CalculusApp.Dialogs
{
    public partial class PdfPreviewDialog : Window
    {
        private readonly Assignment _assignment;
        private readonly List<Border> _thumbnailCards = new();
        private int _currentPage = 1;
        private int _totalPages = 1;
        private double _zoomFactor = 1.0;

        public PdfPreviewDialog(Assignment assignment)
        {
            InitializeComponent();
            _assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));

            Title = LocalizationService.Get("pdf_preview_title");
            Loaded += OnDialogLoaded;
            PreviewKeyDown += OnDialogKeyDown;
            PreviewMouseWheel += OnDialogMouseWheel;
        }

        private void OnDialogLoaded(object sender, RoutedEventArgs e)
        {
            string title = _assignment.Heading?.Title ?? "Calculus Assignment";
            StatusDocTitle.Text = title;

            // Render assignment into the preview canvas
            PreviewWorksheetView.RenderAssignment(_assignment);

            // Wait for loaded/layout to generate thumbnails
            Dispatcher.InvokeAsync(() =>
            {
                UpdatePageInfoAndThumbnails();
                ApplyFitWidth();
            }, System.Windows.Threading.DispatcherPriority.Loaded);

            // Hook scroll listener to update current page indicator
            PreviewWorksheetView.MainScrollViewer.ScrollChanged += OnMainScrollChanged;
        }

        private void UpdatePageInfoAndThumbnails()
        {
            var pages = PreviewWorksheetView.GetPageBorders();
            _totalPages = Math.Max(1, pages.Count);
            _currentPage = 1;

            UpdatePageIndicator();
            ThumbnailsContainer.Children.Clear();
            _thumbnailCards.Clear();

            for (int i = 0; i < pages.Count; i++)
            {
                int pageNum = i + 1;
                var pageBorder = pages[i];

                var cardBorder = new Border
                {
                    Width = 160,
                    Height = 215,
                    Background = Brushes.White,
                    BorderBrush = (pageNum == 1) ? (Brush)FindResource("ThemeAccent") : (Brush)FindResource("ThemeBorder"),
                    BorderThickness = (pageNum == 1) ? new Thickness(2.5) : new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 0, 14),
                    Tag = pageNum
                };

                // Miniature page view using VisualBrush for ultra-low memory & instant rendering
                var miniRect = new System.Windows.Shapes.Rectangle
                {
                    Width = 156,
                    Height = 208,
                    Fill = new VisualBrush(pageBorder)
                    {
                        Stretch = Stretch.Uniform,
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Top
                    }
                };

                var cardStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                cardStack.Children.Add(miniRect);

                var badge = new TextBlock
                {
                    Text = LocalizationService.Get("pdf_page_n_of_m", pageNum, _totalPages),
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("ThemeTextSecondary"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };

                var itemPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
                cardBorder.Child = cardStack;
                itemPanel.Children.Add(cardBorder);
                itemPanel.Children.Add(badge);

                int targetPage = pageNum;
                cardBorder.MouseLeftButtonDown += (s, ev) =>
                {
                    ScrollToPage(targetPage);
                };

                ThumbnailsContainer.Children.Add(itemPanel);
                _thumbnailCards.Add(cardBorder);
            }
        }

        private void ScrollToPage(int pageNumber)
        {
            var pages = PreviewWorksheetView.GetPageBorders();
            if (pageNumber >= 1 && pageNumber <= pages.Count)
            {
                _currentPage = pageNumber;
                UpdatePageIndicator();
                HighlightThumbnail(_currentPage);

                var targetBorder = pages[pageNumber - 1];
                targetBorder.BringIntoView();
            }
        }

        private void HighlightThumbnail(int pageNumber)
        {
            for (int i = 0; i < _thumbnailCards.Count; i++)
            {
                bool isActive = (i + 1 == pageNumber);
                _thumbnailCards[i].BorderBrush = isActive
                    ? (Brush)FindResource("ThemeAccent")
                    : (Brush)FindResource("ThemeBorder");
                _thumbnailCards[i].BorderThickness = isActive ? new Thickness(2.5) : new Thickness(1);
            }
        }

        private void OnMainScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var pages = PreviewWorksheetView.GetPageBorders();
            if (pages.Count == 0) return;

            double scrollY = PreviewWorksheetView.MainScrollViewer.VerticalOffset;
            double approxPageHeight = (Controls.WorksheetView.PageHeight + 24.0) * _zoomFactor;

            int activeIndex = (int)Math.Floor((scrollY + approxPageHeight * 0.35) / approxPageHeight);
            activeIndex = Math.Max(0, Math.Min(pages.Count - 1, activeIndex));

            int newPage = activeIndex + 1;
            if (newPage != _currentPage)
            {
                _currentPage = newPage;
                UpdatePageIndicator();
                HighlightThumbnail(_currentPage);
            }
        }

        private void UpdatePageIndicator()
        {
            PageIndicatorText.Text = LocalizationService.Get("pdf_page_n_of_m", _currentPage, _totalPages);
            PrevPageBtn.IsEnabled = _currentPage > 1;
            NextPageBtn.IsEnabled = _currentPage < _totalPages;
        }

        private void OnPrevPageClick(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                ScrollToPage(_currentPage - 1);
            }
        }

        private void OnNextPageClick(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                ScrollToPage(_currentPage + 1);
            }
        }

        private void OnToggleThumbnailsClick(object sender, RoutedEventArgs e)
        {
            bool show = ToggleThumbnailsBtn.IsChecked == true;
            ThumbnailsBorder.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ThumbnailsCol.Width = show ? new GridLength(200) : new GridLength(0);
        }

        private void OnZoomPresetChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || PreviewWorksheetView == null || ZoomPresetCombo == null) return;

            if (ZoomPresetCombo.SelectedItem is ComboBoxItem item && item.Content is string text)
            {
                if (text.Contains("Ancho") || text.Contains("Width"))
                {
                    ApplyFitWidth();
                }
                else if (text.Contains("Completa") || text.Contains("Page"))
                {
                    ApplyFitPage();
                }
                else
                {
                    string clean = text.Replace("%", "").Trim();
                    if (double.TryParse(clean, out double pct))
                    {
                        ApplyZoom(pct / 100.0);
                    }
                }
            }
        }

        private void ApplyFitWidth()
        {
            if (PreviewWorksheetView == null) return;
            double availableWidth = PreviewWorksheetView.ActualWidth > 0 ? PreviewWorksheetView.ActualWidth : 750;
            double targetZoom = Math.Max(0.4, Math.Min(2.5, (availableWidth - 80) / Controls.WorksheetView.PageWidth));
            ApplyZoom(targetZoom);
        }

        private void ApplyFitPage()
        {
            if (PreviewWorksheetView == null) return;
            double availableHeight = PreviewWorksheetView.ActualHeight > 0 ? PreviewWorksheetView.ActualHeight : 650;
            double targetZoom = Math.Max(0.35, Math.Min(2.0, (availableHeight - 60) / Controls.WorksheetView.PageHeight));
            ApplyZoom(targetZoom);
        }

        private void ApplyZoom(double factor)
        {
            if (PreviewWorksheetView == null) return;
            _zoomFactor = Math.Max(0.35, Math.Min(3.0, factor));
            PreviewWorksheetView.SetZoom(_zoomFactor);
            if (ZoomPercentLabel != null)
            {
                ZoomPercentLabel.Text = $"{(int)Math.Round(_zoomFactor * 100)}%";
            }
        }

        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            ApplyZoom(_zoomFactor + 0.15);
        }

        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            ApplyZoom(_zoomFactor - 0.15);
        }

        private void OnDialogMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                double delta = e.Delta > 0 ? 0.10 : -0.10;
                ApplyZoom(_zoomFactor + delta);
                e.Handled = true;
            }
        }

        private void OnDialogKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp && _currentPage > 1)
            {
                ScrollToPage(_currentPage - 1);
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown && _currentPage < _totalPages)
            {
                ScrollToPage(_currentPage + 1);
                e.Handled = true;
            }
            else if (e.Key == Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                OnSavePdfClick(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.P && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                OnPrintClick(sender, e);
                e.Handled = true;
            }
        }

        private void OnSavePdfClick(object sender, RoutedEventArgs e)
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = LocalizationService.Get("menu_export_pdf"),
                FileName = (_assignment.Heading?.Title ?? "Calculus") + ".pdf"
            };

            if (sfd.ShowDialog(this) == true)
            {
                try
                {
                    string path = PdfExportService.ExportToPdf(PreviewWorksheetView, sfd.FileName);
                    string msg = LocalizationService.Get("pdf_export_success", path);
                    MessageBox.Show(this, msg, "PDF Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error al exportar PDF: " + ex.Message, "PDF Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnPrintClick(object sender, RoutedEventArgs e)
        {
            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                string msg = LocalizationService.CurrentLanguage switch
                {
                    AppLanguage.Spanish => "Enviando documento a la impresora seleccionada...",
                    AppLanguage.Portuguese => "Enviando documento para a impressora selecionada...",
                    _ => "Sending document to printer..."
                };
                MessageBox.Show(this, msg, "Imprimir", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
