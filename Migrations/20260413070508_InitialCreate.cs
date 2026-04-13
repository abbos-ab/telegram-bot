using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Parcels",
                columns: table => new
                {
                    TrackCode = table.Column<string>(type: "TEXT", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    ArrivedAtChina = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parcels", x => x.TrackCode);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "UserParcels",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "INTEGER", nullable: false),
                    TrackCode = table.Column<string>(type: "TEXT", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserParcels", x => new { x.ChatId, x.TrackCode });
                    table.ForeignKey(
                        name: "FK_UserParcels_Parcels_TrackCode",
                        column: x => x.TrackCode,
                        principalTable: "Parcels",
                        principalColumn: "TrackCode",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserParcels_Users_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Users",
                        principalColumn: "ChatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserParcels_TrackCode",
                table: "UserParcels",
                column: "TrackCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserParcels");

            migrationBuilder.DropTable(
                name: "Parcels");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
