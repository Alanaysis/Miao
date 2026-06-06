"""Body Parts 配置测试 — 深度行为验证"""
from playwright.sync_api import Page, expect


def _enter_editor(page: Page, server: str):
    page.goto(server, wait_until="domcontentloaded")
    page.locator(".import-card", has_text="Blank Canvas").click()
    expect(page.locator("#editorCanvas")).to_be_visible(timeout=5000)


def test_body_parts_config_section(server: str, page: Page):
    """Body Parts 区块：8 个 checkbox + 颜色选择器 + Show/Reset/Export 按钮"""
    _enter_editor(page, server)

    # 8 个 checkbox
    checkboxes = page.locator("#bodyPartsConfig input[type='checkbox']")
    expect(checkboxes).to_have_count(8, timeout=5000)

    # 8 个颜色选择器
    color_pickers = page.locator("#bodyPartsConfig input[type='color']")
    expect(color_pickers).to_have_count(8)

    # 按钮存在
    expect(page.locator("#toggleBodyPartsBtn")).to_be_visible()
    expect(page.locator("button", has_text="Reset")).to_be_visible()
    expect(page.locator("button", has_text="Export JSON")).to_be_visible()


def test_all_parts_checked_by_default(server: str, page: Page):
    """所有 8 个部位默认勾选"""
    _enter_editor(page, server)
    checkboxes = page.locator("#bodyPartsConfig input[type='checkbox']")
    for i in range(8):
        expect(checkboxes.nth(i)).to_be_checked()


def test_part_names_correct(server: str, page: Page):
    """部位名称包含 head/torso/arm/leg/weapon/cape"""
    _enter_editor(page, server)
    names = page.evaluate("""() => {
        const labels = document.querySelectorAll('#bodyPartsConfig span');
        return Array.from(labels).map(l => l.textContent.trim());
    }""")
    expected = ["head", "torso", "left_arm", "right_arm", "left_leg", "right_leg", "weapon", "cape"]
    for name in expected:
        assert name in names, f"Missing part: {name}"


def test_overlay_shows_rectangles(server: str, page: Page):
    """Show 后叠加层显示 8 个矩形"""
    _enter_editor(page, server)
    page.locator("#toggleBodyPartsBtn").click()

    overlay = page.locator("#bodyPartsOverlay")
    expect(overlay).to_be_visible(timeout=3000)

    # 8 个子 div（每个部位一个）
    rects = overlay.locator("div")
    expect(rects).to_have_count(8)


def test_overlay_rectangles_have_position(server: str, page: Page):
    """叠加层矩形有正确的 CSS 位置"""
    _enter_editor(page, server)
    page.locator("#toggleBodyPartsBtn").click()

    # 检查 head 矩形有 left/top/width/height
    positions = page.evaluate("""() => {
        const overlay = document.getElementById('bodyPartsOverlay');
        const divs = overlay.querySelectorAll('div');
        return Array.from(divs).map(d => ({
            left: d.style.left,
            top: d.style.top,
            width: d.style.width,
            height: d.style.height,
            label: d.querySelector('span')?.textContent
        }));
    }""")
    assert len(positions) == 8, f"Should have 8 rectangles, got {len(positions)}"

    for pos in positions:
        assert pos["left"], f"Rectangle {pos['label']} missing left"
        assert pos["top"], f"Rectangle {pos['label']} missing top"
        assert pos["width"], f"Rectangle {pos['label']} missing width"
        assert pos["height"], f"Rectangle {pos['label']} missing height"


def test_overlay_toggle(server: str, page: Page):
    """Show/Hide 切换叠加层可见性"""
    _enter_editor(page, server)
    overlay = page.locator("#bodyPartsOverlay")

    # 初始隐藏
    expect(overlay).not_to_be_visible()

    # Show
    page.locator("#toggleBodyPartsBtn").click()
    expect(overlay).to_be_visible()
    expect(page.locator("#toggleBodyPartsBtn")).to_have_text("Hide")

    # Hide
    page.locator("#toggleBodyPartsBtn").click()
    expect(overlay).not_to_be_visible()
    expect(page.locator("#toggleBodyPartsBtn")).to_have_text("Show")


def test_uncheck_part_removes_rectangle(server: str, page: Page):
    """取消勾选部位后叠加层矩形减少"""
    _enter_editor(page, server)
    page.locator("#toggleBodyPartsBtn").click()

    # 初始 8 个矩形
    rects = page.locator("#bodyPartsOverlay div")
    expect(rects).to_have_count(8, timeout=3000)

    # 取消 head
    page.locator("#bodyPartsConfig input[data-part='head']").uncheck()
    page.wait_for_timeout(200)

    # 现在 7 个矩形
    rects = page.locator("#bodyPartsOverlay div")
    expect(rects).to_have_count(7)


