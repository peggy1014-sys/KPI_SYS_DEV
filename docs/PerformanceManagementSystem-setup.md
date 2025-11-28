# PerformanceManagementSystem 初始架構指引

本文件說明如何依循分層架構 (Layered Architecture) 建立 **PerformanceManagementSystem** 的初始 Solution 結構、安裝必要 NuGet 套件與設定連線字串。

## Solution 與專案結構
```
PerformanceManagementSystem.sln
├── PerformanceManagementSystem.Core           (Class Library)
│   ├── Entities/                              # Domain Models
│   ├── DTOs/
│   ├── Enums/
│   └── Interfaces/                            # Repository 介面
├── PerformanceManagementSystem.Infrastructure (Class Library)
│   ├── Data/
│   │   └── ApplicationDbContext.cs            # EF Core DbContext
│   ├── Repositories/                          # Repository 實作
│   └── Migrations/
└── PerformanceManagementSystem.Web            (ASP.NET Core MVC)
    ├── Controllers/
    ├── ViewModels/
    ├── Views/
    ├── wwwroot/
    ├── appsettings.json
    └── Program.cs                             # DI、管線設定
```

## 專案層級 NuGet 套件
- **Infrastructure** (需引用 Core)：
  - `dotnet add PerformanceManagementSystem.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer`
  - `dotnet add PerformanceManagementSystem.Infrastructure package Microsoft.EntityFrameworkCore.Tools`
  - `dotnet add PerformanceManagementSystem.Infrastructure package Microsoft.EntityFrameworkCore.Design`
- **Web** (需引用 Core、Infrastructure，用於設計階段與 Scaffold)：
  - `dotnet add PerformanceManagementSystem.Web package Microsoft.EntityFrameworkCore.Tools`
  - `dotnet add PerformanceManagementSystem.Web package Microsoft.EntityFrameworkCore.Design`

> Core 層為純粹 Domain/介面定義，不需資料庫或 Web 相關套件。

## `appsettings.json` 範例 (Web 專案)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=PerformanceDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 一次性建立 Solution、專案、引用與套件的 PowerShell 腳本
以下腳本假設在空資料夾執行並使用 .NET 8。
```powershell
# 建立 Solution 與專案
$solutionName = "PerformanceManagementSystem"
$root = Get-Location

dotnet new sln -n $solutionName

dotnet new classlib -n "$solutionName.Core"
dotnet new classlib -n "$solutionName.Infrastructure"
dotnet new mvc      -n "$solutionName.Web" --framework net8.0

# 加入 Solution
$sln = "$root/$solutionName.sln"
dotnet sln $sln add "$solutionName.Core/$solutionName.Core.csproj"
dotnet sln $sln add "$solutionName.Infrastructure/$solutionName.Infrastructure.csproj"
dotnet sln $sln add "$solutionName.Web/$solutionName.Web.csproj"

# 參考關係
cd "$solutionName.Infrastructure"
dotnet add reference "../$solutionName.Core/$solutionName.Core.csproj"
cd "$root/$solutionName.Web"
dotnet add reference "../$solutionName.Core/$solutionName.Core.csproj"
dotnet add reference "../$solutionName.Infrastructure/$solutionName.Infrastructure.csproj"

# 安裝 NuGet 套件
cd "$root/$solutionName.Infrastructure"
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
(dotnet add package Microsoft.EntityFrameworkCore.Tools)
(dotnet add package Microsoft.EntityFrameworkCore.Design)

cd "$root/$solutionName.Web"
(dotnet add package Microsoft.EntityFrameworkCore.Tools)
(dotnet add package Microsoft.EntityFrameworkCore.Design)

# 返回根目錄
cd $root
```

> 如需使用 Bash，可將參考與套件安裝命令直接依序執行，同樣的專案路徑與參數即可。
