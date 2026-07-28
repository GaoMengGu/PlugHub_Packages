# 湖北报规九列模板验证 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将模板拆分为 IFC 导出类型与 Revit 参数类型，并以独立 C# 工具验证总图和单体 minimal CSV。

**Architecture:** 原始 CSV 行保留 IFC 属性集和赋值语义；同名参数只在共享参数创建阶段合并 Revit 类别。独立 C# 校验工具按与插件相同的九列契约验证两个 minimal 模板，不依赖 Revit 运行时，因此可在 CI/宿主环境独立执行。

**Tech Stack:** C# 8、.NET Framework 4.8、Revit 2020 API、Mono C# 编译器、PowerShell 包校验。

---

### Task 1: 编写失败的 C# 模板验收工具

**Files:**
- Create: `tests/PlugHub.HubeiReportParameters.TemplateValidation/Program.cs`
- Modify: `tests/Validate-Package.ps1`

- [ ] **Step 1: 创建验收工具**

```csharp
List<Row> rows = ReadRows(path);
ValidateDefinitions(rows, path);
ValidateValues(rows, path);
string hifc = BuildHifcText(rows);
```

- [ ] **Step 2: 运行工具并确认失败**

Run: `mcs ... tests/PlugHub.HubeiReportParameters.TemplateValidation/Program.cs ...`

Expected: 编译失败，因为旧模块仍使用八列表头，且不存在九列 IFC/Revit 类型分离逻辑。

### Task 2: 实现九列解析和定义合并

**Files:**
- Modify: `src/PlugHub.HubeiReportParameters/HubeiReportParametersModels.cs`
- Modify: `src/PlugHub.HubeiReportParameters/HubeiReportTemplate.cs`
- Create: `src/PlugHub.HubeiReportParameters/HubeiReportTemplateWriter.cs`

- [ ] **Step 1: 将行模型拆分为 IFC 数据类型和 Revit 参数类型**
- [ ] **Step 2: 按名称、I/T、Revit 参数类型合并定义并合并类别**
- [ ] **Step 3: 保留全部原始行用于逐类别赋值，并拒绝同类别的冲突值**
- [ ] **Step 4: 使用 IFC 数据类型生成 HIFC 文本**
- [ ] **Step 5: 重新运行独立工具并确认通过**

### Task 3: 按原始行赋值并清理命令职责

**Files:**
- Modify: `src/PlugHub.HubeiReportParameters/SyncHubeiReportParametersCommand.cs`

- [ ] **Step 1: 用合并后的定义创建或更新共享参数绑定**
- [ ] **Step 2: 对每条原始行的 Revit 类别写入该行真实数据或默认值**
- [ ] **Step 3: 调用共享 HIFC 文本生成器输出 `项目名-HIFC.txt`**
- [ ] **Step 4: 编译模块并重新运行独立工具**

### Task 4: 规范样例、文档和包回归检查

**Files:**
- Modify: `docs/单体_minimal.csv`
- Modify: `docs/总图_minimal.csv`
- Modify: `docs/HubeiReportParameters-Template.csv`
- Modify: `docs/HIFC-Revit-Parameter-Bindings.md`
- Modify: `tests/Validate-Package.ps1`

- [ ] **Step 1: 统一九列表头与 Revit 2020 枚举拼写**
- [ ] **Step 2: 在文档中明确 IFC 类型输出、Revit 类型创建与同名逐类别赋值规则**
- [ ] **Step 3: 运行 `pwsh -NoProfile -File tests/Validate-Package.ps1`**
- [ ] **Step 4: 运行独立 C# 工具验证两个 minimal 模板**
