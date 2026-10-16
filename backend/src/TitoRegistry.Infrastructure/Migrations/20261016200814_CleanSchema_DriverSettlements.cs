using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TitoRegistry.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanSchema_DriverSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DefaultPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    Avatar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DefaultPhoneIncentive = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultIsr = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultTss = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverSettlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    TotalGrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CompanyRetentionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CompanyRetentionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OperationalSubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DriverCommissionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsrDeduction = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TssDeduction = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PhoneIncentive = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherIncentives = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherDeductions = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalNetPayable = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverSettlements_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    ServiceTypeId = table.Column<int>(type: "integer", nullable: false),
                    ClientReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PriceCharged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_ServiceTypes_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ServiceTypes",
                columns: new[] { "Id", "Category", "CreatedAt", "DefaultPrice", "DeletedAt", "Description", "IsActive", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Urbano", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1500m, null, "Servicio de transporte local en la ciudad (ej: traslados, personal)", true, false, "Servicio en la Ciudad", null },
                    { 2, "Interurbano", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9000m, null, "Viajes al interior del país (Samaná, Punta Cana, Santiago, Las Terrenas, etc.)", true, false, "Servicio Al Interior", null },
                    { 3, "Aeropuerto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2500m, null, "Traslados hacia y desde aeropuertos nacionales e internacionales", true, false, "Aereopuerto", null },
                    { 4, "Corporativo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 42500m, null, "Servicio de transporte de ruta fija mensual matutina y vespertina", true, false, "Rutas Fijas", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Avatar", "Code", "CommissionRate", "CreatedAt", "DefaultIsr", "DefaultPhoneIncentive", "DefaultTss", "DeletedAt", "Department", "Email", "FullName", "IsActive", "IsDeleted", "IsSuperAdmin", "PasswordHash", "Phone", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "", "ADM-001", 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0m, 0m, 0m, null, "Dirección General & Propietario", "admin@titotransporte.com", "Tito González", true, false, true, "$2a$11$lzkTanuFle3ZoE9lYVea.erqLqWX8aW.hRRj0S/RiQ2v.G0NofU.a", "+1 (809) 555-0100", "SuperAdmin", null },
                    { 2, "", "CHO-001", 25.0m, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), 873.0m, 1000.0m, 1088.69m, null, "Transporte Urbano e Interurbano", "browel@titotransporte.com", "Browel", true, false, false, "$2a$11$lzkTanuFle3ZoE9lYVea.erqLqWX8aW.hRRj0S/RiQ2v.G0NofU.a", "+1 (809) 555-0101", "Chofer", null },
                    { 3, "", "CHO-002", 25.0m, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), 873.0m, 1000.0m, 1088.69m, null, "Rutas Fijas Corporativas", "denny@titotransporte.com", "Denny", true, false, false, "$2a$11$lzkTanuFle3ZoE9lYVea.erqLqWX8aW.hRRj0S/RiQ2v.G0NofU.a", "+1 (809) 555-0102", "Chofer", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverSettlements_DriverId_Year_Month",
                table: "DriverSettlements",
                columns: new[] { "DriverId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_DriverId_ServiceDate",
                table: "ServiceRecords",
                columns: new[] { "DriverId", "ServiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ServiceCode",
                table: "ServiceRecords",
                column: "ServiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ServiceTypeId",
                table: "ServiceRecords",
                column: "ServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Code",
                table: "Users",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverSettlements");

            migrationBuilder.DropTable(
                name: "ServiceRecords");

            migrationBuilder.DropTable(
                name: "ServiceTypes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
