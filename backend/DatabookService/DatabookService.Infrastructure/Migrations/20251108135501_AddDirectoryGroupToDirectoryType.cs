using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabookService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectoryGroupToDirectoryType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DirectoryGroupId",
                table: "DirectoryTypes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.InsertData(
                table: "DirectoryGroups",
                columns: new[] { "Id", "Name" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Без группы" });

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryTypes_DirectoryGroupId",
                table: "DirectoryTypes",
                column: "DirectoryGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_DirectoryTypes_DirectoryGroups_DirectoryGroupId",
                table: "DirectoryTypes",
                column: "DirectoryGroupId",
                principalTable: "DirectoryGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DirectoryTypes_DirectoryGroups_DirectoryGroupId",
                table: "DirectoryTypes");

            migrationBuilder.DropIndex(
                name: "IX_DirectoryTypes_DirectoryGroupId",
                table: "DirectoryTypes");

            migrationBuilder.DeleteData(
                table: "DirectoryGroups",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DropColumn(
                name: "DirectoryGroupId",
                table: "DirectoryTypes");
        }
    }
}
