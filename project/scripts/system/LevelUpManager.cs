using Godot;
using System.Collections.Generic;

namespace Miao.System;

/// <summary>
/// 升级管理器
/// 监听玩家升级信号，暂停游戏并显示升级选项
/// </summary>
public partial class LevelUpManager : Node
{
    /// <summary>升级选项UI场景</summary>
    [Export] public PackedScene LevelUpUIScene;

    private Player.Player _player;
    private UI.LevelUpUI _levelUpUI;

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
        // 暂停游戏
        GetTree().Paused = true;

        // 创建升级UI
        _levelUpUI = LevelUpUIScene.Instantiate<UI.LevelUpUI>();
        _levelUpUI.UpgradeSelected += OnUpgradeSelected;
        CallDeferred(nameof(AddLevelUpUI));
    }

    private void AddLevelUpUI()
    {
        GetTree().CurrentScene.AddChild(_levelUpUI);
    }

    private void OnUpgradeSelected(string upgradeId)
    {
        // 应用升级效果
        ApplyUpgrade(upgradeId);

        // 恢复游戏
        GetTree().Paused = false;

        // 移除UI
        if (_levelUpUI != null)
        {
            _levelUpUI.QueueFree();
            _levelUpUI = null;
        }
    }

    private void ApplyUpgrade(string upgradeId)
    {
        if (_player == null) return;

        switch (upgradeId)
        {
            case "attack_speed":
                // 攻击速度 +20%（需要遍历所有武器）
                foreach (var child in _player.GetChildren())
                {
                    if (child is Weapon.Weapon weapon)
                    {
                        weapon.AttackSpeedMultiplier *= 1.2f;
                    }
                }
                GD.Print("升级效果：攻击速度 +20%");
                break;

            case "move_speed":
                _player.MoveSpeed *= 1.15f;
                GD.Print("升级效果：移动速度 +15%");
                break;

            case "heal":
                _player.Heal((int)(_player.MaxHealth * 0.3f));
                GD.Print("升级效果：恢复 30% 生命");
                break;

            case "max_health":
                _player.MaxHealth += 20;
                _player.Heal(20);
                GD.Print("升级效果：最大生命 +20");
                break;

            case "new_weapon":
                AddNewWeapon();
                GD.Print("升级效果：获得新武器");
                break;
        }
    }

    private void AddNewWeapon()
    {
        // 如果没有远程武器，添加一个
        bool hasProjectile = false;
        foreach (var child in _player.GetChildren())
        {
            if (child is Weapon.Projectile)
            {
                hasProjectile = true;
                break;
            }
        }

        if (!hasProjectile)
        {
            var projectile = new Weapon.Projectile();
            projectile.Damage = 12;
            projectile.AttackCooldown = 0.8f;
            projectile.BulletSpeed = 400f;
            _player.AddChild(projectile);
        }
        else
        {
            // 已有远程武器，增加伤害
            foreach (var child in _player.GetChildren())
            {
                if (child is Weapon.Weapon weapon)
                {
                    weapon.Damage += 5;
                }
            }
        }
    }
}
