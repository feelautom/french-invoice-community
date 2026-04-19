using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    SequenceNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    EntryType = table.Column<string>(type: "TEXT", nullable: false),
                    RevenueId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExpenseId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Tiers = table.Column<string>(type: "TEXT", nullable: false),
                    PreviousHash = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingEntries_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccountingEntries_Revenues_RevenueId",
                        column: x => x.RevenueId,
                        principalTable: "Revenues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccountingPeriodClosings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedBy = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    EntryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalRecettes = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDepenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriodClosings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_EntityId",
                table: "AccountingEntries",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_EntityId_SequenceNumber",
                table: "AccountingEntries",
                columns: new[] { "EntityId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_ExpenseId",
                table: "AccountingEntries",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_RevenueId",
                table: "AccountingEntries",
                column: "RevenueId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriodClosings_EntityId_PeriodEnd",
                table: "AccountingPeriodClosings",
                columns: new[] { "EntityId", "PeriodEnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingEntries");

            migrationBuilder.DropTable(
                name: "AccountingPeriodClosings");
        }
    }
}
