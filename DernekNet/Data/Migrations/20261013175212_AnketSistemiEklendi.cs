using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class AnketSistemiEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anketler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Soru = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    DernekId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anketler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anketler_Dernekler_DernekId",
                        column: x => x.DernekId,
                        principalTable: "Dernekler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnketSecenekleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnketId = table.Column<int>(type: "int", nullable: false),
                    SecenekMetni = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiraNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnketSecenekleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnketSecenekleri_Anketler_AnketId",
                        column: x => x.AnketId,
                        principalTable: "Anketler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnketOylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnketId = table.Column<int>(type: "int", nullable: false),
                    AnketSecenekId = table.Column<int>(type: "int", nullable: false),
                    ZiyaretciAnahtariHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OyTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnketOylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnketOylari_AnketSecenekleri_AnketSecenekId",
                        column: x => x.AnketSecenekId,
                        principalTable: "AnketSecenekleri",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnketOylari_Anketler_AnketId",
                        column: x => x.AnketId,
                        principalTable: "Anketler",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anketler_DernekId",
                table: "Anketler",
                column: "DernekId");

            migrationBuilder.CreateIndex(
                name: "IX_AnketOylari_AnketId_ZiyaretciAnahtariHash",
                table: "AnketOylari",
                columns: new[] { "AnketId", "ZiyaretciAnahtariHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnketOylari_AnketSecenekId",
                table: "AnketOylari",
                column: "AnketSecenekId");

            migrationBuilder.CreateIndex(
                name: "IX_AnketSecenekleri_AnketId",
                table: "AnketSecenekleri",
                column: "AnketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnketOylari");

            migrationBuilder.DropTable(
                name: "AnketSecenekleri");

            migrationBuilder.DropTable(
                name: "Anketler");
        }
    }
}
