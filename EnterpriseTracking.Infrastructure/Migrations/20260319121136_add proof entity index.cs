using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addproofentityindex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProofOfActivitys_DocIdentifier",
                table: "ProofOfActivitys",
                column: "DocIdentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProofOfActivitys_DocIdentifier",
                table: "ProofOfActivitys");
        }
    }
}
