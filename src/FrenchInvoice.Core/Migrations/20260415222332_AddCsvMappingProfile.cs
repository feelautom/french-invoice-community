using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCsvMappingProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CsvMappingProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Separator = table.Column<string>(type: "TEXT", nullable: false),
                    HeaderRow = table.Column<int>(type: "INTEGER", nullable: false),
                    DateFormat = table.Column<string>(type: "TEXT", nullable: false),
                    DateColumn = table.Column<int>(type: "INTEGER", nullable: false),
                    LibelleColumn = table.Column<int>(type: "INTEGER", nullable: false),
                    MontantColumn = table.Column<int>(type: "INTEGER", nullable: true),
                    DebitColumn = table.Column<int>(type: "INTEGER", nullable: true),
                    CreditColumn = table.Column<int>(type: "INTEGER", nullable: true),
                    SoldeColumn = table.Column<int>(type: "INTEGER", nullable: true),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CsvMappingProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CsvMappingProfiles_EntityId_Nom",
                table: "CsvMappingProfiles",
                columns: new[] { "EntityId", "Nom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CsvMappingProfiles");
        }
    }
}
