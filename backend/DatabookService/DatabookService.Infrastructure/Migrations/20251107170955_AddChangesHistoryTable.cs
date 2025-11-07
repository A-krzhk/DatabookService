using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabookService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChangesHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DirectoryTypes");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "DirectoryTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DirectoryTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DirectoryTypes");

            migrationBuilder.CreateTable(
                name: "ChangesHistoryRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DirectoryTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    TableName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    ChangedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangesHistoryRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangesHistoryRecord_DirectoryTypes_DirectoryTypeId",
                        column: x => x.DirectoryTypeId,
                        principalTable: "DirectoryTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangesHistoryRecord_DirectoryTypeId",
                table: "ChangesHistoryRecord",
                column: "DirectoryTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangesHistoryRecord");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DirectoryTypes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "DirectoryTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DirectoryTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DirectoryTypes",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
