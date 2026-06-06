"""
AI 图片生成多后端模块
支持本地 Stable Diffusion、LiblibAI、通义万相、智谱 CogView、OpenAI DALL-E、纯 CPU SD
"""

import os
import io
import json
import base64
from abc import ABC, abstractmethod
from typing import Optional

try:
    from PIL import Image
except ImportError:
    raise ImportError("需要安装 Pillow: pip install Pillow")


# ============================================================
# Prompt 模板 — 像素角色生成专用
# ============================================================

# 风格修饰词（精简，控制 token 数）
STYLE_MODIFIERS = {
    "chibi-fantasy": "chibi RPG, big head, small body",
    "retro-8bit": "8-bit NES, chunky pixels",
    "modern-pixel": "modern pixel art, clean edges",
    "dark-souls": "dark fantasy, gothic, muted colors",
    "cute-chibi": "kawaii chibi, round, pastel",
}

# 基础 Prompt — 精简版，两段分别给两个 CLIP 编码器
# 每段控制在 77 token 以内，无同义词重复
BASE_PROMPT_PART1 = (
    "pixel art sprite, single character, solo, white background, "
    "centered, full body, front view, standing, "
    "{style}, {clothing}, {weapon}, {accessories}, {skin_tone} skin"
)
BASE_PROMPT_PART2 = (
    "sharp edges, black outlines, flat colors, 16-bit, game sprite"
)
BASE_PROMPT = BASE_PROMPT_PART1 + ", " + BASE_PROMPT_PART2

# img2img 动画 Prompt — 基于参考图，描述姿态变化
# 这些 prompt 更短，因为参考图已经提供了角色外观
IMG2IMG_PROMPTS = {
    "idle": "pixel art character, same character, idle standing pose, front view, relaxed",
    "walk_down": "pixel art character, same character, walking forward, front view, mid-stride, legs moving",
    "walk_up": "pixel art character, same character, walking away, back view, mid-stride",
    "walk_left": "pixel art character, same character, walking left, side view, profile, mid-stride",
    "walk_right": "pixel art character, same character, walking right, side view, profile, mid-stride",
    "attack": "pixel art character, same character, attacking, weapon strike, action pose, dynamic motion",
    "hurt": "pixel art character, same character, hurt, recoiling, knocked back, pain expression",
    "death": "pixel art character, same character, falling down, collapsing, defeated, dying",
}

# 帧间微变化 — 用于 img2img 的额外描述
IMG2IMG_VARIANTS = {
    "idle": [
        "breathing, slight body sway, relaxed",
        "looking slightly left, idle",
        "looking slightly right, idle",
        "breathing, slight body sway, relaxed",
    ],
    "walk_down": [
        "left foot forward, right foot back, walking",
        "feet passing, mid-step, walking",
        "right foot forward, left foot back, walking",
        "feet passing, mid-step, walking",
        "left foot forward, right foot back, walking",
        "feet passing, mid-step, walking",
    ],
    "walk_up": [
        "left foot forward, right foot back",
        "feet passing, mid-step",
        "right foot forward, left foot back",
        "feet passing, mid-step",
    ],
    "walk_left": [
        "left leg extended forward, walking left",
        "legs passing, mid-step, walking left",
        "right leg extended forward, walking left",
        "legs passing, mid-step, walking left",
    ],
    "walk_right": [
        "right leg extended forward, walking right",
        "legs passing, mid-step, walking right",
        "left leg extended forward, walking right",
        "legs passing, mid-step, walking right",
    ],
    "attack": [
        "winding up, weapon raised overhead, preparing to strike",
        "weapon overhead, ready to strike, aggressive",
        "mid-swing, weapon coming down, attacking",
        "weapon extended forward, follow through, striking",
        "recovery pose, weapon returning, after strike",
        "back to neutral stance, ready",
    ],
    "hurt": [
        "just got hit, flinching, pain",
        "knocked back, arms raised defensively, hurt",
        "recovering, returning to stance, wincing",
    ],
    "death": [
        "staggering, losing balance, falling",
        "falling backward, collapsing",
        "crumpling to ground, defeated",
        "lying on ground, motionless",
    ],
}

