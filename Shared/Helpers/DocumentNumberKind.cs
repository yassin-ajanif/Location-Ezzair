namespace GestionCommerciale.Shared.Helpers;

public static class DocumentNumberKind
{
    public sealed record Entry(string Prefix, string LabelKey);

    public static readonly Entry[] All =
    [
        new("FAC", "Nav_Factures"),
        new("BC", "Nav_BC"),
        new("BR", "Nav_BR"),
        new("FAF", "Nav_FacturesFournisseur"),
        new("AVF", "Nav_AvoirFournisseur"),
        new("BS", "Nav_Location"),
        new("RES", "Nav_Reservation"),
    ];
}
