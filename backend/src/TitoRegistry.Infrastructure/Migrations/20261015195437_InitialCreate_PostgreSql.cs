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
                    { 1, "CHO-001", 25.0m, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Ruta Norte (Santiago)", "tito.gonzalez@titotransporte.com", "Tito González", true, "+1 (809) 555-0101", "Conductor Ejecutivo de Rutas", null },
                    { 2, "CHO-002", 22.0m, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Ruta Cibao Central", "sofia.ramirez@titotransporte.com", "Sofía Ramírez", true, "+1 (809) 555-0102", "Conductora de Transporte Interurbano", null },
                    { 3, "CHO-003", 20.0m, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Transporte de Encomiendas", "carlos.mendoza@titotransporte.com", "Carlos Mendoza", true, "+1 (809) 555-0103", "Operador de Carga y Fletes", null }
                });

            migrationBuilder.InsertData(
                table: "ServiceTypes",
                columns: new[] { "Id", "Category", "CreatedAt", "DefaultPrice", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Viajes Interurbanos", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 180.0m, "Servicio de transporte privado puerta a puerta hacia Santiago de los Caballeros en vehículo de confort.", true, "Traslado Privado / Ejecutivo a Santiago", null },
                    { 2, "Viajes Interurbanos", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 95.0m, "Servicio directo en van/minibús con paradas programadas hacia Santiago.", true, "Transporte Expreso de Pasajeros (Ruta Santiago)", null },
                    { 3, "Carga y Encomiendas", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 45.0m, "Transporte y entrega de paquetes, valijas y documentos el mismo día en Santiago.", true, "Envío de Paquetería y Encomienda Expresa a Santiago", null },
                    { 4, "Carga y Encomiendas", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 280.0m, "Transporte de mercancía comercial o carga ligera en vehículo utilitario cerrado.", true, "Flete y Carga Liviana Interprovincial", null },
                    { 5, "Turismo & Aeropuerto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 130.0m, "Servicio especializado de recogida y traslado desde o hacia el Aeropuerto del Cibao (STI).", true, "Traslado Turístico / Aeropuerto (Santiago - Cibao)", null },
                    { 6, "Servicios Especiales", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 350.0m, "Disponibilidad completa de chofer y vehículo para gestiones corporativas o familiares en Santiago.", true, "Alquiler de Vehículo con Conductor por Día (Santiago y Región)", null }
                });

            migrationBuilder.InsertData(
                table: "ServiceRecords",
                columns: new[] { "Id", "ClientAddress", "ClientName", "ClientPhone", "CreatedAt", "EmployeeEarnings", "EmployeeId", "Notes", "PriceCharged", "ServiceCode", "ServiceDate", "ServiceTypeId", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Santo Domingo -> Los Jardines Metropolitanos, Santiago", "Ing. Manuel De Los Santos", "+1 (809) 555-1020", new DateTime(2026, 9, 18, 12, 0, 0, 0, DateTimeKind.Utc), 45.0m, 1, "Viaje ejecutivo de ida a Santiago sin contratiempos.", 180.0m, "SRV-2026-001", new DateTime(2026, 9, 18, 8, 30, 0, 0, DateTimeKind.Utc), 1, 3, null },
                    { 2, "Av. Juan Pablo Duarte #50, Santiago", "Grupo Farmacéutico Cibao", "+1 (809) 555-2040", new DateTime(2026, 10, 6, 18, 30, 0, 0, DateTimeKind.Utc), 87.5m, 1, "Recorrido de sucursales en Santiago durante toda la jornada.", 350.0m, "SRV-2026-002", new DateTime(2026, 10, 6, 9, 0, 0, 0, DateTimeKind.Utc), 6, 3, null },
                    { 3, "Zona Franca Industrial Víctor Espaillat Mera, Santiago", "Distribuidora Nacional del Norte", "+1 (809) 555-3050", new DateTime(2026, 10, 14, 15, 0, 0, 0, DateTimeKind.Utc), 12.5m, 1, "Entrega de documentos prioritarios con firma de recepción.", 50.0m, "SRV-2026-003", new DateTime(2026, 10, 14, 11, 0, 0, 0, DateTimeKind.Utc), 3, 3, null }
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
