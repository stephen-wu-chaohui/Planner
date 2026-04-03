using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDepotForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_DepotEndId",
                table: "Vehicles",
                column: "DepotEndId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_DepotStartId",
                table: "Vehicles",
                column: "DepotStartId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Depots_DepotEndId",
                table: "Vehicles",
                column: "DepotEndId",
                principalTable: "Depots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Depots_DepotStartId",
                table: "Vehicles",
                column: "DepotStartId",
                principalTable: "Depots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Depots_DepotEndId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Depots_DepotStartId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_DepotEndId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_DepotStartId",
                table: "Vehicles");
        }
    }
}
