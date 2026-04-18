using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayoutRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Platform = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MontantBrut = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Frais = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontantNet = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StatementDescription = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    BankTransactionId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutRecords_BankTransactions_BankTransactionId",
                        column: x => x.BankTransactionId,
                        principalTable: "BankTransactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecords_BankTransactionId",
                table: "PayoutRecords",
                column: "BankTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecords_Platform_ExternalId",
                table: "PayoutRecords",
                columns: new[] { "Platform", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayoutRecords");
        }
    }
}
