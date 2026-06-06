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
from socketserver import ThreadingMixIn


class QuietHTTPServer(ThreadingMixIn, HTTPServer):
    """多线程 HTTP 服务器，忽略 BrokenPipeError"""
    daemon_threads = True
    def handle_error(self, request, client_address):
        import traceback
        exc = sys.exc_info()[1]
        if isinstance(exc, BrokenPipeError):
            return
        super().handle_error(request, client_address)
from urllib.parse import urlparse, parse_qs

# 配置缓存（静态文件，只读一次）
_config_cache = None

def _load_config():
    global _config_cache
    if _config_cache is None:
        config_path = os.path.join(os.path.dirname(__file__), "pixel_char_config.json")
        if os.path.isfile(config_path):
            with open(config_path, "r") as f:
                _config_cache = json.load(f)
        else:
            _config_cache = {}
    return _config_cache

sys.path.insert(0, os.path.dirname(__file__))

from pixel_char_gen import (
    process_single_image, process_animation_frames,
    generate_sprite_sheet, save_frames, save_sprite_sheet,
    load_frames_from_dir, load_image
)
from godot_export import generate_import_file, generate_sprite_frames_tres

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def _clean_backgrounds(raw_dir: str, bg_threshold: int = 240):
    """清理 AI 生成图片的背景：将接近白色的像素变为透明"""
    from PIL import Image
    cleaned = 0
    for f in os.listdir(raw_dir):
        if not f.endswith(".png"):
            continue
        path = os.path.join(raw_dir, f)
        img = Image.open(path).convert("RGBA")
        pixels = img.load()
        w, h = img.size

        # 找到图片边缘的主色调作为背景色
        bg_colors = []
        edge_ys = sorted(set(y for y in [0, 1, h-1, h-2] if 0 <= y < h))
        edge_xs = sorted(set(x for x in [0, 1, w-1, w-2] if 0 <= x < w))
        for x in range(w):
            for y in edge_ys:
                bg_colors.append(pixels[x, y][:3])
        for y in range(h):
            for x in edge_xs:
                bg_colors.append(pixels[x, y][:3])

        # 计算边缘平均颜色作为背景色
        if bg_colors:
            avg_r = sum(c[0] for c in bg_colors) // len(bg_colors)
            avg_g = sum(c[1] for c in bg_colors) // len(bg_colors)
            avg_b = sum(c[2] for c in bg_colors) // len(bg_colors)
        else:
            avg_r, avg_g, avg_b = 255, 255, 255

        # 将接近背景色的像素变为透明
        threshold = bg_threshold
        for y in range(h):
            for x in range(w):
                r, g, b, a = pixels[x, y]
                # 计算与背景色的距离
                dist = ((r - avg_r)**2 + (g - avg_g)**2 + (b - avg_b)**2) ** 0.5
                if dist < threshold:
                    # 越接近背景色越透明
                    alpha = max(0, int(255 * (dist / threshold)))
                    pixels[x, y] = (r, g, b, alpha)

        img.save(path)
        cleaned += 1
    print(f"[cleanup] Cleaned backgrounds for {cleaned} images")