# img2img 强度 — 控制每帧与基准图的相似度
# 越低越像原图，越高变化越大
IMG2IMG_STRENGTH = {
    "idle": 0.3,        # idle 几乎不变
    "walk_down": 0.4,   # walk 中等变化
    "walk_up": 0.4,
    "walk_left": 0.4,
    "walk_right": 0.4,
    "attack": 0.5,      # attack 变化较大
    "hurt": 0.45,
    "death": 0.5,
}

# 负面 Prompt（强化多人和背景排除）
NEGATIVE_PROMPT = (
    # 多人排除（加强）
    "multiple characters, multiple people, group, crowd, duo, two people, three people, "
    "many people, several people, gathering, army, mob, "
    "second person, other characters, background characters, "
    # 背景排除
    "background, scenery, landscape, environment, room, indoor, outdoor, forest, city, sky, "
    "floor, ground, platform, pedestal, "
    # 质量排除
    "realistic, photograph, 3d render, 3d model, cgi, photo, "
    "blurry, low quality, jpeg artifacts, watermark, text, signature, logo, "
    # 解剖排除
    "deformed, disfigured, extra limbs, extra arms, extra fingers, missing limbs, bad anatomy, "
    "mutated, fused, merged, "
    # 构图排除
    "cropped, out of frame, cut off, partial, "
    # 风格排除
    "anime, cartoon, illustration, painting, drawing, sketch, "
    "monochrome, grayscale, black and white"
)


def build_prompt(character_config: dict, anim_name: str = "idle", frame_index: int = 0) -> tuple[str, str]:
    """构建 txt2img 的 prompt，返回 (prompt_1, prompt_2)

    所有关键词均匀分配到两个编码器，避免单侧超 77 token 被截断
    """
    style = character_config.get("style", "modern-pixel")

    # 收集所有非空关键词（精简，控制总 token ≤ 154）
    all_parts = [
        "pixel art", "single character", "white background",
        "centered", "full body", "standing"
    ]

    style_desc = STYLE_MODIFIERS.get(style, "pixel art")
    if style_desc:
        all_parts.append(style_desc)

    for key in ["clothing", "weapon", "accessories"]:
        val = character_config.get(key, "").strip()
        if val and val.lower() != "none":
            all_parts.append(val)

    skin = character_config.get("skin_tone", "").strip()
    if skin:
        all_parts.append(f"{skin} skin")

    # 像素风关键词
    all_parts.extend(["sharp edges", "black outlines", "flat colors", "16-bit", "game sprite"])

    # 均匀分配：奇数位给 P1，偶数位给 P2
    p1_parts = all_parts[::2]
    p2_parts = all_parts[1::2]

    return ", ".join(p1_parts), ", ".join(p2_parts)


def build_img2img_prompt(anim_name: str, frame_index: int = 0) -> str:
    """构建 img2img 的 prompt（基于基准图生成动画帧）"""
    base = IMG2IMG_PROMPTS.get(anim_name, IMG2IMG_PROMPTS["idle"])
    variants = IMG2IMG_VARIANTS.get(anim_name, IMG2IMG_VARIANTS["idle"])
    variant = variants[frame_index % len(variants)]
    return f"{base}, {variant}"


# ============================================================
# 后端基类
# ============================================================

class AIBackend(ABC):
    """AI 图片生成后端基类"""

    @abstractmethod
    def generate(self, prompt: str, negative_prompt: str,
                 width: int, height: int) -> Image.Image:
        """生成单张图片"""
        ...

    @abstractmethod
    def is_available(self) -> bool:
        """检查后端是否可用"""
        ...

    @property
    @abstractmethod
    def name(self) -> str:
        """后端名称"""
        ...


# ============================================================
# 本地 Stable Diffusion
# ============================================================

