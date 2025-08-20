using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "FormData",
                newName: "Telefono");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "FormData",
                newName: "Nota");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "FormData",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "FormData",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "FormData");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "FormData");

            migrationBuilder.RenameColumn(
                name: "Telefono",
                table: "FormData",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "Nota",
                table: "FormData",
                newName: "Description");
        }
    }
}
