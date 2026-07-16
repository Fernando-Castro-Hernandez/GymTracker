using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymTracker.Migrations
{
    /// <inheritdoc />
    public partial class AgregarExerciseDbId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExerciseDbId",
                table: "Ejercicios",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExerciseDbId",
                table: "Ejercicios");
        }
    }
}
