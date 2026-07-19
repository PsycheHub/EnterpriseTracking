using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class createactivityentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserActivity_ActivityCategorys_CategoryId",
                table: "UserActivity");

            migrationBuilder.DropIndex(
                name: "IX_UserActivity_CategoryId",
                table: "UserActivity");

            migrationBuilder.DropColumn(
                name: "EndTimeUtc",
                table: "UserActivity");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "UserActivity");

            migrationBuilder.DropColumn(
                name: "LastActivityUtc",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "TotalActiveTime",
                table: "Attendances");

            migrationBuilder.RenameColumn(
                name: "StartTimeUtc",
                table: "UserActivity",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "UserActivity",
                newName: "WindowTitle");

            migrationBuilder.RenameColumn(
                name: "AppOrSite",
                table: "UserActivity",
                newName: "MapCategoryId");

            migrationBuilder.RenameColumn(
                name: "ActivityName",
                table: "UserActivity",
                newName: "MachineId");

            migrationBuilder.AddColumn<string>(
                name: "AppName",
                table: "UserActivity",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "UserActivity",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "UserActivity",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "UserActivity",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsIdle",
                table: "UserActivity",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MapAppCatories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapAppCatories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MapAppCatories_ActivityCategorys_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ActivityCategorys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivity_MapCategoryId",
                table: "UserActivity",
                column: "MapCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MapAppCatories_CategoryId",
                table: "MapAppCatories",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserActivity_MapAppCatories_MapCategoryId",
                table: "UserActivity",
                column: "MapCategoryId",
                principalTable: "MapAppCatories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserActivity_MapAppCatories_MapCategoryId",
                table: "UserActivity");

            migrationBuilder.DropTable(
                name: "MapAppCatories");

            migrationBuilder.DropIndex(
                name: "IX_UserActivity_MapCategoryId",
                table: "UserActivity");

            migrationBuilder.DropColumn(
                name: "AppName",
                table: "UserActivity");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "UserActivity");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "UserActivity");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "UserActivity");

            migrationBuilder.DropColumn(
                name: "IsIdle",
                table: "UserActivity");

            migrationBuilder.RenameColumn(
                name: "WindowTitle",
                table: "UserActivity",
                newName: "CategoryId");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "UserActivity",
                newName: "StartTimeUtc");

            migrationBuilder.RenameColumn(
                name: "MapCategoryId",
                table: "UserActivity",
                newName: "AppOrSite");

            migrationBuilder.RenameColumn(
                name: "MachineId",
                table: "UserActivity",
                newName: "ActivityName");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTimeUtc",
                table: "UserActivity",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "UserActivity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityUtc",
                table: "Attendances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TotalActiveTime",
                table: "Attendances",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateIndex(
                name: "IX_UserActivity_CategoryId",
                table: "UserActivity",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserActivity_ActivityCategorys_CategoryId",
                table: "UserActivity",
                column: "CategoryId",
                principalTable: "ActivityCategorys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
