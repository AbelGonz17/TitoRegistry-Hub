using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TitoRegistry.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeededAdministrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Code", "CommissionRate", "CreatedAt", "DeletedAt", "Department", "Email", "FullName", "IsActive", "IsDeleted", "PasswordHash", "Phone", "Role", "UpdatedAt" },
                values: new object[] { 4, "ADM-002", 0m, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), null, "Administración & Nómina", "administracion@titotransporte.com", "Laura Martínez", true, false, "$2a$11$lzkTanuFle3ZoE9lYVea.erqLqWX8aW.hRRj0S/RiQ2v.G0NofU.a", "+1 (809) 555-0103", "Administrador", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
