#!/usr/bin/env python3
"""生成 PlugHub 自动保存插件图标 - 32x32 扁平化风格，保存/软盘图标"""
from PIL import Image, ImageDraw

SIZE = 32
img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

# 背景：深蓝灰圆角矩形
bg = (40, 60, 120, 255)  # 深蓝色
draw.rounded_rectangle([1, 1, SIZE-1, SIZE-1], radius=5, fill=bg)

# 保存图标（软盘形状）- 白色
# 主体外框
draw.rectangle([7, 5, 25, 27], fill=(255, 255, 255, 255))

# 顶部切口（金属滑片窗口）
draw.rectangle([11, 5, 21, 10], fill=bg)

# 右侧小方形（写保护缺口）
draw.rectangle([21, 6, 24, 9], fill=bg)

# 底部标签线
draw.rectangle([11, 20, 21, 22], fill=bg)
draw.rectangle([11, 24, 21, 25], fill=bg)

img.save("/home/yilan/hermes-project/PlugHub_Packages/icons/auto-save.png")
print("图标已生成: icons/auto-save.png")
