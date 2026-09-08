using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftReservationsAndRenameBonSortieTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Detach FKs that will be renamed ---
            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_BonsSortie_ReservationId",
                table: "BonsLivraison");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationProduitLignes_BonsSortie_ReservationId",
                table: "ReservationProduitLignes");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationServiceLignes_BonsSortie_ReservationId",
                table: "ReservationServiceLignes");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationProduitRetours_ReservationProduitLignes_ReservationProduitLigneId",
                table: "ReservationProduitRetours");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationServiceLignes_Services_ServiceId",
                table: "ReservationServiceLignes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReservationProduitRetours_Etat",
                table: "ReservationProduitRetours");

            // --- Rename bon-de-sortie line / retour tables (keep data) ---
            migrationBuilder.RenameTable(
                name: "ReservationProduitLignes",
                newName: "BonSortieProduitLignes");

            migrationBuilder.RenameColumn(
                name: "ReservationId",
                table: "BonSortieProduitLignes",
                newName: "BonSortieId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationProduitLignes_ReservationId",
                table: "BonSortieProduitLignes",
                newName: "IX_BonSortieProduitLignes_BonSortieId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationProduitLignes_ProduitId",
                table: "BonSortieProduitLignes",
                newName: "IX_BonSortieProduitLignes_ProduitId");

            migrationBuilder.RenameTable(
                name: "ReservationServiceLignes",
                newName: "BonSortieServiceLignes");

            migrationBuilder.RenameColumn(
                name: "ReservationId",
                table: "BonSortieServiceLignes",
                newName: "BonSortieId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationServiceLignes_ReservationId",
                table: "BonSortieServiceLignes",
                newName: "IX_BonSortieServiceLignes_BonSortieId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationServiceLignes_ServiceId",
                table: "BonSortieServiceLignes",
                newName: "IX_BonSortieServiceLignes_ServiceId");

            migrationBuilder.RenameTable(
                name: "ReservationProduitRetours",
                newName: "BonSortieProduitRetours");

            migrationBuilder.RenameColumn(
                name: "ReservationProduitLigneId",
                table: "BonSortieProduitRetours",
                newName: "BonSortieProduitLigneId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationProduitRetours_ReservationProduitLigneId",
                table: "BonSortieProduitRetours",
                newName: "IX_BonSortieProduitRetours_BonSortieProduitLigneId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationProduitRetours_DateRetour",
                table: "BonSortieProduitRetours",
                newName: "IX_BonSortieProduitRetours_DateRetour");

            migrationBuilder.RenameColumn(
                name: "ReservationId",
                table: "BonsLivraison",
                newName: "BonSortieId");

            migrationBuilder.RenameIndex(
                name: "IX_BonsLivraison_ReservationId",
                table: "BonsLivraison",
                newName: "IX_BonsLivraison_BonSortieId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BonSortieProduitRetours_Etat",
                table: "BonSortieProduitRetours",
                sql: "\"Etat\" IN ('good', 'damaged', 'lost', 'to clean')");

            // --- Re-attach FKs for bon de sortie ---
            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_BonsSortie_BonSortieId",
                table: "BonsLivraison",
                column: "BonSortieId",
                principalTable: "BonsSortie",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BonSortieProduitLignes_BonsSortie_BonSortieId",
                table: "BonSortieProduitLignes",
                column: "BonSortieId",
                principalTable: "BonsSortie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BonSortieServiceLignes_BonsSortie_BonSortieId",
                table: "BonSortieServiceLignes",
                column: "BonSortieId",
                principalTable: "BonsSortie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BonSortieServiceLignes_Services_ServiceId",
                table: "BonSortieServiceLignes",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BonSortieProduitRetours_BonSortieProduitLignes_BonSortieProduitLigneId",
                table: "BonSortieProduitRetours",
                column: "BonSortieProduitLigneId",
                principalTable: "BonSortieProduitLignes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // --- Soft-booking Reservation tables (new, empty) ---
            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateFinPrevue = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Statut = table.Column<int>(type: "INTEGER", nullable: false),
                    Caution = table.Column<decimal>(type: "TEXT", nullable: false),
                    RemiseGlobale = table.Column<decimal>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    BonSortieId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_BonsSortie_BonSortieId",
                        column: x => x.BonSortieId,
                        principalTable: "BonsSortie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReservationProduitLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReservationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: true),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationProduitLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationProduitLignes_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReservationServiceLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReservationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationServiceLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationServiceLignes_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservationServiceLignes_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "BonsSortie",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonsSortie_ReservationId",
                table: "BonsSortie",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BonSortieId",
                table: "Reservations",
                column: "BonSortieId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ClientId",
                table: "Reservations",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Numero",
                table: "Reservations",
                column: "Numero");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Statut",
                table: "Reservations",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationProduitLignes_ProduitId",
                table: "ReservationProduitLignes",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationProduitLignes_ReservationId",
                table: "ReservationProduitLignes",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationServiceLignes_ReservationId",
                table: "ReservationServiceLignes",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationServiceLignes_ServiceId",
                table: "ReservationServiceLignes",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_BonsSortie_Reservations_ReservationId",
                table: "BonsSortie",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsSortie_Reservations_ReservationId",
                table: "BonsSortie");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_BonsSortie_BonSortieId",
                table: "BonsLivraison");

            migrationBuilder.DropForeignKey(
                name: "FK_BonSortieProduitLignes_BonsSortie_BonSortieId",
                table: "BonSortieProduitLignes");

            migrationBuilder.DropForeignKey(
                name: "FK_BonSortieServiceLignes_BonsSortie_BonSortieId",
                table: "BonSortieServiceLignes");

            migrationBuilder.DropForeignKey(
                name: "FK_BonSortieServiceLignes_Services_ServiceId",
                table: "BonSortieServiceLignes");

            migrationBuilder.DropForeignKey(
                name: "FK_BonSortieProduitRetours_BonSortieProduitLignes_BonSortieProduitLigneId",
                table: "BonSortieProduitRetours");

            migrationBuilder.DropTable(name: "ReservationProduitLignes");
            migrationBuilder.DropTable(name: "ReservationServiceLignes");
            migrationBuilder.DropTable(name: "Reservations");

            migrationBuilder.DropIndex(name: "IX_BonsSortie_ReservationId", table: "BonsSortie");
            migrationBuilder.DropColumn(name: "ReservationId", table: "BonsSortie");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BonSortieProduitRetours_Etat",
                table: "BonSortieProduitRetours");

            migrationBuilder.RenameTable(name: "BonSortieProduitLignes", newName: "ReservationProduitLignes");
            migrationBuilder.RenameColumn(name: "BonSortieId", table: "ReservationProduitLignes", newName: "ReservationId");
            migrationBuilder.RenameIndex(name: "IX_BonSortieProduitLignes_BonSortieId", table: "ReservationProduitLignes", newName: "IX_ReservationProduitLignes_ReservationId");
            migrationBuilder.RenameIndex(name: "IX_BonSortieProduitLignes_ProduitId", table: "ReservationProduitLignes", newName: "IX_ReservationProduitLignes_ProduitId");

            migrationBuilder.RenameTable(name: "BonSortieServiceLignes", newName: "ReservationServiceLignes");
            migrationBuilder.RenameColumn(name: "BonSortieId", table: "ReservationServiceLignes", newName: "ReservationId");
            migrationBuilder.RenameIndex(name: "IX_BonSortieServiceLignes_BonSortieId", table: "ReservationServiceLignes", newName: "IX_ReservationServiceLignes_ReservationId");
            migrationBuilder.RenameIndex(name: "IX_BonSortieServiceLignes_ServiceId", table: "ReservationServiceLignes", newName: "IX_ReservationServiceLignes_ServiceId");

            migrationBuilder.RenameTable(name: "BonSortieProduitRetours", newName: "ReservationProduitRetours");
            migrationBuilder.RenameColumn(name: "BonSortieProduitLigneId", table: "ReservationProduitRetours", newName: "ReservationProduitLigneId");
            migrationBuilder.RenameIndex(name: "IX_BonSortieProduitRetours_BonSortieProduitLigneId", table: "ReservationProduitRetours", newName: "IX_ReservationProduitRetours_ReservationProduitLigneId");
            migrationBuilder.RenameIndex(name: "IX_BonSortieProduitRetours_DateRetour", table: "ReservationProduitRetours", newName: "IX_ReservationProduitRetours_DateRetour");

            migrationBuilder.RenameColumn(name: "BonSortieId", table: "BonsLivraison", newName: "ReservationId");
            migrationBuilder.RenameIndex(name: "IX_BonsLivraison_BonSortieId", table: "BonsLivraison", newName: "IX_BonsLivraison_ReservationId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReservationProduitRetours_Etat",
                table: "ReservationProduitRetours",
                sql: "\"Etat\" IN ('good', 'damaged', 'lost', 'to clean')");

            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_BonsSortie_ReservationId",
                table: "BonsLivraison",
                column: "ReservationId",
                principalTable: "BonsSortie",
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

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationServiceLignes_Services_ServiceId",
                table: "ReservationServiceLignes",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationProduitRetours_ReservationProduitLignes_ReservationProduitLigneId",
                table: "ReservationProduitRetours",
                column: "ReservationProduitLigneId",
                principalTable: "ReservationProduitLignes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
