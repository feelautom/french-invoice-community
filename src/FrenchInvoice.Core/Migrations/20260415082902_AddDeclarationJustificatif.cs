using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDeclarationJustificatif : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JustificatifFileName",
                table: "Declarations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JustificatifFileName",
                table: "Declarations");
        }
    }
}
