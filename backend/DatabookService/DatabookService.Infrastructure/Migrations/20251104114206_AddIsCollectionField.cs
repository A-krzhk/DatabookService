using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabookService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCollectionField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCollection",
                table: "DirectoryFields",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCollection",
                table: "DirectoryFields");
        }
    }
}
