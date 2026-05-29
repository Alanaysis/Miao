"""
网络图片搜索模块
从免费素材网站搜索和下载角色图片
"""

import os
import requests
from PIL import Image
import io


def search_opengameart(query: str, count: int = 10) -> list[str]:
    """从 OpenGameArt.org 搜索图片 URL（简化实现）

    注意：OpenGameArt 没有公开 API，这里提供基础的网页搜索逻辑
    实际使用时可能需要根据网站结构调整
    """
    # 这是一个简化实现，实际可能需要更复杂的爬虫逻辑
    search_url = f"https://opengameart.org/art-search-advanced?keys={query}&field_art_type_tid=9"
    try:
        resp = requests.get(search_url, timeout=15,
                            headers={"User-Agent": "pixel-char-gen/1.0"})
        # 简单提取图片链接（实际需要解析 HTML）
        urls = []
        # 这里需要 BeautifulSoup 解析，暂留接口
        return urls[:count]
    except Exception as e:
        print(f"搜索 OpenGameArt 失败: {e}")
        return []


def download_image(url: str, save_path: str = None) -> Image.Image:
    """下载图片并返回 PIL Image

    Args:
        url: 图片 URL
        save_path: 可选，保存路径

    Returns:
        PIL Image 对象
    """
    resp = requests.get(url, timeout=30,
                        headers={"User-Agent": "pixel-char-gen/1.0"})
    resp.raise_for_status()

    img = Image.open(io.BytesIO(resp.content)).convert("RGBA")

    if save_path:
        os.makedirs(os.path.dirname(save_path) or ".", exist_ok=True)
        img.save(save_path, "PNG")

    return img


def search_and_download(query: str, output_dir: str,
                        count: int = 5) -> list[str]:
    """搜索并下载图片

    Args:
        query: 搜索关键词
        output_dir: 保存目录
        count: 下载数量

    Returns:
        下载的文件路径列表
    """
    urls = search_opengameart(query, count)
    if not urls:
        print(f"未找到匹配 '{query}' 的图片")
        return []

    os.makedirs(output_dir, exist_ok=True)
    downloaded = []

    for i, url in enumerate(urls):
        try:
            path = os.path.join(output_dir, f"{query}_{i:02d}.png")
            download_image(url, path)
            downloaded.append(path)
            print(f"下载: {path}")
        except Exception as e:
            print(f"下载失败 {url}: {e}")

    return downloaded
