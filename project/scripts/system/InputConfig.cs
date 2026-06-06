using Godot;

namespace Miao.System;

/// <summary>
/// 操作方案保存到配置文件，以便后续修改
/// </summary>
public static class InputConfig
{
    private const string ConfigPath = "user://input_config.cfg";

    public static void SaveDefaults()
    {
        var config = new ConfigFile();
        config.SetValue("controls", "move_up", "W");
        config.SetValue("controls", "move_down", "S");
        config.SetValue("controls", "move_left", "A");
        config.SetValue("controls", "move_right", "D");
        config.SetValue("controls", "shoot", "MouseLeft");
        config.SetValue("controls", "skill_1", "Q");
        config.SetValue("controls", "skill_2", "R");
        config.SetValue("controls", "super_ability", "F");
        config.SetValue("controls", "interact", "E");
        config.Save(ConfigPath);
    }

    public static void Load()
    {
        var config = new ConfigFile();
        if (config.Load(ConfigPath) != Error.Ok)
        {
            SaveDefaults();
            return;
        }
        // 未来：读取配置并重新映射 InputMap
        GD.PushWarning("InputConfig: Load() 未实现，使用默认键位");
    }
}
