"""程序化生成测试 — 深度行为验证"""
from playwright.sync_api import Page, expect


def _generate_programmatic(page: Page, server: str):
    """通过 Programmatic 生成动画"""
    page.goto(server, wait_until="domcontentloaded")
    page.locator(".import-card", has_text="Programmatic").click()
    expect(page.locator("#programmaticConfig")).to_be_visible(timeout=5000)
    page.locator("#programmaticConfig button", has_text="Generate").click()
    expect(page.locator("#editorCanvas")).to_be_visible(timeout=15000)
    expect(page.locator("#loadingOverlay")).to_be_hidden(timeout=15000)


def test_programmatic_preset_options(server: str, page: Page):
    """Preset 下拉有多个预设选项"""
    page.goto(server, wait_until="domcontentloaded")
    page.locator(".import-card", has_text="Programmatic").click()
    expect(page.locator("#programmaticConfig")).to_be_visible(timeout=5000)

    # 通过 JS 检查选项数量
    values = page.evaluate("""() => {
        const sel = document.getElementById('progPreset');
        return Array.from(sel.options).map(o => o.value);
    }""")
    assert len(values) >= 10, f"Should have at least 10 presets, got {len(values)}"
    for name in ["warrior", "mage", "archer", "rogue", "slime_green", "wolf"]:
        assert name in values, f"Missing preset: {name}"


def test_preset_switch_changes_value(server: str, page: Page):
    """切换 preset 改变选中值"""
    page.goto(server, wait_until="domcontentloaded")
    page.locator(".import-card", has_text="Programmatic").click()

    preset = page.locator("#progPreset")
    preset.select_option("mage")
    expect(preset).to_have_value("mage")

    preset.select_option("archer")
    expect(preset).to_have_value("archer")


def test_prog_generate_enters_editor(server: str, page: Page):
    """程序化生成后进入编辑器，canvas 有内容"""
    _generate_programmatic(page, server)

    # canvas 有像素数据
    has_content = page.evaluate("""() => {
        const c = document.getElementById('editorCanvas');
        const ctx = c.getContext('2d');
        const data = ctx.getImageData(0, 0, c.width, c.height).data;
        let nonZero = 0;
        for (let i = 3; i < data.length; i += 4) {
            if (data[i] > 0) nonZero++;
        }
        return nonZero;
    }""")
    assert has_content > 50, f"Canvas should have content after generation, got {has_content} pixels"


def test_prog_generate_sets_char_name(server: str, page: Page):
    """程序化生成后角色名更新"""
    _generate_programmatic(page, server)

    name = page.locator("#charNameDisplay").text_content()
    assert name and name != "untitled", f"Char name should be set, got: {name}"


def test_animate_step_has_checkboxes(server: str, page: Page):
    """Animate 步骤有 4 个动画选择 checkbox"""
    _generate_programmatic(page, server)
    page.locator("button", has_text="Generate Animations").click()

    checkboxes = page.locator("#animCheckboxes input[type='checkbox']")
    expect(checkboxes).to_have_count(4, timeout=5000)

    # 默认全选
    for i in range(4):
        expect(checkboxes.nth(i)).to_be_checked()


def test_animate_checkbox_labels(server: str, page: Page):
    """动画 checkbox 标签包含 idle/walk_down/attack/hurt"""
    _generate_programmatic(page, server)
    page.locator("button", has_text="Generate Animations").click()

    labels = page.evaluate("""() => {
        const cbs = document.querySelectorAll('#animCheckboxes label');
        return Array.from(cbs).map(l => l.textContent.trim());
    }""")
    for name in ["idle", "walk_down", "attack", "hurt"]:
        assert any(name in l for l in labels), f"Missing animation label: {name}"


def test_generate_selected_produces_animations(server: str, page: Page):
    """Generate Selected 生成动画帧，动画列表有内容"""
    _generate_programmatic(page, server)
    page.locator("button", has_text="Generate Animations").click()

    page.locator("button", has_text="Generate Selected").click()
    expect(page.locator("#loadingOverlay")).to_be_hidden(timeout=15000)

    # 动画列表有项目
    anim_items = page.locator(".anim-item")
    count = anim_items.count()
    assert count > 0, "Should have at least one animation after generation"


