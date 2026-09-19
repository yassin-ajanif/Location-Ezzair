using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRentedByDayAndLineDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Days",
                table: "ReservationProduitLignes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RentedByDay",
                table: "ReservationProduitLignes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RentedByDay",
                table: "Produits",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Days",
                table: "FactureLignes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RentedByDay",
                table: "FactureLignes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Days",
                table: "BonSortieProduitLignes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RentedByDay",
                table: "BonSortieProduitLignes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Days",
                table: "ReservationProduitLignes");

            migrationBuilder.DropColumn(
                name: "RentedByDay",
                table: "ReservationProduitLignes");

            migrationBuilder.DropColumn(
                name: "RentedByDay",
                table: "Produits");

            migrationBuilder.DropColumn(
                name: "Days",
                table: "FactureLignes");

            migrationBuilder.DropColumn(
                name: "RentedByDay",
                table: "FactureLignes");

            migrationBuilder.DropColumn(
                name: "Days",
                table: "BonSortieProduitLignes");

            migrationBuilder.DropColumn(
                name: "RentedByDay",
                table: "BonSortieProduitLignes");
        }
    }
}
