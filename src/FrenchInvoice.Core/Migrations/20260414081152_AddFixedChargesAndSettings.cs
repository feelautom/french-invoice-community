using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedChargesAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdresseSiege",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "BeneficieACRE",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "FraisVariables",
                table: "UserSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "FranchiseTVA",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MentionsLegales",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NomEntreprise",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroSiret",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrefixeDevis",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrefixeFactures",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProchainNumeroDevis",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProchainNumeroFacture",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxLiberatoire",
                table: "UserSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxTVA",
                table: "UserSettings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Telephone",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TvaIntracommunautaire",
                table: "UserSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "VersementLiberatoire",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FixedCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedCharges", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "UserSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AdresseSiege", "BeneficieACRE", "FraisVariables", "FranchiseTVA", "MentionsLegales", "NomEntreprise", "NumeroSiret", "PrefixeDevis", "PrefixeFactures", "ProchainNumeroDevis", "ProchainNumeroFacture", "TauxLiberatoire", "TauxTVA", "Telephone", "TvaIntracommunautaire", "VersementLiberatoire" },
                values: new object[] { "", false, 0m, true, "Auto-entreprise - TVA non applicable, article 293B du code général des impôts", "", "", "DEV-2026-", "FAC-2026-", 1, 1, 2.2m, 20m, "", "", false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixedCharges");

            migrationBuilder.DropColumn(
                name: "AdresseSiege",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "BeneficieACRE",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "FraisVariables",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "FranchiseTVA",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "MentionsLegales",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "NomEntreprise",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "NumeroSiret",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "PrefixeDevis",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "PrefixeFactures",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "ProchainNumeroDevis",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "ProchainNumeroFacture",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "TauxLiberatoire",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "TauxTVA",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "Telephone",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "TvaIntracommunautaire",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "VersementLiberatoire",
                table: "UserSettings");
        }
    }
}
