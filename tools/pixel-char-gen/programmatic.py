"""
程序化角色动画生成器
基于 PIL 绘制像素角色，支持配置驱动
"""

from PIL import Image, ImageDraw
import os
import json


# ============================================================
# 通用模板定义（部位布局 + 动画配置）
# ============================================================

TEMPLATES = {
    "humanoid": {
        "description": "Humanoid character (2 arms, 2 legs)",
        "required_parts": ["head", "torso", "left_arm", "right_arm", "left_leg", "right_leg"],
        "optional_parts": ["weapon", "cape", "shield", "helmet"],
        "default_rects": {
            "head":      [11, 2, 21, 10],
            "torso":     [10, 10, 22, 22],
            "left_arm":  [6, 10, 10, 20],
            "right_arm": [22, 10, 26, 20],
            "left_leg":  [11, 22, 15, 30],
            "right_leg": [17, 22, 21, 30],
            "weapon":    [24, 14, 27, 30],
            "cape":      [8, 10, 24, 20],
            "shield":    [3, 12, 7, 22],
            "helmet":    [10, 0, 22, 8],
        },
        "animations": {
            "idle":      {"frames": 4, "fps": 6, "loop": True},
            "walk_down": {"frames": 6, "fps": 8, "loop": True},
            "attack":    {"frames": 6, "fps": 10, "loop": False},
            "hurt":      {"frames": 3, "fps": 8, "loop": False},
        }
    },
    "quadruped": {
        "description": "Four-legged creature",
        "required_parts": ["head", "body", "front_left_leg", "front_right_leg", "back_left_leg", "back_right_leg"],
        "optional_parts": ["tail", "horns", "wings"],
        "default_rects": {
            "head":             [18, 4, 28, 12],
            "body":             [6, 10, 26, 22],
            "front_left_leg":   [8, 22, 12, 30],
            "front_right_leg":  [14, 22, 18, 30],
            "back_left_leg":    [20, 22, 24, 30],
            "back_right_leg":   [26, 22, 30, 30],
            "tail":             [0, 8, 6, 16],
            "horns":            [20, 0, 28, 6],
            "wings":            [0, 6, 6, 20],
        },
        "animations": {
            "idle":      {"frames": 4, "fps": 6, "loop": True},
            "walk_down": {"frames": 6, "fps": 8, "loop": True},
            "attack":    {"frames": 4, "fps": 10, "loop": False},
            "hurt":      {"frames": 3, "fps": 8, "loop": False},
        }
    },
    "slime": {
        "description": "Single-body blob creature",
        "required_parts": ["body"],
        "optional_parts": ["eyes", "accessory"],
        "default_rects": {
            "body":      [6, 8, 26, 28],
            "eyes":      [12, 12, 20, 18],
            "accessory": [14, 4, 18, 10],
        },
        "animations": {
            "idle":      {"frames": 2, "fps": 4, "loop": True},
            "walk_down": {"frames": 4, "fps": 6, "loop": True},
            "attack":    {"frames": 4, "fps": 8, "loop": False},
            "hurt":      {"frames": 2, "fps": 6, "loop": False},
        }
    },
}


# ============================================================
# 身体部位默认配置（兼容旧代码）
# ============================================================

DEFAULT_BODY_PARTS = {
    "head":      {"rect": [11, 2, 21, 10],  "color": "#dcb48c"},
    "torso":     {"rect": [10, 10, 22, 22], "color": "#3250b4"},
    "left_arm":  {"rect": [6, 10, 10, 20],  "color": "#dcb48c"},
    "right_arm": {"rect": [22, 10, 26, 20], "color": "#dcb48c"},
    "left_leg":  {"rect": [11, 22, 15, 30], "color": "#3c3228"},
    "right_leg": {"rect": [17, 22, 21, 30], "color": "#3c3228"},
    "weapon":    {"rect": [24, 14, 27, 30], "color": "#b4b4c8"},
    "cape":      {"rect": [8, 10, 24, 20],  "color": "#b42828"},
}


# ============================================================
# 预设配置（基于 humanoid 模板 + 具体颜色）
# ============================================================

def _make_preset(template_name: str, colors: dict, name: str = "") -> dict:
    """基于模板和颜色生成预设配置"""
    tmpl = TEMPLATES[template_name]
    body_parts = {}
    for part in tmpl["required_parts"] + tmpl["optional_parts"]:
        if part in tmpl["default_rects"]:
            body_parts[part] = {
                "rect": tmpl["default_rects"][part],
                "color": colors.get(part, "#888888")
            }
    return {
        "name": name or template_name,
        "template": template_name,
        "body_parts": body_parts,
        "animations": tmpl["animations"],
    }


