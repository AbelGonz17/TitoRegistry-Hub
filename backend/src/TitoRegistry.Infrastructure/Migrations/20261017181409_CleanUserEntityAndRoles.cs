using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TitoRegistry.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanUserEntityAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultIsr",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultPhoneIncentive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultTss",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultIsr",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPhoneIncentive",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultTss",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Avatar", "DefaultIsr", "DefaultPhoneIncentive", "DefaultTss", "IsSuperAdmin" },
                values: new object[] { "", 0m, 0m, 0m, true });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Avatar", "DefaultIsr", "DefaultPhoneIncentive", "DefaultTss", "IsSuperAdmin" },
                values: new object[] { "", 873.0m, 1000.0m, 1088.69m, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Avatar", "DefaultIsr", "DefaultPhoneIncentive", "DefaultTss", "IsSuperAdmin" },
                values: new object[] { "", 873.0m, 1000.0m, 1088.69m, false });
        }
    }
}
