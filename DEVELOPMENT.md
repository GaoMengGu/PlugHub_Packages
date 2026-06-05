# PlugHub 外部插件包开发说明

本目录是 PlugHub 外部插件包开发项目。PlugHub 主框架只负责模块契约、发现、启用/禁用、排序、组合、诊断和 Revit 2020 入口适配，不直接包含业务功能。

## 目录结构

```text
PlugHub_Packages/
  packages.json
  build.ps1
  dist/
  src/
    PlugHub.DuctPreferredJunction/
      DuctPreferredJunctionModule.cs
      DuctPreferredJunctionSwitcherCommand.cs
    PlugHub.FamilyMaterialParameters/
      FamilyMaterialParametersModule.cs
      BatchAddMaterialParameterCommand.cs
    PlugHub.GridVisibility/
      GridVisibilityModule.cs
      ToggleGridVisibilityCommand.cs
    PlugHub.LevelVisibility/
      LevelVisibilityModule.cs
      ToggleLevelVisibilityCommand.cs
```

约定：

- 一个 DLL 项目对应一个独立插件包，或一组强相关功能。
- 一个命令文件只承载一个用户可触发功能的 Revit API 操作。
- 模块描述文件只实现 `IPlugHubModule.Describe()` 元数据，不写业务逻辑。
- `packages.json` 是 PlugHub 读取外部插件包的推荐入口，声明模块、功能和命令类型；平铺投放单个 DLL 时也可以使用 `<DllName>.packages.json`。
- 根 `indexVersion` 表示仓库索引和 Release 快照版本；`modules[].version` 表示单个模块在 PlugHub 框架中展示和比较的版本。

## 构建

```powershell
.\build.ps1 -RevitApiDir "D:\Program Files\Autodesk\Revit 2020"
```

没有安装 Revit 的 CI 环境使用 NuGet 编译引用：

```powershell
.\build.ps1 -UseRevitApiNuGet
```

构建输出位于：

```text
dist/
```

`packages.json` 中的 module `assembly` 默认指向 `dist/*.dll`，feature 默认继承 module 的 `assembly` 作为命令程序集。因此可以直接把整个 `PlugHub_Packages` 文件夹配置为 PlugHub 本地插件包来源，或配置为仓库来源。

GitHub Actions 会在 `main` 分支构建后提交 `dist/*.dll`。`bin/obj` 和 `dist/*.pdb` 不进入仓库。

## 接入 PlugHub

方式一：配置本地来源。

在 PlugHub 的 `config\sources.json` 中启用或新增：

```json
{
  "id": "local-plughub-packages",
  "type": "localFolder",
  "path": "D:/AI/code/PlugHub_Packages",
  "manifestPath": "packages.json",
  "enabled": true
}
```

方式二：配置公开仓库来源。

在 PlugHub 的 `config\sources.json` 中启用或新增：

```json
{
  "id": "plughub-public-packages",
  "provider": "gitee",
  "visibility": "public",
  "repository": "https://gitee.com/GaoMengGu/PlugHub_Packages",
  "ref": "main",
  "manifestPath": "packages.json",
  "enabled": true
}
```

如果需要使用 GitHub 镜像，把 `provider` 改为 `github`，并把 `repository` 写成 `GaoMengGu/PlugHub_Packages`。

方式三：复制到投放目录。

将 `packages.json`、`dist` 和 `icons` 文件夹复制到 PlugHub 部署目录下的 `packages\dropins\<package-folder>`。PlugHub 启动时会递归扫描所有 `packages.json` 和 `*.packages.json`。

## 多插件包组织

一个来源目录可以包含多个插件包子文件夹，每个子文件夹放一份 `packages.json`。如果需要平铺投放，也可以使用 DLL 邻接清单 `<DllName>.packages.json`。例如：

```text
packages/dropins/
  DuctTools/
    packages.json
    dist/PlugHub.DuctPreferredJunction.dll
  FamilyTools/
    packages.json
    dist/PlugHub.FamilyMaterialParameters.dll
  ViewTools/
    packages.json
    dist/PlugHub.GridVisibility.dll
    dist/PlugHub.LevelVisibility.dll
```

## 新增插件包步骤

1. 新建 .NET Framework 4.8 类库项目。
2. 引用 PlugHub 主框架的 `PlugHub.Contracts.csproj`。
3. 创建一个模块描述类，实现 `IPlugHubModule`。
4. 创建一个命令类，实现 Revit 的 `IExternalCommand`。
5. 在 `packages.json` 中新增模块记录和功能记录，并为模块设置 `version` 和 `author`。
6. 运行 `build.ps1`，确认 DLL 输出到 `dist`。
7. 启动或重启 Revit，让 PlugHub 重新扫描插件包。

## 清单字段约定

- 根 `indexVersion` 必须使用 `V<major>.<minor>.<patch>`，由发布 workflow 在普通发布时自动递增；它只表示仓库索引和 Release 快照。
- `module.id` 和 `feature.id` 必须全局唯一。
- `module.version` 必须使用 `V<major>.<minor>.<patch>`。只在该模块实际变更时递增；仓库页展示和更新判断使用该字段。
- `module.author` 当前统一写 `GAOMENGGU`；框架只保留该元数据，暂不参与调用。
- `module.assembly` 指向命令 DLL。相对路径按插件包清单所在目录解析。
- `module.category` 和 `module.tags` 用于仓库筛选、默认布局和展示元数据。
- `feature.displayName`、`feature.description`、`feature.iconPath` 和 `feature.commandType` 是功能入口的主要字段。
- `feature.commandType` 必须是完整类型名，并实现 `Autodesk.Revit.UI.IExternalCommand`。
- `feature.commandAssembly` 不再写入；feature 默认继承所属 module 的 `assembly`。
- `enabled`、`visible`、`order`、`defaultState`、`buttonSize`、`feature.group`、`feature.category` 和 `feature.tags` 不写入仓库清单，默认状态和布局由 PlugHub 框架及用户本地配置负责。
- `iconPath` 为空时使用 PlugHub 默认图标；相对路径按插件包清单所在目录解析。
## 验证边界

本目录可以通过 `dotnet build` 验证 C# 编译。真实 Revit 命令行为必须在 Windows + Revit 2020 中使用测试模型副本或族文件副本验证。