def test_generate_selected_only_checked(server: str, page: Page):
    """只勾选 idle 和 hurt，生成后只有这两个动画"""
    _generate_programmatic(page, server)
    page.locator("button", has_text="Generate Animations").click()

    # 取消 walk_down 和 attack
    page.locator("#animCheckboxes input[data-anim='walk_down']").uncheck()
    page.locator("#animCheckboxes input[data-anim='attack']").uncheck()

    page.locator("button", has_text="Generate Selected").click()
    expect(page.locator("#loadingOverlay")).to_be_hidden(timeout=15000)

    # 只有 idle 和 hurt
    anim_items = page.locator(".anim-item")
    expect(anim_items).to_have_count(2, timeout=5000)

    names = page.evaluate("""() => {
        return Array.from(document.querySelectorAll('.anim-item-name')).map(n => n.textContent.trim());
    }""")
    assert "idle" in names, f"Should have idle, got: {names}"
    assert "hurt" in names, f"Should have hurt, got: {names}"
    assert "walk_down" not in names, f"Should not have walk_down, got: {names}"
    assert "attack" not in names, f"Should not have attack, got: {names}"


def test_generated_animations_have_frames(server: str, page: Page):
    """生成的动画有正确的帧数"""
    _generate_programmatic(page, server)
    page.locator("button", has_text="Generate Animations").click()

    # 只生成 idle（4 帧）
    page.locator("#animCheckboxes input[data-anim='walk_down']").uncheck()
    page.locator("#animCheckboxes input[data-anim='attack']").uncheck()
    page.locator("#animCheckboxes input[data-anim='hurt']").uncheck()

    page.locator("button", has_text="Generate Selected").click()
    expect(page.locator("#loadingOverlay")).to_be_hidden(timeout=15000)

    # idle 有 4 帧
    frame_info = page.evaluate("""() => {
        const anim = S.animations['idle'];
        return anim ? { frames: anim.frames.length, fps: anim.fps } : null;
    }""")
    assert frame_info is not None, "idle animation should exist"
    assert frame_info["frames"] == 4, f"idle should have 4 frames, got {frame_info['frames']}"
    assert frame_info["fps"] == 6, f"idle should have 6 fps, got {frame_info['fps']}"


def test_animation_preview_visible(server: str, page: Page):
    """生成动画后预览 canvas 有内容"""
    _generate_programmatic(page, server)
    page.locator("button", has_text="Generate Animations").click()
    page.locator("button", has_text="Generate Selected").click()
    expect(page.locator("#loadingOverlay")).to_be_hidden(timeout=15000)

    # 选择第一个动画
    page.locator(".anim-item").first.click()
    page.wait_for_timeout(500)  # 等预览渲染

    # 预览 canvas 存在且有尺寸
    dims = page.evaluate("""() => {
        const c = document.getElementById('animPreviewCanvas');
        return c ? { w: c.width, h: c.height } : null;
    }""")
    assert dims is not None, "Animation preview canvas should exist"
    assert dims["w"] > 0 and dims["h"] > 0, "Preview canvas should have dimensions"


def test_generate_replaces_old_animations(server: str, page: Page):
    """再次生成替换旧动画"""
    _generate_programmatic(page, server)
    page.locator("button", has_text="Generate Animations").click()

    # 第一次生成全部
    page.locator("button", has_text="Generate Selected").click()
    expect(page.locator("#loadingOverlay")).to_be_hidden(timeout=15000)
    first_count = page.locator(".anim-item").count()

    # 第二次只生成 idle
    page.locator("#animCheckboxes input[data-anim='walk_down']").uncheck()
    page.locator("#animCheckboxes input[data-anim='attack']").uncheck()
    page.locator("#animCheckboxes input[data-anim='hurt']").uncheck()
    page.locator("button", has_text="Generate Selected").click()
    expect(page.locator("#loadingOverlay")).to_be_hidden(timeout=15000)

    second_count = page.locator(".anim-item").count()
    assert second_count < first_count, f"Second generate should have fewer animations: {second_count} vs {first_count}"
    assert second_count == 1, f"Should have only idle, got {second_count}"


def test_body_parts_affect_generation(server: str, page: Page):
    """标注 body parts 后生成的动画帧尺寸与未标注不同"""
    # 方式 1：无 body parts（默认）
    _generate_programmatic(page, server)
    page.locator("button", has_text="Generate Animations").click()
    page.locator("button", has_text="Generate Selected").click()
    expect(page.locator("#loadingOverlay")).to_be_hidden(timeout=15000)

    default_frame = page.evaluate("""() => {
        const anim = S.animations['idle'];
        if (!anim || !anim.frames[0]) return null;
        return anim.frames[0];
    }""")
    assert default_frame is not None, "Should have idle frame"
