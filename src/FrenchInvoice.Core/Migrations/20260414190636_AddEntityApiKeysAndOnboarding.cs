using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrenchInvoice.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityApiKeysAndOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StancerSecretKey",
                table: "Entities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSecretKey",
                table: "Entities",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StancerSecretKey",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "StripeSecretKey",
                table: "Entities");
        }
    }
}
