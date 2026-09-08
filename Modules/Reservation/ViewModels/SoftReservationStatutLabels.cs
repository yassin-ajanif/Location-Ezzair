using Avalonia.Media;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public static class SoftReservationStatutLabels
{
    public static string Format(ILocaleService locale, StatutReservation s) =>
        locale.T(s switch
        {
            StatutReservation.Transformee => "SoftRes_Statut_Transformee",
            _ => "SoftRes_Statut_Confirmee"
        });

    public static StatutReservation Normalize(StatutReservation stored) =>
        stored is StatutReservation.Confirmee or StatutReservation.Transformee
            ? stored
            : StatutReservation.Confirmee;

    public static IBrush ChipBackground(StatutReservation s) => s switch
    {
        StatutReservation.Transformee => Brush.Parse("#DBEAFE"),
        _ => Brush.Parse("#DCFCE7")
    };

    public static IBrush ChipForeground(StatutReservation s) => s switch
    {
        StatutReservation.Transformee => Brush.Parse("#1E40AF"),
        _ => Brush.Parse("#166534")
    };

    public static IBrush ChipBorder(StatutReservation s) => s switch
    {
        StatutReservation.Transformee => Brush.Parse("#93C5FD"),
        _ => Brush.Parse("#86EFAC")
    };
}
