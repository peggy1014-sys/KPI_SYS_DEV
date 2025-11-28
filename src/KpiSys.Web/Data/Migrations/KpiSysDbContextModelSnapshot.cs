using System;
using KpiSys.Web.Data;
using KpiSys.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace KpiSys.Web.Data.Migrations
{
    [DbContext(typeof(KpiSysDbContext))]
    partial class KpiSysDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("KpiSys.Web.Data.Entities.EmployeeEntity", b =>
                {
                    b.Property<string>("EmployeeId")
                        .HasColumnType("nvarchar(450)")
                        .HasColumnName("employeeId");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("createdAt");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("email");

                    b.Property<string>("EmployeeName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("employeeName");

                    b.Property<string>("EmployeeNo")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("employeeNo");

                    b.Property<string>("OrgId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)")
                        .HasColumnName("orgId");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("status");

                    b.Property<string>("SupervisorId")
                        .HasColumnType("nvarchar(450)")
                        .HasColumnName("supervisorId");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updatedAt");

                    b.HasKey("EmployeeId");

                    b.HasIndex("OrgId");

                    b.HasIndex("SupervisorId");

                    b.ToTable("employees", (string)null);
                });

            modelBuilder.Entity("KpiSys.Web.Data.Entities.EmployeeRoleEntity", b =>
                {
                    b.Property<string>("EmployeeRoleId")
                        .HasColumnType("nvarchar(450)")
                        .HasColumnName("employeeRoleId");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("createdAt");

                    b.Property<string>("EmployeeId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)")
                        .HasColumnName("employeeId");

                    b.Property<bool>("IsPrimary")
                        .HasColumnType("bit")
                        .HasColumnName("isPrimary");

                    b.Property<string>("RoleCode")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("roleCode");

                    b.HasKey("EmployeeRoleId");

                    b.HasIndex("EmployeeId");

                    b.ToTable("employeeRoles", (string)null);
                });

            modelBuilder.Entity("KpiSys.Web.Data.Entities.OrganizationEntity", b =>
                {
                    b.Property<string>("OrgId")
                        .HasColumnType("nvarchar(450)")
                        .HasColumnName("orgId");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("createdAt");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit")
                        .HasColumnName("isActive");

                    b.Property<int?>("OrgLevel")
                        .HasColumnType("int")
                        .HasColumnName("orgLevel");

                    b.Property<string>("OrgName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("orgName");

                    b.Property<string>("ParentOrgId")
                        .HasColumnType("nvarchar(450)")
                        .HasColumnName("parentOrgId");

                    b.Property<string>("PortfolioCode")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("portfolioCode");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2")
                        .HasColumnName("updatedAt");

                    b.HasKey("OrgId");

                    b.HasIndex("ParentOrgId");

                    b.ToTable("organizations", (string)null);

                    b.HasData(
                        new
                        {
                            OrgId = "QTB",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 1,
                            OrgName = "元信達資訊"
                        },
                        new
                        {
                            OrgId = "QTB_001",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 2,
                            OrgName = "應用系統開發部",
                            ParentOrgId = "QTB"
                        },
                        new
                        {
                            OrgId = "QTB_001_001",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 3,
                            OrgName = "應用系統開發部 一組",
                            ParentOrgId = "QTB_001"
                        },
                        new
                        {
                            OrgId = "QTB_001_002",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 3,
                            OrgName = "應用系統開發部 二組",
                            ParentOrgId = "QTB_001"
                        },
                        new
                        {
                            OrgId = "QTB_001_003",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 3,
                            OrgName = "應用系統開發部 三組",
                            ParentOrgId = "QTB_001"
                        },
                        new
                        {
                            OrgId = "QTB_002",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 2,
                            OrgName = "營運數據系統部",
                            ParentOrgId = "QTB"
                        },
                        new
                        {
                            OrgId = "QTB_002_001",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 3,
                            OrgName = "營運數據系統部 一組",
                            ParentOrgId = "QTB_002"
                        },
                        new
                        {
                            OrgId = "QTB_002_002",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 3,
                            OrgName = "營運數據系統部 二組",
                            ParentOrgId = "QTB_002"
                        },
                        new
                        {
                            OrgId = "QTB_002_003",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 3,
                            OrgName = "營運數據系統部 三組",
                            ParentOrgId = "QTB_002"
                        },
                        new
                        {
                            OrgId = "QTB_003",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 2,
                            OrgName = "創新專案管理",
                            ParentOrgId = "QTB"
                        },
                        new
                        {
                            OrgId = "QTB_004",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 2,
                            OrgName = "資訊安全部",
                            ParentOrgId = "QTB"
                        },
                        new
                        {
                            OrgId = "QTB_004_001",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 3,
                            OrgName = "資訊安全部 一組",
                            ParentOrgId = "QTB_004"
                        },
                        new
                        {
                            OrgId = "QTB_004_002",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 3,
                            OrgName = "資訊安全部 二組",
                            ParentOrgId = "QTB_004"
                        },
                        new
                        {
                            OrgId = "QTB_004_003",
                            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            IsActive = true,
                            OrgLevel = 3,
                            OrgName = "資訊安全部 三組",
                            ParentOrgId = "QTB_004"
                        });
                });

            modelBuilder.Entity("KpiSys.Web.Data.Entities.EmployeeEntity", b =>
                {
                    b.HasOne("KpiSys.Web.Data.Entities.OrganizationEntity", "Organization")
                        .WithMany("Employees")
                        .HasForeignKey("OrgId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KpiSys.Web.Data.Entities.EmployeeEntity", "Supervisor")
                        .WithMany()
                        .HasForeignKey("SupervisorId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Organization");

                    b.Navigation("Supervisor");
                });

            modelBuilder.Entity("KpiSys.Web.Data.Entities.EmployeeRoleEntity", b =>
                {
                    b.HasOne("KpiSys.Web.Data.Entities.EmployeeEntity", "Employee")
                        .WithMany("Roles")
                        .HasForeignKey("EmployeeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Employee");
                });

            modelBuilder.Entity("KpiSys.Web.Data.Entities.OrganizationEntity", b =>
                {
                    b.HasOne("KpiSys.Web.Data.Entities.OrganizationEntity", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentOrgId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Children");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("KpiSys.Web.Data.Entities.EmployeeEntity", b =>
                {
                    b.Navigation("Roles");
                });

            modelBuilder.Entity("KpiSys.Web.Data.Entities.OrganizationEntity", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Employees");
                });
#pragma warning restore 612, 618
        }
    }
}
