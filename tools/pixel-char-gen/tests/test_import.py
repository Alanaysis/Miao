"""Import 步骤测试 — 深度行为验证"""
import re
from playwright.sync_api import Page, expect


def test_page_loads(server: str, page: Page):
    """页面加载，4 个 import card 可见，标题正确"""
    page.goto(server, wait_until="domcontentloaded")
    cards = page.locator(".import-card")
    expect(cards).to_have_count(4, timeout=10000)
    # 每个 card 都有标题
    for text in ["AI Generate", "Import Image", "Blank Canvas", "Programmatic"]:
        expect(page.locator(f".import-card-title", has_text=text)).to_be_visible()


def test_step_bar_visible(server: str, page: Page):
    """步骤栏 4 步都可见，第 1 步激活"""
    page.goto(server, wait_until="domcontentloaded")
    steps = page.locator(".step")
    expect(steps).to_have_count(4, timeout=10000)
    expect(page.locator(".step[data-step='1']")).to_have_class(re.compile("active"))
    expect(page.locator(".step[data-step='2']")).not_to_have_class(re.compile("active"))


def test_import_ai_config_fields(server: str, page: Page):
    """AI 配置面板：所有字段存在且有默认值"""
    page.goto(server, wait_until="domcontentloaded")
    page.locator(".import-card", has_text="AI Generate").click()
    expect(page.locator("#aiConfig")).to_be_visible(timeout=5000)

    # 系统 prompt 有默认值
    prompt = page.locator("#cfgSystemPrompt")
    expect(prompt).to_be_visible()
    expect(prompt).to_have_value("pixel art, single character, white background, centered, full body, standing")

    # Name 有默认值
    expect(page.locator("#cfgName")).to_have_value("warrior")

    # Variants 有默认值
    expect(page.locator("#cfgVariants")).to_have_value("4")

    # Style select 有多个选项
    style = page.locator("#cfgStyle")
    expect(style).to_be_visible()
    options = style.locator("option")
    expect(options).to_have_count(4)  # 4 种风格


def test_import_ai_preset_buttons(server: str, page: Page):
    """预设按钮改变表单值"""
    page.goto(server, wait_until="domcontentloaded")
    page.locator(".import-card", has_text="AI Generate").click()
    expect(page.locator("#aiConfig")).to_be_visible(timeout=5000)

    # 点 Mage 预设
    page.locator(".preset-btn", has_text="Mage").click()
    expect(page.locator("#cfgName")).to_have_value("mage")
    expect(page.locator("#cfgWeapon")).to_have_value("magic staff")

    # 点 Archer 预设
    page.locator(".preset-btn", has_text="Archer").click()
    expect(page.locator("#cfgName")).to_have_value("archer")
    expect(page.locator("#cfgWeapon")).to_have_value("bow and arrows")


def test_import_programmatic_mode_select(server: str, page: Page):
    """程序化模式切换：preset/import/custom 显示不同区块"""
    page.goto(server, wait_until="domcontentloaded")
    page.locator(".import-card", has_text="Programmatic").click()
    expect(page.locator("#programmaticConfig")).to_be_visible(timeout=5000)

    # 默认 preset 模式
    mode = page.locator("#progMode")
    expect(mode).to_have_value("preset")
    expect(page.locator("#progPresetSection")).to_be_visible()
    expect(page.locator("#progImportSection")).not_to_be_visible()

    # 切到 import 模式
    mode.select_option("import")
    expect(page.locator("#progImportSection")).to_be_visible()
    expect(page.locator("#progPresetSection")).not_to_be_visible()

    # 切到 custom 模式
    mode.select_option("custom")
    expect(page.locator("#progCustomSection")).to_be_visible()


def test_import_blank_canvas_has_dimensions(server: str, page: Page):
    """Blank Canvas 进入编辑器，canvas 有正确的像素尺寸"""
    page.goto(server, wait_until="domcontentloaded")
    page.locator(".import-card", has_text="Blank Canvas").click()
    expect(page.locator("#editorCanvas")).to_be_visible(timeout=5000)

    dims = page.evaluate("""() => {
        const c = document.getElementById('editorCanvas');
        return { w: c.width, h: c.height, stateW: S.width, stateH: S.height };
    }""")
    assert dims["w"] == 32, f"Canvas width should be 32, got {dims['w']}"
    assert dims["h"] == 32, f"Canvas height should be 32, got {dims['h']}"
    assert dims["stateW"] == 32, f"S.width should be 32, got {dims['stateW']}"
    assert dims["stateH"] == 32, f"S.height should be 32, got {dims['stateH']}"


def test_back_hides_all_panels(server: str, page: Page):
    """Back 按钮隐藏所有配置面板，显示 import 卡片"""
    page.goto(server, wait_until="domcontentloaded")

    # AI config → Back
    page.locator(".import-card", has_text="AI Generate").click()
    expect(page.locator("#aiConfig")).to_be_visible(timeout=5000)
    page.locator("#aiConfig button", has_text="Back").click()
    expect(page.locator("#importPanel")).to_be_visible()
    expect(page.locator("#aiConfig")).not_to_be_visible()
    expect(page.locator("#programmaticConfig")).not_to_be_visible()
    expect(page.locator("#uploadZone")).not_to_be_visible()

    # Programmatic → Back
    page.locator(".import-card", has_text="Programmatic").click()
    expect(page.locator("#programmaticConfig")).to_be_visible(timeout=5000)
    page.locator("#programmaticConfig button", has_text="Back").click()
    expect(page.locator("#importPanel")).to_be_visible()
    expect(page.locator("#programmaticConfig")).not_to_be_visible()


def test_ai_config_sent_to_server(server: str, page: Page):
    """修改 AI 配置后点击 Generate，发送到服务器的数据包含用户输入"""
    page.goto(server, wait_until="domcontentloaded")
    page.locator(".import-card", has_text="AI Generate").click()
    expect(page.locator("#aiConfig")).to_be_visible(timeout=5000)

    # 拦截 fetch 请求
    page.evaluate("""() => {
        window._lastFetchBody = null;
        const origFetch = window.fetch;
        window.fetch = function(url, opts) {
            if (url.includes('generate-base')) {
                window._lastFetchBody = JSON.parse(opts.body);
            }
            return origFetch.apply(this, arguments);
        };
    }""")

    # 修改配置
    page.locator("#cfgClothing").fill("kimono")
    page.locator("#cfgWeapon").fill("katana")
    page.locator("#cfgAccessories").fill("straw hat")
    page.locator("#cfgStyle").select_option("retro-8bit")

    # 点 Generate
    page.locator("#aiConfig button", has_text="Generate").click()
    page.wait_for_timeout(2000)

    # 检查发送的数据
    sent = page.evaluate("() => window._lastFetchBody")
    assert sent is not None, "Should have sent a request"
    assert sent["clothing"] == "kimono", f"clothing should be 'kimono', got '{sent['clothing']}'"
    assert sent["weapon"] == "katana", f"weapon should be 'katana', got '{sent['weapon']}'"
    assert sent["accessories"] == "straw hat", f"accessories should be 'straw hat', got '{sent['accessories']}'"
    assert sent["style"] == "retro-8bit", f"style should be 'retro-8bit', got '{sent['style']}'"
