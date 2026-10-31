using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class KurulUyeIliskisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Dernekler_DernekId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_YonetimKuruluUyeleri_Dernekler_DernekId",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.DropColumn(
                name: "Ad",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.DropColumn(
                name: "FotografUrl",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.DropColumn(
                name: "Soyad",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.AddColumn<string>(
                name: "KurulTipi",
                table: "YonetimKuruluUyeleri",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UyeId",
                table: "YonetimKuruluUyeleri",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_YonetimKuruluUyeleri_UyeId",
                table: "YonetimKuruluUyeleri",
                column: "UyeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Dernekler_DernekId",
                table: "AspNetUsers",
                column: "DernekId",
                principalTable: "Dernekler",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YonetimKuruluUyeleri_Dernekler_DernekId",
                table: "YonetimKuruluUyeleri",
                column: "DernekId",
                principalTable: "Dernekler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YonetimKuruluUyeleri_Uyeler_UyeId",
                table: "YonetimKuruluUyeleri",
                column: "UyeId",
                principalTable: "Uyeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Dernekler_DernekId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_YonetimKuruluUyeleri_Dernekler_DernekId",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.DropForeignKey(
                name: "FK_YonetimKuruluUyeleri_Uyeler_UyeId",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.DropIndex(
                name: "IX_YonetimKuruluUyeleri_UyeId",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.DropColumn(
                name: "KurulTipi",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.DropColumn(
                name: "UyeId",
                table: "YonetimKuruluUyeleri");

            migrationBuilder.AddColumn<string>(
                name: "Ad",
                table: "YonetimKuruluUyeleri",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "YonetimKuruluUyeleri",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotografUrl",
                table: "YonetimKuruluUyeleri",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Soyad",
                table: "YonetimKuruluUyeleri",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "YonetimKuruluUyeleri",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Dernekler_DernekId",
                table: "AspNetUsers",
                column: "DernekId",
                principalTable: "Dernekler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_YonetimKuruluUyeleri_Dernekler_DernekId",
                table: "YonetimKuruluUyeleri",
                column: "DernekId",
                principalTable: "Dernekler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
