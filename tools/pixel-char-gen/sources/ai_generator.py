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

# 风格修饰词（根据 style 配置自动匹配）
STYLE_MODIFIERS = {
    "chibi-fantasy": "chibi style, cute proportions, big head, fantasy RPG character",
    "retro-8bit": "8-bit pixel art, NES style, limited palette, chunky pixels",
    "modern-pixel": "modern pixel art, clean edges, detailed shading, indie game style",
    "dark-souls": "dark fantasy, gothic style, gritty pixel art, muted colors",
    "cute-chibi": "super deformed, kawaii style, round shapes, pastel colors",
}

# 基础 Prompt — 控制角色外观一致性
BASE_PROMPT = (
    "pixel art game sprite, single character, solo, one character only, "
    "centered in frame, full body shot, standing on ground, "
    "clean solid white background, isolated character, no background elements, "
    "high contrast, crisp pixel edges, sharp outlines, "
    "{style}, {clothing}, {weapon}, {accessories}, {skin_tone}"
)

# 动画 Pose Prompt — 控制每个动画的姿态
ANIMATION_PROMPTS = {
    "idle": (
        "standing still, front-facing, idle pose, relaxed stance, "
        "arms at sides, feet together, looking forward"
    ),
    "walk_down": (
        "walking forward, front view, left foot stepping forward, "
        "arms swinging, mid-stride pose, moving towards viewer"
    ),
    "walk_up": (
        "walking away, back view, seen from behind, "
        "arms swinging, mid-stride pose, moving away from viewer"
    ),
    "walk_left": (
        "walking left, side view, profile pose, "
        "left arm forward, right arm back, mid-stride"
    ),
    "walk_right": (
        "walking right, side view, profile pose, "
        "right arm forward, left arm back, mid-stride"
    ),
    "attack": (
        "attacking, action pose, weapon raised, "
        "dynamic strike motion, aggressive stance, mid-swing"
    ),
    "hurt": (
        "recoiling from damage, flinching, knocked back, "
        "pain expression, defensive posture, staggering"
    ),
    "death": (
        "collapsing, falling down, defeated, "
        "falling to ground, losing balance, downed"
    ),
}

# 帧间微变化 — 让同一动画的不同帧有区别
FRAME_VARIANTS = {
    "idle": [
        "breathing, slight sway",
        "looking slightly left",
        "looking slightly right",
        "breathing, slight sway",
    ],
    "walk_down": [
        "left foot forward, right foot back",
        "feet together, passing position",
        "right foot forward, left foot back",
        "feet together, passing position",
        "left foot forward, right foot back",
        "feet together, passing position",
    ],
    "walk_up": [
        "left foot forward, right foot back",
        "feet together, passing position",
        "right foot forward, left foot back",
        "feet together, passing position",
        "left foot forward, right foot back",
        "feet together, passing position",
    ],
    "walk_left": [
        "left leg extended forward",
        "legs passing, mid-step",
        "right leg extended forward",
        "legs passing, mid-step",
    ],
    "walk_right": [
        "right leg extended forward",
        "legs passing, mid-step",
        "left leg extended forward",
        "legs passing, mid-step",
    ],
    "attack": [
        "winding up, weapon raised high",
        "weapon overhead, ready to strike",
        "mid-swing, weapon coming down",
        "weapon extended forward, follow through",
        "recovery pose, weapon returning",
        "back to neutral stance",
    ],
    "hurt": [
        "just got hit, flinching",
        "knocked back, arms raised defensively",
        "recovering, returning to stance",
    ],
    "death": [
        "staggering, losing balance",
        "falling backward",
        "halfway to ground",
        "crumpling to ground",
        "lying on ground",
        "lying still on ground",
    ],
}

# 负面 Prompt — 排除不需要的元素
NEGATIVE_PROMPT = (
    "multiple characters, two characters, group, crowd, "
    "background, scenery, landscape, environment, room, indoor, outdoor, "
    "realistic, photograph, 3d render, 3d model, cgi, "
    "blurry, low quality, low resolution, jpeg artifacts, compression, "
    "watermark, text, signature, logo, label, ui, "
    "deformed, disfigured, mutated, extra limbs, extra arms, extra legs, "
    "fused fingers, too many fingers, missing limbs, "
    "bad anatomy, bad proportions, gross proportions, "
    "cropped, out of frame, cut off, "
    "anime, cartoon, illustration, drawing, painting, sketch, "
    "monochrome, grayscale, black and white"
)


def build_prompt(character_config: dict, anim_name: str, frame_index: int = 0) -> str:
    """构建单帧的完整 prompt

    结构：基础角色描述 + 风格修饰 + 动画姿态 + 帧间变化
    确保：单角色、白色背景、像素风格、姿态一致
    """
    style = character_config.get("style", "modern-pixel")

    # 基础角色描述
    base = BASE_PROMPT.format(
        style=STYLE_MODIFIERS.get(style, f"{style} pixel art"),
        clothing=character_config.get("clothing", ""),
        weapon=character_config.get("weapon", ""),
        accessories=character_config.get("accessories", ""),
        skin_tone=character_config.get("skin_tone", "")
    )

    # 动画姿态
    anim_desc = ANIMATION_PROMPTS.get(anim_name, ANIMATION_PROMPTS["idle"])

    # 帧间变化
    variants = FRAME_VARIANTS.get(anim_name, FRAME_VARIANTS["idle"])
    variant = variants[frame_index % len(variants)]

    # 组合
    parts = [base, anim_desc, variant]
    return ", ".join(p for p in parts if p)


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
        self.model_id = config.get("model", "stabilityai/sd-turbo")
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
        from diffusers import AutoPipelineForText2Image

        device = "cuda" if self.device == "auto" and torch.cuda.is_available() else "cpu"
        dtype = torch.float16 if device == "cuda" else torch.float32

        # 优先从 ModelScope 下载（国内快）
        local_path = self._download_from_modelscope()
        model_id = local_path if local_path else self.model_id

        print(f"[LocalSD] Loading model from: {model_id}")
        self._pipe = AutoPipelineForText2Image.from_pretrained(
            model_id,
            torch_dtype=dtype,
            safety_checker=None
        ).to(device)

    def _download_from_modelscope(self) -> str:
        """从 ModelScope 下载模型，返回本地路径，失败返回空字符串"""
        try:
            from modelscope import snapshot_download
            # ModelScope 上的 SD Turbo 模型 ID
            model_map = {
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
                 width: int, height: int) -> Image.Image:
        self._load_pipeline()
        # SD 需要 8 的倍数
        w = max(64, (width // 8) * 8)
        h = max(64, (height // 8) * 8)

        result = self._pipe(
            prompt=prompt,
            negative_prompt=negative_prompt,
            width=w,
            height=h,
            num_inference_steps=4,  # SD Turbo 只需 4 步
            guidance_scale=0.0
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
            prompt = build_prompt(character_config, anim_name, i)
            print(f"  [{current}/{total_frames}] {anim_name}_{i:02d} ...",
                  end=" ", flush=True)

            try:
                img = backend.generate(prompt, NEGATIVE_PROMPT, gen_w, gen_h)
                frames.append(img)
                print("OK")
            except Exception as e:
                print(f"失败: {e}")
                # 用空白帧替代
                frames.append(Image.new("RGBA", (gen_w, gen_h), (0, 0, 0, 0)))

        result[anim_name] = frames

    return result
