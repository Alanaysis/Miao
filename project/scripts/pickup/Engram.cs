using Godot;
using Miao.Player;
using Miao.Pickup;
using Miao.Weapon;

namespace Miao.Pickup;

// Unidentified loot item
public partial class Engram : Area2D, IPickable
{
	WeaponData _decryptedWeapon;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void PickUp(Player.Player player)
	{
	}
}