class LocalSDBackend(AIBackend):
    """本地 Stable Diffusion（diffusers 库）"""

    def __init__(self, config: dict):
        self.model_id = config.get("model", "stabilityai/sdxl-base-1.0")
        self.lora_path = config.get("lora", "")
        self.lora_weight = config.get("lora_weight", 0.8)
        self.device = config.get("device", "auto")
        self._pipe = None

    @property
    def name(self) -> str:
        return "local_sd"

    def is_available(self) -> bool:
        try:
            import torch
            import diffusers
            if self.device == "auto":
                return torch.cuda.is_available()
            return True
        except ImportError:
            return False

    def _load_pipeline(self):
        if self._pipe is not None:
            return
        import torch
        from diffusers import StableDiffusionXLPipeline

        device = "cuda" if self.device == "auto" and torch.cuda.is_available() else "cpu"
        dtype = torch.float16 if device == "cuda" else torch.float32

        # 从 ModelScope 下载或使用本地路径
        local_path = self._download_from_modelscope()
        model_id = local_path if local_path else self.model_id

        print(f"[LocalSD] Loading SDXL from: {model_id}")
        self._pipe = StableDiffusionXLPipeline.from_pretrained(
            model_id,
            torch_dtype=dtype,
            use_safetensors=True,
            variant="fp16" if dtype == torch.float16 else None
        ).to(device)

        # 启用内存优化
        if device == "cuda":
            self._pipe.enable_model_cpu_offload()

        # 加载 LoRA
        if self.lora_path:
            lora_file = self.lora_path
            # 支持相对路径
            if not os.path.isabs(lora_file):
                lora_file = os.path.join(os.path.dirname(os.path.dirname(__file__)), lora_file)

            if os.path.isfile(lora_file):
                print(f"[LocalSD] Loading LoRA: {lora_file}")
                try:
                    self._pipe.load_lora_weights(
                        os.path.dirname(lora_file),
                        weight_name=os.path.basename(lora_file)
                    )
                    print(f"[LocalSD] LoRA loaded (weight={self.lora_weight})")
                except Exception as e:
                    print(f"[LocalSD] LoRA failed: {e}")
            else:
                print(f"[LocalSD] LoRA not found: {lora_file}")

    def _download_from_modelscope(self) -> str:
        """从 ModelScope 下载 SDXL 模型"""
        try:
            from modelscope import snapshot_download
            model_map = {
                "stabilityai/sdxl-base-1.0": "AI-ModelScope/stable-diffusion-xl-base-1.0",
                "stabilityai/sd-turbo": "AI-ModelScope/sd-turbo",
            }
            ms_id = model_map.get(self.model_id, self.model_id)
            print(f"[LocalSD] Downloading from ModelScope: {ms_id}")
            local_dir = snapshot_download(ms_id, cache_dir="./models")
            print(f"[LocalSD] Model saved to: {local_dir}")
            return local_dir
        except Exception as e:
            print(f"[LocalSD] ModelScope failed ({e}), trying HuggingFace...")
            return ""

    def generate(self, prompt: str, negative_prompt: str,
                 width: int, height: int, prompt_2: str = "") -> Image.Image:
        """txt2img 生成（SDXL 原生 1024x1024，双 CLIP 编码器）"""
        self._load_pipeline()
        import torch, random

        w, h = 1024, 1024

        # 如果没有单独的 prompt_2，自动分段
        if not prompt_2:
            parts = [p.strip() for p in prompt.split(",") if p.strip()]
            mid = len(parts) // 2
            p1 = ", ".join(parts[:mid])
            p2 = ", ".join(parts[mid:])
        else:
            p1 = prompt
            p2 = prompt_2

        device = self._pipe.device
        result = self._pipe(
            prompt=p1,
            prompt_2=p2,
            negative_prompt=negative_prompt,
            negative_prompt_2=negative_prompt,
            width=w,
            height=h,
            num_inference_steps=25,
            guidance_scale=7.5,
            generator=torch.Generator(device).manual_seed(random.randint(0, 2**32))
        )
        return result.images[0]

    def img2img(self, prompt: str, negative_prompt: str,
                reference: Image.Image, strength: float = 0.4) -> Image.Image:
        """img2img 基于参考图生成变体"""
        self._load_pipeline()
        import torch, random
        from diffusers import StableDiffusionXLImg2ImgPipeline

        # 复用 txt2img pipeline 的组件创建 img2img pipeline
        if not hasattr(self, '_img2img_pipe'):
            self._img2img_pipe = StableDiffusionXLImg2ImgPipeline(
                vae=self._pipe.vae,
                text_encoder=self._pipe.text_encoder,
                text_encoder_2=self._pipe.text_encoder_2,
                tokenizer=self._pipe.tokenizer,
                tokenizer_2=self._pipe.tokenizer_2,
                unet=self._pipe.unet,
                scheduler=self._pipe.scheduler,
            )
            if hasattr(self._pipe, 'device'):
                self._img2img_pipe = self._img2img_pipe.to(self._pipe.device)

        device = self._pipe.device
        ref_img = reference.resize((1024, 1024))

        # 分段 prompt
        prompt_parts = [p.strip() for p in prompt.split(",") if p.strip()]
        mid = len(prompt_parts) // 2
        prompt_1 = ", ".join(prompt_parts[:mid])
        prompt_2 = ", ".join(prompt_parts[mid:])

        result = self._img2img_pipe(
            prompt=prompt_1,
            prompt_2=prompt_2,
            negative_prompt=negative_prompt,
            negative_prompt_2=negative_prompt,
            image=ref_img,
            strength=strength,
            num_inference_steps=20,
            guidance_scale=7.5,
            generator=torch.Generator(device).manual_seed(random.randint(0, 2**32))
        )
        return result.images[0]


