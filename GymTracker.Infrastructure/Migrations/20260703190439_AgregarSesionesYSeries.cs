using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GymTracker.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSesionesYSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sesiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    RutinaId = table.Column<int>(type: "integer", nullable: true),
                    NombreRutina = table.Column<string>(type: "text", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notas = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sesiones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeriesRealizadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SesionId = table.Column<int>(type: "integer", nullable: false),
                    EjercicioId = table.Column<int>(type: "integer", nullable: false),
                    NombreEjercicio = table.Column<string>(type: "text", nullable: false),
                    GrupoMuscular = table.Column<int>(type: "integer", nullable: false),
                    NumeroSerie = table.Column<int>(type: "integer", nullable: false),
                    RepeticionesObjetivo = table.Column<int>(type: "integer", nullable: false),
                    PesoObjetivo = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    RepeticionesReales = table.Column<int>(type: "integer", nullable: false),
                    PesoReal = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesRealizadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesRealizadas_Sesiones_SesionId",
                        column: x => x.SesionId,
                        principalTable: "Sesiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeriesRealizadas_SesionId",
                table: "SeriesRealizadas",
                column: "SesionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeriesRealizadas");

            migrationBuilder.DropTable(
                name: "Sesiones");
        }
    }
}
