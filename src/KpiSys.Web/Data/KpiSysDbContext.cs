using System;
using KpiSys.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KpiSys.Web.Data;

public class KpiSysDbContext : DbContext
{
    public KpiSysDbContext(DbContextOptions<KpiSysDbContext> options) : base(options)
    {
    }

    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<EmployeeRoleEntity> EmployeeRoles => Set<EmployeeRoleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationEntity>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(e => e.OrgId);
            entity.Property(e => e.OrgId).HasColumnName("orgId");
            entity.Property(e => e.OrgName).HasColumnName("orgName");
            entity.Property(e => e.ParentOrgId).HasColumnName("parentOrgId");
            entity.Property(e => e.PortfolioCode).HasColumnName("portfolioCode");
            entity.Property(e => e.OrgLevel).HasColumnName("orgLevel");
            entity.Property(e => e.IsActive).HasColumnName("isActive");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt");
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
                .HasForeignKey(e => e.OrgId);

            entity.HasOne(e => e.Supervisor)
                .WithMany()
                .HasForeignKey(e => e.SupervisorId);
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
                .HasForeignKey(e => e.EmployeeId);
        });
    }
}

namespace KpiSys.Web.Data.Entities;

public class OrganizationEntity
{
    public Guid OrgId { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public Guid? ParentOrgId { get; set; }
    public string? PortfolioCode { get; set; }
    public int OrgLevel { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<EmployeeEntity> Employees { get; set; } = new List<EmployeeEntity>();
}

public class EmployeeEntity
{
    public Guid EmployeeId { get; set; }
    public string EmployeeNo { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public Guid OrgId { get; set; }
    public Guid? SupervisorId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public OrganizationEntity? Organization { get; set; }
    public EmployeeEntity? Supervisor { get; set; }
    public ICollection<EmployeeRoleEntity> Roles { get; set; } = new List<EmployeeRoleEntity>();
}

public class EmployeeRoleEntity
{
    public Guid EmployeeRoleId { get; set; }
    public Guid EmployeeId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }

    public EmployeeEntity? Employee { get; set; }
}
