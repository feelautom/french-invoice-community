using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class MultiStatutAndAnnualReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiry",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Revenues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Revenues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Expenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateChangementStatut",
                table: "Entities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NombrePartsFiscales",
                table: "Entities",
                type: "TEXT",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "StatutJuridique",
                table: "Entities",
                type: "TEXT",
                nullable: false,
                defaultValue: "MicroEntreprise");

            migrationBuilder.AddColumn<decimal>(
                name: "TauxCFP",
                table: "Entities",
                type: "TEXT",
                nullable: false,
                defaultValue: 0.2m);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxCotisation",
                table: "Entities",
                type: "TEXT",
                nullable: false,
                defaultValue: 25.6m);

            migrationBuilder.AddColumn<bool>(
                name: "IsApportPersonnel",
                table: "BankTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "BankTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AnnualReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseClosureId = table.Column<int>(type: "INTEGER", nullable: true),
                    Annee = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodeDebut = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodeFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StatutJuridique = table.Column<string>(type: "TEXT", nullable: false),
                    ChiffreAffaires = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AutresProduits = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalProduits = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AchatsChargesExternes = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CotisationsSociales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CFP = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VersementLiberatoire = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FraisPlateforme = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AutresCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResultatExploitation = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ImpotSurBenefice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResultatNet = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TresorerieFinExercice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreancesClients = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CapitalApports = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ResultatExercice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DettesUrssaf = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PdfPath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseClosures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    StatutJuridique = table.Column<string>(type: "TEXT", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TauxCotisation = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TauxCFP = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TauxLiberatoire = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    VersementLiberatoire = table.Column<bool>(type: "INTEGER", nullable: false),
                    FranchiseTVA = table.Column<bool>(type: "INTEGER", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PlafondCA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TypeActivite = table.Column<string>(type: "TEXT", nullable: false),
                    TotalCA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDepenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCotisations = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCFP = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalVL = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BeneficeNet = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseClosures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnualReports_EntityId_Annee",
                table: "AnnualReports",
                columns: new[] { "EntityId", "Annee" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseClosures_EntityId",
                table: "ExerciseClosures",
                column: "EntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnualReports");

            migrationBuilder.DropTable(
                name: "ExerciseClosures");

            migrationBuilder.DropColumn(
                name: "PasswordResetExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Revenues");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Revenues");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "DateChangementStatut",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "NombrePartsFiscales",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "StatutJuridique",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "TauxCFP",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "TauxCotisation",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "IsApportPersonnel",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "BankTransactions");
        }
    }
}
