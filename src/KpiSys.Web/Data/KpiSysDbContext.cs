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
    }
}
