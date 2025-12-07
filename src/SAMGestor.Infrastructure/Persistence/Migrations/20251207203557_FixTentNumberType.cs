using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTentNumberType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cria índice único para evitar duplicação
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_tents_unique_number 
                ON tents (retreat_id, number);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ix_tents_unique_number;
            ");
        }
    }
}