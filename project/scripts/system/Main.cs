using Godot;
using Miao.System;
using Miao.Weapon;
using Miao.Player;
using Miao.UI;

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

		// 装备界面（游戏内按 Tab 打开）
		var equipLayer = new CanvasLayer();
		equipLayer.Layer = 15;
		equipLayer.ProcessMode = Node.ProcessModeEnum.Always;
		AddChild(equipLayer);

		var equipScreen = new EquipmentScreen();
		equipLayer.AddChild(equipScreen);

		player.SetEquipmentScreen(equipScreen);

		// 开局直接装备武器（而非掉落拾取，避免重叠导致拾取失败）
		var weaponData = LootTable.GenerateWeapon(0, false);
		player.Equipment.EquipWeapon(weaponData);
	}
}
