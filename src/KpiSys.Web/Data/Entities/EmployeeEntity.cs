using System;
using System.Collections.Generic;

namespace KpiSys.Web.Data.Entities
{
    public class EmployeeEntity
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeNo { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string OrgId { get; set; } = string.Empty;
        public string? SupervisorId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public OrganizationEntity? Organization { get; set; }
        public EmployeeEntity? Supervisor { get; set; }
        public ICollection<EmployeeRoleEntity> Roles { get; set; } = new List<EmployeeRoleEntity>();
    }
}
