using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiSys.Web.Data.Migrations
{
    public partial class AddOrganizations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    orgId = table.Column<string>(type: "TEXT", nullable: false),
                    orgName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    parentOrgId = table.Column<string>(type: "TEXT", nullable: true),
                    portfolioCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    orgLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    isActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.orgId);
                    table.ForeignKey(
                        name: "FK_organizations_organizations_parentOrgId",
                        column: x => x.parentOrgId,
                        principalTable: "organizations",
                        principalColumn: "orgId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    employeeId = table.Column<string>(type: "TEXT", nullable: false),
                    employeeNo = table.Column<string>(type: "TEXT", nullable: false),
                    employeeName = table.Column<string>(type: "TEXT", nullable: false),
                    orgId = table.Column<string>(type: "TEXT", nullable: false),
                    supervisorId = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    email = table.Column<string>(type: "TEXT", nullable: true),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.employeeId);
                    table.ForeignKey(
                        name: "FK_employees_employees_supervisorId",
                        column: x => x.supervisorId,
                        principalTable: "employees",
                        principalColumn: "employeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_organizations_orgId",
                        column: x => x.orgId,
                        principalTable: "organizations",
                        principalColumn: "orgId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employeeRoles",
                columns: table => new
                {
                    employeeRoleId = table.Column<string>(type: "TEXT", nullable: false),
                    employeeId = table.Column<string>(type: "TEXT", nullable: false),
                    roleCode = table.Column<string>(type: "TEXT", nullable: false),
                    isPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employeeRoles", x => x.employeeRoleId);
                    table.ForeignKey(
                        name: "FK_employeeRoles_employees_employeeId",
                        column: x => x.employeeId,
                        principalTable: "employees",
                        principalColumn: "employeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "orgId", "createdAt", "isActive", "orgLevel", "orgName", "parentOrgId", "portfolioCode", "updatedAt" },
                values: new object[,]
                {
                    { "QTB", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 1, "元信達資訊", null, null, null },
                    { "QTB_001", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 2, "應用系統開發部", "QTB", null, null },
                    { "QTB_001_001", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 3, "應用系統開發部 一組", "QTB_001", null, null },
                    { "QTB_001_002", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 3, "應用系統開發部 二組", "QTB_001", null, null },
                    { "QTB_001_003", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 3, "應用系統開發部 三組", "QTB_001", null, null },
                    { "QTB_002", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 2, "營運數據系統部", "QTB", null, null },
                    { "QTB_002_001", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 3, "營運數據系統部 一組", "QTB_002", null, null },
                    { "QTB_002_002", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 3, "營運數據系統部 二組", "QTB_002", null, null },
                    { "QTB_002_003", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 3, "營運數據系統部 三組", "QTB_002", null, null },
                    { "QTB_003", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 2, "創新專案管理", "QTB", null, null },
                    { "QTB_004", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 2, "資訊安全部", "QTB", null, null },
                    { "QTB_004_001", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 3, "資訊安全部 一組", "QTB_004", null, null },
                    { "QTB_004_002", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 3, "資訊安全部 二組", "QTB_004", null, null },
                    { "QTB_004_003", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, 3, "資訊安全部 三組", "QTB_004", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_employeeRoles_employeeId",
                table: "employeeRoles",
                column: "employeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_orgId",
                table: "employees",
                column: "orgId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_supervisorId",
                table: "employees",
                column: "supervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_parentOrgId",
                table: "organizations",
                column: "parentOrgId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employeeRoles");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
