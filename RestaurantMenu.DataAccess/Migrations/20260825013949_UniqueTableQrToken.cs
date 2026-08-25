using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UniqueTableQrToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tables_QrToken",
                table: "Tables");

            migrationBuilder.Sql("""
                UPDATE Tables
                SET QrToken = REPLACE(LOWER(CONVERT(nvarchar(36), NEWID())), '-', ''),
                    UpdatedAt = SYSUTCDATETIME()
                WHERE QrToken IS NULL OR LTRIM(RTRIM(QrToken)) = '';

                UPDATE t
                SET QrToken = REPLACE(LOWER(CONVERT(nvarchar(36), NEWID())), '-', ''),
                    UpdatedAt = SYSUTCDATETIME()
                FROM Tables t
                INNER JOIN (
                    SELECT QrToken
                    FROM Tables
                    GROUP BY QrToken
                    HAVING COUNT(*) > 1
                ) d ON t.QrToken = d.QrToken;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Tables_QrToken",
                table: "Tables",
                column: "QrToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tables_QrToken",
                table: "Tables");

            migrationBuilder.CreateIndex(
                name: "IX_Tables_QrToken",
                table: "Tables",
                column: "QrToken");
        }
    }
}
