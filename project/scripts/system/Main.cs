using Godot;
using System.Collections.Generic;
using Miao.System;
using Miao.Weapon;
using Miao.UI;
using PlayerClass = Miao.Player.Player;

namespace Miao.System;

public partial class Main : Node2D
{
	private static readonly Dictionary<string, string> ClassScenes = new()
	{
		{ "hunter", "res://scenes/player/Hunter.tscn" },
		{ "titan", "res://scenes/player/Titan.tscn" },
	};

	public override void _Ready()
	{
		string classId = GameManager.Instance?.SelectedClassId ?? "hunter";
		string scenePath = ClassScenes.GetValueOrDefault(classId, ClassScenes["hunter"]);

		// Remove the statically defined Hunter node if it exists
		var existingPlayer = GetNodeOrNull<Node2D>("Hunter");
		if (existingPlayer != null)
		{
			existingPlayer.QueueFree();
		}

		// Instantiate the correct player scene
		var playerScene = GD.Load<PackedScene>(scenePath);
		var player = playerScene.Instantiate<PlayerClass>();
		player.Name = classId == "hunter" ? "Hunter" : "Titan";
		player.SetMultiplayerAuthority(1);
		player.Position = new Vector2(640, 360);
		AddChild(player);

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

		// Apply subclass for the selected class
		player.Subclass?.SetSubclassForClass(classId, player.Subclass.ActiveSubclass);

		// 开局直接装备武器（而非掉落拾取，避免重叠导致拾取失败）
		var weaponData = LootTable.GenerateWeapon(0, false);
		player.Equipment.EquipWeapon(weaponData);
	}
}
