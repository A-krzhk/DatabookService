using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabookService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TableName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DirectoryGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectoryTypes_DirectoryGroups_DirectoryGroupId",
                        column: x => x.DirectoryGroupId,
                        principalTable: "DirectoryGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "DirectoryFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectoryTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ColumnName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataType = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ReferenceDirectoryTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsCollection = table.Column<bool>(type: "boolean", nullable: false),
                    EnumValues = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectoryFields_DirectoryTypes_DirectoryTypeId",
                        column: x => x.DirectoryTypeId,
                        principalTable: "DirectoryTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DirectoryFields_DirectoryTypes_ReferenceDirectoryTypeId",
                        column: x => x.ReferenceDirectoryTypeId,
                        principalTable: "DirectoryTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DirectoryGroups",
                columns: new[] { "Id", "Name" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Без группы" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash_IsActive",
                table: "ApiKeys",
                columns: new[] { "KeyHash", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ChangesHistoryRecord_DirectoryTypeId",
                table: "ChangesHistoryRecord",
                column: "DirectoryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryFields_DirectoryTypeId",
                table: "DirectoryFields",
                column: "DirectoryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryFields_ReferenceDirectoryTypeId",
                table: "DirectoryFields",
                column: "ReferenceDirectoryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryTypes_DirectoryGroupId",
                table: "DirectoryTypes",
                column: "DirectoryGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DirectoryTypes_TableName",
                table: "DirectoryTypes",
                column: "TableName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "ChangesHistoryRecord");

            migrationBuilder.DropTable(
                name: "DirectoryFields");

            migrationBuilder.DropTable(
                name: "DirectoryTypes");

            migrationBuilder.DropTable(
                name: "DirectoryGroups");
        }
    }
}
