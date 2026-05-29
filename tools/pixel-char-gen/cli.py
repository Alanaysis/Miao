#!/usr/bin/env python3
"""
pixel-char-gen CLI
像素化角色精灵生成工具

用法：
    # 从本地图片像素化
    python cli.py --input ./raw.png --width 32 --height 32

    # 从配置文件批量生成
    python cli.py --config pixel_char_config.json

    # 仅生成 sprite sheet（已有逐帧图片）
    python cli.py --input-dir ./pixel/ --sheet-only --width 32 --height 32
"""

import argparse
import os
import sys
import json

from pixel_char_gen import (
    load_image, process_single_image, process_animation_frames,
    generate_sprite_sheet, save_frames, save_sprite_sheet,
    load_frames_from_dir
)


def parse_args():
    parser = argparse.ArgumentParser(
        description="像素化角色精灵生成工具"
    )

    # 单图模式
    parser.add_argument("--input", type=str,
                        help="输入图片路径（单图像素化模式）")
    parser.add_argument("--input-dir", type=str,
                        help="输入目录路径（逐帧图片目录）")

    # 像素化参数
    parser.add_argument("--width", type=int, default=32,
                        help="目标宽度（像素），默认 32")
    parser.add_argument("--height", type=int, default=32,
                        help="目标高度（像素），默认 32")
    parser.add_argument("--palette-limit", type=int, default=16,
                        help="最大颜色数，默认 16（0=不限制）")
    parser.add_argument("--min-contrast", type=int, default=40,
                        help="与背景的最小 RGB 对比度，默认 40")
    parser.add_argument("--bg-rgb", type=str, default="0,0,0",
                        help="背景色 RGB，格式 R,G,B，默认 0,0,0")

    # Sprite sheet 参数
    parser.add_argument("--sheet-only", action="store_true",
                        help="仅生成 sprite sheet（需要 --input-dir）")
    parser.add_argument("--cols", type=int, default=None,
                        help="sprite sheet 每行帧数，默认自动计算")

    # 输出
    parser.add_argument("--output", "-o", type=str, default="./output",
                        help="输出目录，默认 ./output")
    parser.add_argument("--prefix", type=str, default="",
                        help="输出文件名前缀")

    # 配置文件模式
    parser.add_argument("--config", type=str,
                        help="配置文件路径（JSON），批量生成模式")
    parser.add_argument("--name", type=str,
                        help="指定配置文件中的角色名称")

    return parser.parse_args()


def parse_bg_rgb(rgb_str: str) -> tuple:
    """解析 R,G,B 字符串"""
    parts = rgb_str.split(",")
    return (int(parts[0]), int(parts[1]), int(parts[2]))


def process_single_image_mode(args):
    """单图像素化模式"""
    img = load_image(args.input)
    bg_rgb = parse_bg_rgb(args.bg_rgb)

    result = process_single_image(
        img, args.width, args.height,
        args.palette_limit, args.min_contrast, bg_rgb
    )

    os.makedirs(args.output, exist_ok=True)
    filename = f"{args.prefix}.png" if args.prefix else "pixel_output.png"
    output_path = os.path.join(args.output, filename)
    result.save(output_path, "PNG")
    print(f"输出: {output_path}")
    return output_path


def process_sheet_only_mode(args):
    """仅 sprite sheet 模式"""
    frames = load_frames_from_dir(args.input_dir, args.prefix)
    if not frames:
        print(f"错误: 目录 {args.input_dir} 中没有找到 PNG 文件")
        sys.exit(1)

    sheet = generate_sprite_sheet(frames, args.cols)
    os.makedirs(args.output, exist_ok=True)
    sheet_path = os.path.join(args.output, "sprite_sheet.png")
    save_sprite_sheet(sheet, sheet_path)
    print(f"Sprite sheet: {sheet_path} ({len(frames)} 帧)")
    return sheet_path


def process_config_mode(args):
    """配置文件批量生成模式"""
    with open(args.config, "r", encoding="utf-8") as f:
        config = json.load(f)

    characters = config.get("characters", [])
    if args.name:
        characters = [c for c in characters if c["name"] == args.name]
        if not characters:
            print(f"错误: 配置中未找到角色 '{args.name}'")
            sys.exit(1)

    output_base = config.get("output", {}).get("dir", args.output)

    for char_config in characters:
        name = char_config["name"]
        w = char_config.get("width", 32)
        h = char_config.get("height", 32)
        palette = char_config.get("palette_limit", 16)
        min_contrast = char_config.get("min_rgb_contrast", 40)
        bg = tuple(char_config.get("background_rgb", [0, 0, 0]))
        animations = char_config.get("animations", {})

        print(f"\n处理角色: {name} ({w}x{h})")

        char_output = os.path.join(output_base, name)
        pixel_dir = os.path.join(char_output, "pixel")

        # 检查是否有 raw 输入目录
        raw_dir = os.path.join(char_output, "raw")
        if os.path.isdir(raw_dir):
            # 从 raw 目录读取帧并像素化
            all_frames = {}
            for anim_name in animations:
                prefix_frames = load_frames_from_dir(raw_dir, anim_name)
                if prefix_frames:
                    all_frames[anim_name] = prefix_frames

            if all_frames:
                processed = process_animation_frames(
                    all_frames, w, h, palette, min_contrast, bg
                )
                for anim_name, frames in processed.items():
                    save_frames(frames, pixel_dir, anim_name)
                    sheet = generate_sprite_sheet(frames)
                    sheet_path = os.path.join(char_output, f"{anim_name}_sheet.png")
                    save_sprite_sheet(sheet, sheet_path)
                    print(f"  {anim_name}: {len(frames)} 帧 -> {sheet_path}")
            else:
                print(f"  警告: raw 目录中未找到帧图片")
        else:
            print(f"  跳过: 未找到 raw 目录 {raw_dir}")
            print(f"  请先将原始图片放入 {raw_dir}/ 目录")

    print(f"\n输出目录: {output_base}")


def main():
    args = parse_args()

    if args.config:
        process_config_mode(args)
    elif args.sheet_only:
        if not args.input_dir:
            print("错误: --sheet-only 需要 --input-dir 参数")
            sys.exit(1)
        process_sheet_only_mode(args)
    elif args.input:
        process_single_image_mode(args)
    else:
        print("错误: 需要 --input、--input-dir 或 --config 参数")
        parse_args()  # 显示帮助
        sys.exit(1)


if __name__ == "__main__":
    main()
