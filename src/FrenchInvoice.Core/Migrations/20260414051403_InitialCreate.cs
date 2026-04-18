using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Declarations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Periode = table.Column<string>(type: "TEXT", nullable: false),
                    DateLimite = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MontantCA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontantCotisations = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TauxApplique = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Statut = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Declarations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Fournisseur = table.Column<string>(type: "TEXT", nullable: false),
                    Categorie = table.Column<string>(type: "TEXT", nullable: false),
                    Justificatif = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Revenues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Client = table.Column<string>(type: "TEXT", nullable: false),
                    ModePaiement = table.Column<string>(type: "TEXT", nullable: false),
                    Categorie = table.Column<string>(type: "TEXT", nullable: false),
                    ReferenceFacture = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Revenues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeActivite = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodiciteDeclaration = table.Column<string>(type: "TEXT", nullable: false),
                    DateDebutActivite = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PlafondCA = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Libelle = table.Column<string>(type: "TEXT", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Solde = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Categorise = table.Column<bool>(type: "INTEGER", nullable: false),
                    RevenueId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExpenseId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankTransactions_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BankTransactions_Revenues_RevenueId",
                        column: x => x.RevenueId,
                        principalTable: "Revenues",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "UserSettings",
                columns: new[] { "Id", "DateDebutActivite", "PeriodiciteDeclaration", "PlafondCA", "TypeActivite" },
                values: new object[] { 1, null, "Mensuelle", 77700m, "BNC" });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_ExpenseId",
                table: "BankTransactions",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_RevenueId",
                table: "BankTransactions",
                column: "RevenueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankTransactions");

            migrationBuilder.DropTable(
                name: "Declarations");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "Revenues");
        }
    }
}
