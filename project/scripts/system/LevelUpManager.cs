using Godot;
using System.Collections.Generic;
using Miao.Player;

namespace Miao.System;

/// <summary>
/// 升级管理器
/// 监听玩家升级信号，暂停游戏并显示碎片选择
/// </summary>
public partial class LevelUpManager : Node
{
    /// <summary>升级选项UI场景</summary>
    [Export] public PackedScene LevelUpUIScene;

    private Player.Player _player;
    private UI.LevelUpUI _levelUpUI;
    private List<FragmentConfig> _currentChoices;

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("player") as Player.Player;
        if (_player != null)
        {
            _player.LevelUp += OnPlayerLevelUp;
        }
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        ShowLevelUpUI();
    }

    private void ShowLevelUpUI()
    {
        // 从 FragmentManager 获取 3 个随机碎片
        var fragmentManager = _player?.Fragments;
        if (fragmentManager == null) return;

        _currentChoices = fragmentManager.RollRandomFragments(3);
        if (_currentChoices.Count == 0) return; // 没有可用碎片

        // 暂停游戏
        GetTree().Paused = true;

        // 创建升级UI
        _levelUpUI = LevelUpUIScene.Instantiate<UI.LevelUpUI>();
        _levelUpUI.FragmentSelected += OnFragmentSelected;
        _levelUpUI.SetFragments(_currentChoices);
        CallDeferred(nameof(AddLevelUpUI));
    }

    private void AddLevelUpUI()
    {
        GetTree().CurrentScene.AddChild(_levelUpUI);
    }

    private void OnFragmentSelected(int index)
    {
        if (index >= 0 && index < _currentChoices.Count)
        {
            // 将选中的碎片添加到 FragmentManager
            _player?.Fragments?.AddFragment(_currentChoices[index]);
            GD.Print($"碎片已选择: {_currentChoices[index].Name}");
        }

        // 恢复游戏
        GetTree().Paused = false;

        // 移除UI
        if (_levelUpUI != null)
        {
            _levelUpUI.QueueFree();
            _levelUpUI = null;
        }

        _currentChoices = null;
    }
}
