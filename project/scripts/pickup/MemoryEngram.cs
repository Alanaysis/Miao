using Godot;
using Miao.Player;
using Miao.System;
using Miao.Weapon;

namespace Miao.Pickup;

/// <summary>
/// 记忆水晶 — 从敌人/Boss 掉落的可收集物品
/// 收集后存储到 GameManager，结算时解码为武器
/// </summary>
public partial class MemoryEngram : Area2D, IPickable
{
    [Export] public Rarity EngramRarity = Rarity.Rare;

    private WeaponData _weaponData;
    private bool _collected = false;

    public void Init(WeaponData weaponData, Rarity rarity)
    {
        _weaponData = weaponData;
        EngramRarity = rarity;
    }

    public override void _Ready()
    {
        CollisionLayer = 4; // Pickup layer
        CollisionMask = 1;  // Player layer

        var shape = new CircleShape2D();
        shape.Radius = 16;
        var collision = new CollisionShape2D();
        collision.Shape = shape;
        AddChild(collision);

        // Visual: colored crystal based on rarity
        var visual = new ColorRect();
        visual.Size = new Vector2(16, 24);
        visual.Position = new Vector2(-8, -12);
        visual.Color = GetRarityColor();
        AddChild(visual);

        // Glow effect
        var glow = new PointLight2D();
        glow.Color = GetRarityColor();
        glow.Energy = 0.5f;
        glow.Texture = GD.Load<Texture2D>("res://icon.svg");
        glow.Scale = new Vector2(0.3f, 0.3f);
        AddChild(glow);

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_collected) return;
        if (body is Player.Player player)
        {
            PickUp(player);
        }
    }

    public void PickUp(Player.Player player)
    {
        if (_collected) return;
        _collected = true;

        // Store engram data in GameManager for settlement
        GameManager.Instance?.CollectEngram(this);
        QueueFree();
    }

    public WeaponData GetWeaponData() => _weaponData;
    public Rarity GetRarity() => EngramRarity;

    private Color GetRarityColor()
    {
        return EngramRarity switch
        {
            Rarity.Common => new Color(0.7f, 0.7f, 0.7f),
            Rarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
            Rarity.Rare => new Color(0.2f, 0.4f, 1.0f),
            Rarity.Epic => new Color(0.6f, 0.2f, 0.8f),
            Rarity.Legendary => new Color(1.0f, 0.8f, 0.0f),
            _ => Colors.White
        };
    }
}
