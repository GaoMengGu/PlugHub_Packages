# 🛠️ PlugHub 通用图标设计语言规范

## 1. 核心设计哲学 (Design Philosophy)

PlugHub 的图标设计核心在于“极简的几何抽象”。无论多么复杂的功能或概念（无论是软件底层的逻辑，还是具体的业务工具），都必须剥离所有写实细节，将其降维、凝练为最基础的几何形体或符号组合。

## 2. 视觉特征特征 (Visual Specifications) — 核心硬性指标

### 🎨 颜色与图层

- **绝对扁平化（100% Flat）：** 严禁使用任何渐变色、阴影、高光、描边或 3D 透视效果。

- **高对比单色（Monochrome）：** 统一使用纯黑/深碳灰（如 `#1A1A1A`）作为图标主体。最终交付必须是透明背景 PNG；纯白（`#FFFFFF`）背景只允许用于设计预览或对比检查，不得写入 Revit 图标成品。

### 📐 图形特征（形状语言）

- **以“块”代“线”（Solid Glyph）：** 优先使用实心的块状剪影。如果必须使用线条（如箭头、关联线），线条必须具备**极高的粗细体量**，使其在视觉上等同于“块”，严禁出现精致的细线条。

- **微圆角包边（Micro Rounded Corners）：** 所有生硬的几何转角（矩形、多边形边缘）必须做微小的圆角处理。这能让硬朗的工业、技术图形在视觉上显得精致、现代，不呆板。

- **高负空间利用（Negative Space）：** 善于在大块的实心几何体内部进行“镂空”切割，用负空间来表达次级概念或细节，保证图标在小尺寸（如 16x16）下依然清晰可辨。

### 🧩 Revit Ribbon 输出约束（硬性指标）

- **画布尺寸：** 图标画布必须严格为 `32×32px`。

- **安全区：** 图标主体建议控制在居中的 `24×24px` 安全区内，即四周保留 `4px` 透明留白，避免 Revit Ribbon 原生渲染时被裁切。

- **文件格式：** 使用带 Alpha 通道的透明底 PNG。最终 PNG 不得包含描边、阴影、渐变、白底、彩色底或其他多余底色。

- **缩放策略：** 高分屏由 Revit/系统自动缩放，不额外制作 `@2x`、`64×64` 或其他多倍图；仓库图标成品只保留 `32×32` 原图。

### 🤖 默认生成方式

- **默认使用文生图生成：** 新增或重做功能图标时，先使用本文档的提示词模板进行文生图，生成符合 PlugHub 设计语言的候选图标。

- **后处理必须收敛到 Revit 成品规格：** 文生图输出不得直接入库。必须经过透明背景处理、居中缩放、32×32 画布裁切和安全区检查，确保最终 PNG 满足 Revit Ribbon 输出约束。

- **允许本地程序化后处理：** 本地脚本可以用于去底、转 Alpha、缩放、裁切、压缩和像素级验证；不得借后处理引入渐变、阴影、描边、白底或偏离文生图候选语义的新图形。

- **验收优先于生成方式：** 无论文生图源文件尺寸多大，最终提交到 `icons/` 的只能是通过校验的 `32×32` 透明 PNG。

## 3. 核心图形抽象公式 (Abstraction Methodology)

任何新功能在设计图标时，请直接套用以下“三步降维法”：

1. **提取核心动词/名词：** 例如“批量导出”，核心是“多”和“向外”。

2. **剔除写实，寻找几何本体：** “多”转化为重叠的方块或阵列，“向外”转化为粗壮的单向箭头。

3. **PlugHub 风格化：** 将方块和箭头加粗、转角做微圆角、合并为一个高对比度的黑色剪影。

## 4. 面向 Agent 的文生图提示词模板 (Universal Text-to-Image Prompt)

```
Role: Expert UI/UX Icon Designer
Task: Generate a professional app icon candidate matching a specific strict minimalistic design system.

[Icon Concept]
Create a flat, solid glyph icon that abstractly represents: [在这里输入新功能/概念，例如：Data Sync / User Permission / Batch Process].
The concept should be highly simplified into basic geometric shapes (like blocks, arrows, matrices, or clean overlapping silhouettes). Do NOT draw realistic objects or fine lines.

[Visual Style Constraints - STRICT]
1. Style: Ultra-minimalist, 100% flat design, solid glyph icon. NO gradients, NO 3D shading, NO fine details, NO outline strokes.
2. Color: Strictly monochrome. Solid dark charcoal (#1A1A1A). Final asset must use a transparent background; use a pure white background only for preview if needed.
3. Shape Language: Heavy visual weight. If lines or arrows are used, they must be very thick and bold. All sharp corners and edges must have a subtle, micro-rounded finish (smooth geometry).
4. Scale & Contrast: The design must rely heavily on positive/negative space contrast, making it instantly recognizable even at a small 16x16 pixel size.
5. Canvas: Final PNG must be exactly 32x32 px, with the glyph centered inside a 24x24 px safe area and 4 px transparent padding on all sides.

Output: Only the solid monochrome icon candidate, perfectly centered, without any text, frame, outline, shadow, gradient, or background fill. The final repository asset will be post-processed into a transparent-background 32x32 PNG.
```
