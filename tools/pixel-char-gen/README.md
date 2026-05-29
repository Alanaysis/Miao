# pixel-char-gen

像素化角色精灵生成工具，为 Godot 游戏项目生成像素风角色美术资源。

## 功能

- 将任意图片像素化为指定尺寸（最近邻插值，保持像素锐利）
- 限制调色板颜色数（中位切分法）
- 对比度校验（确保角色与背景可区分）
- 多帧拼接为 sprite sheet
- AI 生成角色动画帧（支持 6 种后端）
- 导出 Godot .tres SpriteFrames 资源文件

## 安装

```bash
# 创建虚拟环境
python3 -m venv tools/pixel-char-gen/.venv
source tools/pixel-char-gen/.venv/bin/activate

# 安装依赖
pip install Pillow requests

# 可选：本地 Stable Diffusion
pip install diffusers transformers accelerate torch

# 可选：OpenAI DALL-E
pip install openai
```

## 使用

```bash
# 激活虚拟环境
source tools/pixel-char-gen/.venv/bin/activate

# 从本地图片像素化
python tools/pixel-char-gen/cli.py --input ./raw.png --width 32 --height 32

# 从配置文件批量生成
python tools/pixel-char-gen/cli.py --config tools/pixel-char-gen/pixel_char_config.json

# 仅生成 sprite sheet
python tools/pixel-char-gen/cli.py --input-dir ./pixel/ --sheet-only
```

## AI 后端

按优先级自动选择可用后端：

| 后端 | 环境变量 | 说明 |
|------|----------|------|
| local_sd | - | 本地 SD，需 GPU 4GB+ |
| liblibai | LIBLIBAI_API_KEY | 国内 SD 平台 |
| tongyi | TONGYI_API_KEY | 通义万相 |
| zhipu | ZHIPU_API_KEY | 智谱 CogView |
| openai | OPENAI_API_KEY | DALL-E 3 |
| cpu_sd | - | CPU 兜底，慢 |

## 输出结构

```
project/assets/sprites/characters/<角色名>/
├── raw/           # 原始图片
├── pixel/         # 像素化逐帧 PNG
├── sprite_sheet.png
├── sprite_sheet.png.import
└── <角色名>_frames.tres
```

## 配置

参见 `pixel_char_config.json` 配置模板。
