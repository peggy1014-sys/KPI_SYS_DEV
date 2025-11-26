-- Database schema for KPI & timesheet management system (PostgreSQL)
-- Aligns with Phase 1 MVP priorities: lookup data, users, organization,
-- projects, tasks, timesheets, audit logging, and KPI period scaffolding.

-- Use UUIDs if preferred by deployment; BIGSERIAL keeps the demo simple.

CREATE TABLE lookup_groups (
    id           BIGSERIAL PRIMARY KEY,
    group_key    TEXT NOT NULL UNIQUE,
    name         TEXT NOT NULL,
    description  TEXT,
    is_active    BOOLEAN NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE lookup_values (
    id          BIGSERIAL PRIMARY KEY,
    group_id    BIGINT NOT NULL REFERENCES lookup_groups(id),
    value_key   TEXT NOT NULL,
    name        TEXT NOT NULL,
    sort_order  INTEGER NOT NULL DEFAULT 0,
    metadata    JSONB NOT NULL DEFAULT '{}'::JSONB,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (group_id, value_key)
);

CREATE TABLE departments (
    id           BIGSERIAL PRIMARY KEY,
    code         TEXT NOT NULL UNIQUE,
    name         TEXT NOT NULL,
    parent_id    BIGINT REFERENCES departments(id),
    manager_id   BIGINT,
    is_active    BOOLEAN NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE roles (
    id           BIGSERIAL PRIMARY KEY,
    code         TEXT NOT NULL UNIQUE,
    name         TEXT NOT NULL,
    level        SMALLINT NOT NULL DEFAULT 1,
    cost_rate    NUMERIC(12,2),
    description  TEXT,
    is_active    BOOLEAN NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE users (
    id              BIGSERIAL PRIMARY KEY,
    employee_no     TEXT NOT NULL UNIQUE,
    email           TEXT NOT NULL UNIQUE,
    full_name       TEXT NOT NULL,
    password_hash   TEXT,
    department_id   BIGINT REFERENCES departments(id),
    role_id         BIGINT REFERENCES roles(id),
    manager_id      BIGINT REFERENCES users(id),
    employment_status TEXT NOT NULL DEFAULT 'ACTIVE',
    join_date       DATE,
    leave_date      DATE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Deferred constraint to allow manager/user bootstrapping after inserts.
ALTER TABLE users
    ADD CONSTRAINT fk_user_manager FOREIGN KEY (manager_id) REFERENCES users(id)
    DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE departments
    ADD CONSTRAINT fk_department_manager_user FOREIGN KEY (manager_id) REFERENCES users(id)
    DEFERRABLE INITIALLY DEFERRED;

CREATE TABLE projects (
    id            BIGSERIAL PRIMARY KEY,
    code          TEXT NOT NULL UNIQUE,
    name          TEXT NOT NULL,
    description   TEXT,
    status        TEXT NOT NULL DEFAULT 'DRAFT',
    category      TEXT,
    project_size  TEXT,
    pm_id         BIGINT REFERENCES users(id),
    start_date    DATE,
    end_date      DATE,
    billable      BOOLEAN NOT NULL DEFAULT FALSE,
    budget_hours  NUMERIC(10,2),
    is_active     BOOLEAN NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE project_members (
    id             BIGSERIAL PRIMARY KEY,
    project_id     BIGINT NOT NULL REFERENCES projects(id),
    user_id        BIGINT NOT NULL REFERENCES users(id),
    role_id        BIGINT REFERENCES roles(id),
    allocation_pct NUMERIC(5,2) NOT NULL DEFAULT 100 CHECK (allocation_pct >= 0 AND allocation_pct <= 100),
    is_active      BOOLEAN NOT NULL DEFAULT TRUE,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (project_id, user_id)
);

CREATE TABLE tasks (
    id            BIGSERIAL PRIMARY KEY,
    project_id    BIGINT NOT NULL REFERENCES projects(id),
    code          TEXT NOT NULL,
    name          TEXT NOT NULL,
    description   TEXT,
    assignee_id   BIGINT REFERENCES users(id),
    status        TEXT NOT NULL DEFAULT 'OPEN',
    priority      TEXT NOT NULL DEFAULT 'NORMAL',
    start_date    DATE,
    due_date      DATE,
    completed_at  TIMESTAMPTZ,
    quality_score NUMERIC(5,2),
    is_active     BOOLEAN NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (project_id, code)
);

CREATE TABLE timesheets (
    id             BIGSERIAL PRIMARY KEY,
    user_id        BIGINT NOT NULL REFERENCES users(id),
    project_id     BIGINT NOT NULL REFERENCES projects(id),
    task_id        BIGINT REFERENCES tasks(id),
    work_date      DATE NOT NULL,
    hours          NUMERIC(5,2) NOT NULL CHECK (hours >= 0 AND hours <= 24),
    status         TEXT NOT NULL DEFAULT 'DRAFT',
    submitted_at   TIMESTAMPTZ,
    approved_at    TIMESTAMPTZ,
    approver_id    BIGINT REFERENCES users(id),
    rejection_reason TEXT,
    notes          TEXT,
    is_billable    BOOLEAN,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, project_id, task_id, work_date)
);

CREATE TABLE audit_logs (
    id            BIGSERIAL PRIMARY KEY,
    module        TEXT NOT NULL,
    entity        TEXT NOT NULL,
    entity_id     TEXT,
    action        TEXT NOT NULL,
    old_value     JSONB,
    new_value     JSONB,
    performed_by  BIGINT REFERENCES users(id),
    performed_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_address    INET,
    user_agent    TEXT
);

CREATE TABLE system_parameters (
    id            BIGSERIAL PRIMARY KEY,
    key           TEXT NOT NULL UNIQUE,
    value         TEXT NOT NULL,
    description   TEXT,
    is_sensitive  BOOLEAN NOT NULL DEFAULT FALSE,
    updated_by    BIGINT REFERENCES users(id),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE kpi_periods (
    id            BIGSERIAL PRIMARY KEY,
    period_code   TEXT NOT NULL UNIQUE,
    name          TEXT NOT NULL,
    start_date    DATE NOT NULL,
    end_date      DATE NOT NULL,
    status        TEXT NOT NULL DEFAULT 'OPEN',
    calc_version  TEXT NOT NULL DEFAULT 'v1',
    locked_at     TIMESTAMPTZ,
    locked_by     BIGINT REFERENCES users(id),
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE kpi_configs (
    id              BIGSERIAL PRIMARY KEY,
    period_id       BIGINT REFERENCES kpi_periods(id),
    work_hours_pct  NUMERIC(5,2) NOT NULL DEFAULT 30 CHECK (work_hours_pct >= 0 AND work_hours_pct <= 100),
    role_weight_pct NUMERIC(5,2) NOT NULL DEFAULT 25 CHECK (role_weight_pct >= 0 AND role_weight_pct <= 100),
    quality_pct     NUMERIC(5,2) NOT NULL DEFAULT 25 CHECK (quality_pct >= 0 AND quality_pct <= 100),
    project_pct     NUMERIC(5,2) NOT NULL DEFAULT 20 CHECK (project_pct >= 0 AND project_pct <= 100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT kpi_config_pct_total CHECK ((work_hours_pct + role_weight_pct + quality_pct + project_pct) = 100)
);

CREATE TABLE kpi_results (
    id               BIGSERIAL PRIMARY KEY,
    period_id        BIGINT NOT NULL REFERENCES kpi_periods(id),
    user_id          BIGINT NOT NULL REFERENCES users(id),
    project_id       BIGINT REFERENCES projects(id),
    workload_score   NUMERIC(6,2),
    role_score       NUMERIC(6,2),
    quality_score    NUMERIC(6,2),
    contribution_score NUMERIC(6,2),
    final_score      NUMERIC(6,2),
    calc_version     TEXT NOT NULL DEFAULT 'v1',
    calculated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_locked        BOOLEAN NOT NULL DEFAULT FALSE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (period_id, user_id, project_id)
);

-- Indexes for common lookups and filtering
CREATE INDEX idx_lookup_values_group ON lookup_values(group_id);
CREATE INDEX idx_users_department ON users(department_id);
CREATE INDEX idx_users_manager ON users(manager_id);
CREATE INDEX idx_projects_pm ON projects(pm_id);
CREATE INDEX idx_tasks_project ON tasks(project_id);
CREATE INDEX idx_timesheets_user_date ON timesheets(user_id, work_date);
CREATE INDEX idx_timesheets_project ON timesheets(project_id);
CREATE INDEX idx_kpi_results_period_user ON kpi_results(period_id, user_id);

-- Ensure optional task references are enforced while allowing bootstrapping.
ALTER TABLE timesheets
    ADD CONSTRAINT fk_timesheets_task_project
    FOREIGN KEY (task_id) REFERENCES tasks(id)
    DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE tasks
    ADD CONSTRAINT fk_tasks_assignee FOREIGN KEY (assignee_id) REFERENCES users(id)
    DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE projects
    ADD CONSTRAINT fk_projects_pm FOREIGN KEY (pm_id) REFERENCES users(id)
    DEFERRABLE INITIALLY DEFERRED;

