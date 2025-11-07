using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabookService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnumValuesToDirectoryField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "EnumValues",
                table: "DirectoryFields",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnumValues",
                table: "DirectoryFields");
        }
    }
}
