using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services.Printing;
using GestionCommerciale.Shared.ViewModels;
using GestionCommerciale.Shared.Views;

namespace GestionCommerciale.Shared.Services;

internal static class PdfPrintPreviewHost
{
    public static async Task ShowAsync(
        string pdfPath,
        string documentTitle,
        ILocaleService locale,
        CancellationToken cancellationToken = default)
    {
        await ShowCoreAsync(
            pdfPath,
            documentTitle,
            locale,
            rebuildPdf: null,
            writeTempPdf: null,
            cancellationToken);
    }

    public static async Task ShowWithFormatPickerAsync(
        Func<PrintPaperFormat, CancellationToken, Task<byte[]>> buildPdf,
        string documentTitle,
        ILocaleService locale,
        Func<byte[], string, CancellationToken, Task<string>> writeTempPdf,
        CancellationToken cancellationToken = default)
    {
        var initialBytes = await buildPdf(PrintPaperFormatExtensions.Default, cancellationToken);
        if (initialBytes.Length == 0)
            throw new InvalidOperationException("Le contenu PDF est vide.");

        var path = await writeTempPdf(initialBytes, documentTitle, cancellationToken);

        await ShowCoreAsync(
            path,
            documentTitle,
            locale,
            async (format, ct) =>
            {
                var bytes = await buildPdf(format, ct);
                if (bytes.Length == 0)
                    throw new InvalidOperationException("Le contenu PDF est vide.");
                return await writeTempPdf(bytes, documentTitle, ct);
            },
            writeTempPdf,
            cancellationToken);
    }

    private static async Task ShowCoreAsync(
        string pdfPath,
        string documentTitle,
        ILocaleService locale,
        Func<PrintPaperFormat, CancellationToken, Task<string>>? rebuildPdf,
        Func<byte[], string, CancellationToken, Task<string>>? writeTempPdf,
        CancellationToken cancellationToken)
    {
        _ = writeTempPdf;

        var owner = Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        var window = new PdfPrintPreviewWindow();
        string currentPath = pdfPath;

        var vm = new PdfPrintPreviewViewModel(
            pdfPath,
            documentTitle,
            locale,
            enablePaperFormatPicker: rebuildPdf is not null,
            rebuildForFormat: rebuildPdf is null
                ? null
                : async (format, ct) =>
                {
                    currentPath = await rebuildPdf(format, ct);
                    return currentPath;
                },
            printWithSystemDialog: async ct =>
            {
                var result = await WindowsNativePdfPrinter.PrintAsync(currentPath, documentTitle, ct);
                if (!result.Success && !result.CancelledByUser)
                    throw new InvalidOperationException(result.ErrorMessage ?? "L'impression a échoué.");
                return result.Success;
            });

        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.CloseRequested += _ =>
        {
            closed.TrySetResult();
            if (window.IsVisible)
                window.Close();
        };
        window.Closed += (_, _) =>
        {
            vm.Dispose();
            closed.TrySetResult();
        };
        window.DataContext = vm;

        if (owner != null)
            await window.ShowDialog(owner);
        else
            window.Show();

        await closed.Task.WaitAsync(cancellationToken);
    }
}
