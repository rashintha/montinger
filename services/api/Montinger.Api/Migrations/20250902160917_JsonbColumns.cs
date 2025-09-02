using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Montinger.Api.Migrations
{
    /// <inheritdoc />
    public partial class JsonbColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_check_result_ts",
                table: "CheckResults",
                newName: "ix_results_check_ts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_results_check_ts",
                table: "CheckResults",
                newName: "ix_check_result_ts");
        }
    }
}
