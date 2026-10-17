using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TitoRegistry.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverPayrollConfigOptionA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverPayrollConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    ReportedSalary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FixedTssAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FixedIsrAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsExemptTss = table.Column<bool>(type: "boolean", nullable: false),
                    IsExemptIsr = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverPayrollConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverPayrollConfigs_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DriverPayrollConfigs",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "DriverId", "FixedIsrAmount", "FixedTssAmount", "IsDeleted", "IsExemptIsr", "IsExemptTss", "Notes", "ReportedSalary", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, 873.00m, 1088.69m, false, false, false, "Salario cotizable base SUIR TSS RD$ 18,421.15 y retención ISR", 18421.15m, null },
                    { 2, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, 873.00m, 1088.69m, false, false, false, "Salario cotizable base SUIR TSS RD$ 18,421.15 y retención ISR", 18421.15m, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverPayrollConfigs_DriverId",
                table: "DriverPayrollConfigs",
                column: "DriverId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverPayrollConfigs");
        }
    }
}
