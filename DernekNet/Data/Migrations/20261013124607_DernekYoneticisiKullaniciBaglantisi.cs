using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class DernekYoneticisiKullaniciBaglantisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DernekId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DernekId",
                table: "AspNetUsers",
                column: "DernekId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Dernekler_DernekId",
                table: "AspNetUsers",
                column: "DernekId",
                principalTable: "Dernekler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Dernekler_DernekId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DernekId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DernekId",
                table: "AspNetUsers");
        }
    }
}
