using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class SoftResBsOneToOneRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsSortie_Reservations_ReservationId",
                table: "BonsSortie");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_BonsSortie_BonSortieId",
                table: "Reservations");

            // Keep the 1:1 link on BS before dropping Reservations.BonSortieId.
            migrationBuilder.Sql(
                """
                UPDATE "BonsSortie"
                SET "ReservationId" = (
                    SELECT r."Id"
                    FROM "Reservations" r
                    WHERE r."BonSortieId" = "BonsSortie"."Id"
                    LIMIT 1
                )
                WHERE "ReservationId" IS NULL
                  AND EXISTS (
                    SELECT 1 FROM "Reservations" r2
                    WHERE r2."BonSortieId" = "BonsSortie"."Id"
                  );
                """);

            migrationBuilder.DropIndex(
                name: "IX_Reservations_BonSortieId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_BonsSortie_ReservationId",
                table: "BonsSortie");

            migrationBuilder.DropColumn(
                name: "BonSortieId",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_BonsSortie_ReservationId",
                table: "BonsSortie",
                column: "ReservationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsSortie_Reservations_ReservationId",
                table: "BonsSortie",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsSortie_Reservations_ReservationId",
                table: "BonsSortie");

            migrationBuilder.DropIndex(
                name: "IX_BonsSortie_ReservationId",
                table: "BonsSortie");

            migrationBuilder.AddColumn<int>(
                name: "BonSortieId",
                table: "Reservations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BonSortieId",
                table: "Reservations",
                column: "BonSortieId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsSortie_ReservationId",
                table: "BonsSortie",
                column: "ReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_BonsSortie_Reservations_ReservationId",
                table: "BonsSortie",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_BonsSortie_BonSortieId",
                table: "Reservations",
                column: "BonSortieId",
                principalTable: "BonsSortie",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
