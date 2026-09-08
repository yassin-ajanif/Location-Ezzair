using System.Globalization;
using Avalonia.Media;
using GestionCommerciale.Modules.Reservation.Models;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Reservation.ViewModels;

public sealed class SoftReservationListRow
{
    public required Models.Reservation Reservation { get; init; }
    public string ClientNom { get; init; } = string.Empty;
    public string DateShort { get; init; } = string.Empty;
    public string PeriodeLabel { get; init; } = string.Empty;
    public string StatutLabel { get; init; } = string.Empty;
    public IBrush StatutChipBackground { get; init; } = Brushes.Transparent;
    public IBrush StatutChipForeground { get; init; } = Brushes.Black;
    public IBrush StatutChipBorder { get; init; } = Brushes.Transparent;
    public string TtcLabel { get; init; } = string.Empty;
    public string NotePreview { get; init; } = string.Empty;

    public static SoftReservationListRow Create(Models.Reservation res, string clientNom, string devise, ILocaleService locale)
    {
        var statut = SoftReservationStatutLabels.Normalize(res.Statut);
        var (_, _, ttc) = DocumentTotalsHelper.ReservationTotals(
            res.ProduitLignes ?? [],
            res.ServiceLignes ?? [],
            res.RemiseGlobale);
        return new SoftReservationListRow
        {
            Reservation = res,
            ClientNom = clientNom,
            DateShort = res.Date.ToString("d", CultureInfo.CurrentCulture),
            PeriodeLabel = $"{res.DateDebut:dd/MM} → {res.DateFinPrevue:dd/MM}",
            StatutLabel = SoftReservationStatutLabels.Format(locale, statut),
            StatutChipBackground = SoftReservationStatutLabels.ChipBackground(statut),
            StatutChipForeground = SoftReservationStatutLabels.ChipForeground(statut),
            StatutChipBorder = SoftReservationStatutLabels.ChipBorder(statut),
            TtcLabel = $"{ttc:N2} {devise}",
            NotePreview = DocumentListFormat.NotePreview(res.Note),
        };
    }
}
