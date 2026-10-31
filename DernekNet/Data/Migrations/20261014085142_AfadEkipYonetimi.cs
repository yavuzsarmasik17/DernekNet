using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class AfadEkipYonetimi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AfadEkipler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DernekId = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadEkipler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadEkipler_Dernekler_DernekId",
                        column: x => x.DernekId,
                        principalTable: "Dernekler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadEkipUyeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AfadEkipId = table.Column<int>(type: "int", nullable: false),
                    AfadGonulluId = table.Column<int>(type: "int", nullable: false),
                    EkipLideriMi = table.Column<bool>(type: "bit", nullable: false),
                    KatilimTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadEkipUyeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadEkipUyeleri_AfadEkipler_AfadEkipId",
                        column: x => x.AfadEkipId,
                        principalTable: "AfadEkipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AfadEkipUyeleri_AfadGonulluler_AfadGonulluId",
                        column: x => x.AfadGonulluId,
                        principalTable: "AfadGonulluler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AfadEkipler_DernekId_Ad",
                table: "AfadEkipler",
                columns: new[] { "DernekId", "Ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AfadEkipUyeleri_AfadEkipId",
                table: "AfadEkipUyeleri",
                column: "AfadEkipId",
                unique: true,
                filter: "[EkipLideriMi] = 1 AND [AktifMi] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AfadEkipUyeleri_AfadEkipId_AfadGonulluId",
                table: "AfadEkipUyeleri",
                columns: new[] { "AfadEkipId", "AfadGonulluId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AfadEkipUyeleri_AfadGonulluId",
                table: "AfadEkipUyeleri",
                column: "AfadGonulluId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AfadEkipUyeleri");

            migrationBuilder.DropTable(
                name: "AfadEkipler");
        }
    }
}
