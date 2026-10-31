using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DernekNet.Data.Migrations
{
    /// <inheritdoc />
    public partial class AfadModuluIlkKurulum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AfadDuyurular",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DernekId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SadeceGonullulereMi = table.Column<bool>(type: "bit", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    YayinTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadDuyurular", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadDuyurular_Dernekler_DernekId",
                        column: x => x.DernekId,
                        principalTable: "Dernekler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadEgitimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DernekId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadEgitimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadEgitimler_Dernekler_DernekId",
                        column: x => x.DernekId,
                        principalTable: "Dernekler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadGonulluler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DernekId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AdSoyad = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OnaylandiMi = table.Column<bool>(type: "bit", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadGonulluler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadGonulluler_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AfadGonulluler_Dernekler_DernekId",
                        column: x => x.DernekId,
                        principalTable: "Dernekler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadGorevler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DernekId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadGorevler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadGorevler_Dernekler_DernekId",
                        column: x => x.DernekId,
                        principalTable: "Dernekler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadHaberler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DernekId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YayinTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadHaberler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadHaberler_Dernekler_DernekId",
                        column: x => x.DernekId,
                        principalTable: "Dernekler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadTatbikatlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DernekId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadTatbikatlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadTatbikatlar_Dernekler_DernekId",
                        column: x => x.DernekId,
                        principalTable: "Dernekler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadYetkinlikler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DernekId = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadYetkinlikler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadYetkinlikler_Dernekler_DernekId",
                        column: x => x.DernekId,
                        principalTable: "Dernekler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadEgitimKatilimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AfadEgitimId = table.Column<int>(type: "int", nullable: false),
                    AfadGonulluId = table.Column<int>(type: "int", nullable: false),
                    KatildiMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadEgitimKatilimlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadEgitimKatilimlari_AfadEgitimler_AfadEgitimId",
                        column: x => x.AfadEgitimId,
                        principalTable: "AfadEgitimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AfadEgitimKatilimlari_AfadGonulluler_AfadGonulluId",
                        column: x => x.AfadGonulluId,
                        principalTable: "AfadGonulluler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadGorevAtamalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AfadGorevId = table.Column<int>(type: "int", nullable: false),
                    AfadGonulluId = table.Column<int>(type: "int", nullable: false),
                    AtamaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Durum = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadGorevAtamalari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadGorevAtamalari_AfadGonulluler_AfadGonulluId",
                        column: x => x.AfadGonulluId,
                        principalTable: "AfadGonulluler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AfadGorevAtamalari_AfadGorevler_AfadGorevId",
                        column: x => x.AfadGorevId,
                        principalTable: "AfadGorevler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadTatbikatKatilimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AfadTatbikatId = table.Column<int>(type: "int", nullable: false),
                    AfadGonulluId = table.Column<int>(type: "int", nullable: false),
                    Gorevi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    KatildiMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadTatbikatKatilimlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadTatbikatKatilimlari_AfadGonulluler_AfadGonulluId",
                        column: x => x.AfadGonulluId,
                        principalTable: "AfadGonulluler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AfadTatbikatKatilimlari_AfadTatbikatlar_AfadTatbikatId",
                        column: x => x.AfadTatbikatId,
                        principalTable: "AfadTatbikatlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfadGonulluYetkinlikleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AfadGonulluId = table.Column<int>(type: "int", nullable: false),
                    AfadYetkinlikId = table.Column<int>(type: "int", nullable: false),
                    BelgeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GecerlilikBitisi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BelgeNo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfadGonulluYetkinlikleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfadGonulluYetkinlikleri_AfadGonulluler_AfadGonulluId",
                        column: x => x.AfadGonulluId,
                        principalTable: "AfadGonulluler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AfadGonulluYetkinlikleri_AfadYetkinlikler_AfadYetkinlikId",
                        column: x => x.AfadYetkinlikId,
                        principalTable: "AfadYetkinlikler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AfadDuyurular_DernekId",
                table: "AfadDuyurular",
                column: "DernekId");

            migrationBuilder.CreateIndex(
                name: "IX_AfadEgitimKatilimlari_AfadEgitimId_AfadGonulluId",
                table: "AfadEgitimKatilimlari",
                columns: new[] { "AfadEgitimId", "AfadGonulluId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AfadEgitimKatilimlari_AfadGonulluId",
                table: "AfadEgitimKatilimlari",
                column: "AfadGonulluId");

            migrationBuilder.CreateIndex(
                name: "IX_AfadEgitimler_DernekId",
                table: "AfadEgitimler",
                column: "DernekId");

            migrationBuilder.CreateIndex(
                name: "IX_AfadGonulluler_DernekId",
                table: "AfadGonulluler",
                column: "DernekId");

            migrationBuilder.CreateIndex(
                name: "IX_AfadGonulluler_KullaniciId",
                table: "AfadGonulluler",
                column: "KullaniciId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AfadGonulluYetkinlikleri_AfadGonulluId_AfadYetkinlikId",
                table: "AfadGonulluYetkinlikleri",
                columns: new[] { "AfadGonulluId", "AfadYetkinlikId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AfadGonulluYetkinlikleri_AfadYetkinlikId",
                table: "AfadGonulluYetkinlikleri",
                column: "AfadYetkinlikId");

            migrationBuilder.CreateIndex(
                name: "IX_AfadGorevAtamalari_AfadGonulluId",
                table: "AfadGorevAtamalari",
                column: "AfadGonulluId");

            migrationBuilder.CreateIndex(
                name: "IX_AfadGorevAtamalari_AfadGorevId_AfadGonulluId",
                table: "AfadGorevAtamalari",
                columns: new[] { "AfadGorevId", "AfadGonulluId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AfadGorevler_DernekId",
                table: "AfadGorevler",
                column: "DernekId");

            migrationBuilder.CreateIndex(
                name: "IX_AfadHaberler_DernekId",
                table: "AfadHaberler",
                column: "DernekId");

            migrationBuilder.CreateIndex(
                name: "IX_AfadTatbikatKatilimlari_AfadGonulluId",
                table: "AfadTatbikatKatilimlari",
                column: "AfadGonulluId");

            migrationBuilder.CreateIndex(
                name: "IX_AfadTatbikatKatilimlari_AfadTatbikatId_AfadGonulluId",
                table: "AfadTatbikatKatilimlari",
                columns: new[] { "AfadTatbikatId", "AfadGonulluId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AfadTatbikatlar_DernekId",
                table: "AfadTatbikatlar",
                column: "DernekId");

            migrationBuilder.CreateIndex(
                name: "IX_AfadYetkinlikler_DernekId",
                table: "AfadYetkinlikler",
                column: "DernekId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AfadDuyurular");

            migrationBuilder.DropTable(
                name: "AfadEgitimKatilimlari");

            migrationBuilder.DropTable(
                name: "AfadGonulluYetkinlikleri");

            migrationBuilder.DropTable(
                name: "AfadGorevAtamalari");

            migrationBuilder.DropTable(
                name: "AfadHaberler");

            migrationBuilder.DropTable(
                name: "AfadTatbikatKatilimlari");

            migrationBuilder.DropTable(
                name: "AfadEgitimler");

            migrationBuilder.DropTable(
                name: "AfadYetkinlikler");

            migrationBuilder.DropTable(
                name: "AfadGorevler");

            migrationBuilder.DropTable(
                name: "AfadGonulluler");

            migrationBuilder.DropTable(
                name: "AfadTatbikatlar");
        }
    }
}
