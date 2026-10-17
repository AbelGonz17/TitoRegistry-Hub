using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TitoRegistry.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicBenefitsAndSettlementAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Benefits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverBenefits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    BenefitId = table.Column<int>(type: "integer", nullable: false),
                    CustomAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverBenefits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverBenefits_Benefits_BenefitId",
                        column: x => x.BenefitId,
                        principalTable: "Benefits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverBenefits_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SettlementAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverSettlementId = table.Column<int>(type: "integer", nullable: false),
                    BenefitId = table.Column<int>(type: "integer", nullable: true),
                    Concept = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementAdjustments_Benefits_BenefitId",
                        column: x => x.BenefitId,
                        principalTable: "Benefits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SettlementAdjustments_DriverSettlements_DriverSettlementId",
                        column: x => x.DriverSettlementId,
                        principalTable: "DriverSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Benefits",
                columns: new[] { "Id", "Category", "Code", "CreatedAt", "DefaultAmount", "DeletedAt", "Description", "IsActive", "IsDeleted", "IsRecurring", "Name", "Type", "UpdatedAt" },
                values: new object[] { 1, "Flota", "BEN-TEL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000m, null, "Incentivo mensual por disponibilidad y uso de teléfono móvil corporativo", true, false, true, "Incentivo Teléfono", 1, null });

            migrationBuilder.InsertData(
                table: "DriverBenefits",
                columns: new[] { "Id", "BenefitId", "CreatedAt", "CustomAmount", "DeletedAt", "DriverId", "IsActive", "IsDeleted", "Notes", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1000m, null, 2, true, false, "Incentivo mensual estándar de flota", null },
                    { 2, 1, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), 1000m, null, 3, true, false, "Incentivo mensual estándar de flota", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_Code",
                table: "Benefits",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverBenefits_BenefitId",
                table: "DriverBenefits",
                column: "BenefitId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverBenefits_DriverId_BenefitId",
                table: "DriverBenefits",
                columns: new[] { "DriverId", "BenefitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementAdjustments_BenefitId",
                table: "SettlementAdjustments",
                column: "BenefitId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementAdjustments_DriverSettlementId",
                table: "SettlementAdjustments",
                column: "DriverSettlementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverBenefits");

            migrationBuilder.DropTable(
                name: "SettlementAdjustments");

            migrationBuilder.DropTable(
                name: "Benefits");
        }
    }
}
