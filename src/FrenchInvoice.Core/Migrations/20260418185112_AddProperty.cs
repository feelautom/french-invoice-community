using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "Revenues",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "Quotes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "PayoutRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "FixedCharges",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "Expenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "Clients",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "BankTransactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Couleur = table.Column<string>(type: "TEXT", nullable: true),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_PropertyId",
                table: "Revenues",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_PropertyId",
                table: "Quotes",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecords_PropertyId",
                table: "PayoutRecords",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PropertyId",
                table: "Invoices",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedCharges_PropertyId",
                table: "FixedCharges",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_PropertyId",
                table: "Expenses",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_PropertyId",
                table: "Clients",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_PropertyId",
                table: "BankTransactions",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_EntityId_Nom",
                table: "Properties",
                columns: new[] { "EntityId", "Nom" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BankTransactions_Properties_PropertyId",
                table: "BankTransactions",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Properties_PropertyId",
                table: "Clients",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Properties_PropertyId",
                table: "Expenses",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedCharges_Properties_PropertyId",
                table: "FixedCharges",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Properties_PropertyId",
                table: "Invoices",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PayoutRecords_Properties_PropertyId",
                table: "PayoutRecords",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotes_Properties_PropertyId",
                table: "Quotes",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Revenues_Properties_PropertyId",
                table: "Revenues",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankTransactions_Properties_PropertyId",
                table: "BankTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Properties_PropertyId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Properties_PropertyId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_FixedCharges_Properties_PropertyId",
                table: "FixedCharges");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Properties_PropertyId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PayoutRecords_Properties_PropertyId",
                table: "PayoutRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotes_Properties_PropertyId",
                table: "Quotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Revenues_Properties_PropertyId",
                table: "Revenues");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Revenues_PropertyId",
                table: "Revenues");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_PropertyId",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_PayoutRecords_PropertyId",
                table: "PayoutRecords");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PropertyId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_FixedCharges_PropertyId",
                table: "FixedCharges");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_PropertyId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Clients_PropertyId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_PropertyId",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "Revenues");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "PayoutRecords");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "FixedCharges");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "BankTransactions");
        }
    }
}
