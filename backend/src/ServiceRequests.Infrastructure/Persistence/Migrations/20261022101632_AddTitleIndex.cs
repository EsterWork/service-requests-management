using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceRequests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_Title",
                table: "ServiceRequests",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_Title",
                table: "ServiceRequests");
        }
    }
}
