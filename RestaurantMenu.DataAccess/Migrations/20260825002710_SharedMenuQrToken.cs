using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantMenu.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SharedMenuQrToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tables_QrToken",
                table: "Tables");

            migrationBuilder.AddColumn<string>(
                name: "MenuQrToken",
                table: "Restaurants",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE r
                SET MenuQrToken = COALESCE(
                    (SELECT TOP (1) t.QrToken FROM Tables t WHERE t.RestaurantId = r.Id ORDER BY t.TableNumber, t.Id),
                    REPLACE(LOWER(CONVERT(nvarchar(36), NEWID())), '-', ''))
                FROM Restaurants r
                WHERE r.MenuQrToken IS NULL OR r.MenuQrToken = '';

                UPDATE t
                SET QrToken = r.MenuQrToken
                FROM Tables t
                INNER JOIN Restaurants r ON r.Id = t.RestaurantId;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Tables_QrToken",
                table: "Tables",
                column: "QrToken");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_MenuQrToken",
                table: "Restaurants",
                column: "MenuQrToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tables_QrToken",
                table: "Tables");

            migrationBuilder.DropIndex(
                name: "IX_Restaurants_MenuQrToken",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "MenuQrToken",
                table: "Restaurants");

            migrationBuilder.CreateIndex(
                name: "IX_Tables_QrToken",
                table: "Tables",
                column: "QrToken",
                unique: true);
        }
    }
}
