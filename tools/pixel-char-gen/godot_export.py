"""
Godot 资源导出模块
生成 .tres SpriteFrames 文件和 .import 元数据
"""

import os
import hashlib


def generate_import_file(source_png_relative: str) -> str:
    """生成 Godot .import 文件内容（让 Godot 正确导入 PNG 为 Texture2D）"""
    # 计算文件名的 hash 用于 Godot 内部标识（使用确定性哈希）
    filename = os.path.basename(source_png_relative)
    file_hash = int(hashlib.md5(filename.encode()).hexdigest()[:16], 16)

    return f"""[remap]

importer="texture"
type="CompressedTexture2D"
uid="uid://{file_hash:016x}"
path="res://.godot/imported/{filename}-{file_hash:016x}.ctex"
metadata={{}}

[deps]

source_file="{source_png_relative}"
dest_files=["res://.godot/imported/{filename}-{file_hash:016x}.ctex"]

[params]

compress/mode=0
compress/high_quality=false
compress/lossy_quality=0.7
compress/hdr_compression=1
compress/normal_map=0
compress/channel_pack=0
mipmaps/generate=false
mipmaps/limit=-1
roughness/mode=0
roughness/src_normal=""
process/fix_alpha_border=true
process/premult_alpha=false
process/normal_map_invert_y=false
process/hdr_as_srgb=false
process/hdr_clamp_exposure=false
process/size_limit=0
detect_3d/compress_to=1
svg/scale=1.0
editor/scale_with_editor_scale=false
editor/convert_colors_with_editor_theme=false
"""


def generate_sprite_frames_tres(
    character_name: str,
    animations: dict[str, dict],
    sprite_sheet_res_path: str,
    frame_w: int,
    frame_h: int,
    sprite_sheet_cols: int
) -> str:
    """生成 Godot 4.x SpriteFrames .tres 文件内容

    Args:
        character_name: 角色名称
        animations: {"idle": {"frames": 4, "fps": 6}, "walk": {"frames": 6, "fps": 8}, ...}
        sprite_sheet_res_path: sprite sheet 的 res:// 路径
        frame_w, frame_h: 单帧尺寸
        sprite_sheet_cols: sprite sheet 每行帧数
    """
    lines = [
        '[gd_resource type="SpriteFrames" format=3]',
        f'[ext_resource type="Texture2D" path="{sprite_sheet_res_path}" id="1"]',
        ''
    ]

    sub_id = 1
    atlas_ids = {}  # anim_name -> [sub_resource_id, ...]

    # 为每个动画的每一帧创建 AtlasTexture sub_resource
    for anim_name, anim_config in animations.items():
        frame_count = anim_config.get("frames", 1)
        ids = []
        for i in range(frame_count):
            col = i % sprite_sheet_cols
            row = i // sprite_sheet_cols
            x = col * frame_w
            y = row * frame_h

            lines.append(f'[sub_resource type="AtlasTexture" id="{sub_id}"]')
            lines.append(f'atlas = ExtResource("1")')
            lines.append(f'region = Rect2({x}, {y}, {frame_w}, {frame_h})')
            lines.append('')
            ids.append(sub_id)
            sub_id += 1

        atlas_ids[anim_name] = ids

    # 构建 SpriteFrames 资源
    lines.append('[resource]')
    lines.append('animations = [')

    for anim_name, anim_config in animations.items():
        fps = anim_config.get("fps", 6)
        loop = anim_config.get("loop", True)
        ids = atlas_ids[anim_name]

        lines.append('{')
        lines.append(f'"frames": [')

        for i, sid in enumerate(ids):
            comma = "," if i < len(ids) - 1 else ""
            lines.append(f'{{"duration": 1.0, "texture": SubResource("{sid}")}}{comma}')

        lines.append('],')
        lines.append(f'"loop": {"true" if loop else "false"},')
        lines.append(f'"name": &"{anim_name}",')
        lines.append(f'"speed": {fps}.0')
        lines.append('},')

    lines.append(']')

    return '\n'.join(lines)


def export_godot_assets(
    output_dir: str,
    character_name: str,
    animations: dict[str, dict],
    sprite_sheet_path: str,
    frame_w: int,
    frame_h: int,
    sprite_sheet_cols: int
) -> dict[str, str]:
    """导出完整的 Godot 资源文件

    Returns:
        {"sprite_sheet": path, "tres": path, "import": path}
    """
    # res:// 路径（相对于 Godot 项目根目录）
    # 假设 output_dir 是项目内的相对路径
    res_path = sprite_sheet_path
    if not res_path.startswith("res://"):
        # 将绝对路径转为相对路径
        if "project/" in res_path:
            res_path = "res://" + res_path.split("project/", 1)[1]
        elif "assets/" in res_path:
            res_path = "res://" + res_path.split("assets/", 1)[1]

    # 生成 .import 文件
    import_path = sprite_sheet_path + ".import"
    import_content = generate_import_file(res_path)
    with open(import_path, "w") as f:
        f.write(import_content)

    # 生成 .tres 文件
    tres_content = generate_sprite_frames_tres(
        character_name, animations, res_path,
        frame_w, frame_h,  # 需要从调用方传入
        _calc_cols(animations)
    )
    tres_path = os.path.join(output_dir, f"{character_name}_frames.tres")
    with open(tres_path, "w") as f:
        f.write(tres_content)

    return {
        "sprite_sheet": sprite_sheet_path,
        "import": import_path,
        "tres": tres_path
    }


def _calc_cols(animations: dict) -> int:
    """计算 sprite sheet 的列数（取最大帧数的平方根向上）"""
    max_frames = max(
        (cfg.get("frames", 1) for cfg in animations.values()),
        default=1
    )
    cols = int(max_frames ** 0.5)
    if cols * cols < max_frames:
        cols += 1
    return max(cols, 1)
