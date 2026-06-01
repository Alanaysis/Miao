"""
网络图片搜索模块
从免费素材网站搜索和下载角色图片
"""

import os
import re
import requests
from PIL import Image
import io

from typing import Optional


def search_images(query: str, source: str = "opengameart",
                  count: int = 10) -> list[dict]:
    """搜索图片，返回 [{url, title, source}, ...]"""
    if source == "opengameart":
        return _search_opengameart(query, count)
    elif source == "itchio":
        return _search_itchio(query, count)
    else:
        print(f"不支持的来源: {source}")
        return []


def _search_opengameart(query: str, count: int) -> list[dict]:
    """从 OpenGameArt.org 搜索 2D 精灵素材"""
    search_url = "https://opengameart.org/art-search-advanced"
    params = {
        "keys": query,
        "field_art_type_tid": 9,  # 2D Art
        "sort_by": "count",
        "sort_order": "DESC"
    }
    headers = {"User-Agent": "pixel-char-gen/1.0 (game dev tool)"}

    try:
        resp = requests.get(search_url, params=params,
                            headers=headers, timeout=15)
        resp.raise_for_status()

        # 简单提取图片链接
        urls = re.findall(r'(https?://[^\s"]+\.(?:png|jpg|jpeg))', resp.text)
        # 去重
        seen = set()
        results = []
        for url in urls:
            if url not in seen and "sprite" in url.lower() or "character" in url.lower():
                seen.add(url)
                results.append({"url": url, "title": "", "source": "opengameart"})
        return results[:count]
    except Exception as e:
        print(f"搜索 OpenGameArt 失败: {e}")
        return []


def _search_itchio(query: str, count: int) -> list[dict]:
    """从 itch.io 搜索免费素材"""
    search_url = f"https://itch.io/game-assets/free/tag-sprites/tag-{query}"
    headers = {"User-Agent": "pixel-char-gen/1.0 (game dev tool)"}

    try:
        resp = requests.get(search_url, headers=headers, timeout=15)
        urls = re.findall(r'(https?://[^\s"]+\.(?:png|jpg|jpeg))', resp.text)
        results = [{"url": u, "title": "", "source": "itchio"}
                    for u in list(dict.fromkeys(urls))[:count]]
        return results
    except Exception as e:
        print(f"搜索 itch.io 失败: {e}")
        return []


def download_image(url: str, save_path: Optional[str] = None) -> Image.Image:
    """下载图片并返回 PIL Image"""
    headers = {"User-Agent": "pixel-char-gen/1.0 (game dev tool)"}
    resp = requests.get(url, timeout=30, headers=headers)
    resp.raise_for_status()

    img = Image.open(io.BytesIO(resp.content)).convert("RGBA")

    if save_path:
        os.makedirs(os.path.dirname(save_path) or ".", exist_ok=True)
        img.save(save_path, "PNG")

    return img


def search_and_download(query: str, output_dir: str,
                        count: int = 5,
                        source: str = "opengameart") -> list[str]:
    """搜索并下载图片

    Returns:
        下载的文件路径列表
    """
    results = search_images(query, source, count)
    if not results:
        print(f"未找到匹配 '{query}' 的图片")
        return []

    os.makedirs(output_dir, exist_ok=True)
    downloaded = []

    for i, item in enumerate(results):
        try:
            path = os.path.join(output_dir, f"{query}_{i:02d}.png")
            download_image(item["url"], path)
            downloaded.append(path)
            print(f"下载: {path}")
        except Exception as e:
            print(f"下载失败 {item['url']}: {e}")

    return downloaded
