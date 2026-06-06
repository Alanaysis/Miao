using Godot;
using System.Collections.Generic;

namespace Miao.System;

/// <summary>
/// 局外进度系统
/// 管理金币、解锁内容
/// </summary>
public partial class MetaProgression : Node
{
    public static MetaProgression Instance { get; private set; }

    /// <summary>金币数量</summary>
    public int Gold { get; private set; }

    /// <summary>已解锁的角色ID列表</summary>
    public List<string> UnlockedCharacters { get; private set; } = new() { "warrior" };

    /// <summary>已解锁的武器ID列表</summary>
    public List<string> UnlockedWeapons { get; private set; } = new() { "spin_blade" };

    public override void _Ready()
    {
        Instance = this;
        LoadProgress();
    }

    /// <summary>
    /// 添加金币（游戏结束时调用）
    /// </summary>
    /// <param name="amount">金币数量</param>
    public void AddGold(int amount)
    {
        Gold += amount;
        SaveProgress();
        GD.Print($"获得 {amount} 金币，总计: {Gold}");
    }

    /// <summary>
    /// 解锁角色
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="cost">花费金币</param>
    /// <returns>是否解锁成功</returns>
    public bool UnlockCharacter(string characterId, int cost)
    {
        if (UnlockedCharacters.Contains(characterId)) return false;
        if (Gold < cost) return false;

        Gold -= cost;
        UnlockedCharacters.Add(characterId);
        SaveProgress();
        GD.Print($"解锁角色: {characterId}");
        return true;
    }

    /// <summary>
    /// 解锁武器
    /// </summary>
    /// <param name="weaponId">武器ID</param>
    /// <param name="cost">花费金币</param>
    /// <returns>是否解锁成功</returns>
    public bool UnlockWeapon(string weaponId, int cost)
    {
        if (UnlockedWeapons.Contains(weaponId)) return false;
        if (Gold < cost) return false;

        Gold -= cost;
        UnlockedWeapons.Add(weaponId);
        SaveProgress();
        GD.Print($"解锁武器: {weaponId}");
        return true;
    }

    /// <summary>
    /// 根据游戏表现计算金币奖励
    /// </summary>
    /// <param name="gameTime">存活时间（秒）</param>
    /// <param name="killCount">击杀数</param>
    /// <returns>金币数量</returns>
    public int CalculateGoldReward(float gameTime, int killCount)
    {
        int timeGold = (int)(gameTime / 10); // 每10秒1金币
        int killGold = killCount / 5;         // 每5击杀1金币
        return timeGold + killGold;
    }

    private void SaveProgress()
    {
        var config = new ConfigFile();
        config.SetValue("meta", "gold", Gold);

        // 保存为逗号分隔的字符串
        config.SetValue("meta", "unlocked_characters", string.Join(",", UnlockedCharacters));
        config.SetValue("meta", "unlocked_weapons", string.Join(",", UnlockedWeapons));
        config.Save("user://meta_progress.cfg");
    }

    private void LoadProgress()
    {
        var config = new ConfigFile();
        var error = config.Load("user://meta_progress.cfg");
        if (error != Error.Ok) return;

        Gold = (int)(long)config.GetValue("meta", "gold", 0);

        var charactersStr = (string)config.GetValue("meta", "unlocked_characters", "warrior");
        UnlockedCharacters = new List<string>(charactersStr.Split(','));

        var weaponsStr = (string)config.GetValue("meta", "unlocked_weapons", "spin_blade");
        UnlockedWeapons = new List<string>(weaponsStr.Split(','));
    }
}
