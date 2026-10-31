using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class YonetimKuruluEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YonetimKuruluUyeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DernekId = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gorev = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FotografUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    GorevBaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GorevBitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    SiraNo = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YonetimKuruluUyeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YonetimKuruluUyeleri_Dernekler_DernekId",
                        column: x => x.DernekId,
                        principalTable: "Dernekler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YonetimKuruluUyeleri_DernekId",
                table: "YonetimKuruluUyeleri",
                column: "DernekId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YonetimKuruluUyeleri");
        }
    }
}
