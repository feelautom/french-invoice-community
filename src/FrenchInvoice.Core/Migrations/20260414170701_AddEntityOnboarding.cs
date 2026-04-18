using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodePostal",
                table: "Entities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Configured",
                table: "Entities",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SiretDataId",
                table: "Entities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ville",
                table: "Entities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_SiretDataId",
                table: "Entities",
                column: "SiretDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Entities_SiretDatas_SiretDataId",
                table: "Entities",
                column: "SiretDataId",
                principalTable: "SiretDatas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entities_SiretDatas_SiretDataId",
                table: "Entities");

            migrationBuilder.DropIndex(
                name: "IX_Entities_SiretDataId",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "CodePostal",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "Configured",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "SiretDataId",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "Ville",
                table: "Entities");
        }
    }
}
