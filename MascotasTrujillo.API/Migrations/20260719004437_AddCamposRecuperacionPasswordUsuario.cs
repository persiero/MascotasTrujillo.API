using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MascotasTrujillo.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposRecuperacionPasswordUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoRecuperacionExpira",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoRecuperacionPassword",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoRecuperacionExpira",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CodigoRecuperacionPassword",
                table: "AspNetUsers");
        }
    }
}
