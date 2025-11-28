using System;
using System.Collections.Generic;

namespace KpiSys.Web.Data.Entities
{
    public class OrganizationEntity
    {
        public string OrgId { get; set; } = string.Empty;
        public string OrgName { get; set; } = string.Empty;
        public string? ParentOrgId { get; set; }
        public string? PortfolioCode { get; set; }
        public int OrgLevel { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<EmployeeEntity> Employees { get; set; } = new List<EmployeeEntity>();
    }
}
