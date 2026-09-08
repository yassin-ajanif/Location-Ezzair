using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.Reservation.Views;

public partial class SoftReservationEditView : UserControl
{
    public SoftReservationEditView()
    {
        InitializeComponent();
    }

    private void OnHeaderContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (sender is ContextMenu cm && cm.PlacementTarget is { DataContext: { } dc })
            cm.DataContext = dc;
    }

    private void OnBonSortieChipTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ViewModels.SoftReservationEditViewModel vm) return;
        e.Handled = true;
        vm.ToBonSortieCommand.Execute(null);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
