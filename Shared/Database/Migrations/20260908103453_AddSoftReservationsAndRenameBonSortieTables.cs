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
            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_BonsSortie_ReservationId",
                table: "BonsLivraison");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationProduitLignes_BonsSortie_ReservationId",
                table: "ReservationProduitLignes");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationServiceLignes_BonsSortie_ReservationId",
                table: "ReservationServiceLignes");

            migrationBuilder.DropTable(
                name: "ReservationProduitRetours");

            migrationBuilder.DropColumn(
                name: "QuantiteRetournee",
                table: "ReservationProduitLignes");

            migrationBuilder.RenameColumn(
                name: "ReservationId",
                table: "BonsLivraison",
                newName: "BonSortieId");

            migrationBuilder.RenameIndex(
                name: "IX_BonsLivraison_ReservationId",
                table: "BonsLivraison",
                newName: "IX_BonsLivraison_BonSortieId");

            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "BonsSortie",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BonSortieProduitLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonSortieId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: true),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    QuantiteRetournee = table.Column<decimal>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_BonSortieProduitLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonSortieProduitLignes_BonsSortie_BonSortieId",
                        column: x => x.BonSortieId,
                        principalTable: "BonsSortie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BonSortieServiceLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonSortieId = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_BonSortieServiceLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonSortieServiceLignes_BonsSortie_BonSortieId",
                        column: x => x.BonSortieId,
                        principalTable: "BonsSortie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BonSortieServiceLignes_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "BonSortieProduitRetours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonSortieProduitLigneId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateRetour = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    Etat = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonSortieProduitRetours", x => x.Id);
                    table.CheckConstraint("CK_BonSortieProduitRetours_Etat", "\"Etat\" IN ('good', 'damaged', 'lost', 'to clean')");
                    table.ForeignKey(
                        name: "FK_BonSortieProduitRetours_BonSortieProduitLignes_BonSortieProduitLigneId",
                        column: x => x.BonSortieProduitLigneId,
                        principalTable: "BonSortieProduitLignes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonsSortie_ReservationId",
                table: "BonsSortie",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_BonSortieProduitLignes_BonSortieId",
                table: "BonSortieProduitLignes",
                column: "BonSortieId");

            migrationBuilder.CreateIndex(
                name: "IX_BonSortieProduitLignes_ProduitId",
                table: "BonSortieProduitLignes",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_BonSortieProduitRetours_BonSortieProduitLigneId",
                table: "BonSortieProduitRetours",
                column: "BonSortieProduitLigneId");

            migrationBuilder.CreateIndex(
                name: "IX_BonSortieProduitRetours_DateRetour",
                table: "BonSortieProduitRetours",
                column: "DateRetour");

            migrationBuilder.CreateIndex(
                name: "IX_BonSortieServiceLignes_BonSortieId",
                table: "BonSortieServiceLignes",
                column: "BonSortieId");

            migrationBuilder.CreateIndex(
                name: "IX_BonSortieServiceLignes_ServiceId",
                table: "BonSortieServiceLignes",
                column: "ServiceId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_BonsSortie_BonSortieId",
                table: "BonsLivraison",
                column: "BonSortieId",
                principalTable: "BonsSortie",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsSortie_Reservations_ReservationId",
                table: "BonsSortie",
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
                name: "FK_ReservationServiceLignes_Reservations_ReservationId",
                table: "ReservationServiceLignes",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_BonsSortie_BonSortieId",
                table: "BonsLivraison");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsSortie_Reservations_ReservationId",
                table: "BonsSortie");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationProduitLignes_Reservations_ReservationId",
                table: "ReservationProduitLignes");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationServiceLignes_Reservations_ReservationId",
                table: "ReservationServiceLignes");

            migrationBuilder.DropTable(
                name: "BonSortieProduitRetours");

            migrationBuilder.DropTable(
                name: "BonSortieServiceLignes");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "BonSortieProduitLignes");

            migrationBuilder.DropIndex(
                name: "IX_BonsSortie_ReservationId",
                table: "BonsSortie");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "BonsSortie");

            migrationBuilder.RenameColumn(
                name: "BonSortieId",
                table: "BonsLivraison",
                newName: "ReservationId");

            migrationBuilder.RenameIndex(
                name: "IX_BonsLivraison_BonSortieId",
                table: "BonsLivraison",
                newName: "IX_BonsLivraison_ReservationId");

            migrationBuilder.AddColumn<decimal>(
                name: "QuantiteRetournee",
                table: "ReservationProduitLignes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ReservationProduitRetours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReservationProduitLigneId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    DateRetour = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Etat = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationProduitRetours", x => x.Id);
                    table.CheckConstraint("CK_ReservationProduitRetours_Etat", "\"Etat\" IN ('good', 'damaged', 'lost', 'to clean')");
                    table.ForeignKey(
                        name: "FK_ReservationProduitRetours_ReservationProduitLignes_ReservationProduitLigneId",
                        column: x => x.ReservationProduitLigneId,
                        principalTable: "ReservationProduitLignes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationProduitRetours_DateRetour",
                table: "ReservationProduitRetours",
                column: "DateRetour");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationProduitRetours_ReservationProduitLigneId",
                table: "ReservationProduitRetours",
                column: "ReservationProduitLigneId");

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
        }
    }
}
