using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSiretData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SiretDataId",
                table: "Clients",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SiretDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Siren = table.Column<string>(type: "TEXT", nullable: false),
                    Siret = table.Column<string>(type: "TEXT", nullable: false),
                    NomComplet = table.Column<string>(type: "TEXT", nullable: false),
                    NomRaisonSociale = table.Column<string>(type: "TEXT", nullable: true),
                    Sigle = table.Column<string>(type: "TEXT", nullable: true),
                    NatureJuridique = table.Column<string>(type: "TEXT", nullable: true),
                    SectionActivitePrincipale = table.Column<string>(type: "TEXT", nullable: true),
                    ActivitePrincipale = table.Column<string>(type: "TEXT", nullable: true),
                    ActivitePrincipaleNAF25 = table.Column<string>(type: "TEXT", nullable: true),
                    CategorieEntreprise = table.Column<string>(type: "TEXT", nullable: true),
                    TrancheEffectifSalarie = table.Column<string>(type: "TEXT", nullable: true),
                    EtatAdministratif = table.Column<string>(type: "TEXT", nullable: true),
                    StatutDiffusion = table.Column<string>(type: "TEXT", nullable: true),
                    EstEntrepreneurIndividuel = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstAssociation = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstEss = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstServicePublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstSocieteMission = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstQualiopi = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstRge = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstBio = table.Column<bool>(type: "INTEGER", nullable: false),
                    IdentifiantAssociation = table.Column<string>(type: "TEXT", nullable: true),
                    NombreEtablissements = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreEtablissementsOuverts = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreationEntreprise = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateFermetureEntreprise = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NomCommercial = table.Column<string>(type: "TEXT", nullable: true),
                    Enseigne = table.Column<string>(type: "TEXT", nullable: true),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    NumeroVoie = table.Column<string>(type: "TEXT", nullable: true),
                    TypeVoie = table.Column<string>(type: "TEXT", nullable: true),
                    LibelleVoie = table.Column<string>(type: "TEXT", nullable: true),
                    ComplementAdresse = table.Column<string>(type: "TEXT", nullable: true),
                    CodePostal = table.Column<string>(type: "TEXT", nullable: true),
                    LibelleCommune = table.Column<string>(type: "TEXT", nullable: true),
                    Commune = table.Column<string>(type: "TEXT", nullable: true),
                    Cedex = table.Column<string>(type: "TEXT", nullable: true),
                    LibelleCedex = table.Column<string>(type: "TEXT", nullable: true),
                    Departement = table.Column<string>(type: "TEXT", nullable: true),
                    Region = table.Column<string>(type: "TEXT", nullable: true),
                    Epci = table.Column<string>(type: "TEXT", nullable: true),
                    CodePaysEtranger = table.Column<string>(type: "TEXT", nullable: true),
                    LibellePaysEtranger = table.Column<string>(type: "TEXT", nullable: true),
                    LibelleCommuneEtranger = table.Column<string>(type: "TEXT", nullable: true),
                    DistributionSpeciale = table.Column<string>(type: "TEXT", nullable: true),
                    IndiceRepetition = table.Column<string>(type: "TEXT", nullable: true),
                    EstSiege = table.Column<bool>(type: "INTEGER", nullable: false),
                    CaractereEmployeur = table.Column<string>(type: "TEXT", nullable: true),
                    ActivitePrincipaleSiege = table.Column<string>(type: "TEXT", nullable: true),
                    ActivitePrincipaleNAF25Siege = table.Column<string>(type: "TEXT", nullable: true),
                    TrancheEffectifSalarieSiege = table.Column<string>(type: "TEXT", nullable: true),
                    DateCreationSiege = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateDebutActivite = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateFermetureSiege = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Latitude = table.Column<string>(type: "TEXT", nullable: true),
                    Longitude = table.Column<string>(type: "TEXT", nullable: true),
                    DirigeantsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ListeIdccJson = table.Column<string>(type: "TEXT", nullable: true),
                    ReponseJson = table.Column<string>(type: "TEXT", nullable: false),
                    DateMiseAJourInsee = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateMiseAJourRne = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiretDatas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_SiretDataId",
                table: "Clients",
                column: "SiretDataId");

            migrationBuilder.CreateIndex(
                name: "IX_SiretDatas_Siren",
                table: "SiretDatas",
                column: "Siren");

            migrationBuilder.CreateIndex(
                name: "IX_SiretDatas_Siret",
                table: "SiretDatas",
                column: "Siret",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_SiretDatas_SiretDataId",
                table: "Clients",
                column: "SiretDataId",
                principalTable: "SiretDatas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_SiretDatas_SiretDataId",
                table: "Clients");

            migrationBuilder.DropTable(
                name: "SiretDatas");

            migrationBuilder.DropIndex(
                name: "IX_Clients_SiretDataId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "SiretDataId",
                table: "Clients");
        }
    }
}