# ============================================================
# CPU Stable Diffusion（慢速兜底）
# ============================================================

class CPUSDBackend(AIBackend):
    """纯 CPU Stable Diffusion（慢速兜底）"""

    def __init__(self, config: dict):
        self.model_id = config.get("model", "stabilityai/sd-turbo")
        self._pipe = None

    @property
    def name(self) -> str:
        return "cpu_sd"

    def is_available(self) -> bool:
        try:
            import diffusers
            return True
        except ImportError:
            return False

    def _load_pipeline(self):
        if self._pipe is not None:
            return
        from diffusers import AutoPipelineForText2Image
        import torch

        self._pipe = AutoPipelineForText2Image.from_pretrained(
            self.model_id,
            torch_dtype=torch.float32,
            safety_checker=None
        ).to("cpu")

    def generate(self, prompt: str, negative_prompt: str,
                 width: int, height: int) -> Image.Image:
        self._load_pipeline()
        w = max(64, (width // 8) * 8)
        h = max(64, (height // 8) * 8)

        result = self._pipe(
            prompt=prompt,
            negative_prompt=negative_prompt,
            width=w,
            height=h,
            num_inference_steps=4,
            guidance_scale=0.0
        )
        return result.images[0]


# ============================================================
# LiblibAI API（国内 SD 平台）
# ============================================================

class LiblibAIBackend(AIBackend):
    """LiblibAI API（国内 Stable Diffusion 平台）"""

    BASE_URL = "https://api.liblib.art/api"

    def __init__(self, config: dict):
        self.api_key = os.environ.get(config.get("api_key_env", "LIBLIBAI_API_KEY"), "")
        self.model = config.get("model", "sd-xl")

    @property
    def name(self) -> str:
        return "liblibai"

    def is_available(self) -> bool:
        return bool(self.api_key)

    def generate(self, prompt: str, negative_prompt: str,
                 width: int, height: int) -> Image.Image:
        import requests

        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }

        payload = {
            "prompt": prompt,
            "negativePrompt": negative_prompt,
            "width": width,
            "height": height,
            "steps": 20,
            "cfgScale": 7.0,
            "model": self.model
        }

        # 提交生成任务
        resp = requests.post(
            f"{self.BASE_URL}/v1/text2img",
            headers=headers, json=payload, timeout=30
        )
        resp.raise_for_status()
        data = resp.json()

        # 轮询结果
        task_id = data.get("data", {}).get("taskId")
        if not task_id:
            raise RuntimeError(f"LiblibAI 任务创建失败: {data}")

        import time
        for _ in range(60):  # 最多等 5 分钟
            time.sleep(5)
            status_resp = requests.get(
                f"{self.BASE_URL}/v1/text2img/{task_id}",
                headers=headers, timeout=10
            )
            status_data = status_resp.json()
            status = status_data.get("data", {}).get("status")

            if status == "success":
                img_url = status_data["data"]["images"][0]["url"]
                img_resp = requests.get(img_url, timeout=30)
                return Image.open(io.BytesIO(img_resp.content)).convert("RGBA")
            elif status == "failed":
                raise RuntimeError(f"LiblibAI 生成失败: {status_data}")

        raise RuntimeError("LiblibAI 任务超时")


# ============================================================
# 通义万相 API
# ============================================================

class TongyiBackend(AIBackend):
    """通义万相 API（阿里云）"""

    BASE_URL = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text2image/image-synthesis"

    def __init__(self, config: dict):
        self.api_key = os.environ.get(config.get("api_key_env", "TONGYI_API_KEY"), "")

    @property
    def name(self) -> str:
        return "tongyi"

    def is_available(self) -> bool:
        return bool(self.api_key)

    def generate(self, prompt: str, negative_prompt: str,
                 width: int, height: int) -> Image.Image:
        import requests

        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json",
            "X-DashScope-Async": "enable"
        }

        payload = {
            "model": "wanx-v1",
            "input": {
                "prompt": prompt,
                "negative_prompt": negative_prompt
            },
            "parameters": {
                "size": f"{width}*{height}",
                "n": 1
            }
        }

        # 提交任务
        resp = requests.post(self.BASE_URL, headers=headers, json=payload, timeout=30)
        resp.raise_for_status()
        task_id = resp.json().get("output", {}).get("task_id")

        if not task_id:
            raise RuntimeError(f"通义万相任务创建失败: {resp.json()}")

        # 轮询结果
        import time
        check_url = f"https://dashscope.aliyuncs.com/api/v1/tasks/{task_id}"
        for _ in range(60):
            time.sleep(3)
            check_resp = requests.get(check_url, headers=headers, timeout=10)
            data = check_resp.json()
            status = data.get("output", {}).get("task_status")

            if status == "SUCCEEDED":
                img_url = data["output"]["results"][0]["url"]
                img_resp = requests.get(img_url, timeout=30)
                return Image.open(io.BytesIO(img_resp.content)).convert("RGBA")
            elif status == "FAILED":
                raise RuntimeError(f"通义万相生成失败: {data}")

        raise RuntimeError("通义万相任务超时")


# ============================================================
# 智谱 CogView API
# ============================================================

class ZhipuBackend(AIBackend):
    """智谱 CogView API"""

    BASE_URL = "https://open.bigmodel.cn/api/paas/v4/images/generations"

    def __init__(self, config: dict):
        self.api_key = os.environ.get(config.get("api_key_env", "ZHIPU_API_KEY"), "")

    @property
    def name(self) -> str:
        return "zhipu"

    def is_available(self) -> bool:
        return bool(self.api_key)

    def generate(self, prompt: str, negative_prompt: str,
                 width: int, height: int) -> Image.Image:
        import requests

        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }

        # CogView 尺寸选项
        size_map = {
            (512, 512): "512x512",
            (1024, 1024): "1024x1024",
            (768, 768): "768x768"
        }
        # 选择最接近的尺寸
        closest = min(size_map.keys(),
                      key=lambda s: abs(s[0] - width) + abs(s[1] - height))

        payload = {
            "model": "cogview-3",
            "prompt": prompt,
            "size": size_map[closest],
            "n": 1
        }

        resp = requests.post(self.BASE_URL, headers=headers,
                             json=payload, timeout=60)
        resp.raise_for_status()
        data = resp.json()

        img_url = data.get("data", [{}])[0].get("url")
        if not img_url:
            raise RuntimeError(f"智谱 CogView 生成失败: {data}")

        img_resp = requests.get(img_url, timeout=30)
        return Image.open(io.BytesIO(img_resp.content)).convert("RGBA")


# ============================================================
# OpenAI DALL-E 3
# ============================================================

class OpenAIBackend(AIBackend):
    """OpenAI DALL-E 3"""

    def __init__(self, config: dict):
        self.api_key = os.environ.get(config.get("api_key_env", "OPENAI_API_KEY"), "")
        self.model = config.get("model", "dall-e-3")

    @property
    def name(self) -> str:
        return "openai"

    def is_available(self) -> bool:
        return bool(self.api_key)

    def generate(self, prompt: str, negative_prompt: str,
                 width: int, height: int) -> Image.Image:
        from openai import OpenAI
        client = OpenAI(api_key=self.api_key)

        # DALL-E 3 支持的尺寸
        if width <= 512:
            size = "512x512"  # 不支持更小
        elif width <= 1024:
            size = "1024x1024"
        else:
            size = "1024x1024"

        response = client.images.generate(
            model=self.model,
            prompt=prompt,
            size=size,
            n=1,
            response_format="b64_json"
        )

        img_data = base64.b64decode(response.data[0].b64_json)
        return Image.open(io.BytesIO(img_data)).convert("RGBA")


# ============================================================
# Qwen-VL 视觉语言模型（图片分析，非生成）
# ============================================================

class QwenVLAnalyzer:
    """通义千问 VL — 分析图片中的身体部件位置和颜色"""

    def __init__(self, api_key: str = ""):
        self.api_key = api_key or os.environ.get("DASHSCOPE_API_KEY", "")

    def is_available(self) -> bool:
        return bool(self.api_key)

    def analyze_body_parts(self, image: Image.Image) -> dict:
        """分析图片，返回 body parts 配置

        Returns:
            {"head": {"rect": [x1,y1,x2,y2], "color": "#hex"}, ...}
        """
        if not self.is_available():
            raise RuntimeError("Qwen-VL API key not set (DASHSCOPE_API_KEY)")

        # 图片转 base64
        buf = io.BytesIO()
        image.save(buf, format="PNG")
        img_b64 = base64.b64encode(buf.getvalue()).decode()

        prompt = """分析这张像素角色图片。识别以下身体部件的位置（边界框坐标）和主色调。

部件列表：head, torso, left_arm, right_arm, left_leg, right_leg, weapon, cape

严格按以下 JSON 格式返回，不要有任何其他文字：
{
  "head": {"rect": [x1, y1, x2, y2], "color": "#RRGGBB"},
  "torso": {"rect": [x1, y1, x2, y2], "color": "#RRGGBB"},
  "left_arm": {"rect": [x1, y1, x2, y2], "color": "#RRGGBB"},
  "right_arm": {"rect": [x1, y1, x2, y2], "color": "#RRGGBB"},
  "left_leg": {"rect": [x1, y1, x2, y2], "color": "#RRGGBB"},
  "right_leg": {"rect": [x1, y1, x2, y2], "color": "#RRGGBB"}
}

规则：
- 坐标基于图片实际像素尺寸（宽{w}，高{h}）
- 颜色取该区域的主色调，格式 #RRGGBB
- 如果某个部件不存在（比如没有武器、没有披风），不要包含它
- 如果有武器，加入 "weapon"；如果有披风/斗篷，加入 "cape"
- rect 格式为 [左上x, 左上y, 右下x, 右下y]""".format(w=image.width, h=image.height)

        try:
            import requests
            resp = requests.post(
                "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
                headers={
                    "Authorization": f"Bearer {self.api_key}",
                    "Content-Type": "application/json"
                },
                json={
                    "model": "qwen-vl-max",
                    "messages": [
                        {
                            "role": "user",
                            "content": [
                                {"type": "image_url", "image_url": {"url": f"data:image/png;base64,{img_b64}"}},
                                {"type": "text", "text": prompt}
                            ]
                        }
                    ],
                    "max_tokens": 1024,
                    "temperature": 0.1
                },
                timeout=30
            )
            resp.raise_for_status()
            result = resp.json()
            content = result["choices"][0]["message"]["content"]

            # 从回复中提取 JSON
            import re
            json_match = re.search(r'\{[^{}]*\{[^{}]*\}[^{}]*\}', content, re.DOTALL)
            if json_match:
                body_parts = json.loads(json_match.group())
                return body_parts
            else:
                print(f"[QwenVL] Failed to parse JSON from: {content[:200]}")
                return {}

        except Exception as e:
            print(f"[QwenVL] Analysis failed: {e}")
            return {}


# ============================================================
# 后端工厂
# ============================================================

BACKEND_CLASSES = {
    "local_sd": LocalSDBackend,
    "liblibai": LiblibAIBackend,
    "tongyi": TongyiBackend,
    "zhipu": ZhipuBackend,
    "openai": OpenAIBackend,
    "cpu_sd": CPUSDBackend
}

DEFAULT_PRIORITY = ["local_sd", "liblibai", "tongyi", "zhipu", "openai", "cpu_sd"]


def get_backend(config: dict) -> AIBackend:
    """根据配置和可用性自动选择最佳后端

    Args:
        config: 完整配置字典（包含 source 字段）

    Returns:
        可用的 AI 后端实例
    """
    source_config = config.get("source", {})
    backends_config = source_config.get("backends", {})
    priority = source_config.get("backend_priority", DEFAULT_PRIORITY)

    for backend_name in priority:
        backend_conf = backends_config.get(backend_name, {})
        cls = BACKEND_CLASSES.get(backend_name)
        if cls is None:
            continue

        backend = cls(backend_conf)
        if backend.is_available():
            print(f"使用 AI 后端: {backend.name}")
            return backend

    raise RuntimeError(
        "没有可用的 AI 后端。请配置以下任一方式:\n"
        "1. 安装 diffusers + torch (本地 SD): pip install diffusers torch\n"
        "2. 设置环境变量 LIBLIBAI_API_KEY / TONGYI_API_KEY / ZHIPU_API_KEY / OPENAI_API_KEY\n"
        "3. 安装 diffusers (CPU SD): pip install diffusers"
    )


def generate_character_frames(character_config: dict,
                              config: dict) -> dict[str, list[Image.Image]]:
    """为角色的所有动画生成帧图片

    Args:
        character_config: 单个角色配置
        config: 完整配置（用于获取后端信息）

    Returns:
        {"idle": [img1, img2, ...], "walk_down": [...], ...}
    """
    backend = get_backend(config)
    animations = character_config.get("animations", {})
    w = character_config.get("width", 32)
    h = character_config.get("height", 32)
    # 生成图要比目标大，后续再像素化
    gen_w = max(256, w * 4)
    gen_h = max(256, h * 4)

    result = {}
    total_frames = sum(cfg.get("frames", 1) for cfg in animations.values())
    current = 0

    for anim_name, anim_config in animations.items():
        frame_count = anim_config.get("frames", 1)
        frames = []

        for i in range(frame_count):
            current += 1
            prompt_1, prompt_2 = build_prompt(character_config, anim_name, i)
            print(f"  [{current}/{total_frames}] {anim_name}_{i:02d} ...",
                  end=" ", flush=True)

            try:
                gen_kwargs = {}
                if hasattr(backend.generate, '__code__') and 'prompt_2' in backend.generate.__code__.co_varnames:
                    gen_kwargs['prompt_2'] = prompt_2
                img = backend.generate(prompt_1, NEGATIVE_PROMPT, gen_w, gen_h, **gen_kwargs)
                frames.append(img)
                print("OK")
            except Exception as e:
                print(f"失败: {e}")
                # 用空白帧替代
                frames.append(Image.new("RGBA", (gen_w, gen_h), (0, 0, 0, 0)))

        result[anim_name] = frames

    return result
