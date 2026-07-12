using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MergeCat.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_address = table.Column<string>(type: "char(42)", nullable: false),
                    balance = table.Column<double>(type: "double precision", nullable: false),
                    total_earned = table.Column<double>(type: "double precision", nullable: false),
                    income_rate = table.Column<double>(type: "double precision", nullable: false),
                    last_collected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_players", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cells",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false),
                    unit_level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cells", x => x.id);
                    table.ForeignKey(
                        name: "fk_cells_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "merge_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cell_a_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cell_b_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resulting_level = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merge_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_merge_logs_cells_cell_a_id",
                        column: x => x.cell_a_id,
                        principalTable: "cells",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_merge_logs_cells_cell_b_id",
                        column: x => x.cell_b_id,
                        principalTable: "cells",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cells_player_id_index",
                table: "cells",
                columns: new[] { "player_id", "index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_merge_logs_cell_a_id",
                table: "merge_logs",
                column: "cell_a_id");

            migrationBuilder.CreateIndex(
                name: "ix_merge_logs_cell_b_id",
                table: "merge_logs",
                column: "cell_b_id");

            migrationBuilder.CreateIndex(
                name: "ix_players_wallet_address",
                table: "players",
                column: "wallet_address",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "merge_logs");

            migrationBuilder.DropTable(
                name: "cells");

            migrationBuilder.DropTable(
                name: "players");
        }
    }
}
