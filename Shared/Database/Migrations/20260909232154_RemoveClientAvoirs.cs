using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClientAvoirs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM MouvementsStock WHERE OrigineType = 'Avoir'");

            migrationBuilder.DropTable(
                name: "AvoirLignes");

            migrationBuilder.DropTable(
                name: "Avoirs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Avoirs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FactureId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Motif = table.Column<string>(type: "TEXT", nullable: false),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    RetourMarchandise = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avoirs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Avoirs_Factures_FactureId",
                        column: x => x.FactureId,
                        principalTable: "Factures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AvoirLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvoirId = table.Column<int>(type: "INTEGER", nullable: false),
                    Conditionnement = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: true),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvoirLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvoirLignes_Avoirs_AvoirId",
                        column: x => x.AvoirId,
                        principalTable: "Avoirs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvoirLignes_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvoirLignes_AvoirId",
                table: "AvoirLignes",
                column: "AvoirId");

            migrationBuilder.CreateIndex(
                name: "IX_AvoirLignes_ServiceId",
                table: "AvoirLignes",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Avoirs_FactureId",
                table: "Avoirs",
                column: "FactureId");
        }
    }
}
