using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturXBasicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DebutPeriode",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinPeriode",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroCommande",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeFacture",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Bic",
                table: "Entities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodePays",
                table: "Entities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Entities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "Entities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodePays",
                table: "Clients",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebutPeriode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FinPeriode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "NumeroCommande",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TypeFacture",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Bic",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "CodePays",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "Iban",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "CodePays",
                table: "Clients");
        }
    }
}
