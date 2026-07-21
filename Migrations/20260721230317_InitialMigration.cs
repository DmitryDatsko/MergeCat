using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MergeCat.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "indexer_states",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    last_processed_block = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_indexer_states", x => x.id);
                    table.CheckConstraint("CK_IndexerState_SingletonId", "\"id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_address = table.Column<string>(type: "text", nullable: false),
                    balance = table.Column<double>(type: "double precision", nullable: false),
                    total_earned = table.Column<double>(type: "double precision", nullable: false),
                    income_rate = table.Column<double>(type: "double precision", nullable: false),
                    daily_purchases = table.Column<int>(type: "integer", nullable: false),
                    league = table.Column<string>(type: "text", nullable: false),
                    last_collected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    boost_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_purchase_date = table.Column<DateOnly>(type: "date", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_players", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processed_purchases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tx_hash = table.Column<string>(type: "text", nullable: false),
                    log_index = table.Column<int>(type: "integer", nullable: false),
                    buyer_address = table.Column<string>(type: "text", nullable: false),
                    event_type = table.Column<byte>(type: "smallint", nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    block_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_purchases", x => x.id);
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

            migrationBuilder.InsertData(
                table: "indexer_states",
                columns: new[] { "id", "last_processed_block" },
                values: new object[] { 1, 72766968m });

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

            migrationBuilder.CreateIndex(
                name: "ix_processed_purchases_buyer_address",
                table: "processed_purchases",
                column: "buyer_address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_processed_purchases_tx_hash_log_index",
                table: "processed_purchases",
                columns: new[] { "tx_hash", "log_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "indexer_states");

            migrationBuilder.DropTable(
                name: "merge_logs");

            migrationBuilder.DropTable(
                name: "processed_purchases");

            migrationBuilder.DropTable(
                name: "cells");

            migrationBuilder.DropTable(
                name: "players");
        }
    }
}
