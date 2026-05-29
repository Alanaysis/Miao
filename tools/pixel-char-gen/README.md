# pixel-char-gen

像素化角色精灵生成工具，为 Godot 游戏项目生成像素风角色美术资源。

## 功能

- 将任意图片像素化为指定尺寸（最近邻插值，保持像素锐利）
- 限制调色板颜色数（中位切分法）
- 对比度校验（确保角色与背景可区分）
- 多帧拼接为 sprite sheet
- AI 生成角色动画帧（支持 6 种后端）
- 网络搜索免费素材（OpenGameArt、itch.io）
- 导出 Godot .tres SpriteFrames 资源文件

## 安装

```bash
# 创建虚拟环境
python3 -m venv tools/pixel-char-gen/.venv
source tools/pixel-char-gen/.venv/bin/activate

# 安装依赖
pip install Pillow requests

# 可选：本地 Stable Diffusion（需 GPU 4GB+）
pip install diffusers transformers accelerate torch

# 可选：OpenAI DALL-E
pip install openai
```

## 使用

### 1. 单图像素化

```bash
python tools/pixel-char-gen/cli.py --input ./character.png --width 32 --height 32
```

### 2. 从配置文件批量处理

```bash
# 处理所有角色
python tools/pixel-char-gen/cli.py --config tools/pixel-char-gen/pixel_char_config.json

# 处理指定角色
python tools/pixel-char-gen/cli.py --config tools/pixel-char-gen/pixel_char_config.json --name warrior
```

### 3. 生成 sprite sheet

```bash
python tools/pixel-char-gen/cli.py --input-dir ./pixel/ --sheet-only --width 32 --height 32
```

### 4. 网络搜索素材

```bash
# 从 OpenGameArt 搜索
python tools/pixel-char-gen/cli.py --search "pixel warrior" --count 5 --output ./downloaded/

# 从 itch.io 搜索
python tools/pixel-char-gen/cli.py --search "pixel character" --source itchio --count 10
```

### 5. AI 生成角色

```bash
# 需要先配置 AI 后端（设置环境变量或安装 diffusers）
python tools/pixel-char-gen/cli.py --ai-generate --config tools/pixel-char-gen/pixel_char_config.json --name warrior
```

## 配置文件

`pixel_char_config.json` 格式：

```json
{
  "characters": [
    {
      "name": "warrior",
      "width": 32,
      "height": 32,
      "style": "chibi-fantasy",
      "palette_limit": 16,
      "min_rgb_contrast": 40,
      "background_rgb": [0, 0, 0],
      "clothing": "plate armor, red cape",
      "weapon": "sword and shield",
      "animations": {
        "idle": { "frames": 4, "fps": 6 },
        "walk_down": { "frames": 6, "fps": 8 },
        "attack": { "frames": 6, "fps": 10 }
      }
    }
  ],
  "source": {
    "backend_priority": ["local_sd", "liblibai", "tongyi", "zhipu", "openai", "cpu_sd"]
  }
}
```

## AI 后端

按优先级自动选择：

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
├── idle_sheet.png
├── walk_down_sheet.png
├── attack_sheet.png
├── *_sheet.png.import   # Godot 导入文件
└── *_frames.tres        # Godot SpriteFrames 资源
```

## 测试

```bash
# 生成测试角色动画帧（程序化绘制，不需要 AI）
source tools/pixel-char-gen/.venv/bin/activate
python tools/pixel-char-gen/test_generate.py

# 处理测试角色
python tools/pixel-char-gen/cli.py --config tools/pixel-char-gen/pixel_char_config.json --name warrior
```
