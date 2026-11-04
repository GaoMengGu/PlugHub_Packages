# 任务分支命名约定

这是一套适用于所有项目的新任务分支的通用约定。它帮助人和 Codex 从分支名快速判断工作目的。

## 格式

```text
type/short-description
```

`short-description` 建议使用简短的小写英文单词，并用连字符连接；不强制任务编号或额外目录层级。示例：`fix/package-validator`。

## 类型

| 前缀 | 使用场景 | 示例 |
| --- | --- | --- |
| `feature/` | 新增功能 | `feature/family-export` |
| `fix/` | 常规 bug 修复 | `fix/package-validator` |
| `hotfix/` | 已发布版本的紧急修复 | `hotfix/startup-crash` |
| `docs/` | 文档或开发约定 | `docs/branch-naming` |
| `refactor/` | 不改变预期外部行为的代码重构 | `refactor/template-reader` |
| `perf/` | 性能优化 | `perf/category-lookup` |
| `test/` | 以新增或调整测试为主 | `test/package-validation` |
| `build/` | 构建、打包或依赖调整 | `build/update-sdk` |
| `ci/` | CI 工作流及自动化流水线调整 | `ci/package-build` |
| `chore/` | 其余可明确归为维护的任务 | `chore/clean-unused-config` |
| `revert/` | 撤销既有改动 | `revert/export-change` |
| `release/` | 发布准备 | `release/v1-2-0` |
| `codex/` | 无法明确归类时的例外兜底 | `codex/investigate-package-flow` |

## 选择规则

- 混合任务按主要目的选择一个前缀。新增功能同时附带测试和文档仍使用 `feature/`；修复已知 bug 并补回归测试使用 `fix/`。
- `hotfix/` 表示已发布版本的紧急修复，不自动改变发布或合并流程。
- `codex/` 只在任务无法明确归类时使用；能够归类的任务应使用对应前缀。
- 规则只约束实施后创建的新任务分支。现有分支保持原名，不迁移、重命名或删除远程引用。
- `main`、`develop` 等长期分支保留原名；分支起点和合并目标继续遵循项目原有约定。

## 如何采用

把本文件复制到其他项目，或在项目开发说明中链接它。当前用户的 Codex 默认规则另见 `~/.codex/AGENTS.md`；该文件只影响使用同一 Codex home 的新会话。

Codex 每次运行开始时读取指引链：默认全局位置是 `~/.codex`，也可以由 `CODEX_HOME` 指定；同一目录的 `AGENTS.override.md` 优先于 `AGENTS.md`，项目中更近的 `AGENTS.override.md` 或 `AGENTS.md` 会在后面覆盖全局规则。修改后请开启新会话。加载方式见 [OpenAI Docs](https://learn.chatgpt.com/docs/agent-configuration/agents-md)。

采用全局规则时，先确认实际的 Codex home 和覆盖文件，在生效入口保留其他指引并加入完整规则。遇到项目规则冲突，应明确该项目采用的约定，保留覆盖文件供审查；不要批量删除它们。其他成员、主机或代理工具需要各自采用本文件或等价规则；本机写入不会自动改变它们的行为。

## 验证边界

可以使用 `git check-ref-format --branch <branch-name>` 检查候选分支名。该命令只检查 Git 引用格式，不判断任务类型是否选对；类型选择仍按上表和主要目的判断。本约定不新增脚本、Git hook 或 CI 拒绝规则。
