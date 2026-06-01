#!/usr/bin/env python3
"""
PixelForge Web Server
提供前端页面 + 后端 API
"""

import json
import os
import sys
import io
import base64
import threading
from http.server import HTTPServer, SimpleHTTPRequestHandler
from urllib.parse import urlparse, parse_qs

sys.path.insert(0, os.path.dirname(__file__))

from pixel_char_gen import (
    process_single_image, process_animation_frames,
    generate_sprite_sheet, save_frames, save_sprite_sheet,
    load_frames_from_dir, load_image
)
from godot_export import generate_import_file, generate_sprite_frames_tres

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def _generate_raw_frames(raw_dir: str, animations: dict,
                         style: str = "", clothing: str = "", weapon: str = ""):
    """程序化生成原始角色帧（128x128），保存到 raw 目录"""
    from PIL import Image, ImageDraw
    os.makedirs(raw_dir, exist_ok=True)

    size = 128
    cx, cy = 64, 55

    def draw_char(draw, arm="down", leg="stand", weap="down",
                  head_tilt=0, cape_sway=0):
        # Body
        draw.rectangle([cx-12, cy-5, cx+12, cy+20], fill=(50, 80, 180, 255))
        # Head
        hx = cx + head_tilt
        draw.ellipse([hx-8, cy-22, hx+8, cy-5], fill=(220, 180, 140, 255))
        draw.rectangle([hx-5, cy-15, hx-2, cy-10], fill=(255, 255, 255, 255))
        draw.rectangle([hx+2, cy-15, hx+5, cy-10], fill=(255, 255, 255, 255))
        draw.rectangle([hx-4, cy-14, hx-3, cy-11], fill=(30, 30, 30, 255))
        draw.rectangle([hx+3, cy-14, hx+4, cy-11], fill=(30, 30, 30, 255))
        # Legs
        if leg == "stand":
            draw.rectangle([cx-8, cy+20, cx-3, cy+38], fill=(60, 50, 40, 255))
            draw.rectangle([cx+3, cy+20, cx+8, cy+38], fill=(60, 50, 40, 255))
        elif leg == "walk_left":
            draw.rectangle([cx-12, cy+20, cx-7, cy+36], fill=(60, 50, 40, 255))
            draw.rectangle([cx+5, cy+20, cx+10, cy+40], fill=(60, 50, 40, 255))
        elif leg == "walk_right":
            draw.rectangle([cx-10, cy+20, cx-5, cy+40], fill=(60, 50, 40, 255))
            draw.rectangle([cx+7, cy+20, cx+12, cy+36], fill=(60, 50, 40, 255))
        # Arms
        if arm == "down":
            draw.rectangle([cx-18, cy-2, cx-12, cy+15], fill=(220, 180, 140, 255))
            draw.rectangle([cx+12, cy-2, cx+18, cy+15], fill=(220, 180, 140, 255))
        elif arm == "left_up":
            draw.rectangle([cx-20, cy-10, cx-14, cy+5], fill=(220, 180, 140, 255))
            draw.rectangle([cx+12, cy-2, cx+18, cy+15], fill=(220, 180, 140, 255))
        elif arm == "both_up":
            draw.rectangle([cx-20, cy-15, cx-14, cy+0], fill=(220, 180, 140, 255))
            draw.rectangle([cx+14, cy-15, cx+20, cy+0], fill=(220, 180, 140, 255))
        elif arm == "attack":
            draw.rectangle([cx-18, cy-2, cx-12, cy+15], fill=(220, 180, 140, 255))
            draw.rectangle([cx+12, cy-5, cx+30, cy+2], fill=(220, 180, 140, 255))
        # Weapon
        if weap == "down":
            draw.rectangle([cx+14, cy+15, cx+17, cy+35], fill=(180, 180, 200, 255))
        elif weap == "attack":
            draw.rectangle([cx+28, cy-8, cx+50, cy-4], fill=(180, 180, 200, 255))
            draw.polygon([(cx+50, cy-10), (cx+55, cy-6), (cx+50, cy-2)],
                         fill=(220, 220, 240, 255))
        # Cape
        sx = cape_sway
        draw.polygon([(cx-10, cy-3), (cx+10, cy-3),
                      (cx+8+sx, cy+18), (cx-8+sx, cy+18)],
                     fill=(180, 40, 40, 200))

    # Frame configs per animation
    frame_configs = {
        "idle": [
            {"cape_sway": 0, "head_tilt": 0},
            {"cape_sway": 1, "head_tilt": 1},
            {"cape_sway": 0, "head_tilt": 0},
            {"cape_sway": -1, "head_tilt": -1},
        ],
        "walk_down": [
            {"arm": "down", "leg": "stand"},
            {"arm": "left_up", "leg": "walk_left"},
            {"arm": "down", "leg": "stand"},
            {"arm": "left_up", "leg": "walk_right"},
            {"arm": "down", "leg": "stand"},
            {"arm": "left_up", "leg": "walk_left"},
        ],
        "walk_up": [
            {"arm": "down", "leg": "stand"},
            {"arm": "left_up", "leg": "walk_left"},
            {"arm": "down", "leg": "stand"},
            {"arm": "left_up", "leg": "walk_right"},
            {"arm": "down", "leg": "stand"},
            {"arm": "left_up", "leg": "walk_left"},
        ],
        "walk_left": [
            {"arm": "down", "leg": "stand"},
            {"arm": "left_up", "leg": "walk_left"},
            {"arm": "down", "leg": "stand"},
            {"arm": "left_up", "leg": "walk_right"},
        ],
        "walk_right": [
            {"arm": "down", "leg": "stand"},
            {"arm": "left_up", "leg": "walk_left"},
            {"arm": "down", "leg": "stand"},
            {"arm": "left_up", "leg": "walk_right"},
        ],
        "attack": [
            {"arm": "down", "leg": "stand", "weap": "down"},
            {"arm": "left_up", "leg": "stand", "weap": "down", "head_tilt": 1},
            {"arm": "both_up", "leg": "walk_right", "weap": "down", "head_tilt": 2},
            {"arm": "attack", "leg": "walk_right", "weap": "attack", "head_tilt": 2},
            {"arm": "attack", "leg": "stand", "weap": "attack", "head_tilt": 1},
            {"arm": "down", "leg": "stand", "weap": "down"},
        ],
        "hurt": [
            {"arm": "down", "leg": "stand", "cape_sway": 0},
            {"arm": "both_up", "leg": "walk_left", "cape_sway": -3},
            {"arm": "down", "leg": "stand", "cape_sway": -1},
        ],
        "death": [
            {"arm": "down", "leg": "stand"},
            {"arm": "both_up", "leg": "walk_left", "head_tilt": -2},
            {"arm": "both_up", "leg": "walk_left", "head_tilt": -4},
            {"arm": "down", "leg": "walk_left", "cape_sway": -3},
            {"arm": "down", "leg": "walk_left", "cape_sway": -5},
            {"arm": "down", "leg": "walk_left", "cape_sway": -5},
        ],
    }

    for anim_name in animations:
        configs = frame_configs.get(anim_name, frame_configs["idle"])
        for i, cfg in enumerate(configs):
            img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
            draw = ImageDraw.Draw(img)
            draw_char(draw, **cfg)
            img.save(os.path.join(raw_dir, f"{anim_name}_{i:02d}.png"))
