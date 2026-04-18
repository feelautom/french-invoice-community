using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class MultiEntityAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_Numero",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Numero",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Clients_Nom",
                table: "Clients");

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "Revenues",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "Quotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "PayoutRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "Invoices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "FixedCharges",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "Expenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "Declarations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "Clients",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "BankTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Entities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    TypeActivite = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodiciteDeclaration = table.Column<string>(type: "TEXT", nullable: false),
                    DateDebutActivite = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PlafondCA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NumeroSiret = table.Column<string>(type: "TEXT", nullable: false),
                    TvaIntracommunautaire = table.Column<string>(type: "TEXT", nullable: false),
                    Telephone = table.Column<string>(type: "TEXT", nullable: false),
                    AdresseSiege = table.Column<string>(type: "TEXT", nullable: false),
                    PrefixeFactures = table.Column<string>(type: "TEXT", nullable: false),
                    ProchainNumeroFacture = table.Column<int>(type: "INTEGER", nullable: false),
                    PrefixeDevis = table.Column<string>(type: "TEXT", nullable: false),
                    ProchainNumeroDevis = table.Column<int>(type: "INTEGER", nullable: false),
                    MentionsLegales = table.Column<string>(type: "TEXT", nullable: false),
                    FranchiseTVA = table.Column<bool>(type: "INTEGER", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    VersementLiberatoire = table.Column<bool>(type: "INTEGER", nullable: false),
                    TauxLiberatoire = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FraisVariables = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BeneficieACRE = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Revenues_EntityId",
                table: "Revenues",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_EntityId_Numero",
                table: "Quotes",
                columns: new[] { "EntityId", "Numero" },
                unique: true,
                filter: "Numero != ''");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutRecords_EntityId",
                table: "PayoutRecords",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_EntityId_Numero",
                table: "Invoices",
                columns: new[] { "EntityId", "Numero" },
                unique: true,
                filter: "Numero != ''");

            migrationBuilder.CreateIndex(
                name: "IX_FixedCharges_EntityId",
                table: "FixedCharges",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_EntityId",
                table: "Expenses",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Declarations_EntityId",
                table: "Declarations",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_EntityId_Nom",
                table: "Clients",
                columns: new[] { "EntityId", "Nom" });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_EntityId",
                table: "BankTransactions",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EntityId",
                table: "Users",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Entities");

            migrationBuilder.DropIndex(
                name: "IX_Revenues_EntityId",
                table: "Revenues");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_EntityId_Numero",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_PayoutRecords_EntityId",
                table: "PayoutRecords");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_EntityId_Numero",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_FixedCharges_EntityId",
                table: "FixedCharges");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_EntityId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Declarations_EntityId",
                table: "Declarations");

            migrationBuilder.DropIndex(
                name: "IX_Clients_EntityId_Nom",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_EntityId",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Revenues");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "PayoutRecords");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "FixedCharges");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Declarations");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "BankTransactions");

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AdresseSiege = table.Column<string>(type: "TEXT", nullable: false),
                    BeneficieACRE = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateDebutActivite = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FraisVariables = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FranchiseTVA = table.Column<bool>(type: "INTEGER", nullable: false),
                    MentionsLegales = table.Column<string>(type: "TEXT", nullable: false),
                    NomEntreprise = table.Column<string>(type: "TEXT", nullable: false),
                    NumeroSiret = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodiciteDeclaration = table.Column<string>(type: "TEXT", nullable: false),
                    PlafondCA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrefixeDevis = table.Column<string>(type: "TEXT", nullable: false),
                    PrefixeFactures = table.Column<string>(type: "TEXT", nullable: false),
                    ProchainNumeroDevis = table.Column<int>(type: "INTEGER", nullable: false),
                    ProchainNumeroFacture = table.Column<int>(type: "INTEGER", nullable: false),
                    TauxLiberatoire = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Telephone = table.Column<string>(type: "TEXT", nullable: false),
                    TvaIntracommunautaire = table.Column<string>(type: "TEXT", nullable: false),
                    TypeActivite = table.Column<string>(type: "TEXT", nullable: false),
                    VersementLiberatoire = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "UserSettings",
                columns: new[] { "Id", "AdresseSiege", "BeneficieACRE", "DateDebutActivite", "FraisVariables", "FranchiseTVA", "MentionsLegales", "NomEntreprise", "NumeroSiret", "PeriodiciteDeclaration", "PlafondCA", "PrefixeDevis", "PrefixeFactures", "ProchainNumeroDevis", "ProchainNumeroFacture", "TauxLiberatoire", "TauxTVA", "Telephone", "TvaIntracommunautaire", "TypeActivite", "VersementLiberatoire" },
                values: new object[] { 1, "", false, null, 0m, true, "Auto-entreprise - TVA non applicable, article 293B du code général des impôts", "", "", "Mensuelle", 77700m, "DEV-2026-", "FAC-2026-", 1, 1, 2.2m, 20m, "", "", "BNC", false });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Numero",
                table: "Quotes",
                column: "Numero",
                unique: true,
                filter: "Numero != ''");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Numero",
                table: "Invoices",
                column: "Numero",
                unique: true,
                filter: "Numero != ''");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Nom",
                table: "Clients",
                column: "Nom");
        }
    }
}
