using System;

namespace KpiSys.Web.Data.Entities
{
    public class EmployeeRoleEntity
    {
        public string EmployeeRoleId { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }

        public EmployeeEntity? Employee { get; set; }
    }
}
