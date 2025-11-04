using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArticleDatabase.Migrations.Global
{
    /// <inheritdoc />
    public partial class AddRegionColumn_Global : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Region",
                table: "Articles");
        }
    }
}
