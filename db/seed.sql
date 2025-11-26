-- Seed data for lookup tables and minimal scaffolding

-- Lookup groups
INSERT INTO lookup_groups (group_key, name, description) VALUES
('timesheet_status', 'Timesheet Status', 'Workflow states for timesheets'),
('project_status', 'Project Status', 'Lifecycle states for projects'),
('task_status', 'Task Status', 'Lifecycle states for tasks'),
('task_priority', 'Task Priority', 'Priority for tasks'),
('kpi_period_status', 'KPI Period Status', 'Status of KPI cycles'),
('employment_status', 'Employment Status', 'Worker lifecycle');

-- Lookup values
INSERT INTO lookup_values (group_id, value_key, name, sort_order) VALUES
((SELECT id FROM lookup_groups WHERE group_key = 'timesheet_status'), 'DRAFT', 'Draft', 1),
((SELECT id FROM lookup_groups WHERE group_key = 'timesheet_status'), 'SUBMITTED', 'Submitted', 2),
((SELECT id FROM lookup_groups WHERE group_key = 'timesheet_status'), 'APPROVED', 'Approved', 3),
((SELECT id FROM lookup_groups WHERE group_key = 'timesheet_status'), 'REJECTED', 'Rejected', 4),
((SELECT id FROM lookup_groups WHERE group_key = 'project_status'), 'DRAFT', 'Draft', 1),
((SELECT id FROM lookup_groups WHERE group_key = 'project_status'), 'ACTIVE', 'Active', 2),
((SELECT id FROM lookup_groups WHERE group_key = 'project_status'), 'ON_HOLD', 'On Hold', 3),
((SELECT id FROM lookup_groups WHERE group_key = 'project_status'), 'CLOSED', 'Closed', 4),
((SELECT id FROM lookup_groups WHERE group_key = 'task_status'), 'OPEN', 'Open', 1),
((SELECT id FROM lookup_groups WHERE group_key = 'task_status'), 'IN_PROGRESS', 'In Progress', 2),
((SELECT id FROM lookup_groups WHERE group_key = 'task_status'), 'DONE', 'Done', 3),
((SELECT id FROM lookup_groups WHERE group_key = 'task_status'), 'CANCELLED', 'Cancelled', 4),
((SELECT id FROM lookup_groups WHERE group_key = 'task_priority'), 'LOW', 'Low', 1),
((SELECT id FROM lookup_groups WHERE group_key = 'task_priority'), 'NORMAL', 'Normal', 2),
((SELECT id FROM lookup_groups WHERE group_key = 'task_priority'), 'HIGH', 'High', 3),
((SELECT id FROM lookup_groups WHERE group_key = 'task_priority'), 'URGENT', 'Urgent', 4),
((SELECT id FROM lookup_groups WHERE group_key = 'kpi_period_status'), 'OPEN', 'Open', 1),
((SELECT id FROM lookup_groups WHERE group_key = 'kpi_period_status'), 'CALCULATED', 'Calculated', 2),
((SELECT id FROM lookup_groups WHERE group_key = 'kpi_period_status'), 'LOCKED', 'Locked', 3),
((SELECT id FROM lookup_groups WHERE group_key = 'employment_status'), 'ACTIVE', 'Active', 1),
((SELECT id FROM lookup_groups WHERE group_key = 'employment_status'), 'ON_LEAVE', 'On Leave', 2),
((SELECT id FROM lookup_groups WHERE group_key = 'employment_status'), 'INACTIVE', 'Inactive', 3);

-- Minimal org/role scaffolding
INSERT INTO departments (code, name) VALUES ('HQ', 'Headquarters')
ON CONFLICT (code) DO NOTHING;

INSERT INTO roles (code, name, level, cost_rate) VALUES
('PM', 'Project Manager', 3, 1200),
('SD', 'Senior Developer', 2, 900),
('PG', 'Programmer', 1, 700)
ON CONFLICT (code) DO NOTHING;

-- Example admin user placeholder (password_hash intentionally blank)
INSERT INTO users (employee_no, email, full_name, department_id, role_id, employment_status)
VALUES ('E0001', 'admin@example.com', 'System Admin',
        (SELECT id FROM departments WHERE code = 'HQ'),
        (SELECT id FROM roles WHERE code = 'PM'),
        'ACTIVE')
ON CONFLICT (employee_no) DO NOTHING;

-- Initial KPI period and config template
INSERT INTO kpi_periods (period_code, name, start_date, end_date, status)
VALUES ('2025-Q1', '2025 Q1', '2025-01-01', '2025-03-31', 'OPEN')
ON CONFLICT (period_code) DO NOTHING;

INSERT INTO kpi_configs (period_id, work_hours_pct, role_weight_pct, quality_pct, project_pct)
SELECT id, 30, 25, 25, 20 FROM kpi_periods WHERE period_code = '2025-Q1'
ON CONFLICT DO NOTHING;

-- System parameters to align with Phase 1 setup
INSERT INTO system_parameters (key, value, description)
VALUES
('timesheet.max_daily_hours', '24', 'Upper bound for daily work hours'),
('timesheet.default_status', 'DRAFT', 'New entries start in Draft'),
('kpi.current_period', '2025-Q1', 'Active KPI calculation period'),
('security.password_policy', '>=8 chars; upper/lower/number/symbol', 'Baseline complexity rule')
ON CONFLICT (key) DO NOTHING;
