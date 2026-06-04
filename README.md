# PlugHub Packages

PlugHub Packages 是 [PlugHub](https://github.com/GaoMengGu/PlugHub) 的外部插件包仓库，用于独立开发、构建和分发 Revit 业务功能。PlugHub 主框架负责模块发现、启用状态、Ribbon 组合和 Revit 入口适配，本仓库只保存可被 PlugHub 加载的功能模块。

仓库中的 `package.json` 是插件包清单，`dist/*.dll` 是 PlugHub 运行时加载的程序集。GitHub Actions 会在 `main` 分支构建插件 DLL，并发布 Release ZIP，便于本地安装。

版本分为两层：根 `version` 表示整体 Release ZIP 版本，`modules[].version` 表示单个模块的用户可见版本。普通发布仍然是单 ZIP；只有实际变更的模块需要递增自己的 `version`。

## 功能列表

| 插件包 | 功能 | 说明 |
| --- | --- | --- |
| `PlugHub.GridVisibility` | 轴网显隐切换 | 在当前视图中切换轴网类别的可见性。 |
| `PlugHub.LevelVisibility` | 标高显隐切换 | 在当前视图中切换标高类别的可见性。 |
| `PlugHub.ReferencePlaneVisibility` | 参照平面显隐切换 | 在当前视图中切换参照平面类别的可见性。 |
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
    PlugHub.ReferencePlaneVisibility.dll
  icons/
    duct-preferred-junction.png
    family-material-parameters.png
    grid-visibility.png
    level-visibility.png
    reference-plane-visibility.png
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

## 开发说明

新增插件包时，通常需要：

1. 新建 .NET Framework 4.8 类库项目。
2. 引用 PlugHub 主框架的 `PlugHub.Contracts.csproj`。
3. 创建模块描述类，实现 `IPlugHubModule`。
4. 创建命令类，实现 Revit 的 `IExternalCommand`。
5. 在 `package.json` 中新增模块和功能记录，并设置模块 `version`。

如果使用 AI agent 辅助编写插件，推荐尝试安装并使用 [GaoMengGu/PlugHub_Packages_skill](https://github.com/GaoMengGu/PlugHub_Packages_skill)，让 agent 按本仓库约定生成模块代码、清单记录和验证步骤。

更多目录约定和清单字段说明见 [DEVELOPMENT.md](DEVELOPMENT.md)。
