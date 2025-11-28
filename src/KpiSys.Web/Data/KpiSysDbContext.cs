using System;
using System.Collections.Generic;
using KpiSys.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KpiSys.Web.Data
{
    public class KpiSysDbContext : DbContext
    {
        public KpiSysDbContext(DbContextOptions<KpiSysDbContext> options) : base(options)
        {
        }

        public DbSet<OrganizationEntity> Organizations { get; set; } = null!;
        public DbSet<EmployeeEntity> Employees { get; set; } = null!;
        public DbSet<EmployeeRoleEntity> EmployeeRoles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OrganizationEntity>(entity =>
            {
                entity.ToTable("organizations");
                entity.HasKey(e => e.OrgId);
                entity.Property(e => e.OrgId)
                    .HasMaxLength(36)
                    .HasColumnName("orgId")
                    .IsRequired();
                entity.Property(e => e.OrgName)
                    .HasMaxLength(100)
                    .IsRequired()
                    .HasColumnName("orgName");
                entity.Property(e => e.ParentOrgId)
                    .HasMaxLength(36)
                    .HasColumnName("parentOrgId");
                entity.Property(e => e.PortfolioCode)
                    .HasMaxLength(50)
                    .HasColumnName("portfolioCode");
                entity.Property(e => e.OrgLevel)
                    .HasColumnName("orgLevel");
                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true)
                    .HasColumnName("isActive");
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()")
                    .HasColumnName("createdAt");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updatedAt");

                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentOrgId)
                    .HasPrincipalKey(e => e.OrgId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasData(SeedOrganizations());
            });

            modelBuilder.Entity<EmployeeEntity>(entity =>
            {
                entity.ToTable("employees");
                entity.HasKey(e => e.EmployeeId);
                entity.Property(e => e.EmployeeId).HasColumnName("employeeId");
                entity.Property(e => e.EmployeeNo).HasColumnName("employeeNo");
                entity.Property(e => e.EmployeeName).HasColumnName("employeeName");
                entity.Property(e => e.OrgId).HasColumnName("orgId");
                entity.Property(e => e.SupervisorId).HasColumnName("supervisorId");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
                entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");

                entity.HasOne(e => e.Organization)
                    .WithMany(o => o.Employees)
                    .HasForeignKey(e => e.OrgId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Supervisor)
                    .WithMany()
                    .HasForeignKey(e => e.SupervisorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeRoleEntity>(entity =>
            {
                entity.ToTable("employeeRoles");
                entity.HasKey(e => e.EmployeeRoleId);
                entity.Property(e => e.EmployeeRoleId).HasColumnName("employeeRoleId");
                entity.Property(e => e.EmployeeId).HasColumnName("employeeId");
                entity.Property(e => e.RoleCode).HasColumnName("roleCode");
                entity.Property(e => e.IsPrimary).HasColumnName("isPrimary");
                entity.Property(e => e.CreatedAt).HasColumnName("createdAt");

                entity.HasOne(e => e.Employee)
                    .WithMany(e => e.Roles)
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static IEnumerable<OrganizationEntity> SeedOrganizations()
        {
            var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var updatedAt = createdAt;

            return new List<OrganizationEntity>
            {
                new() { OrgId = "QTB", OrgName = "元信達資訊", OrgLevel = 1, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_001", OrgName = "應用系統開發部", ParentOrgId = "QTB", OrgLevel = 2, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_001_001", OrgName = "應用系統開發部 一組", ParentOrgId = "QTB_001", OrgLevel = 3, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_001_002", OrgName = "應用系統開發部 二組", ParentOrgId = "QTB_001", OrgLevel = 3, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_001_003", OrgName = "應用系統開發部 三組", ParentOrgId = "QTB_001", OrgLevel = 3, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_002", OrgName = "營運數據系統部", ParentOrgId = "QTB", OrgLevel = 2, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_002_001", OrgName = "營運數據系統部 一組", ParentOrgId = "QTB_002", OrgLevel = 3, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_002_002", OrgName = "營運數據系統部 二組", ParentOrgId = "QTB_002", OrgLevel = 3, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_002_003", OrgName = "營運數據系統部 三組", ParentOrgId = "QTB_002", OrgLevel = 3, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_003", OrgName = "創新專案管理", ParentOrgId = "QTB", OrgLevel = 2, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_004", OrgName = "資訊安全部", ParentOrgId = "QTB", OrgLevel = 2, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_004_001", OrgName = "資訊安全部 一組", ParentOrgId = "QTB_004", OrgLevel = 3, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_004_002", OrgName = "資訊安全部 二組", ParentOrgId = "QTB_004", OrgLevel = 3, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt },
                new() { OrgId = "QTB_004_003", OrgName = "資訊安全部 三組", ParentOrgId = "QTB_004", OrgLevel = 3, IsActive = true, CreatedAt = createdAt, UpdatedAt = updatedAt }
            };
        }
    }
}
