using Godot;
using Miao.System;
using Miao.Weapon;
using Miao.Player;

namespace Miao.System;

public partial class Main : Node2D
{
	public override void _Ready()
	{
		var player = GetNode<Hunter>("Hunter");
		GameManager.Instance.RegisterPlayer(player);

		var hud = GetNode<UI.HUD>("HUD");
		hud.SetPlayer(player);

		var roomGen = GetNode<RoomGenerator>("RoomGenerator");
		GameManager.Instance.RegisterRoomGenerator(roomGen);

		var weaponData = LootTable.GenerateWeapon(0, false);
		GameManager.Instance.SpawnWeaponDrop(player.GlobalPosition, weaponData);
	}
}
