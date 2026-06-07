using Godot;
using Miao.System;

namespace Miao.UI;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		var vbox = new VBoxContainer();
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.Position = new Vector2(540, 200);

		var title = new Label();
		title.Text = "MIAO";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		var subtitle = new Label();
		subtitle.Text = "命运·像素";
		subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(subtitle);

		var classLabel = new Label();
		classLabel.Text = "\n选择职业：";
		vbox.AddChild(classLabel);

		var hunterBtn = new Button();
		hunterBtn.Text = "猎人（敏捷远程）";
		hunterBtn.Pressed += () => StartWithClass("hunter");
		vbox.AddChild(hunterBtn);

		var titanBtn = new Button();
		titanBtn.Text = "泰坦（近战坦克）";
		titanBtn.Disabled = true;
		vbox.AddChild(titanBtn);

		var warlockBtn = new Button();
		warlockBtn.Text = "术士（AOE法师）";
		warlockBtn.Disabled = true;
		vbox.AddChild(warlockBtn);

		var metaBtn = new Button();
		metaBtn.Text = "Meta 升级";
		metaBtn.Pressed += () => { /* TODO: 打开 Meta 升级界面 */ };
		vbox.AddChild(metaBtn);

		var codexBtn = new Button();
		codexBtn.Text = "收藏图鉴";
		codexBtn.Pressed += () => { /* TODO: 打开图鉴界面 */ };
		vbox.AddChild(codexBtn);

		var quitBtn = new Button();
		quitBtn.Text = "退出";
		quitBtn.Pressed += () => GetTree().Quit();
		vbox.AddChild(quitBtn);

		AddChild(vbox);
	}

	private void StartWithClass(string classId)
	{
		GameManager.Instance.StartGame();
	}
}
