using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services;
using PdfiumViewer;
using DrawingImage = System.Drawing.Image;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;

namespace GestionCommerciale.Shared.ViewModels;

public sealed class PaperFormatOption
{
    public required PrintPaperFormat Format { get; init; }
    public required string Label { get; init; }
}

public sealed partial class PdfPrintPreviewViewModel : ObservableObject, IDisposable
{
    private PdfDocument _document;
    private readonly ILocaleService _locale;
    private readonly Func<CancellationToken, Task<bool>> _printWithSystemDialog;
    private readonly Func<PrintPaperFormat, CancellationToken, Task<string>>? _rebuildForFormat;
    private int _reloadGeneration;
    private bool _paperFormatReady;

    public event Action<bool>? CloseRequested;

    public PdfPrintPreviewViewModel(
        string pdfPath,
        string documentTitle,
        ILocaleService locale,
        bool enablePaperFormatPicker,
        Func<PrintPaperFormat, CancellationToken, Task<string>>? rebuildForFormat,
        Func<CancellationToken, Task<bool>> printWithSystemDialog)
    {
        _document = PdfDocument.Load(pdfPath);
        _locale = locale;
        _rebuildForFormat = rebuildForFormat;
        _printWithSystemDialog = printWithSystemDialog;
        DocumentTitle = documentTitle;
        ShowPaperFormatPicker = enablePaperFormatPicker;
        RefreshLocalizedLabels();
        if (ShowPaperFormatPicker)
        {
            PaperFormats =
            [
                new PaperFormatOption { Format = PrintPaperFormat.A4, Label = _locale.T("PrintPreview_FormatA4") },
                new PaperFormatOption { Format = PrintPaperFormat.Ticket80mm, Label = _locale.T("PrintPreview_FormatTicket80") },
                new PaperFormatOption { Format = PrintPaperFormat.Ticket58mm, Label = _locale.T("PrintPreview_FormatTicket58") }
            ];
            SelectedPaperFormat = PaperFormats.First(f => f.Format == PrintPaperFormatExtensions.Default);
            PreviewMaxWidth = 300;
        }

        _paperFormatReady = ShowPaperFormatPicker;
        _ = LoadPreviewPagesAsync();
    }

    public ObservableCollection<Bitmap> PreviewPages { get; } = [];
    public IReadOnlyList<PaperFormatOption> PaperFormats { get; private set; } = Array.Empty<PaperFormatOption>();

    [ObservableProperty] private string _documentTitle = string.Empty;
    [ObservableProperty] private string _titleLabel = string.Empty;
    [ObservableProperty] private string _btnCancel = string.Empty;
    [ObservableProperty] private string _btnPrint = string.Empty;
    [ObservableProperty] private string _btnZoomOut = string.Empty;
    [ObservableProperty] private string _btnZoomIn = string.Empty;
    [ObservableProperty] private string _paperSizeLabel = string.Empty;
    [ObservableProperty] private bool _showPaperFormatPicker;
    [ObservableProperty] private PaperFormatOption? _selectedPaperFormat;
    [ObservableProperty] private bool _isLoadingPreview = true;
    [ObservableProperty] private bool _isPrinting;
    [ObservableProperty] private bool _isRebuilding;
    [ObservableProperty] private double _zoomScale = 1.0;
    [ObservableProperty] private double _previewMaxWidth = 680;

    public string ZoomLabel => $"{(int)Math.Round(ZoomScale * 100)} %";
    public bool CanChangeFormat => ShowPaperFormatPicker && !IsRebuilding && !IsPrinting;

    partial void OnZoomScaleChanged(double value) => OnPropertyChanged(nameof(ZoomLabel));
    partial void OnIsRebuildingChanged(bool value) => OnPropertyChanged(nameof(CanChangeFormat));
    partial void OnIsPrintingChanged(bool value) => OnPropertyChanged(nameof(CanChangeFormat));

    partial void OnSelectedPaperFormatChanged(PaperFormatOption? value)
    {
        if (!_paperFormatReady || value is null || _rebuildForFormat is null)
            return;

        PreviewMaxWidth = value.Format switch
        {
            PrintPaperFormat.Ticket58mm => 220,
            PrintPaperFormat.Ticket80mm => 300,
            _ => 680
        };

        _ = RebuildForFormatAsync(value.Format);
    }

    private void RefreshLocalizedLabels()
    {
        TitleLabel = _locale.T("PrintPreview_Title");
        BtnCancel = _locale.T("Btn_Cancel");
        BtnPrint = _locale.T("Btn_Print");
        BtnZoomOut = _locale.T("PrintPreview_ZoomOut");
        BtnZoomIn = _locale.T("PrintPreview_ZoomIn");
        PaperSizeLabel = _locale.T("PrintPreview_PaperSize");
    }

    private async Task RebuildForFormatAsync(PrintPaperFormat format)
    {
        if (_rebuildForFormat is null)
            return;

        var generation = ++_reloadGeneration;
        IsRebuilding = true;
        IsLoadingPreview = true;
        try
        {
            var path = await _rebuildForFormat(format, CancellationToken.None);
            if (generation != _reloadGeneration)
                return;

            _document.Dispose();
            _document = PdfDocument.Load(path);
            await LoadPreviewPagesAsync(generation);
        }
        finally
        {
            if (generation == _reloadGeneration)
                IsRebuilding = false;
        }
    }

    private async Task LoadPreviewPagesAsync(int? expectedGeneration = null)
    {
        IsLoadingPreview = true;
        try
        {
            var pages = await Task.Run(RenderAllPages);
            if (expectedGeneration is int g && g != _reloadGeneration)
            {
                foreach (var page in pages)
                    page.Dispose();
                return;
            }

            foreach (var old in PreviewPages)
                old.Dispose();
            PreviewPages.Clear();
            foreach (var page in pages)
                PreviewPages.Add(page);
        }
        finally
        {
            IsLoadingPreview = false;
        }
    }

    private IReadOnlyList<Bitmap> RenderAllPages()
    {
        const float dpi = 120f;
        var result = new List<Bitmap>(_document.PageCount);

        for (var i = 0; i < _document.PageCount; i++)
        {
            var size = _document.PageSizes[i];
            var width = Math.Max(1, (int)(size.Width / 72f * dpi));
            var height = Math.Max(1, (int)(size.Height / 72f * dpi));
            using DrawingImage rendered = _document.Render(i, width, height, dpi, dpi, false);
            result.Add(ToAvaloniaBitmap(rendered));
        }

        return result;
    }

    private static Bitmap ToAvaloniaBitmap(DrawingImage image)
    {
        using var ms = new MemoryStream();
        image.Save(ms, DrawingImageFormat.Png);
        ms.Position = 0;
        return new Bitmap(ms);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    [RelayCommand]
    private void ZoomIn() =>
        ZoomScale = Math.Min(2.5, Math.Round((ZoomScale + 0.15) * 100) / 100);

    [RelayCommand]
    private void ZoomOut() =>
        ZoomScale = Math.Max(0.35, Math.Round((ZoomScale - 0.15) * 100) / 100);

    [RelayCommand]
    private void ZoomReset() => ZoomScale = 1.0;

    [RelayCommand]
    private async Task PrintAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsPrinting = true;
            if (await _printWithSystemDialog(cancellationToken))
                CloseRequested?.Invoke(true);
        }
        finally
        {
            IsPrinting = false;
        }
    }

    public void Dispose()
    {
        _document.Dispose();
        foreach (var page in PreviewPages)
            page.Dispose();
        PreviewPages.Clear();
    }
}
