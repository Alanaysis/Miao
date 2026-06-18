#!/usr/bin/env python3
"""
Godot 2D 像素游戏视觉质量审计工具
分析素材色彩一致性、UI对比度、像素质量、缺失效果等
"""

import os
import json
import glob
from collections import Counter, defaultdict
from pathlib import Path

try:
    from PIL import Image
    import numpy as np
    HAS_PIL = True
except ImportError:
    HAS_PIL = False

PROJECT_ROOT = Path(__file__).parent.parent / "project"
SCRIPTS_DIR = PROJECT_ROOT / "scripts"
ASSETS_DIR = PROJECT_ROOT / "assets"
DATA_DIR = PROJECT_ROOT / "data"


def analyze_sprite_palettes():
    """分析所有角色精灵的调色板一致性"""
    print("\n=== 1. 角色调色板分析 ===")

    char_dirs = sorted(glob.glob(str(ASSETS_DIR / "sprites/characters/*/")))
    all_palettes = {}

    for char_dir in char_dirs:
        char_name = Path(char_dir).name
        pngs = glob.glob(os.path.join(char_dir, "*.png"))
        if not pngs:
            continue

        colors = Counter()
        for png_path in pngs[:2]:  # 只分析前2张
            try:
                img = Image.open(png_path).convert("RGBA")
                pixels = np.array(img)
                # 只统计非透明像素
                mask = pixels[:, :, 3] > 128
                if mask.any():
                    visible = pixels[mask]
                    # 量化到 8x8x8 色彩空间减少噪声
                    quantized = (visible[:, :3] // 32) * 32
                    for p in quantized:
                        colors[tuple(p)] += 1
            except Exception as e:
                print(f"  ⚠ 读取失败 {png_path}: {e}")

        if colors:
            top_colors = colors.most_common(8)
            all_palettes[char_name] = top_colors
            unique = len(colors)
            print(f"  {char_name}: {unique} 种颜色 (前3: {', '.join(f'RGB{c}' for c, _ in top_colors[:3])})")

    # 检查跨角色颜色一致性
    if len(all_palettes) > 1:
        all_top = set()
        for pal in all_palettes.values():
            for color, _ in pal[:3]:
                all_top.add(color)

        if len(all_top) > 15:
            print(f"  ⚠ 跨角色颜色差异较大 ({len(all_top)} 种主色)，建议统一调色板")
        else:
            print(f"  ✓ 跨角色颜色较一致 ({len(all_top)} 种主色)")
    return all_palettes


def analyze_pixel_quality():
    """检查素材是否正确使用 Nearest 过滤（无模糊）"""
    print("\n=== 2. 像素质量检查 ===")

    issues = []
    pngs = glob.glob(str(ASSETS_DIR / "**/*.png"), recursive=True)

    for png_path in pngs[:50]:  # 采样前50张
        try:
            img = Image.open(png_path).convert("RGBA")
            w, h = img.size

            # 检查1：是否为常见像素美术尺寸的倍数
            if w % 8 != 0 or h % 8 != 0:
                rel = os.path.relpath(png_path, PROJECT_ROOT)
                issues.append(f"  ⚠ {rel}: 尺寸 {w}x{h} 不是 8 的倍数")

            # 检查2：是否有半透明像素（可能导入设置有问题）
            pixels = np.array(img)
            semi_trans = np.sum((pixels[:, :, 3] > 0) & (pixels[:, :, 3] < 255))
            if semi_trans > pixels.shape[0] * pixels.shape[1] * 0.1:
                rel = os.path.relpath(png_path, PROJECT_ROOT)
                issues.append(f"  ⚠ {rel}: 大量半透明像素 ({semi_trans} 个)，像素美术应避免半透明")

        except Exception:
            pass

    if issues:
        for issue in issues[:10]:
            print(issue)
    else:
        print("  ✓ 采样的素材像素质量正常")

    print(f"  共检查 {min(len(pngs), 50)} 张图片")
    return issues


def analyze_ui_code():
    """分析 UI 代码中的视觉问题"""
    print("\n=== 3. UI 代码质量分析 ===")

    issues = []
    findings = []

    # 检查是否有 Theme 资源
    theme_files = glob.glob(str(PROJECT_ROOT / "**/*.tres"), recursive=True)
    has_theme = any("theme" in f.lower() for f in theme_files)
    if not has_theme:
        issues.append("  ❌ 没有 UI Theme 资源 — 所有 UI 使用默认样式")
        findings.append("创建 pixel_theme.tres，用 StyleBoxTexture 做像素风边框")

    # 检查 HUD 代码
    hud_path = SCRIPTS_DIR / "ui" / "HUD.cs"
    if hud_path.exists():
        code = hud_path.read_text()

        # 检查是否使用硬编码位置
        if "Position = new Vector2(" in code:
            count = code.count("Position = new Vector2(")
            issues.append(f"  ⚠ HUD 使用了 {count} 处硬编码位置 — 不同分辨率会错位")
            findings.append("改用 Anchor + Offset 实现响应式布局")

        # 检查是否有动画效果
        if "Tween" not in code and "tween" not in code.lower():
            issues.append("  ⚠ HUD 没有任何 Tween 动画 — 状态变化生硬")
            findings.append("给血条、技能CD、伤害数字加 Tween 过渡")

        # 检查字体大小
        if "font_size" in code:
            sizes = [int(s) for s in code.split("font_size") if s.strip().lstrip("-").isdigit()]
            if sizes:
                min_s, max_s = min(sizes), max(sizes)
                if max_s - min_s > 12:
                    issues.append(f"  ⚠ HUD 字体大小范围过大 ({min_s}-{max_s})")

    # 检查 UIStyle.cs
    ui_style_path = SCRIPTS_DIR / "ui" / "UIStyle.cs"
    if ui_style_path.exists():
        code = ui_style_path.read_text()

        # 检查是否有 Overlay 方法
        if "Overlay" not in code:
            issues.append("  ⚠ UIStyle 缺少 Overlay 方法 — 弹窗背景不统一")

        # 检查稀有度颜色
        exotic_count = code.count("Exotic")
        if exotic_count < 2:
            issues.append("  ⚠ UIStyle.Exotic 颜色定义不完整")

    # 检查 ScreenManager
    has_screen_manager = False
    for cs in SCRIPTS_DIR.rglob("*.cs"):
        if "ScreenManager" in cs.name or "ScreenTransition" in cs.name:
            has_screen_manager = True
            break
    if not has_screen_manager:
        issues.append("  ❌ 没有 ScreenManager / 过渡管理器 — 场景切换无动画")
        findings.append("创建 TransitionManager 实现淡入淡出/擦除效果")

    for issue in issues:
        print(issue)
    if findings:
        print("\n  建议改进:")
        for f in findings:
            print(f"    → {f}")

    return issues, findings


def analyze_effects_gaps():
    """检查缺失的视觉效果"""
    print("\n=== 4. 视觉效果缺口分析 ===")

    gaps = []

    # 检查是否有粒子系统
    has_particles = False
    for cs in SCRIPTS_DIR.rglob("*.cs"):
        try:
            code = cs.read_text()
            if "GPUParticles2D" in code or "CPUParticles2D" in code:
                has_particles = True
                break
        except:
            pass
    if not has_particles:
        gaps.append("  ❌ 没有使用任何粒子系统 — 缺少环境氛围和战斗特效")
        gaps.append("    建议: 火焰粒子、烟雾粒子、命中火花、升级特效")

    # 检查是否有 Shader
    has_shader = False
    for gdshader in PROJECT_ROOT.rglob("*.gdshader"):
        has_shader = True
        break
    for cs in SCRIPTS_DIR.rglob("*.cs"):
        try:
            code = cs.read_text()
            if "ShaderMaterial" in code or "shader_parameter" in code:
                has_shader = True
                break
        except:
            pass
    if not has_shader:
        gaps.append("  ❌ 没有使用任何 Shader — 缺少受击闪光、描边等效果")

    # 检查屏幕震动
    has_shake = False
    for cs in SCRIPTS_DIR.rglob("*.cs"):
        try:
            code = cs.read_text()
            if "Shake" in code or "shake" in code:
                has_shake = True
                break
        except:
            pass
    if not has_shake:
        gaps.append("  ❌ 没有屏幕震动 — 战斗缺乏冲击感")

    # 检查停顿效果
    has_hitstop = False
    for cs in SCRIPTS_DIR.rglob("*.cs"):
        try:
            code = cs.read_text()
            if "HitStop" in code or "TimeScale" in code or "hitstop" in code:
                has_hitstop = True
                break
        except:
            pass
    if not has_hitstop:
        gaps.append("  ❌ 没有停顿效果（Hitstop）— 重击缺乏重量感")

    # 检查场景过渡
    has_transition = False
    for cs in SCRIPTS_DIR.rglob("*.cs"):
        try:
            code = cs.read_text()
            if "FadeToBlack" in code or "TransitionManager" in code:
                has_transition = True
                break
        except:
            pass
    if not has_transition:
        gaps.append("  ❌ 没有场景过渡动画 — 切换场景生硬")

    # 检查伤害数字
    has_damage_numbers = False
    for cs in SCRIPTS_DIR.rglob("*.cs"):
        try:
            code = cs.read_text()
            if "FloatingDamage" in code or "damage_number" in code.lower():
                has_damage_numbers = True
                break
        except:
            pass
    if not has_damage_numbers:
        gaps.append("  ⚠ 本地玩家没有伤害数字（只有远程同步）")

    for gap in gaps:
        print(gap)

    return gaps


def analyze_color_contrast():
    """分析 UI 颜色对比度"""
    print("\n=== 5. UI 颜色对比度分析 ===")

    ui_style_path = SCRIPTS_DIR / "ui" / "UIStyle.cs"
    if not ui_style_path.exists():
        print("  ⚠ UIStyle.cs 不存在")
        return

    code = ui_style_path.read_text()

    # 提取颜色定义
    import re
    colors = {}
    for match in re.finditer(r'(Accent\w+|Text\w+|Border\w+|Background)\s*=\s*new Color\("([^"]+)"\)', code):
        name, hex_color = match.groups()
        try:
            hex_color = hex_color.lstrip('#')
            if len(hex_color) == 3:
                hex_color = ''.join(c*2 for c in hex_color)
            r, g, b = int(hex_color[:2], 16), int(hex_color[2:4], 16), int(hex_color[4:6], 16)
            colors[name] = (r, g, b)
        except:
            pass

    if colors:
        print(f"  定义了 {len(colors)} 种颜色:")
        for name, (r, g, b) in colors.items():
            # 计算相对亮度
            luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255
            print(f"    {name}: #{r:02x}{g:02x}{b:02x} (亮度: {luminance:.2f})")

        # 检查对比度
        bg = colors.get("Background", (15, 15, 25))
        for name, (r, g, b) in colors.items():
            if "Text" in name or "Accent" in name:
                # 计算 WCAG 对比度
                l1 = (0.299 * bg[0] + 0.587 * bg[1] + 0.114 * bg[2]) / 255
                l2 = (0.299 * r + 0.587 * g + 0.114 * b) / 255
                lighter = max(l1, l2)
                darker = min(l1, l2)
                contrast = (lighter + 0.05) / (darker + 0.05)

                if contrast < 3.0:
                    print(f"  ⚠ {name} vs Background 对比度 {contrast:.1f}:1 (建议 ≥ 4.5:1)")
                else:
                    print(f"  ✓ {name} vs Background 对比度 {contrast:.1f}:1")


def generate_report():
    """生成完整审计报告"""
    print("=" * 60)
    print("  Godot 2D 像素游戏 — 视觉质量审计报告")
    print("=" * 60)

    all_issues = []
    all_findings = []
    all_gaps = []

    # 1. 角色调色板
    palettes = analyze_sprite_palettes()

    # 2. 像素质量
    pixel_issues = analyze_pixel_quality()
    all_issues.extend(pixel_issues)

    # 3. UI 代码
    ui_issues, ui_findings = analyze_ui_code()
    all_issues.extend(ui_issues)
    all_findings.extend(ui_findings)

    # 4. 效果缺口
    gaps = analyze_effects_gaps()
    all_gaps.extend(gaps)

    # 5. 颜色对比度
    analyze_color_contrast()

    # 汇总
    print("\n" + "=" * 60)
    print("  审计总结")
    print("=" * 60)

    total_issues = len(all_issues) + len(all_gaps)
    if total_issues == 0:
        print("  ✅ 未发现明显问题")
    else:
        print(f"  发现 {len(all_issues)} 个代码问题 + {len(all_gaps)} 个效果缺口")

        # 优先级排序
        print("\n  🔴 必须修复:")
        critical = [i for i in all_issues if "❌" in i] + [g for g in all_gaps if "❌" in g]
        for c in critical:
            print(f"    {c}")

        print("\n  🟡 建议改进:")
        warnings = [i for i in all_issues if "⚠" in i] + [g for g in all_gaps if "⚠" in g]
        for w in warnings:
            print(f"    {w}")

        if all_findings:
            print("\n  💡 改进建议:")
            for f in all_findings:
                print(f"    → {f}")

    print(f"\n  评分: {max(0, 100 - total_issues * 5)}/100")
    print("=" * 60)

    return {
        "issues": all_issues,
        "gaps": all_gaps,
        "findings": all_findings,
        "score": max(0, 100 - total_issues * 5)
    }


if __name__ == "__main__":
    generate_report()
