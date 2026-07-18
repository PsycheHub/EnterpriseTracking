using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addproofentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProofOfActivitys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DocIdentifier = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    MapCategoryId = table.Column<string>(type: "text", nullable: false),
                    AppName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    ImageData = table.Column<byte[]>(type: "bytea", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProofOfActivitys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProofOfActivitys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProofOfActivitys_MapAppCatories_MapCategoryId",
                        column: x => x.MapCategoryId,
                        principalTable: "MapAppCatories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfActivitys_MapCategoryId",
                table: "ProofOfActivitys",
                column: "MapCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfActivitys_UserId",
                table: "ProofOfActivitys",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProofOfActivitys");
        }
    }
}
