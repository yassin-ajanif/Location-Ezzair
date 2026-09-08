using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameReservationsToBonsSortie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_Reservations_ReservationId",
                table: "BonsLivraison");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationProduitLignes_Reservations_ReservationId",
                table: "ReservationProduitLignes");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_BonsLivraison_BonLivraisonId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Factures_FactureId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationServiceLignes_Reservations_ReservationId",
                table: "ReservationServiceLignes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reservations",
                table: "Reservations");

            migrationBuilder.RenameTable(
                name: "Reservations",
                newName: "BonsSortie");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_Numero",
                table: "BonsSortie",
                newName: "IX_BonsSortie_Numero");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_FactureId",
                table: "BonsSortie",
                newName: "IX_BonsSortie_FactureId");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_ClientId",
                table: "BonsSortie",
                newName: "IX_BonsSortie_ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_BonLivraisonId",
                table: "BonsSortie",
                newName: "IX_BonsSortie_BonLivraisonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BonsSortie",
                table: "BonsSortie",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_BonsSortie_ReservationId",
                table: "BonsLivraison",
                column: "ReservationId",
                principalTable: "BonsSortie",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsSortie_BonsLivraison_BonLivraisonId",
                table: "BonsSortie",
                column: "BonLivraisonId",
                principalTable: "BonsLivraison",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsSortie_Factures_FactureId",
                table: "BonsSortie",
                column: "FactureId",
                principalTable: "Factures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationProduitLignes_BonsSortie_ReservationId",
                table: "ReservationProduitLignes",
                column: "ReservationId",
                principalTable: "BonsSortie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationServiceLignes_BonsSortie_ReservationId",
                table: "ReservationServiceLignes",
                column: "ReservationId",
                principalTable: "BonsSortie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_BonsSortie_ReservationId",
                table: "BonsLivraison");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsSortie_BonsLivraison_BonLivraisonId",
                table: "BonsSortie");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsSortie_Factures_FactureId",
                table: "BonsSortie");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationProduitLignes_BonsSortie_ReservationId",
                table: "ReservationProduitLignes");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationServiceLignes_BonsSortie_ReservationId",
                table: "ReservationServiceLignes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BonsSortie",
                table: "BonsSortie");

            migrationBuilder.RenameTable(
                name: "BonsSortie",
                newName: "Reservations");

            migrationBuilder.RenameIndex(
                name: "IX_BonsSortie_Numero",
                table: "Reservations",
                newName: "IX_Reservations_Numero");

            migrationBuilder.RenameIndex(
                name: "IX_BonsSortie_FactureId",
                table: "Reservations",
                newName: "IX_Reservations_FactureId");

            migrationBuilder.RenameIndex(
                name: "IX_BonsSortie_ClientId",
                table: "Reservations",
                newName: "IX_Reservations_ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_BonsSortie_BonLivraisonId",
                table: "Reservations",
                newName: "IX_Reservations_BonLivraisonId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reservations",
                table: "Reservations",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_Reservations_ReservationId",
                table: "BonsLivraison",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationProduitLignes_Reservations_ReservationId",
                table: "ReservationProduitLignes",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_BonsLivraison_BonLivraisonId",
                table: "Reservations",
                column: "BonLivraisonId",
                principalTable: "BonsLivraison",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Factures_FactureId",
                table: "Reservations",
                column: "FactureId",
                principalTable: "Factures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationServiceLignes_Reservations_ReservationId",
                table: "ReservationServiceLignes",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
