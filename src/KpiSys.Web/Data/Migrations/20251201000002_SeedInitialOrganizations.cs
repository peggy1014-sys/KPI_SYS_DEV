using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KpiSys.Web.Data.Migrations
{
    public partial class SeedInitialOrganizations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_004_003");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_004_002");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_004_001");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_004");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_003");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_002_003");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_002_002");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_002_001");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_002");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_001_003");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_001_002");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_001_001");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_001");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB");

            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "orgId", "createdAt", "isActive", "orgLevel", "orgName", "parentOrgId", "portfolioCode", "updatedAt" },
                values: new object[,]
                {
                    { "QTB", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1, "元信達資訊", null, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, "應用系統開發部", "QTB", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_001_001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "應用系統開發部 一組", "QTB_001", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_001_002", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "應用系統開發部 二組", "QTB_001", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_001_003", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "應用系統開發部 三組", "QTB_001", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_002", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, "營運數據系統部", "QTB", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_002_001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "營運數據系統部 一組", "QTB_002", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_002_002", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "營運數據系統部 二組", "QTB_002", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_002_003", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "營運數據系統部 三組", "QTB_002", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_003", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, "創新專案管理", "QTB", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_004", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, "資訊安全部", "QTB", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_004_001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "資訊安全部 一組", "QTB_004", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_004_002", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "資訊安全部 二組", "QTB_004", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "QTB_004_003", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "資訊安全部 三組", "QTB_004", null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_004_003");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_004_002");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_004_001");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_004");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_003");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_002_003");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_002_002");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_002_001");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_002");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_001_003");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_001_002");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_001_001");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB_001");

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "orgId",
                keyValue: "QTB");

            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "orgId", "createdAt", "isActive", "orgLevel", "orgName", "parentOrgId", "portfolioCode", "updatedAt" },
                values: new object[,]
                {
                    { "QTB", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1, "元信達資訊", null, null, null },
                    { "QTB_001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, "應用系統開發部", "QTB", null, null },
                    { "QTB_001_001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "應用系統開發部 一組", "QTB_001", null, null },
                    { "QTB_001_002", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "應用系統開發部 二組", "QTB_001", null, null },
                    { "QTB_001_003", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "應用系統開發部 三組", "QTB_001", null, null },
                    { "QTB_002", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, "營運數據系統部", "QTB", null, null },
                    { "QTB_002_001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "營運數據系統部 一組", "QTB_002", null, null },
                    { "QTB_002_002", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "營運數據系統部 二組", "QTB_002", null, null },
                    { "QTB_002_003", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "營運數據系統部 三組", "QTB_002", null, null },
                    { "QTB_003", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, "創新專案管理", "QTB", null, null },
                    { "QTB_004", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, "資訊安全部", "QTB", null, null },
                    { "QTB_004_001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "資訊安全部 一組", "QTB_004", null, null },
                    { "QTB_004_002", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "資訊安全部 二組", "QTB_004", null, null },
                    { "QTB_004_003", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3, "資訊安全部 三組", "QTB_004", null, null }
                });
        }
    }
}
