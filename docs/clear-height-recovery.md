# 净高分析 V3 代码恢复记录

恢复日期：2026-11-05。恢复分支：`fix/restore-clear-height`。

## 来源与范围

- 基线是远程最新 `main`：`fac1dc734c6ab3557750533c1db22aa49c6dfe90`。
- 净高来源是本地 `clear-height-v3-optimization` 的 `fc6ae6657ed370b2af967c81a4af33fef952fa89`，包含该工作树当时的全部未提交净高改动。
- 找回 85 个源码文件、23 个测试文件和原净高图标，并重新编译 `dist/PlugHub.ClearHeightAnalysis.dll`。
- 原工作树的 17 个未提交文件保持原状。源码副本、完整补丁和 SHA-256 清单位于本地 `.tmp/clear-height-recovery-20261105T010739Z/`，不提交临时备份。
- 其他 9 个模块的源码、图标、DLL 和清单数据保留最新 `main`。共享文件仅增加净高构建、清单、说明与测试接入；现有载荷测试数量从 9 调整为 10。

## 接入调整

净高模块沿用当前主线 Contracts 引用路径，并移除已被框架删除的 `State`、`DefaultState` 描述字段。原未提交代码中 `AnalysisWorkflowController` 缺少 `Core.Services` 引用，恢复时补齐。

只移植 V3 校验脚本中净高相关检查，保留当前主线的湖北、自动保存、图标和 CI 检查。CI 增加净高核心测试。

## 验证

- `dotnet test tests/PlugHub.ClearHeightAnalysis.Tests -c Release --verbosity minimal`：103 项通过。
- `dotnet build src/PlugHub.ClearHeightAnalysis/PlugHub.ClearHeightAnalysis.csproj -c Release -t:Rebuild /p:RevitApiReferenceMode=NuGet /p:EnableWindowsTargeting=true --no-restore`：完整重编译成功，包括三个 WPF 窗口；有引用程序集、可空性及 NuGet 审计缓存警告。
- `dotnet run --project tests/PlugHub.ProjectAutoSave.LifecycleValidation -c Release --no-restore`：自动保存生命周期及全部 10 个包的入口、依赖和当前契约检查通过。
- `pwsh -NoProfile -File tests/Validate-Package.ps1`：通过。
- 原 V3 工作树快照校验通过；主线其他模块的源码、图标和 DLL 按 Git 属性归一化后逐文件一致，9 个清单模块数据一致。

## 使用边界

这是代码恢复结果，尚未完成 Windows + Revit 2020 实机验证。楼层与链接模型读取、进度取消、批次保存与重开、派生输出替换与删除、面积色块、PNG 和 CSV 仍需用测试模型副本验证。

V3 是开发工作名称，清单沿用来源的模块版本 `V2.0.0`，本次未调整仓库发布版本。
