using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace MascotasTrujillo.API.Migrations
{
    /// <inheritdoc />
    public partial class ExpandMascotaWithIoTTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DispositivoId",
                table: "Mascotas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaActualizacion",
                table: "Mascotas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "UltimaUbicacion",
                table: "Mascotas",
                type: "geometry",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DispositivoId",
                table: "Mascotas");

            migrationBuilder.DropColumn(
                name: "UltimaActualizacion",
                table: "Mascotas");

            migrationBuilder.DropColumn(
                name: "UltimaUbicacion",
                table: "Mascotas");
        }
    }
}