OUTPUT_BASE = os.path.join(PROJECT_ROOT, "project", "assets", "sprites", "characters")
TOOL_DIR = os.path.dirname(__file__)


def image_to_base64(img):
    """PIL Image 转 base64 data URL"""
    buf = io.BytesIO()
    img.save(buf, format="PNG")
    b64 = base64.b64encode(buf.getvalue()).decode()
    return f"data:image/png;base64,{b64}"


class PixelForgeHandler(SimpleHTTPRequestHandler):
    """处理前端页面和 API 请求"""

    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=TOOL_DIR, **kwargs)

    def end_headers(self):
        self.send_header("Cache-Control", "no-cache, no-store, must-revalidate")
        self.send_header("Pragma", "no-cache")
        self.send_header("Expires", "0")
        super().end_headers()

    def do_GET(self):
        parsed = urlparse(self.path)
        if parsed.path == "/" or parsed.path == "/index.html":
            self.path = "/index.html"
            return super().do_GET()
        elif parsed.path == "/api/health":
            self._json_response({"status": "ok", "output_base": OUTPUT_BASE})
        elif parsed.path == "/api/animations":
            # 返回已有角色的动画列表
            name = parse_qs(parsed.query).get("name", ["warrior"])[0]
            char_dir = os.path.join(OUTPUT_BASE, name)
            anims = {}
            if os.path.isdir(char_dir):
                for f in os.listdir(char_dir):
                    if f.endswith("_sheet.png"):
                        anim_name = f.replace("_sheet.png", "")
                        anims[anim_name] = f"/api/sprite/{name}/{f}"
            self._json_response({"name": name, "animations": anims})
        elif parsed.path.startswith("/api/sprite/"):
            # 返回精灵图
            parts = parsed.path.split("/")
            if len(parts) >= 5:
                name = parts[3]
                filename = "/".join(parts[4:])
                filepath = os.path.join(OUTPUT_BASE, name, filename)
                if os.path.isfile(filepath):
                    self.send_response(200)
                    self.send_header("Content-Type", "image/png")
                    self.end_headers()
                    with open(filepath, "rb") as f:
                        self.wfile.write(f.read())
                    return
            self._json_response({"error": "not found"}, 404)
        else:
            return super().do_GET()

    def do_POST(self):
        print(f"[POST] {self.path}")
        parsed = urlparse(self.path)
        content_len = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(content_len) if content_len > 0 else b""

        try:
            data = json.loads(body) if body else {}
        except json.JSONDecodeError as e:
            print(f"[POST] JSON error: {e}, body={body[:200]}")
            self._json_response({"error": "invalid JSON"}, 400)
            return

        print(f"[POST] {parsed.path} -> data keys: {list(data.keys())}")

        if parsed.path == "/api/generate":
            self._handle_generate(data)
        elif parsed.path == "/api/pixelate":
            self._handle_pixelate(data)
        elif parsed.path == "/api/export":
            self._handle_export(data)
        else:
            self._json_response({"error": "not found"}, 404)

    def _handle_generate(self, data):
        """生成角色帧：有 raw 目录则读取，没有则程序化生成"""
        name = data.get("name", "warrior")
        width = data.get("width", 32)
        height = data.get("height", 32)
        palette = data.get("palette_limit", 16)
        min_contrast = data.get("min_rgb_contrast", 40)
        bg = tuple(data.get("background_rgb", [0, 0, 0]))
        animations = data.get("animations", {})
        style = data.get("style", "chibi-fantasy")
        clothing = data.get("clothing", "")
        weapon = data.get("weapon", "")

        char_dir = os.path.join(OUTPUT_BASE, name)
        raw_dir = os.path.join(char_dir, "raw")
        pixel_dir = os.path.join(char_dir, "pixel")

        if not os.path.isdir(raw_dir):
            os.makedirs(raw_dir, exist_ok=True)

        # 如果 raw 目录为空，用程序化方式生成原始帧
        existing = [f for f in os.listdir(raw_dir) if f.endswith(".png")] if os.path.isdir(raw_dir) else []
        if not existing:
            print(f"[generate] No raw frames for '{name}', generating programmatically...")
            _generate_raw_frames(raw_dir, animations, style, clothing, weapon)

        # 加载 raw 帧
        all_frames = {}
        for anim_name in animations:
            prefix_frames = load_frames_from_dir(raw_dir, anim_name)
            if prefix_frames:
                all_frames[anim_name] = prefix_frames

        if not all_frames:
            self._json_response({"error": "raw 目录中未找到帧图片"}, 404)
            return

        # 像素化处理
        processed = process_animation_frames(
            all_frames, width, height, palette, min_contrast, bg
        )

        # 保存结果
        os.makedirs(pixel_dir, exist_ok=True)
        result = {}
        for anim_name, frames in processed.items():
            save_frames(frames, pixel_dir, anim_name)
            sheet = generate_sprite_sheet(frames)
            sheet_path = os.path.join(char_dir, f"{anim_name}_sheet.png")
            save_sprite_sheet(sheet, sheet_path)

            result[anim_name] = {
                "frame_count": len(frames),
                "sheet_url": f"/api/sprite/{name}/{anim_name}_sheet.png",
                "frames": [
                    f"/api/sprite/{name}/pixel/{anim_name}_{i:02d}.png"
                    for i in range(len(frames))
                ]
            }

        self._json_response({
            "status": "ok",
            "name": name,
            "size": f"{width}x{height}",
            "animations": result
        })

    def _handle_pixelate(self, data):
        """单图像素化"""
        image_data = data.get("image")  # base64 data URL
        width = data.get("width", 32)
        height = data.get("height", 32)
        palette = data.get("palette_limit", 16)
        min_contrast = data.get("min_rgb_contrast", 40)
        bg = tuple(data.get("background_rgb", [0, 0, 0]))

        if not image_data:
            self._json_response({"error": "缺少 image 参数"}, 400)
            return

        # 解码 base64 图片
        try:
            if "," in image_data:
                image_data = image_data.split(",")[1]
            img_bytes = base64.b64decode(image_data)
            img = load_image_from_bytes(img_bytes)
        except Exception as e:
            self._json_response({"error": f"图片解码失败: {e}"}, 400)
            return

        result = process_single_image(img, width, height, palette, min_contrast, bg)
        result_b64 = image_to_base64(result)

        self._json_response({
            "status": "ok",
            "image": result_b64,
            "size": f"{width}x{height}",
            "palette_colors": palette
        })

    def _handle_export(self, data):
        """导出 Godot 资源"""
        name = data.get("name", "warrior")
        animations = data.get("animations", {})
        width = data.get("width", 32)
        height = data.get("height", 32)

        char_dir = os.path.join(OUTPUT_BASE, name)

        if not os.path.isdir(char_dir):
            self._json_response({"error": f"角色目录不存在: {char_dir}"}, 404)
            return

        exported = {}
        for anim_name, anim_cfg in animations.items():
            sheet_path = os.path.join(char_dir, f"{anim_name}_sheet.png")
            if not os.path.isfile(sheet_path):
                continue

            res_path = f"res://assets/sprites/characters/{name}/{anim_name}_sheet.png"

            # .import 文件
            import_path = sheet_path + ".import"
            with open(import_path, "w") as f:
                f.write(generate_import_file(res_path))

            # .tres 文件
            frame_count = anim_cfg.get("frames", 1)
            cols = int(frame_count ** 0.5)
            if cols * cols < frame_count:
                cols += 1

            tres_content = generate_sprite_frames_tres(
                f"{name}_{anim_name}",
                {anim_name: anim_cfg},
                res_path, width, height, cols
            )
            tres_path = os.path.join(char_dir, f"{anim_name}_frames.tres")
            with open(tres_path, "w") as f:
                f.write(tres_content)

            exported[anim_name] = {
                "sheet": f"{anim_name}_sheet.png",
                "import": f"{anim_name}_sheet.png.import",
                "tres": f"{anim_name}_frames.tres"
            }

        self._json_response({
            "status": "ok",
            "name": name,
            "exported": exported,
            "output_dir": char_dir
        })

    def _json_response(self, data, code=200):
        body = json.dumps(data, ensure_ascii=False).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Access-Control-Allow-Origin", "*")
        self.end_headers()
        self.wfile.write(body)

    def do_OPTIONS(self):
        self.send_response(200)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.end_headers()


def load_image_from_bytes(data: bytes):
    """从字节加载图片"""
    from PIL import Image
    return Image.open(io.BytesIO(data)).convert("RGBA")


def main():
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8080
    server = HTTPServer(("0.0.0.0", port), PixelForgeHandler)
    print(f"PixelForge server running at http://localhost:{port}")
    print(f"Output directory: {OUTPUT_BASE}")
    print("Press Ctrl+C to stop")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")


if __name__ == "__main__":
    main()
