"""Edit 步骤测试 — 深度行为验证"""
import re
from playwright.sync_api import Page, expect


def _enter_editor(page: Page, server: str):
    """通过 Blank Canvas 进入 Edit"""
    page.goto(server, wait_until="domcontentloaded")
    page.locator(".import-card", has_text="Blank Canvas").click()
    expect(page.locator("#editorCanvas")).to_be_visible(timeout=5000)


def test_canvas_has_correct_dimensions(server: str, page: Page):
    """canvas 有正确的像素尺寸"""
    _enter_editor(page, server)
    dims = page.evaluate("""() => {
        const c = document.getElementById('editorCanvas');
        const ctx = c.getContext('2d');
        const data = ctx.getImageData(0, 0, c.width, c.height).data;
        return { w: c.width, h: c.height, totalPixels: c.width * c.height, dataLen: data.length };
    }""")
    assert dims["w"] == 32, f"Canvas width should be 32, got {dims['w']}"
    assert dims["h"] == 32, f"Canvas height should be 32, got {dims['h']}"
    assert dims["dataLen"] == 32 * 32 * 4, f"ImageData should have 32*32*4 bytes, got {dims['dataLen']}"


def test_canvas_dimensions_match_state(server: str, page: Page):
    """canvas 尺寸与 S.width/S.height 一致"""
    _enter_editor(page, server)
    result = page.evaluate("""() => {
        const c = document.getElementById('editorCanvas');
        return { canvasW: c.width, canvasH: c.height, stateW: S.width, stateH: S.height };
    }""")
    assert result["canvasW"] == result["stateW"], "Canvas width should match S.width"
    assert result["canvasH"] == result["stateH"], "Canvas height should match S.height"


def test_zoom_changes_transform(server: str, page: Page):
    """zoom 改变后 canvas-wrap transform 更新"""
    _enter_editor(page, server)

    # 设置 zoom = 4
    page.locator("#zoomSlider").fill("4")
    page.locator("#zoomSlider").dispatch_event("input")

    transform = page.evaluate("() => document.getElementById('canvasWrap').style.transform")
    assert "scale(4)" in transform, f"Transform should contain scale(4), got: {transform}"

    # 设置 zoom = 1
    page.locator("#zoomSlider").fill("1")
    page.locator("#zoomSlider").dispatch_event("input")

    transform = page.evaluate("() => document.getElementById('canvasWrap').style.transform")
    assert "scale(1)" in transform, f"Transform should contain scale(1), got: {transform}"


def test_tool_switch_changes_cursor_and_status(server: str, page: Page):
    """切换工具：光标样式和状态栏都更新"""
    _enter_editor(page, server)

    tools = {
        "pencil": ("crosshair", "Pencil"),
        "eraser": ("cell", "Eraser"),
        "fill": ("pointer", "Fill"),
        "picker": ("copy", "Picker"),
        "select": ("crosshair", "Select"),
        "move": ("grab", "Move"),
    }

    for tool_name, (expected_cursor, expected_status) in tools.items():
        page.locator(f"[data-tool='{tool_name}']").click()
        expect(page.locator("#statusTool")).to_have_text(expected_status, timeout=3000)

        cursor = page.evaluate("() => document.getElementById('canvasArea').style.cursor")
        assert cursor == expected_cursor, f"Tool {tool_name}: expected cursor {expected_cursor}, got {cursor}"


def test_zoom_label_sync(server: str, page: Page):
    """zoom 滑块值与 label 同步"""
    _enter_editor(page, server)

    for val in ["2", "8", "16"]:
        page.locator("#zoomSlider").fill(val)
        page.locator("#zoomSlider").dispatch_event("input")
        expect(page.locator("#zoomLabel")).to_have_text(f"{val}x", timeout=3000)


def test_preview_resize_shows_comparison(server: str, page: Page):
    """Preview Resize 弹窗显示原图和结果对比"""
    _enter_editor(page, server)
    page.locator("button", has_text="Preview Resize").click()

    # 弹窗可见
    expect(page.locator("text=Preview:")).to_be_visible(timeout=5000)
    expect(page.locator("text=Original")).to_be_visible()
    expect(page.locator("text=Result")).to_be_visible()

    # 有 canvas 元素用于显示对比
    expect(page.locator("#previewOrigCanvas")).to_be_visible()
    expect(page.locator("#previewResultCanvas")).to_be_visible()


def test_preview_resize_algorithms(server: str, page: Page):
    """Preview Resize 算法选择器有 4 种算法"""
    _enter_editor(page, server)
    algo = page.locator("#previewAlgo")
    options = algo.locator("option")
    expect(options).to_have_count(4)

    values = [options.nth(i).get_attribute("value") for i in range(4)]
    assert "nearest" in values
    assert "bilinear" in values
    assert "bicubic" in values
    assert "lanczos" in values


def test_grid_toggle(server: str, page: Page):
    """Grid 按钮切换网格显示"""
    _enter_editor(page, server)

    grid_btn = page.locator("#gridBtn")

    # 初始状态：showGrid = true 但按钮可能没有 active class
    # 点击切换
    grid_btn.click()
    show_grid = page.evaluate("() => S.showGrid")
    assert show_grid == False, "Grid should be off after first click"

    grid_btn.click()
    show_grid = page.evaluate("() => S.showGrid")
    assert show_grid == True, "Grid should be on after second click"


def test_editor_status_bar(server: str, page: Page):
    """状态栏显示正确的尺寸和工具信息"""
    _enter_editor(page, server)

    size_text = page.locator("#statusSize").text_content()
    assert "32" in size_text, f"Status size should contain '32', got: {size_text}"

    tool_text = page.locator("#statusTool").text_content()
    assert tool_text == "Pencil", f"Default tool should be Pencil, got: {tool_text}"

    zoom_text = page.locator("#statusZoom").text_content()
    assert "8x" in zoom_text, f"Default zoom should be 8x, got: {zoom_text}"


def test_resize_canvas(server: str, page: Page):
    """修改画布尺寸后 canvas 和状态栏更新"""
    _enter_editor(page, server)

    # 改尺寸为 64x64
    page.locator("#editW").fill("64")
    page.locator("#editH").fill("64")
    page.locator("button", has_text="Apply").click()

    # 等 canvas 更新
    page.wait_for_timeout(500)

    dims = page.evaluate("""() => {
        const c = document.getElementById('editorCanvas');
        return { w: c.width, h: c.height, stateW: S.width, stateH: S.height };
    }""")
    assert dims["w"] == 64, f"Canvas width should be 64, got {dims['w']}"
    assert dims["h"] == 64, f"Canvas height should be 64, got {dims['h']}"
    assert dims["stateW"] == 64, f"S.width should be 64, got {dims['stateW']}"
    assert dims["stateH"] == 64, f"S.height should be 64, got {dims['stateH']}"
