"""
像素化核心引擎
负责图片像素化、调色板限制、sprite sheet 拼接、对比度校验
"""

from PIL import Image, ImageDraw
import os
import json
from typing import Optional


def pixelate(image: Image.Image, target_w: int, target_h: int) -> Image.Image:
    """将图片缩放到目标像素尺寸，使用最近邻插值保持像素锐利"""
    return image.resize((target_w, target_h), Image.NEAREST)


def reduce_palette(image: Image.Image, max_colors: int) -> Image.Image:
    """限制调色板颜色数"""
    if max_colors <= 0:
        return image
    img = image.convert("RGBA")
    # RGBA 图片只能用 FASTOCTREE 方法
    quantized = img.quantize(colors=max_colors, method=Image.Quantize.FASTOCTREE)
    return quantized.convert("RGBA")


def check_contrast(image: Image.Image, min_contrast: int,
                   bg_rgb: tuple[int, int, int]) -> Image.Image:
    """校验每个像素与背景色的对比度，不满足的像素变为半透明"""
    img = image.convert("RGBA")
    pixels = img.load()
    w, h = img.size

    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a < 128:  # 跳过透明像素
                continue
            # 计算 RGB 欧氏距离
            dist = ((r - bg_rgb[0]) ** 2 +
                    (g - bg_rgb[1]) ** 2 +
                    (b - bg_rgb[2]) ** 2) ** 0.5
            if dist < min_contrast:
                # 降低不透明度作为标记（不删除，保留信息）
                pixels[x, y] = (r, g, b, max(30, int(a * 0.3)))

    return img


def process_single_image(image: Image.Image, target_w: int, target_h: int,
                         palette_limit: int = 0,
                         min_contrast: int = 0,
                         bg_rgb: tuple = (0, 0, 0)) -> Image.Image:
    """完整处理流程：像素化 → 调色板限制 → 对比度校验"""
    result = pixelate(image, target_w, target_h)
    if palette_limit > 0:
        result = reduce_palette(result, palette_limit)
    if min_contrast > 0:
        result = check_contrast(result, min_contrast, bg_rgb)
    return result


def generate_sprite_sheet(frames: list[Image.Image],
                          cols: Optional[int] = None) -> Image.Image:
    """将多帧图片拼成 sprite sheet

    Args:
        frames: 帧图片列表（尺寸必须一致）
        cols: 每行帧数，None 则自动计算为尽量方正的布局
    """
    if not frames:
        raise ValueError("帧列表为空")

    frame_w, frame_h = frames[0].size
    total = len(frames)

    if cols is None:
        # 自动计算：尽量接近正方形
        cols = int(total ** 0.5)
        if cols * cols < total:
            cols += 1

    rows = (total + cols - 1) // cols

    sheet = Image.new("RGBA", (cols * frame_w, rows * frame_h), (0, 0, 0, 0))

    for i, frame in enumerate(frames):
        x = (i % cols) * frame_w
        y = (i // cols) * frame_h
        sheet.paste(frame, (x, y))

    return sheet


def load_frames_from_dir(dir_path: str,
                         prefix: str = "") -> list[Image.Image]:
    """从目录加载帧图片，按文件名排序"""
    files = sorted([
        f for f in os.listdir(dir_path)
        if f.endswith(".png") and f.startswith(prefix)
    ])
    frames = []
    for f in files:
        img = Image.open(os.path.join(dir_path, f))
        frames.append(img.convert("RGBA"))
    return frames


def save_frames(frames: list[Image.Image], output_dir: str,
                prefix: str = "") -> list[str]:
    """逐帧保存为 PNG，返回文件路径列表"""
    os.makedirs(output_dir, exist_ok=True)
    paths = []
    for i, frame in enumerate(frames):
        filename = f"{prefix}_{i:02d}.png" if prefix else f"{i:02d}.png"
        path = os.path.join(output_dir, filename)
        frame.save(path, "PNG")
        paths.append(path)
    return paths


def save_sprite_sheet(sheet: Image.Image, output_path: str) -> str:
    """保存 sprite sheet"""
    os.makedirs(os.path.dirname(output_path) or ".", exist_ok=True)
    sheet.save(output_path, "PNG")
    return output_path


def process_animation_frames(raw_frames: dict[str, list[Image.Image]],
                             target_w: int, target_h: int,
                             palette_limit: int = 0,
                             min_contrast: int = 0,
                             bg_rgb: tuple = (0, 0, 0)
                             ) -> dict[str, list[Image.Image]]:
    """批量处理所有动画帧

    Args:
        raw_frames: {"idle": [img1, img2], "walk": [img3, img4], ...}
        target_w, target_h: 目标像素尺寸

    Returns:
        处理后的帧字典，格式同输入
    """
    processed = {}
    for anim_name, frames in raw_frames.items():
        processed[anim_name] = [
            process_single_image(f, target_w, target_h,
                                 palette_limit, min_contrast, bg_rgb)
            for f in frames
        ]
    return processed


def load_image(path: str) -> Image.Image:
    """加载图片文件"""
    return Image.open(path).convert("RGBA")
