using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabookService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExtraColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedAt", table: "DirectoryTypes");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "DirectoryTypes");
            migrationBuilder.DropColumn(name: "IsDeleted", table: "DirectoryTypes");
            migrationBuilder.DropColumn(name: "DeletedDate", table: "DirectoryTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
