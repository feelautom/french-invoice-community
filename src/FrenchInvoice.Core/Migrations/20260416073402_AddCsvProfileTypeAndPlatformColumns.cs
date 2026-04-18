using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCsvProfileTypeAndPlatformColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientColumn",
                table: "CsvMappingProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FraisColumn",
                table: "CsvMappingProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModePaiementColumn",
                table: "CsvMappingProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileType",
                table: "CsvMappingProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReferenceColumn",
                table: "CsvMappingProfiles",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientColumn",
                table: "CsvMappingProfiles");

            migrationBuilder.DropColumn(
                name: "FraisColumn",
                table: "CsvMappingProfiles");

            migrationBuilder.DropColumn(
                name: "ModePaiementColumn",
                table: "CsvMappingProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileType",
                table: "CsvMappingProfiles");

            migrationBuilder.DropColumn(
                name: "ReferenceColumn",
                table: "CsvMappingProfiles");
        }
    }
}
