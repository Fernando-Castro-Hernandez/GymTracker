using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GymTracker.Migrations
{
    /// <inheritdoc />
    public partial class AgregarChatMensajes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMensajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    EsDelUsuario = table.Column<bool>(type: "boolean", nullable: false),
                    Contenido = table.Column<string>(type: "text", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Proveedor = table.Column<string>(type: "text", nullable: true),
                    TokensEntrada = table.Column<int>(type: "integer", nullable: true),
                    TokensSalida = table.Column<int>(type: "integer", nullable: true),
                    LatenciaMs = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMensajes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMensajes_UsuarioId_FechaUtc",
                table: "ChatMensajes",
                columns: new[] { "UsuarioId", "FechaUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMensajes");
        }
    }
}
