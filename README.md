# PlugHub Packages

PlugHub Packages 是 [PlugHub](https://github.com/GaoMengGu/PlugHub) 的外部插件包仓库，用于独立开发、构建和分发 Revit 业务功能。PlugHub 主框架负责模块发现、启用状态、Ribbon 组合和 Revit 入口适配，本仓库只保存可被 PlugHub 加载的功能模块。

仓库中的 `package.json` 是插件包清单，`dist/*.dll` 是 PlugHub 运行时加载的程序集。GitHub Actions 会在 `main` 分支构建插件 DLL，并发布 Release ZIP，便于本地安装。

## 功能列表

| 插件包 | 功能 | 说明 |
| --- | --- | --- |
| `PlugHub.GridVisibility` | 轴网显隐切换 | 在当前视图中切换轴网类别的可见性。 |
| `PlugHub.LevelVisibility` | 标高显隐切换 | 在当前视图中切换标高类别的可见性。 |
| `PlugHub.DuctPreferredJunction` | 风管接头切换 | 在已选或点选风管的类型上切换风管首选连接类型。 |
| `PlugHub.FamilyMaterialParameters` | 批量材质参数 | 批量打开族文件，添加材质参数并关联实体材质参数。 |

## 安装方式

### 方式一：下载 Release ZIP

这是面向普通用户的推荐方式。

1. 打开 [Releases](https://github.com/GaoMengGu/PlugHub_Packages/releases)。
2. 下载 Release Assets 中的 `PlugHub-Packages-V*.zip`。
3. 解压后得到 `PlugHub_Packages` 文件夹。
4. 将该文件夹复制到 PlugHub 的投放目录：

```text
<PlugHub 安装目录>/packages/dropins/PlugHub_Packages/
```

5. 确认目录结构如下：

```text
packages/dropins/PlugHub_Packages/
  package.json
  dist/
    PlugHub.DuctPreferredJunction.dll
    PlugHub.FamilyMaterialParameters.dll
    PlugHub.GridVisibility.dll
    PlugHub.LevelVisibility.dll
  icons/
    duct-preferred-junction.png
    family-material-parameters.png
    grid-visibility.png
    level-visibility.png
```

6. 重启 Revit，或在 PlugHub 中重新加载插件来源。

### 方式二：配置 GitHub 来源

如果希望 PlugHub 自动从 GitHub 拉取本仓库，在 PlugHub 的 `config/sources.json` 中启用或新增：

```json
{
  "id": "plughub-packages",
  "type": "github",
  "repository": "GaoMengGu/PlugHub_Packages",
  "ref": "main",
  "path": "packages/github/GaoMengGu_PlugHub_Packages",
  "manifestPath": "package.json",
  "enabled": true,
  "autoUpdate": true
}
```

PlugHub 会克隆仓库缓存，并读取仓库中的 `package.json` 和 `dist/*.dll`。因此本仓库保留 `dist` DLL，`bin/obj` 和 PDB 文件不进入版本库。

### 方式三：本地源码目录

开发调试时，可以直接把本仓库目录配置为本地来源。请将示例中的路径替换为你自己的本地仓库路径：

```json
{
  "id": "local-plughub-packages",
  "type": "localFolder",
  "path": "D:/path/to/PlugHub_Packages",
  "manifestPath": "package.json",
  "enabled": true,
  "autoUpdate": false
}
```

运行构建后，PlugHub 会从本地 `dist` 目录加载最新 DLL。

## 本地构建

本仓库面向 Revit 2020 和 .NET Framework 4.8。普通本地构建需要安装 Revit 2020，或至少提供 `RevitAPI.dll` 和 `RevitAPIUI.dll`。

```powershell
.\build.ps1 -RevitApiDir "D:\Program Files\Autodesk\Revit 2020"
```

CI 环境没有安装 Revit，使用 NuGet 编译引用：

```powershell
.\build.ps1 -UseRevitApiNuGet
```

构建输出位于：

```text
dist/
```

## 发布流程

- 推送到 `main`：GitHub Actions 构建 DLL，基于当前 `package.json` 或最新 `V*` 标签自动递增补丁版本，回写 `package.json`、`dist/*.dll` 和 `icons/*.png`，并发布对应 Release。
- 推送 `V*` 标签或手动运行 workflow 并填写版本：GitHub Actions 构建 DLL，并按声明版本发布 Release。
- `main` 更新后会同步推送到 Gitee 仓库 `GaoMengGu/PlugHub_Packages`。
- Release ZIP 只包含用户安装所需的 `package.json`、`dist/*.dll` 和 `icons/*.png`。

## 开发说明

新增插件包时，通常需要：

1. 新建 .NET Framework 4.8 类库项目。
2. 引用 PlugHub 主框架的 `PlugHub.Contracts.csproj`。
3. 创建模块描述类，实现 `IPlugHubModule`。
4. 创建命令类，实现 Revit 的 `IExternalCommand`。
5. 在 `package.json` 中新增模块和功能记录。
6. 运行 `build.ps1`，确认 DLL 输出到 `dist`。

更多目录约定和清单字段说明见 [DEVELOPMENT.md](DEVELOPMENT.md)。
