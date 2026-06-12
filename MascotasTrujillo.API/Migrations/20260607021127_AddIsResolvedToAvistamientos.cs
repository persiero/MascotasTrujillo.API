using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MascotasTrujillo.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsResolvedToAvistamientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "Avistamientos",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "Avistamientos");
        }
    }
}
