#!/usr/bin/env python3
"""生成测试用的角色动画帧（程序化绘制，不需要 AI）
角色固定在画布中心，只动局部（手臂/腿/武器），确保像素对齐
"""

import os
from pathlib import Path

from PIL import Image, ImageDraw


OUTPUT_DIR = Path(__file__).parents[2] / "project/assets/sprites/characters/warrior/raw"
TARGET_SIZE = 128  # 生成大图，后续像素化到 32x32
CX, CY = 64, 55   # 角色中心固定点


def draw_character(draw: ImageDraw.ImageDraw,
                   arm_angle: str = "down",
                   leg_phase: str = "stand",
                   weapon_angle: str = "down",
                   head_tilt: int = 0,
                   cape_sway: int = 0):
    """绘制角色，所有部件相对于固定中心点 CX, CY"""
    cx, cy = CX, CY

    # === 身体（蓝色盔甲）===
    draw.rectangle([cx - 12, cy - 5, cx + 12, cy + 20], fill=(50, 80, 180, 255))

    # === 头 ===
    hx = cx + head_tilt
    draw.ellipse([hx - 8, cy - 22, hx + 8, cy - 5], fill=(220, 180, 140, 255))

    # 眼睛
    draw.rectangle([hx - 5, cy - 15, hx - 2, cy - 10], fill=(255, 255, 255, 255))
    draw.rectangle([hx + 2, cy - 15, hx + 5, cy - 10], fill=(255, 255, 255, 255))
    draw.rectangle([hx - 4, cy - 14, hx - 3, cy - 11], fill=(30, 30, 30, 255))
    draw.rectangle([hx + 3, cy - 14, hx + 4, cy - 11], fill=(30, 30, 30, 255))

    # === 腿 ===
    if leg_phase == "stand":
        draw.rectangle([cx - 8, cy + 20, cx - 3, cy + 38], fill=(60, 50, 40, 255))
        draw.rectangle([cx + 3, cy + 20, cx + 8, cy + 38], fill=(60, 50, 40, 255))
    elif leg_phase == "walk_left":
        draw.rectangle([cx - 12, cy + 20, cx - 7, cy + 36], fill=(60, 50, 40, 255))
        draw.rectangle([cx + 5, cy + 20, cx + 10, cy + 40], fill=(60, 50, 40, 255))
    elif leg_phase == "walk_right":
        draw.rectangle([cx - 10, cy + 20, cx - 5, cy + 40], fill=(60, 50, 40, 255))
        draw.rectangle([cx + 7, cy + 20, cx + 12, cy + 36], fill=(60, 50, 40, 255))

    # === 手臂 ===
    if arm_angle == "down":
        draw.rectangle([cx - 18, cy - 2, cx - 12, cy + 15], fill=(220, 180, 140, 255))
        draw.rectangle([cx + 12, cy - 2, cx + 18, cy + 15], fill=(220, 180, 140, 255))
    elif arm_angle == "left_up":
        draw.rectangle([cx - 20, cy - 10, cx - 14, cy + 5], fill=(220, 180, 140, 255))
        draw.rectangle([cx + 12, cy - 2, cx + 18, cy + 15], fill=(220, 180, 140, 255))
    elif arm_angle == "both_up":
        draw.rectangle([cx - 20, cy - 15, cx - 14, cy + 0], fill=(220, 180, 140, 255))
        draw.rectangle([cx + 14, cy - 15, cx + 20, cy + 0], fill=(220, 180, 140, 255))
    elif arm_angle == "attack":
        draw.rectangle([cx - 18, cy - 2, cx - 12, cy + 15], fill=(220, 180, 140, 255))
        draw.rectangle([cx + 12, cy - 5, cx + 30, cy + 2], fill=(220, 180, 140, 255))

    # === 武器（剑）===
    if weapon_angle == "down":
        draw.rectangle([cx + 14, cy + 15, cx + 17, cy + 35], fill=(180, 180, 200, 255))
    elif weapon_angle == "attack":
        draw.rectangle([cx + 28, cy - 8, cx + 50, cy - 4], fill=(180, 180, 200, 255))
        draw.polygon([(cx + 50, cy - 10), (cx + 55, cy - 6), (cx + 50, cy - 2)],
                     fill=(220, 220, 240, 255))

    # === 披风 ===
    sx = cape_sway
    draw.polygon([(cx - 10, cy - 3), (cx + 10, cy - 3),
                  (cx + 8 + sx, cy + 18), (cx - 8 + sx, cy + 18)],
                 fill=(180, 40, 40, 200))


def render_frame(arm="down", leg="stand", weapon="down",
                 head_tilt=0, cape_sway=0) -> Image.Image:
    """渲染单帧并保存"""
    img = Image.new("RGBA", (TARGET_SIZE, TARGET_SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw_character(draw, arm_angle=arm, leg_phase=leg,
                   weapon_angle=weapon, head_tilt=head_tilt,
                   cape_sway=cape_sway)
    return img


def save_frames(prefix: str, configs: list[dict]):
    """保存一组动画帧"""
    for i, cfg in enumerate(configs):
        img = render_frame(**cfg)
        path = os.path.join(OUTPUT_DIR, f"{prefix}_{i:02d}.png")
        img.save(path)
        print(f"{prefix}_{i:02d}.png")


def main() -> None:

    os.makedirs(OUTPUT_DIR, exist_ok=True)

    # ============================================================
    # idle: 4帧，只有披风微微摆动 + 头微倾
    # ============================================================
    save_frames("idle", [
        {"cape_sway": 0, "head_tilt": 0},
        {"cape_sway": 1, "head_tilt": 1},
        {"cape_sway": 0, "head_tilt": 0},
        {"cape_sway": -1, "head_tilt": -1},
    ])

    # ============================================================
    # walk_down: 6帧，交替迈步 + 手臂摆动
    # ============================================================
    save_frames("walk_down", [
        {"arm": "down", "leg": "stand"},
        {"arm": "left_up", "leg": "walk_left"},
        {"arm": "down", "leg": "stand"},
        {"arm": "left_up", "leg": "walk_right"},
        {"arm": "down", "leg": "stand"},
        {"arm": "left_up", "leg": "walk_left"},
    ])

    # ============================================================
    # attack: 6帧，蓄力→举剑→刺出→收回
    # ============================================================
    save_frames("attack", [
        {"arm": "down", "leg": "stand", "weapon": "down"},
        {"arm": "left_up", "leg": "stand", "weapon": "down", "head_tilt": 1},
        {"arm": "both_up", "leg": "walk_right", "weapon": "down", "head_tilt": 2},
        {"arm": "attack", "leg": "walk_right", "weapon": "attack", "head_tilt": 2},
        {"arm": "attack", "leg": "stand", "weapon": "attack", "head_tilt": 1},
        {"arm": "down", "leg": "stand", "weapon": "down"},
    ])

    # ============================================================
    # hurt: 3帧，手臂张开 + 披风甩动
    # ============================================================
    save_frames("hurt", [
        {"arm": "down", "leg": "stand", "cape_sway": 0},
        {"arm": "both_up", "leg": "walk_left", "cape_sway": -3},
        {"arm": "down", "leg": "stand", "cape_sway": -1},
    ])

    total = 4 + 6 + 6 + 3
    print(f"\n生成完成! 共 {total} 帧 -> {OUTPUT_DIR}")


if __name__ == '__main__':
    main()
