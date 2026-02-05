using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestion_de_proyectos.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileImageKeyToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageKey",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageKey",
                table: "AspNetUsers");
        }
    }
}
