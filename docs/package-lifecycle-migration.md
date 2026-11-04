# 新框架契约迁移验证记录

日期：2026-11-04。来源：Chorus Idea `76f94edc-a4ae-42ee-ac77-5f00e6ca5a33`，用户明确要求按新契约调整并让自动保存随框架加载；执行 Task `26606f9b-48d6-44a5-8fdc-4b416d70bc32`。本地任务分支 `feature/package-lifecycle-migration`，起点 `249fb48`。本记录不代表已发布或完成 Revit 实机验收。

## 影响范围

| 模块 | 迁移后清单版本 | 启动声明 |
| --- | --- | --- |
| GridVisibility | V1.0.5 | 省略 |
| LevelVisibility | V1.0.5 | 省略 |
| ReferencePlaneVisibility | V1.0.5 | 省略 |
| DuctPreferredJunction | V1.0.3 | 省略 |
| FamilyMaterialParameters | V1.0.4 | 省略 |
| FamilyFileSaver | V1.0.3 | 省略 |
| HubeiReportParameters | V1.0.1 | 省略 |
| ProjectAutoSave | V1.1.0 | autoStart=true；PlugHub.ProjectAutoSave.ProjectAutoSaveApplication |
| MepTypeFilterVisibility | V1.0.3 | 省略 |

9 个模块各删除 `State = ModuleState.Enabled` 和 `DefaultState = FeatureState.Visible`，9 个 csproj 改引相邻 `PlugHub/src/PlugHub.Contracts/PlugHub.Contracts.csproj`，CI 检出路径同步改为 `PlugHub`。原 ID、命令映射、分类、排序和图标保留。根 indexVersion 保持 V1.1.0，未触发发布。

仓库仅有根 packages.json，无模块源码生成器、模块模板或自带 Contracts DLL；湖北 CSV 模板读写器处理业务数据，不输出模块状态，未修改。保留空 IPlugHubModule.Initialize/Shutdown、描述接口及 commandKey。未删除框架的依赖/预设/来源机制；本仓库清单原已禁止 enabled/visible/defaultState。

自动保存新增公开无参 IExternalApplication；启动只订阅 UIControlledApplication.Idling，关闭注销并清理会话计时。Idling 从 sender 获取 UIApplication，沿用现有设置与保存保护；禁用时跳过保存，重新启用无需重建订阅。设置窗口只写原 XML，不再拥有事件生命周期。首次默认关闭，已有启用设置会继续生效；未创建额外 .addin。

## 构建和验证

框架工作区 `/home/yilan/codex-project/PlugHub`，HEAD `122ce6b9ec6cf6463b0467a028161c2202933ccd`，生命周期实现提交 `dbb5ca2`。实际 Contracts 为该工作区 `src/PlugHub.Contracts/bin/Release/net48/PlugHub.Contracts.dll`，开发程序集版本 0.0.0；SHA256 为 `1d693513195c1f7813e1f22cc2ea8d38767229074e3e1a33aa879f53e98f4643`。与交接的 Debug 哈希不同，不能只凭版本号区分契约。

Revit 引用：Autodesk.Revit.SDK 2020.2.1.2001；.NET SDK 10.0.302；生产包 net48/Release，回归工具 net8.0。

- `pwsh -NoProfile -File build.ps1 -UseRevitApiNuGet`：9 个项目成功、0 errors，DLL 已更新到本仓库 dist。每项目有 4 条 SDK 原生 DLL 的 MSB3246 告警；湖北包另有原业务代码编译告警，详见本地日志。
- `pwsh -NoProfile -File tests/Validate-Package.ps1`：通过；临时副本中字符串/缺失 autoStart、错误 startupType、其他包自启、旧 JSON 状态、旧源码状态、旧 Contracts 路径共 8 种非法迁移均被拒绝。
- `dotnet run --project tests/PlugHub.ProjectAutoSave.LifecycleValidation`：通过。链接生产服务及入口，用测试替身覆盖无文档启动、重复启动、禁用/启用、间隔、文档保护、保存异常、重入、关闭和新会话。直接读取全部9个 DLL 的 PE 元数据，检查 net48、Contracts 引用、无旧状态 setter、命令/启动接口、公开无参构造及私有依赖是否齐全；不加载 Revit 原生运行时。
- `dotnet run --project tests/PlugHub.HubeiReportParameters.SchedulePlanningValidation`：通过。
- 临时 net48/Mono 工具调用当前框架 PackageManifestReader/Writer：读入9模块、仅自动保存自启、读写往返保留启动字段，通过。
- 图标 SHA256 逐一比对与 `git diff --check`：通过。

本地日志、旧 dist 副本和哈希清单位于 `.tmp/package-lifecycle/`；备份目录位于本次新建的原有受忽略 .tmp 下，不作为部署目录使用。

## 迁移产物 SHA256

```text
3543f0331761ba1eebd71d633ebe9dcf12ad431a1a323e76cf22fc5644b492fd  dist/PlugHub.DuctPreferredJunction.dll
1bb7fc93c338fa8cbd9a860dbfaa2f38ee2edbc81cd14135ebc5f2ea2769c3bc  dist/PlugHub.FamilyFileSaver.dll
cd4eabfec76ba79b7d283e3207cc4e21116c34ced04b7099f7249aedc7b048d8  dist/PlugHub.FamilyMaterialParameters.dll
1d1f057020347811bac8619c80e84130a79655d959598c93558dc0ca4bb1a12a  dist/PlugHub.GridVisibility.dll
95ed67dd53e7724a28b2ec1a36ef27d8fb0d88710e1b8187a64d326100cb8fac  dist/PlugHub.HubeiReportParameters.dll
5181ec2f44d78599d90925b074fa3f2be1ff995bea701cb806cccbdaa59876cf  dist/PlugHub.LevelVisibility.dll
6d47b7b00a563c15428b7b980b3fb23817432150a333443b35de37d76dd45786  dist/PlugHub.MepTypeFilterVisibility.dll
12118c02a8e9f5d4550187906ab0446f3d25cc688cdfad9e4a81f47c0b5a43f6  dist/PlugHub.ProjectAutoSave.dll
1c44e5ab234d28d213a656ad9d282cb27eab92e983d7a385102270801c6c995c  dist/PlugHub.ReferencePlaneVisibility.dll
```

## 未测与交付边界

Windows/Revit 2020 实机、真实 XML 的读写与 UI、实际事件 sender 和 Revit 保存行为均未在此 Linux 会话执行；测试替身不能替代实机。现有默认 Idling 频率不保证严格墙钟周期，长时间完全空闲时可能延后保存。

尚未部署、推送、合并或发布。CI 仍跟随框架仓库默认分支检出，须先协调其包含新生命周期实现并完成匹配构建联调；`frameworkVersionRange >=1.3.0` 是保留的元数据，不执行兼容性拦截。验收须覆盖无文档启动、已有开关生效、刷新不重复注册、关闭注销、安装/更新/卸载重启切换。回退须先关闭 Revit，再同时恢复框架、Contracts、全部包 DLL/依赖和清单。
