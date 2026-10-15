using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TitoRegistry.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_PostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

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
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    ServiceTypeId = table.Column<int>(type: "integer", nullable: false),
                    ClientName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ClientAddress = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ClientPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PriceCharged = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployeeEarnings = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_ServiceTypes_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Code", "CommissionRate", "CreatedAt", "Department", "Email", "FullName", "IsActive", "Phone", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "EMP-001", 25.0m, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Infraestructura & Redes", "tito.gonzalez@empresa.com", "Tito González", true, "+1 (555) 234-8901", "Especialista Técnico Senior", null },
                    { 2, "EMP-002", 22.0m, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Soporte Técnico", "sofia.ramirez@empresa.com", "Sofía Ramírez", true, "+1 (555) 456-1122", "Ingeniera de Soporte", null },
                    { 3, "EMP-003", 20.0m, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mantenimiento", "carlos.mendoza@empresa.com", "Carlos Mendoza", true, "+1 (555) 789-3344", "Técnico de Campo", null }
                });

            migrationBuilder.InsertData(
                table: "ServiceTypes",
                columns: new[] { "Id", "Category", "CreatedAt", "DefaultPrice", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Redes", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 450.0m, "Montaje de rack, cableado estructurado Cat6A/Fibra y configuración de routers.", true, "Instalación de Fibra Óptica y Enlace de Red", null },
                    { 2, "Mantenimiento", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 320.0m, "Limpieza física, revisión de redundancia, actualización de firmware y logs.", true, "Mantenimiento Preventivo de Servidores", null },
                    { 3, "Soporte", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 180.0m, "Reemplazo de componentes, fuentes de poder y pruebas de banco.", true, "Diagnóstico y Reparación de Hardware", null },
                    { 4, "Seguridad", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 520.0m, "Configuración de NVR, cámaras PoE y acceso móvil remoto.", true, "Instalación de Sistema CCTV / Cámaras IP", null }
                });

            migrationBuilder.InsertData(
                table: "ServiceRecords",
                columns: new[] { "Id", "ClientAddress", "ClientName", "ClientPhone", "CreatedAt", "EmployeeEarnings", "EmployeeId", "Notes", "PriceCharged", "ServiceCode", "ServiceDate", "ServiceTypeId", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Av. Principal #405", "Corporación Andina S.A.", "+1 555-901-2001", new DateTime(2026, 9, 15, 12, 0, 0, 0, DateTimeKind.Utc), 120.0m, 1, "Instalación de 8 puntos de red y prueba de ancho de banda.", 480.0m, "SRV-2026-001", new DateTime(2026, 9, 15, 10, 0, 0, 0, DateTimeKind.Utc), 1, 3, null },
                    { 2, "Zona Industrial Este, Galpón 4", "Logística Express del Norte", "+1 555-901-2002", new DateTime(2026, 10, 5, 18, 0, 0, 0, DateTimeKind.Utc), 137.5m, 1, "Instalación de 6 cámaras IP con visión nocturna calibrada.", 550.0m, "SRV-2026-002", new DateTime(2026, 10, 5, 14, 0, 0, 0, DateTimeKind.Utc), 4, 3, null },
                    { 3, "Torre Empresarial, Nivel 14", "Financiera Futuro", "+1 555-901-2003", new DateTime(2026, 10, 12, 13, 0, 0, 0, DateTimeKind.Utc), 87.5m, 1, "Mantenimiento preventivo semestral de servidores en rack.", 350.0m, "SRV-2026-003", new DateTime(2026, 10, 12, 9, 0, 0, 0, DateTimeKind.Utc), 2, 3, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Code",
                table: "Employees",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_EmployeeId_ServiceDate",
                table: "ServiceRecords",
                columns: new[] { "EmployeeId", "ServiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ServiceCode",
                table: "ServiceRecords",
                column: "ServiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_ServiceTypeId",
                table: "ServiceRecords",
                column: "ServiceTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceRecords");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "ServiceTypes");
        }
    }
}
