# pixel-char-gen Skill

像素化角色精灵生成工具。用于为 Godot 游戏项目生成像素风角色美术资源。

## 使用方式

运行 CLI 工具生成角色精灵：

```bash
# 从本地图片像素化
python tools/pixel-char-gen/cli.py --input <图片路径> --width <宽> --height <高> --output <输出目录>

# 从配置文件批量生成
python tools/pixel-char-gen/cli.py --config tools/pixel-char-gen/pixel_char_config.json

# 仅生成 sprite sheet
python tools/pixel-char-gen/cli.py --input-dir <逐帧图片目录> --sheet-only --width <宽> --height <高>
```

## 参数说明

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `--input` | 输入图片路径 | - |
| `--input-dir` | 输入目录（逐帧图片） | - |
| `--width` | 目标宽度（像素） | 32 |
| `--height` | 目标高度（像素） | 32 |
| `--palette-limit` | 最大颜色数 | 16 |
| `--min-contrast` | 与背景最小 RGB 对比度 | 40 |
| `--bg-rgb` | 背景色 R,G,B | 0,0,0 |
| `--output` | 输出目录 | ./output |
| `--config` | 配置文件路径（JSON） | - |
| `--name` | 指定配置中的角色名 | - |
| `--sheet-only` | 仅生成 sprite sheet | - |
| `--cols` | sprite sheet 每行帧数 | 自动 |

## 配置文件格式

配置文件为 JSON 格式，定义在 `tools/pixel-char-gen/pixel_char_config.json`。

关键字段：
- `characters[].name` — 角色名称
- `characters[].width/height` — 像素尺寸
- `characters[].style` — 美术风格
- `characters[].palette_limit` — 颜色数限制
- `characters[].animations` — 动画定义（帧数和 FPS）
- `source.backend_priority` — AI 后端优先级
- `output.dir` — 输出目录

## 输出结构

```
project/assets/sprites/characters/<角色名>/
├── raw/           # 原始图片
├── pixel/         # 像素化逐帧 PNG
├── sprite_sheet.png
├── sprite_sheet.png.import
└── <角色名>_frames.tres
```

## AI 后端

工具按优先级自动选择可用的 AI 后端：
1. `local_sd` — 本地 Stable Diffusion（需 GPU 4GB+）
2. `liblibai` — LiblibAI API（国内 SD 平台）
3. `tongyi` — 通义万相 API（阿里云）
4. `zhipu` — 智谱 CogView API
5. `openai` — OpenAI DALL-E 3
6. `cpu_sd` — 纯 CPU SD（慢速兜底）

配置对应 API Key 环境变量即可使用。

## 依赖

```bash
pip install Pillow requests

# 可选（本地 SD）
pip install diffusers transformers accelerate torch

# 可选（OpenAI）
pip install openai
```
