using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldTentCategoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        DROP INDEX IF EXISTS ux_tents_retreat_category_number;
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        CREATE UNIQUE INDEX ux_tents_retreat_category_number 
        ON tents (retreat_id, category, number);
    ");
        }
    }
}