PRESETS = {
    "warrior": _make_preset("humanoid", {
        "head": "#dcb48c", "torso": "#3250b4", "left_arm": "#dcb48c", "right_arm": "#dcb48c",
        "left_leg": "#3c3228", "right_leg": "#3c3228", "weapon": "#b4b4c8", "cape": "#b42828",
    }, "warrior"),

    "mage": _make_preset("humanoid", {
        "head": "#dcb48c", "torso": "#2244aa", "left_arm": "#dcb48c", "right_arm": "#dcb48c",
        "left_leg": "#223366", "right_leg": "#223366", "weapon": "#aa88ff", "cape": "#1a1a4e",
    }, "mage"),

    "archer": _make_preset("humanoid", {
        "head": "#dcb48c", "torso": "#2d6b3f", "left_arm": "#dcb48c", "right_arm": "#dcb48c",
        "left_leg": "#4a3728", "right_leg": "#4a3728", "weapon": "#8b6914", "cape": "#1a4a2a",
    }, "archer"),

    "rogue": _make_preset("humanoid", {
        "head": "#dcb48c", "torso": "#2a2a2a", "left_arm": "#dcb48c", "right_arm": "#dcb48c",
        "left_leg": "#1a1a1a", "right_leg": "#1a1a1a", "weapon": "#888888", "cape": "#333333",
    }, "rogue"),

    "cleric": _make_preset("humanoid", {
        "head": "#dcb48c", "torso": "#eeeeee", "left_arm": "#dcb48c", "right_arm": "#dcb48c",
        "left_leg": "#cccccc", "right_leg": "#cccccc", "weapon": "#ffdd44", "cape": "#ffffff",
    }, "cleric"),

    "skeleton": _make_preset("humanoid", {
        "head": "#e8e0d0", "torso": "#d0c8b8", "left_arm": "#d0c8b8", "right_arm": "#d0c8b8",
        "left_leg": "#c8c0b0", "right_leg": "#c8c0b0", "weapon": "#888888",
    }, "skeleton"),

    "goblin": _make_preset("humanoid", {
        "head": "#6a8a3a", "torso": "#8b4513", "left_arm": "#6a8a3a", "right_arm": "#6a8a3a",
        "left_leg": "#5a7a2a", "right_leg": "#5a7a2a", "weapon": "#888888",
    }, "goblin"),

    "slime_green": _make_preset("slime", {
        "body": "#44cc44", "eyes": "#ffffff",
    }, "slime_green"),

    "slime_blue": _make_preset("slime", {
        "body": "#4488cc", "eyes": "#ffffff",
    }, "slime_blue"),

    "wolf": _make_preset("quadruped", {
        "head": "#888888", "body": "#666666",
        "front_left_leg": "#555555", "front_right_leg": "#555555",
        "back_left_leg": "#555555", "back_right_leg": "#555555",
        "tail": "#777777",
    }, "wolf"),
}


def hex_to_rgba(hex_color: str, alpha: int = 255) -> tuple:
    """#RRGGBB → (R, G, B, A)"""
    hex_color = hex_color.lstrip('#')
    r, g, b = int(hex_color[0:2], 16), int(hex_color[2:4], 16), int(hex_color[4:6], 16)
    return (r, g, b, alpha)


def draw_character(draw: ImageDraw.ImageDraw, parts: dict,
                   offsets: dict = None, scale: int = 1):
    """绘制角色

    Args:
        draw: PIL ImageDraw
        parts: 身体部位配置 {name: {"rect": [x1,y1,x2,y2], "color": "#hex"}}
        offsets: 每个部位的偏移 {name: (dx, dy)}
        scale: 缩放倍数
    """
    if offsets is None:
        offsets = {}

    for part_name, part_cfg in parts.items():
        rect = part_cfg["rect"]
        color = hex_to_rgba(part_cfg["color"])

        dx, dy = offsets.get(part_name, (0, 0))
        x1 = rect[0] * scale + dx
        y1 = rect[1] * scale + dy
        x2 = rect[2] * scale + dx
        y2 = rect[3] * scale + dy

        # 特殊部位处理
        if part_name == "head":
            # 头用椭圆
            draw.ellipse([x1, y1, x2, y2], fill=color)
            # 眼睛
            cx = (x1 + x2) / 2
            eye_y = y1 + (y2 - y1) * 0.4
            draw.rectangle([cx - 3*scale, eye_y, cx - 1*scale, eye_y + 2*scale], fill=(30, 30, 30, 255))
            draw.rectangle([cx + 1*scale, eye_y, cx + 3*scale, eye_y + 2*scale], fill=(30, 30, 30, 255))
        elif part_name == "cape":
            # 披风用多边形（梯形）
            draw.polygon([
                (x1, y1), (x2, y1),
                (x2 + 2*scale + dx, y2 + 4*scale),
                (x1 - 2*scale + dx, y2 + 4*scale)
            ], fill=color)
        else:
            # 其他部位用矩形
            draw.rectangle([x1, y1, x2, y2], fill=color)


