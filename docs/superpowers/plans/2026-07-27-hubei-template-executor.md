# 湖北报规模板执行器 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将湖北报规模块替换为用户选择 CSV 模板的共享参数和 HIFC 映射文件执行器。

**Architecture:** 用独立 CSV 解析、模板验证和 Revit 类别目录隔离纯模板逻辑；命令层只执行已验证模板、更新共享参数绑定和写入值。旧嵌入资源和按 HIFC/mini 推导范围的代码完全删除。

**Tech Stack:** C# 8、.NET Framework 4.8、Revit 2020 API、WinForms、PowerShell 包校验。

---

### Task 1: 替换回归校验

**Files:**
- Modify: `tests/Validate-Package.ps1`

- [ ] 用模板选择、CSV、项目名 HIFC 输出、清除同名参数和 `ParameterType` 断言替换旧 HIFC/mini/分类断言。
- [ ] 运行 `pwsh -NoProfile -File tests/Validate-Package.ps1`，确认旧实现因缺少新模板执行器标识而失败。

### Task 2: 实现模板模型、解析和类别目录

**Files:**
- Modify: `src/PlugHub.HubeiReportParameters/HubeiReportParametersModels.cs`
- Create: `src/PlugHub.HubeiReportParameters/HubeiReportTemplate.cs`
- Create: `src/PlugHub.HubeiReportParameters/RevitCategoryCatalog.cs`
- Delete: `src/PlugHub.HubeiReportParameters/HubeiReportCatalog.cs`

- [ ] 实现 UTF-8 BOM CSV 解析、八列表头验证、I/T、`ParameterType`、中文类别、同名冲突和 HIFC 分组验证。
- [ ] 提供建筑、结构、暖通、给排水、电气的常用中文类别目录。

### Task 3: 替换界面和命令

**Files:**
- Modify: `src/PlugHub.HubeiReportParameters/HubeiReportSelectionForm.cs`
- Modify: `src/PlugHub.HubeiReportParameters/SyncHubeiReportParametersCommand.cs`

- [ ] 界面提供 CSV 文件选择和“清除当前项目同名参数”选项。
- [ ] 命令显示合并确认，创建实例/类型绑定，以真实数据优先、默认值兜底覆盖值，并输出项目名 HIFC 文件。

### Task 4: 清理旧模块输入和说明

**Files:**
- Modify: `src/PlugHub.HubeiReportParameters/PlugHub.HubeiReportParameters.csproj`
- Modify: `src/PlugHub.HubeiReportParameters/HubeiReportParametersModule.cs`
- Modify: `packages.json`
- Create: `docs/HubeiReportParameters-Template.csv`
- Delete: `src/PlugHub.HubeiReportParameters/Resources/HIFC.txt`
- Delete: `src/PlugHub.HubeiReportParameters/Resources/mini.txt`

- [ ] 移除嵌入资源，更新功能说明，提供可直接用 Excel 编辑的 CSV 样例。

### Task 5: 验证

**Files:**
- Modify: `tests/Validate-Package.ps1`

- [ ] 运行包校验。
- [ ] 运行模块 Release 编译（NuGet Revit API 模式）；若宿主未安装 .NET，记录准确错误而不伪称编译成功。
