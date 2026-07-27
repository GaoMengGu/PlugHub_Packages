# 湖北报规参数模板说明

湖北报规模块不再内置 HIFC、mini 清单或 IFC 到 Revit 类别的推导规则。每次运行时用户选择一个 CSV 模板，插件仅验证并执行模板内容。

## CSV 模板

模板为 UTF-8 with BOM CSV，固定表头和顺序如下。用 Excel 另存时选择“CSV UTF-8（逗号分隔）”。

```csv
属性集名称,参数类型,IFC构件,Revit构件,属性名称,属性类型,默认值,真实数据
```

可从 `docs/HubeiReportParameters-Template.csv` 复制并用 Excel 编辑。`Revit构件` 有多个类别时使用英文逗号分隔，整个单元格以 CSV 双引号包裹。

| 列 | 用途 |
| --- | --- |
| 属性集名称 | HIFC `PropertySet` 名称 |
| 参数类型 | `I` 为实例参数、`T` 为类型参数；也写入 HIFC 映射 |
| IFC构件 | 写入 HIFC 映射 |
| Revit构件 | 以中文类别名指定共享参数绑定；可用逗号绑定多个类别 |
| 属性名称 | HIFC 属性名称和共享参数名称 |
| 属性类型 | 当前 Revit 版本的 `ParameterType` 枚举名称，同时写入 HIFC 映射 |
| 默认值 | 真实数据为空时写入的值 |
| 真实数据 | 非空时优先写入并覆盖目标参数现有值 |

## 输出与赋值

项目必须先保存。插件在项目目录生成 `项目名-HIFC.txt`，使用模板中的 `属性集名称、参数类型、IFC构件、属性名称、属性类型` 生成，例如：

```text
PropertySet:	Pset_建筑技术信息属性集	I	IfcBuilding
    建筑高度	Length	建筑高度
```

参数值始终由模板控制：`真实数据` 非空时使用真实数据；为空时使用默认值。唯一执行选项为“清除当前项目同名参数”。未勾选时，已有同名参数只有在 `I/T` 和 `ParameterType` 与模板一致时才会复用并补齐类别；否则停止并提示冲突。

## Revit 类别

模板可使用建筑、结构、暖通、给排水和电气的常用中文类别，例如墙、门、窗、楼板、结构柱、风管、管道、电缆桥架等。完整可用目录由 `src/PlugHub.HubeiReportParameters/RevitCategoryCatalog.cs` 集中维护；未支持类别会在模板预校验阶段按行号提示。