# ============================================================
# 动画帧偏移配置
# ============================================================

ANIMATION_OFFSETS = {
    "idle": [
        {"cape": (0, 0)},
        {"cape": (1, 0), "head": (0, -1)},
        {"cape": (0, 0), "head": (0, 0)},
        {"cape": (-1, 0), "head": (0, -1)},
    ],
    "walk_down": [
        {"left_leg": (0, 0), "right_leg": (0, 0)},
        {"left_leg": (-2, -1), "right_leg": (2, 1), "left_arm": (0, -2)},
        {"left_leg": (0, 0), "right_leg": (0, 0)},
        {"left_leg": (2, 1), "right_leg": (-2, -1), "right_arm": (0, -2)},
        {"left_leg": (0, 0), "right_leg": (0, 0)},
        {"left_leg": (-2, -1), "right_leg": (2, 1), "left_arm": (0, -2)},
    ],
    "attack": [
        {},
        {"right_arm": (0, -4), "weapon": (0, -6)},
        {"right_arm": (0, -8), "weapon": (0, -12), "head": (0, -1)},
        {"right_arm": (6, -2), "weapon": (8, -4), "torso": (2, 0)},
        {"right_arm": (4, 0), "weapon": (6, 0)},
        {},
    ],
    "hurt": [
        {},
        {"left_arm": (-2, -4), "right_arm": (2, -4), "cape": (-3, 0)},
        {"cape": (-1, 0)},
    ],
}


def generate_animation_frames(config: dict, size: int = 32) -> dict[str, list[Image.Image]]:
    """根据配置生成动画帧

    Args:
        config: 角色配置 {"name", "body_parts", "animations"}
        size: 画布尺寸

    Returns:
        {"idle": [Image, ...], "walk_down": [Image, ...], ...}
    """
    parts = config.get("body_parts", DEFAULT_BODY_PARTS)
    animations = config.get("animations", {})
    scale = size // 32  # 基于 32x32 设计，按比例缩放

    result = {}
    for anim_name, anim_cfg in animations.items():
        frame_count = anim_cfg.get("frames", 4)
        offsets_list = ANIMATION_OFFSETS.get(anim_name, ANIMATION_OFFSETS["idle"])

        frames = []
        for i in range(frame_count):
            img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
            draw = ImageDraw.Draw(img)

            # 获取当前帧的部位偏移
            offsets = offsets_list[i % len(offsets_list)] if offsets_list else {}
            # 转换为像素偏移
            pixel_offsets = {}
            for part_name, (dx, dy) in offsets.items():
                pixel_offsets[part_name] = (dx * scale, dy * scale)

            draw_character(draw, parts, pixel_offsets, scale)
            frames.append(img)

        result[anim_name] = frames

    return result


def load_config_from_file(path: str) -> dict:
    """从 JSON 文件加载配置"""
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def save_config(config: dict, path: str):
    """保存配置到 JSON 文件"""
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(config, f, indent=2, ensure_ascii=False)


def get_preset(name: str) -> dict:
    """获取预设配置"""
    if name in PRESETS:
        return PRESETS[name]
    return {"name": name, "body_parts": DEFAULT_BODY_PARTS, "animations": {
        "idle": {"frames": 4, "fps": 6, "loop": True},
        "walk_down": {"frames": 6, "fps": 8, "loop": True},
        "attack": {"frames": 6, "fps": 10, "loop": False},
        "hurt": {"frames": 3, "fps": 8, "loop": False},
    }}


def list_presets() -> list[str]:
    """列出所有预设"""
    return list(PRESETS.keys())
