# KPI_SYS_DEV

初始版本專注於**資料庫架構與初始化**，支援《專案工時KPI績效管理系統-系統分析文件1119.txt》與《績效管理系統三階段功能開發細節順序.md》中的 Phase 1/MVP 功能：
- Lookup 代碼項目維護
- 使用者、組織、角色主檔
- 專案、任務、工時填報的基本交易模型
- 稽核紀錄與 KPI 期別設定的骨架

## 專案需求對應
- Phase 1 強調先建基礎資料（代碼、使用者、組織）再到工時交易與審核，因此 schema 依此順序設計，外鍵與唯一鍵確保參照完整性。【F:績效管理系統三階段功能開發細節順序.md†L7-L38】【F:績效管理系統 系統分析V1.md†L5365-L5376】
- KPI 計算與鎖定需保留歷史與稽核，因此提供 `kpi_periods`、`kpi_configs`、`kpi_results` 以及 `audit_logs` 表作為後續引擎與 UI 的基礎。【F:績效管理系統 系統分析V1.md†L4404-L4465】

## 檔案說明
- `db/schema.sql`：PostgreSQL DDL，涵蓋 lookup、組織、使用者、專案/任務、工時、稽核、KPI 期別與結果表，含必要索引、百分比檢查約束、延遲外鍵等。
- `db/seed.sql`：初始資料，包含狀態類別、角色/部門範例、預設 KPI 期別與系統參數。

## 使用方式
1. 準備 PostgreSQL 資料庫（建議版本 14+），並確認有操作權限的帳號。
2. 匯入 schema：
   ```bash
   psql "$DATABASE_URL" -f db/schema.sql
   ```
3. 匯入初始資料（可重複執行，使用 `ON CONFLICT` 避免重覆）：
   ```bash
   psql "$DATABASE_URL" -f db/seed.sql
   ```
4. 如需調整 KPI 權重或狀態代碼，可直接更新 `kpi_configs`、`lookup_groups`/`lookup_values` 後透過應用程式 UI 曝露。

## 後續延伸
- Phase 1 的登入/權限與審核流程可直接對應 `users`、`project_members`、`timesheets` 與 `audit_logs`。
- Phase 2/3 可在此基礎上新增批次審核、外部 SSO 整合、KPI 審核調整與報表匯出功能。
