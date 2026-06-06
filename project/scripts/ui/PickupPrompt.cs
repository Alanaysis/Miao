using Godot;

namespace Miao.UI;

public partial class PickupPrompt : CanvasLayer
{
    private Label _equipLabel;
    private Label _infuseLabel;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2(640, 500);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;

        _equipLabel = new Label();
        _equipLabel.Text = "[E] 装备";
        _equipLabel.HorizontalAlignment = HorizontalAlignment.Center;

        _infuseLabel = new Label();
        _infuseLabel.Text = "[E] 灌注Perk";
        _infuseLabel.HorizontalAlignment = HorizontalAlignment.Center;

        vbox.AddChild(_equipLabel);
        vbox.AddChild(_infuseLabel);
        AddChild(vbox);

        Hide();
    }

    public void ShowPrompt(bool canInfuse)
    {
        _infuseLabel.Visible = canInfuse;
        Show();
    }

    public new void Hide()
    {
        base.Hide();
    }
}
