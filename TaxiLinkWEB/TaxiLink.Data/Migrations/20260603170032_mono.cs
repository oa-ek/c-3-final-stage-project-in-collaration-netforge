using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxiLink.Data.Migrations
{
    /// <inheritdoc />
    public partial class mono : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "UserPaymentCards",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalPaymentId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 1,
                column: "ExternalPaymentId",
                value: null);

            migrationBuilder.UpdateData(
                table: "UserPaymentCards",
                keyColumn: "Id",
                keyValue: 1,
                column: "Token",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Token",
                table: "UserPaymentCards");

            migrationBuilder.DropColumn(
                name: "ExternalPaymentId",
                table: "Orders");
        }
    }
}
