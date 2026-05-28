# PlugHub Packages

这是从 PlugHub 主框架中分离出来的外部插件包项目。主框架不再包含任何业务功能，本目录用于独立开发、构建和投放插件包。

当前包含两个独立插件包：

- `PlugHub.DuctPreferredJunction`：切换风管 Tee/Tap 首选连接。
- `PlugHub.FamilyMaterialParameters`：批量为族文件添加并关联材质参数。

插件包清单是 `package.json`。`dist/*.dll` 是 PlugHub 直接从 GitHub 来源加载时需要的运行时产物，由 GitHub Actions 自动构建并回写。

开发与接入说明见 [DEVELOPMENT.md](DEVELOPMENT.md)。