def _generate_ai_frames(raw_dir: str, config: dict):
    """用 AI 后端生成原始角色帧

    流程：txt2img 生成基准图 → img2img 基于基准图生成动画帧
    这样角色外观一致，只有姿态变化
    """
    from sources.ai_generator import (
        get_backend, build_prompt, NEGATIVE_PROMPT,
        IMG2IMG_PROMPTS, IMG2IMG_VARIANTS, IMG2IMG_STRENGTH
    )

    name = config.get("name", "character")
    w = config.get("width", 32)
    h = config.get("height", 32)
    animations = config.get("animations", {})
    # AI 生成始终用 1024x1024，像素化在后面处理
    gen_w, gen_h = 1024, 1024

    # 加载完整配置以获取后端信息
    full_config = _load_config()

    char_config = {
        "name": name,
        "width": w,
        "height": h,
        "style": config.get("style", "chibi-fantasy"),
        "clothing": config.get("clothing", ""),
        "weapon": config.get("weapon", ""),
        "accessories": config.get("accessories", ""),
        "skin_tone": config.get("skin_tone", "fair"),
    }

    backend = get_backend(full_config)
    print(f"[AI] Using backend: {backend.name}")
    print(f"[AI] char_config: {char_config}")

    # === Step 1: txt2img 生成基准图（idle 第一帧）===
    style = char_config.get("style", "chibi-fantasy")

    # 构建双编码器 prompt
    prompt_1, prompt_2 = build_prompt(char_config, "idle", 0)
    print(f"[AI] === Step 1: txt2img base image ===")
    print(f"[AI] prompt_1 ({len(prompt_1.split())} words): {prompt_1}")
    print(f"[AI] prompt_2 ({len(prompt_2.split())} words): {prompt_2}")

    # 只有 LocalSDBackend 支持 prompt_2 参数
    gen_kwargs = {}
    if hasattr(backend.generate, '__code__') and 'prompt_2' in backend.generate.__code__.co_varnames:
        gen_kwargs['prompt_2'] = prompt_2
    base_image = backend.generate(prompt_1, NEGATIVE_PROMPT, gen_w, gen_h, **gen_kwargs)
    base_path = os.path.join(raw_dir, "idle_00.png")
    base_image.save(base_path)
    print(f"[AI] Base image saved: {base_path} ({os.path.getsize(base_path)} bytes)")

    # === Step 2: img2img 基于基准图生成所有动画帧（仅 LocalSDBackend 支持）===
    if not hasattr(backend, 'img2img'):
        print(f"[AI] Backend {backend.name} does not support img2img, using txt2img fallback for all frames")
        # 用 txt2img 为每帧生成独立图片
        for anim_name, anim_cfg in animations.items():
            frame_count = anim_cfg.get("frames", 1)
            for i in range(frame_count):
                if anim_name == "idle" and i == 0:
                    continue
                anim_prompt = IMG2IMG_PROMPTS.get(anim_name, IMG2IMG_PROMPTS["idle"])
                variant = IMG2IMG_VARIANTS.get(anim_name, IMG2IMG_VARIANTS["idle"])[i % len(IMG2IMG_VARIANTS.get(anim_name, IMG2IMG_VARIANTS["idle"]))]
                full_prompt = f"{anim_prompt}, {variant}"
                try:
                    img = backend.generate(full_prompt, NEGATIVE_PROMPT, gen_w, gen_h, **gen_kwargs)
                    img.save(os.path.join(raw_dir, f"{anim_name}_{i:02d}.png"))
                except Exception as e:
                    print(f"[AI]   FAIL {anim_name}_{i:02d}: {e}")
                    base_image.save(os.path.join(raw_dir, f"{anim_name}_{i:02d}.png"))
        print(f"[AI] === All frames generated (txt2img fallback) ===")
        return

    print(f"[AI] === Step 2: img2img animation frames ===")

    for anim_name, anim_cfg in animations.items():
        frame_count = anim_cfg.get("frames", 1)
        img2img_prompt = IMG2IMG_PROMPTS.get(anim_name, IMG2IMG_PROMPTS["idle"])
        variants = IMG2IMG_VARIANTS.get(anim_name, IMG2IMG_VARIANTS["idle"])
        strength = IMG2IMG_STRENGTH.get(anim_name, 0.4)

        for i in range(frame_count):
            variant = variants[i % len(variants)]
            full_prompt = f"{img2img_prompt}, {variant}"

            print(f"[AI] {anim_name}_{i:02d} (strength={strength})")
            print(f"[AI]   prompt: {full_prompt[:150]}")

            try:
                if anim_name == "idle" and i == 0:
                    # 第一帧已经生成了
                    continue

                img = backend.img2img(full_prompt, NEGATIVE_PROMPT, base_image, strength)
                save_path = os.path.join(raw_dir, f"{anim_name}_{i:02d}.png")
                img.save(save_path)
                print(f"[AI]   saved: {save_path} ({os.path.getsize(save_path)} bytes)")
            except Exception as e:
                print(f"[AI]   FAIL: {e}")
                # 如果 img2img 失败，用基准图的副本
                base_image.save(os.path.join(raw_dir, f"{anim_name}_{i:02d}.png"))

    print(f"[AI] === All frames generated ===")


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
            from urllib.parse import unquote
            decoded_path = unquote(parsed.path)
            parts = decoded_path.split("/")
            if len(parts) >= 5:
                name = parts[3]
                filename = "/".join(parts[4:])
                filepath = os.path.realpath(os.path.join(OUTPUT_BASE, name, filename))
                # 路径穿越防护：确保解析后的路径在 OUTPUT_BASE 内
                if not filepath.startswith(os.path.realpath(OUTPUT_BASE)):
                    self._json_response({"error": "forbidden"}, 403)
                    return
                if os.path.isfile(filepath):
                    self.send_response(200)
                    self.send_header("Content-Type", "image/png")
                    self.send_header("Access-Control-Allow-Origin", "*")
                    self.end_headers()
                    with open(filepath, "rb") as f:
                        while chunk := f.read(65536):
                            self.wfile.write(chunk)
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
        elif parsed.path == "/api/generate-base":
            self._handle_generate_base(data)
        elif parsed.path == "/api/generate-animations":
            self._handle_generate_animations(data)
        elif parsed.path == "/api/animate-from-canvas":
            self._handle_animate_from_canvas(data)
        elif parsed.path == "/api/preview-from-canvas":
            self._handle_preview_from_canvas(data)
        elif parsed.path == "/api/pixelate":
            self._handle_pixelate(data)
        elif parsed.path == "/api/programmatic-generate":
            self._handle_programmatic_generate(data)
        elif parsed.path == "/api/analyze-body-parts":
            self._handle_analyze_body_parts(data)
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

        print(f"[generate] === NEW REQUEST ===")
        print(f"[generate] name={name}, size={width}x{height}, style={style}")
        print(f"[generate] clothing={clothing}, weapon={weapon}")
        print(f"[generate] animations={list(animations.keys())}")

        char_dir = os.path.join(OUTPUT_BASE, name)
        raw_dir = os.path.join(char_dir, "raw")
        pixel_dir = os.path.join(char_dir, "pixel")

        # 强制重新生成：清空 raw 和 pixel 目录
        import shutil
        for d in [raw_dir, pixel_dir]:
            if os.path.isdir(d):
                file_count = len([f for f in os.listdir(d) if f.endswith(".png")])
                print(f"[generate] Removing old {d} ({file_count} files)")
                shutil.rmtree(d)
        os.makedirs(raw_dir, exist_ok=True)

        # 尝试 AI 生成，失败则程序化生成
        used_method = "unknown"
        try:
            print(f"[generate] Trying AI generation...")
            _generate_ai_frames(raw_dir, data)
            used_method = "AI"
            print(f"[generate] AI done, cleaning backgrounds...")
            _clean_backgrounds(raw_dir)
        except Exception as e:
            import traceback
            print(f"[generate] AI FAILED: {e}")
            traceback.print_exc()
            used_method = "programmatic"
            print(f"[generate] Falling back to programmatic...")
            _generate_raw_frames(raw_dir, animations, style, clothing, weapon)

        # 检查 raw 文件
        raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith(".png")])
        print(f"[generate] Raw files ({len(raw_files)}): {raw_files[:5]}...")

        # 加载 raw 帧
        all_frames = {}
        for anim_name in animations:
            prefix_frames = load_frames_from_dir(raw_dir, anim_name)
            if prefix_frames:
                all_frames[anim_name] = prefix_frames
            print(f"[generate] Loaded {anim_name}: {len(prefix_frames)} frames")

        if not all_frames:
            print(f"[generate] ERROR: no frames loaded!")
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

            from urllib.parse import quote
            encoded_name = quote(name)
            result[anim_name] = {
                "frame_count": len(frames),
                "fps": animations.get(anim_name, {}).get("fps", 6),
                "sheet_url": f"/api/sprite/{encoded_name}/{anim_name}_sheet.png",
                "frames": [
                    f"/api/sprite/{encoded_name}/pixel/{anim_name}_{i:02d}.png"
                    for i in range(len(frames))
                ]
            }

        total_frames = sum(v["frame_count"] for v in result.values())
        print(f"[generate] === DONE === method={used_method}, total_frames={total_frames}")
        for anim_name, info in result.items():
            print(f"[generate]   {anim_name}: {info['frame_count']} frames -> {info['frames'][0] if info['frames'] else 'none'}")

        self._json_response({
            "status": "ok",
            "name": name,
            "size": f"{width}x{height}",
            "method": used_method,
            "animations": result
        })

    def _handle_generate_base(self, data):
        """Stage 1: 生成多张基准图供用户选择"""
        from sources.ai_generator import (
            get_backend, build_prompt, NEGATIVE_PROMPT, BASE_PROMPT, STYLE_MODIFIERS
        )
        import shutil

        name = data.get("name", "character")
        count = data.get("count", 4)  # 生成几张
        char_config = {
            "style": data.get("style", "chibi-fantasy"),
            "clothing": data.get("clothing", ""),
            "weapon": data.get("weapon", ""),
            "accessories": data.get("accessories", ""),
            "skin_tone": data.get("skin_tone", "fair"),
        }
        system_prompt = data.get("system_prompt", "")

        print(f"[generate-base] === Generating {count} base images for '{name}' ===")
        print(f"[generate-base] config: {char_config}")
        print(f"[generate-base] system_prompt: {system_prompt[:100]}")

        # 加载后端
        full_config = _load_config()

        try:
            backend = get_backend(full_config)
        except Exception as e:
            self._json_response({"error": f"No AI backend: {e}"}, 500)
            return

        # 构建 prompt（双编码器分段）
        prompt_1, prompt_2 = build_prompt(char_config, "idle", 0)

        # 如果用户提供了系统 prompt，替换 P1 的前缀部分
        if system_prompt.strip():
            # 把用户系统 prompt 放在 P1 前面
            prompt_1 = f"{system_prompt.strip()}, {prompt_1}"

        print(f"[generate-base] prompt_1 ({len(prompt_1.split())} words): {prompt_1}")
        print(f"[generate-base] prompt_2 ({len(prompt_2.split())} words): {prompt_2}")

        # 生成多张基准图
        char_dir = os.path.join(OUTPUT_BASE, name)
        base_dir = os.path.join(char_dir, "base")
        if os.path.isdir(base_dir):
            shutil.rmtree(base_dir)
        os.makedirs(base_dir, exist_ok=True)

        # AI 生成始终用 1024x1024
        gen_w, gen_h = 1024, 1024

        # 使用 SSE 推送进度
        self.send_response(200)
        self.send_header("Content-Type", "text/event-stream")
        self.send_header("Cache-Control", "no-cache")
        self.send_header("Connection", "keep-alive")
        self.send_header("Access-Control-Allow-Origin", "*")
        self.end_headers()

        results = []
        for i in range(count):
            print(f"[generate-base] Generating variant {i+1}/{count}...")
            try:
                gen_kwargs = {}
                if hasattr(backend.generate, '__code__') and 'prompt_2' in backend.generate.__code__.co_varnames:
                    gen_kwargs['prompt_2'] = prompt_2
                img = backend.generate(prompt_1, NEGATIVE_PROMPT, gen_w, gen_h, **gen_kwargs)
                path = os.path.join(base_dir, f"base_{i:02d}.png")
                img.save(path)
                # 转 base64 内嵌，避免 BrokenPipe
                data_url = image_to_base64(img)
                results.append({"index": i, "url": f"/api/sprite/{name}/base/base_{i:02d}.png", "data_url": data_url, "path": path})
                print(f"[generate-base]   saved: {path} ({os.path.getsize(path)} bytes)")

                # 发送进度事件
                event_data = json.dumps({
                    "status": "progress",
                    "current": i + 1,
                    "total": count,
                    "image": {"index": i, "data_url": data_url}
                })
                self.wfile.write(f"data: {event_data}\n\n".encode())
                self.wfile.flush()

            except Exception as e:
                print(f"[generate-base]   FAIL: {e}")
                event_data = json.dumps({
                    "status": "error",
                    "current": i + 1,
                    "total": count,
                    "error": str(e)
                })
                self.wfile.write(f"data: {event_data}\n\n".encode())
                self.wfile.flush()

        # 发送完成事件
        done_data = json.dumps({
            "status": "done",
            "name": name,
            "prompt": prompt_1,
            "count": len(results),
            "images": results
        })
        self.wfile.write(f"data: {done_data}\n\n".encode())
        self.wfile.flush()
        print(f"[generate-base] === Done: {len(results)} images ===")

    def _handle_generate_animations(self, data):
        """Stage 2: 基于选中的基准图生成动画帧"""
        from sources.ai_generator import (
            get_backend, NEGATIVE_PROMPT,
            IMG2IMG_PROMPTS, IMG2IMG_VARIANTS, IMG2IMG_STRENGTH
        )
        from PIL import Image as PILImage
        import shutil

        name = data.get("name", "character")
        base_url = data.get("base_url", "")  # 用户选中的基准图 URL
        animations = data.get("animations", {})
        w = data.get("width", 32)
        h = data.get("height", 32)

        print(f"[generate-anim] === Generating animations for '{name}' ===")
        print(f"[generate-anim] base_url: {base_url}")

        # 解析基准图本地路径
        from urllib.parse import unquote
        base_path_relative = unquote(base_url).replace(f"/api/sprite/{name}/", "")
        base_path = os.path.join(OUTPUT_BASE, name, base_path_relative)

        if not os.path.isfile(base_path):
            self._json_response({"error": f"Base image not found: {base_path}"}, 404)
            return

        base_image = PILImage.open(base_path).convert("RGBA")
        print(f"[generate-anim] Base image loaded: {base_image.size}")

        # 加载后端
        full_config = _load_config()

        try:
            backend = get_backend(full_config)
        except Exception as e:
            self._json_response({"error": f"No AI backend: {e}"}, 500)
            return

        # 清理旧文件
        char_dir = os.path.join(OUTPUT_BASE, name)
        raw_dir = os.path.join(char_dir, "raw")
        pixel_dir = os.path.join(char_dir, "pixel")
        for d in [raw_dir, pixel_dir]:
            if os.path.isdir(d):
                shutil.rmtree(d)
        os.makedirs(raw_dir, exist_ok=True)

        # 用基准图作为 idle_00
        base_image.save(os.path.join(raw_dir, "idle_00.png"))

        # img2img 生成其他帧
        for anim_name, anim_cfg in animations.items():
            frame_count = anim_cfg.get("frames", 1)
            img2img_prompt = IMG2IMG_PROMPTS.get(anim_name, IMG2IMG_PROMPTS["idle"])
            variants = IMG2IMG_VARIANTS.get(anim_name, IMG2IMG_VARIANTS["idle"])
            strength = IMG2IMG_STRENGTH.get(anim_name, 0.4)

            for i in range(frame_count):
                if anim_name == "idle" and i == 0:
                    continue  # 已经有了

                variant = variants[i % len(variants)]
                full_prompt = f"{img2img_prompt}, {variant}"

                print(f"[generate-anim] {anim_name}_{i:02d} (strength={strength})")
                try:
                    img = backend.img2img(full_prompt, NEGATIVE_PROMPT, base_image, strength)
                    save_path = os.path.join(raw_dir, f"{anim_name}_{i:02d}.png")
                    img.save(save_path)
                    print(f"[generate-anim]   saved ({os.path.getsize(save_path)} bytes)")
                except Exception as e:
                    print(f"[generate-anim]   FAIL: {e}")
                    base_image.save(os.path.join(raw_dir, f"{anim_name}_{i:02d}.png"))

        # 像素化处理（默认 32x32，可在 Edit 阶段调整）
        pixel_w = data.get("pixel_width", 32)
        pixel_h = data.get("pixel_height", 32)

        all_frames = {}
        for anim_name in animations:
            prefix_frames = load_frames_from_dir(raw_dir, anim_name)
            if prefix_frames:
                all_frames[anim_name] = prefix_frames

        processed = process_animation_frames(all_frames, pixel_w, pixel_h)
        os.makedirs(pixel_dir, exist_ok=True)

        from urllib.parse import quote
        encoded_name = quote(name)
        result = {}
        for anim_name, frames in processed.items():
            save_frames(frames, pixel_dir, anim_name)
            sheet = generate_sprite_sheet(frames)
            sheet_path = os.path.join(char_dir, f"{anim_name}_sheet.png")
            save_sprite_sheet(sheet, sheet_path)
            result[anim_name] = {
                "frame_count": len(frames),
                "sheet_url": f"/api/sprite/{encoded_name}/{anim_name}_sheet.png",
                "frames": [
                    f"/api/sprite/{encoded_name}/pixel/{anim_name}_{i:02d}.png"
                    for i in range(len(frames))
                ]
            }

        print(f"[generate-anim] === Done ===")
        self._json_response({
            "status": "ok",
            "name": name,
            "animations": result
        })

    def _handle_preview_from_canvas(self, data):
        """基于画布内容生成预览（img2img 单张，返回 base64）"""
        from sources.ai_generator import (
            get_backend, NEGATIVE_PROMPT, IMG2IMG_PROMPTS, IMG2IMG_STRENGTH
        )
        from PIL import Image as PILImage
        import base64, io

        canvas_b64 = data.get("canvas_image", "")
        strength = data.get("strength", 0.4)

        print(f"[preview] Generating preview (strength={strength})")

        # 解码画布
        try:
            if "," in canvas_b64:
                canvas_b64 = canvas_b64.split(",")[1]
            canvas_img = PILImage.open(io.BytesIO(base64.b64decode(canvas_b64))).convert("RGBA")
        except Exception as e:
            self._json_response({"error": f"Failed to decode canvas: {e}"}, 400)
            return

        # 放大到 1024x1024
        upscaled = canvas_img.resize((1024, 1024), PILImage.Resampling.NEAREST)

        # 加载后端
        full_config = _load_config()

        try:
            backend = get_backend(full_config)
        except Exception as e:
            self._json_response({"error": f"No AI backend: {e}"}, 500)
            return

        # img2img 生成预览（用 idle prompt）
        prompt = IMG2IMG_PROMPTS.get("idle", "pixel art character")
        try:
            result = backend.img2img(prompt, NEGATIVE_PROMPT, upscaled, strength)
            # 缩回原尺寸
            pixel_w, pixel_h = canvas_img.size
            result_small = result.resize((pixel_w, pixel_h), PILImage.Resampling.NEAREST)

            # 转 base64
            buf = io.BytesIO()
            result_small.save(buf, format="PNG")
            b64 = base64.b64encode(buf.getvalue()).decode()

            print(f"[preview] Done ({result_small.size})")
            self._json_response({
                "status": "ok",
                "image": f"data:image/png;base64,{b64}",
                "size": f"{pixel_w}x{pixel_h}"
            })
        except Exception as e:
            print(f"[preview] FAIL: {e}")
            self._json_response({"error": str(e)}, 500)

    def _handle_animate_from_canvas(self, data):
        """基于画布内容用 img2img 生成动画帧

        流程：接收画布 base64 → 最近邻放大到 1024x1024 → img2img 生成各动画帧 → 缩回原尺寸
        """
        from sources.ai_generator import (
            get_backend, NEGATIVE_PROMPT, IMG2IMG_PROMPTS, IMG2IMG_VARIANTS, IMG2IMG_STRENGTH
        )
        from PIL import Image as PILImage
        import base64, io, shutil

        name = data.get("name", "character")
        canvas_b64 = data.get("canvas_image", "")  # base64 data URL
        pixel_w = data.get("pixel_width", 32)
        pixel_h = data.get("pixel_height", 32)
        animations = data.get("animations", {})

        print(f"[animate-canvas] === Generating animations from canvas for '{name}' ===")
        print(f"[animate-canvas] pixel size: {pixel_w}x{pixel_h}")

        # 解码画布图片
        try:
            if "," in canvas_b64:
                canvas_b64 = canvas_b64.split(",")[1]
            img_bytes = base64.b64decode(canvas_b64)
            canvas_img = PILImage.open(io.BytesIO(img_bytes)).convert("RGBA")
            print(f"[animate-canvas] Canvas image: {canvas_img.size}")
        except Exception as e:
            self._json_response({"error": f"Failed to decode canvas: {e}"}, 400)
            return

        # 最近邻放大到 1024x1024（保持像素锐利）
        upscaled = canvas_img.resize((1024, 1024), PILImage.Resampling.NEAREST)

        # 加载后端
        full_config = _load_config()

        try:
            backend = get_backend(full_config)
        except Exception as e:
            self._json_response({"error": f"No AI backend: {e}"}, 500)
            return

        # 清理旧文件
        char_dir = os.path.join(OUTPUT_BASE, name)
        raw_dir = os.path.join(char_dir, "raw")
        pixel_dir = os.path.join(char_dir, "pixel")
        for d in [raw_dir, pixel_dir]:
            if os.path.isdir(d):
                shutil.rmtree(d)
        os.makedirs(raw_dir, exist_ok=True)

        # 保存基准图（画布内容）
        upscaled.save(os.path.join(raw_dir, "idle_00.png"))

        # img2img 生成动画帧（仅 LocalSDBackend 支持）
        has_img2img = hasattr(backend, 'img2img')
        for anim_name, anim_cfg in animations.items():
            frame_count = anim_cfg.get("frames", 1)
            img2img_prompt = IMG2IMG_PROMPTS.get(anim_name, IMG2IMG_PROMPTS["idle"])
            variants = IMG2IMG_VARIANTS.get(anim_name, IMG2IMG_VARIANTS["idle"])
            strength = IMG2IMG_STRENGTH.get(anim_name, 0.4)

            for i in range(frame_count):
                if anim_name == "idle" and i == 0:
                    continue

                variant = variants[i % len(variants)]
                full_prompt = f"{img2img_prompt}, {variant}"

                print(f"[animate-canvas] {anim_name}_{i:02d} (strength={strength})")
                try:
                    if has_img2img:
                        img = backend.img2img(full_prompt, NEGATIVE_PROMPT, upscaled, strength)
                    else:
                        img = backend.generate(full_prompt, NEGATIVE_PROMPT, pixel_w, pixel_h)
                    # 缩回像素尺寸（最近邻保持像素风）
                    img_small = img.resize((pixel_w, pixel_h), PILImage.Resampling.NEAREST)
                    save_path = os.path.join(raw_dir, f"{anim_name}_{i:02d}.png")
                    img_small.save(save_path)
                    print(f"[animate-canvas]   saved ({os.path.getsize(save_path)} bytes)")
                except Exception as e:
                    print(f"[animate-canvas]   FAIL: {e}")
                    canvas_img.resize((pixel_w, pixel_h)).save(
                        os.path.join(raw_dir, f"{anim_name}_{i:02d}.png"))

        # 把基准图也缩回像素尺寸
        base_small = upscaled.resize((pixel_w, pixel_h), PILImage.Resampling.NEAREST)
        base_small.save(os.path.join(raw_dir, "idle_00.png"))

        # 加载所有帧并返回
        all_frames = {}
        for anim_name in animations:
            prefix_frames = load_frames_from_dir(raw_dir, anim_name)
            if prefix_frames:
                all_frames[anim_name] = prefix_frames

        from urllib.parse import quote
        encoded_name = quote(name)
        result = {}
        for anim_name, frames in all_frames.items():
            # 生成 sprite sheet
            sheet = generate_sprite_sheet(frames)
            sheet_path = os.path.join(char_dir, f"{anim_name}_sheet.png")
            save_sprite_sheet(sheet, sheet_path)
            result[anim_name] = {
                "frame_count": len(frames),
                "sheet_url": f"/api/sprite/{encoded_name}/{anim_name}_sheet.png",
                "frames": [
                    f"/api/sprite/{encoded_name}/raw/{anim_name}_{i:02d}.png"
                    for i in range(len(frames))
                ]
            }

        print(f"[animate-canvas] === Done ===")
        self._json_response({
            "status": "ok",
            "name": name,
            "animations": result
        })

    def _handle_programmatic_generate(self, data):
        """程序化生成动画帧"""
        import programmatic

        size = data.get("size", 32)

        # 支持 preset 模式：前端发 {preset: "warrior", size: 32}
        preset_name = data.get("preset")
        if preset_name:
            config = programmatic.get_preset(preset_name)
        else:
            config = data.get("config", {})
        name = config.get("name", "character")

        print(f"[programmatic] Generating animations for '{name}' (size={size})")

        # 生成动画帧
        frames_dict = programmatic.generate_animation_frames(config, size)

        # 保存到文件
        char_dir = os.path.join(OUTPUT_BASE, name)
        raw_dir = os.path.join(char_dir, "raw")
        import shutil
        if os.path.isdir(raw_dir):
            shutil.rmtree(raw_dir)
        os.makedirs(raw_dir, exist_ok=True)

        from urllib.parse import quote
        encoded_name = quote(name)
        result = {}

        for anim_name, frames in frames_dict.items():
            # 保存逐帧
            for i, frame in enumerate(frames):
                frame.save(os.path.join(raw_dir, f"{anim_name}_{i:02d}.png"))

            # 生成 sprite sheet
            sheet = generate_sprite_sheet(frames)
            sheet_path = os.path.join(char_dir, f"{anim_name}_sheet.png")
            save_sprite_sheet(sheet, sheet_path)

            result[anim_name] = {
                "frame_count": len(frames),
                "fps": config.get("animations", {}).get(anim_name, {}).get("fps", 6),
                "sheet_url": f"/api/sprite/{encoded_name}/{anim_name}_sheet.png",
                "frames": [
                    f"/api/sprite/{encoded_name}/raw/{anim_name}_{i:02d}.png"
                    for i in range(len(frames))
                ]
            }

        print(f"[programmatic] Done: {sum(v['frame_count'] for v in result.values())} frames")
        self._json_response({
            "status": "ok",
            "name": name,
            "animations": result
        })

    def _handle_analyze_body_parts(self, data):
        """用 Qwen-VL 分析图片中的身体部件"""
        from sources.ai_generator import QwenVLAnalyzer
        from PIL import Image as PILImage

        image_data = data.get("image", "")
        if not image_data:
            self._json_response({"error": "No image provided"}, 400)
            return

        # base64 → PIL Image
        try:
            if image_data.startswith("data:"):
                image_data = image_data.split(",", 1)[1]
            img_bytes = base64.b64decode(image_data)
            img = PILImage.open(io.BytesIO(img_bytes)).convert("RGBA")
        except Exception as e:
            self._json_response({"error": f"Invalid image: {e}"}, 400)
            return

        # 加载 API key
        full_config = _load_config()
        api_key = full_config.get("source", {}).get("backends", {}).get("qwen_vl", {}).get("api_key_env", "")
        if api_key:
            api_key = os.environ.get(api_key, "")

        analyzer = QwenVLAnalyzer(api_key)
        if not analyzer.is_available():
            self._json_response({"error": "Qwen-VL API key not set. Set DASHSCOPE_API_KEY env var."}, 500)
            return

        print(f"[analyze] Analyzing {img.width}x{img.height} image...")
        body_parts = analyzer.analyze_body_parts(img)

        if not body_parts:
            self._json_response({"error": "Failed to detect body parts"}, 500)
            return

        print(f"[analyze] Detected {len(body_parts)} parts: {list(body_parts.keys())}")
        self._json_response({
            "status": "ok",
            "body_parts": body_parts,
            "image_size": {"width": img.width, "height": img.height}
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
    server = QuietHTTPServer(("0.0.0.0", port), PixelForgeHandler)
    print(f"PixelForge server running at http://localhost:{port}")
    print(f"Output directory: {OUTPUT_BASE}")
    print("Press Ctrl+C to stop")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")


if __name__ == "__main__":
    main()