def test_color_change_updates_overlay(server: str, page: Page):
    """修改颜色后叠加层矩形颜色更新"""
    _enter_editor(page, server)
    page.locator("#toggleBodyPartsBtn").click()

    # 获取 head 矩形的初始背景色
    initial_bg = page.evaluate("""() => {
        const overlay = document.getElementById('bodyPartsOverlay');
        const headDiv = overlay.querySelector('div');
        return headDiv.style.background;
    }""")

    # 修改 head 颜色
    page.locator("[data-part-color='head']").evaluate("el => el.value = '#ff0000'")
    page.locator("[data-part-color='head']").dispatch_event("input")
    page.wait_for_timeout(200)

    # 背景色应该改变
    new_bg = page.evaluate("""() => {
        const overlay = document.getElementById('bodyPartsOverlay');
        const headDiv = overlay.querySelector('div');
        return headDiv.style.background;
    }""")
    assert new_bg != initial_bg, "Background should change after color update"


def test_reset_restores_defaults(server: str, page: Page):
    """Reset 恢复默认颜色"""
    _enter_editor(page, server)

    # 修改 head 颜色
    page.locator("[data-part-color='head']").evaluate("el => el.value = '#ff0000'")
    page.locator("[data-part-color='head']").dispatch_event("input")

    # 点 Reset
    page.locator("button", has_text="Reset").click()
    page.wait_for_timeout(200)

    # head 颜色恢复默认
    color = page.locator("[data-part-color='head']").input_value()
    assert color.lower() == "#dcb48c", f"Head color should be #dcb48c after reset, got {color}"


def test_default_colors_match(server: str, page: Page):
    """默认颜色与 DEFAULT_PARTS 一致"""
    _enter_editor(page, server)
    expected = {
        "head": "#dcb48c",
        "torso": "#3250b4",
        "left_arm": "#dcb48c",
        "right_arm": "#dcb48c",
        "left_leg": "#3c3228",
        "right_leg": "#3c3228",
        "weapon": "#b4b4c8",
        "cape": "#b42828",
    }
    for part, expected_color in expected.items():
        actual = page.locator(f"[data-part-color='{part}']").input_value()
        assert actual.lower() == expected_color, f"{part}: expected {expected_color}, got {actual}"


def test_export_json_produces_valid_config(server: str, page: Page):
    """Export JSON 产生有效配置"""
    _enter_editor(page, server)

    # 拦截下载
    with page.expect_download() as download_info:
        page.locator("button", has_text="Export JSON").click()

    download = download_info.value
    assert download.suggested_filename.endswith("_config.json"), f"Filename should end with _config.json: {download.suggested_filename}"

    # 读取下载内容
    path = download.path()
    import json
    with open(path) as f:
        config = json.load(f)

    assert "name" in config, "Config should have 'name'"
    assert "body_parts" in config, "Config should have 'body_parts'"
    assert "animations" in config, "Config should have 'animations'"

    # body_parts 有 8 个部位
    assert len(config["body_parts"]) == 8, f"Should have 8 body parts, got {len(config['body_parts'])}"

    # 每个部位有 rect 和 color
    for part_name, part_data in config["body_parts"].items():
        assert "rect" in part_data, f"{part_name} missing 'rect'"
        assert "color" in part_data, f"{part_name} missing 'color'"
        assert len(part_data["rect"]) == 4, f"{part_name} rect should have 4 values"


def test_overlay_scales_with_canvas_size(server: str, page: Page):
    """画布尺寸变化后叠加层坐标按比例缩放"""
    _enter_editor(page, server)
    page.locator("#toggleBodyPartsBtn").click()

    # 获取 32x32 时的 head 矩形尺寸
    small_pos = page.evaluate("""() => {
        const div = document.getElementById('bodyPartsOverlay').querySelector('div');
        return { w: parseInt(div.style.width), h: parseInt(div.style.height) };
    }""")

    # 改为 64x64
    page.locator("#editW").fill("64")
    page.locator("#editH").fill("64")
    page.locator("button", has_text="Apply").click()
    page.wait_for_timeout(500)

    # 重新显示叠加层
    page.locator("#toggleBodyPartsBtn").click()
    page.locator("#toggleBodyPartsBtn").click()

    large_pos = page.evaluate("""() => {
        const div = document.getElementById('bodyPartsOverlay').querySelector('div');
        return { w: parseInt(div.style.width), h: parseInt(div.style.height) };
    }""")

    # 64x64 的矩形应该是 32x32 的 2 倍
    assert large_pos["w"] == small_pos["w"] * 2, f"Width should double: {small_pos['w']} -> {large_pos['w']}"
    assert large_pos["h"] == small_pos["h"] * 2, f"Height should double: {small_pos['h']} -> {large_pos['h']}"
