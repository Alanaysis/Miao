# Destiny-Inspired Looter Shooter — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a full Destiny 2-inspired looter shooter with subclasses, fragments, armor+mods, weapon unlock pool, memory engrams, light level system, and meta shop.

**Architecture:** Extend the existing Godot 4.x C# codebase. Add subclass system, fragment-based upgrades, armor+mod equipment, weapon unlock pool, memory engram decode flow, light level system, and meta shop. Use JSON config files for all game data.

**Tech Stack:** Godot 4.x, C#, .NET, JSON config files

---

## Phase 1: Core Systems (Subclass + Fragments + Weapons) ✅ 基本完成

### Task 1.1: JSON Data Infrastructure ✅
- [x] Create `project/data/` directory structure
- [x] Create `DataLoader.cs` — static class to load/parse JSON configs
- [x] Create initial JSON files: `weapons.json`, `fragments.json`, `subclasses.json`
- [x] Wire DataLoader into GameManager._Ready()

### Task 1.2: Subclass System ✅
- [x] Create `project/scripts/player/SubclassType.cs` — enum (Void/Arc/Solar/Stasis)
- [x] Create `project/scripts/player/SubclassData.cs` — data class for subclass config
- [x] Create `project/scripts/player/SubclassManager.cs` — manages active subclass, skills
- [x] Modify `Player.cs` — add SubclassManager, swap skills based on active subclass
- [x] Modify `Hunter.cs` — implement 3 subclass variants (Void/Arc/Solar) with different skills
- [x] `Titan.cs` — 3 subclass variants (Solar/Arc/Void)
- [x] `Warlock.cs` — 3 subclass variants (Arc/Void/Solar)

### Task 1.3: Fragment System (In-Run Upgrades) ✅
- [x] Create `project/scripts/player/FragmentData.cs` — fragment definition
- [x] Create `project/scripts/player/FragmentManager.cs` — tracks collected fragments
- [x] Modify `LevelUpManager.cs` — replace 5 upgrades with fragment selection
- [x] Modify `LevelUpUI.cs` — show 3 random fragments from active subclass pool
- [ ] Implement fragment effects (stat bonuses work, trigger effects partially wired)

### Task 1.4: Weapon Framework Expansion ✅
- [x] Update `WeaponData.cs` — add 9 new weapon types (total 10: Auto/Pulse/Scout/HandCannon/SMG/Shotgun/Sniper/Fusion/Rocket/Sword)
- [x] Update `Weapon.cs` — implement new weapon behaviors (burst fire, charge, AOE, melee)
- [x] Update `LootTable.cs` — support all weapon types
- [x] Update `PerkSystem.cs` — per-weapon-type perk pools
- [x] Update `Bullet.cs` — support new projectile types (explosive, piercing, burst)

### Task 1.5: Memory Engram System ✅
- [x] Create `project/scripts/pickup/MemoryEngram.cs` — implement IPickable
- [x] Create `project/scripts/system/EngramDecoder.cs` — decode logic (single/batch)
- [x] Modify `Enemy.cs` — add rare engram drop chance
- [x] Modify `RoomGenerator.cs` — boss drops purple/gold engrams on clear
- [x] Create `project/scripts/ui/DecodeUI.cs` — decode interface (single click + batch)

### Task 1.6: Weapon Unlock Pool ✅
- [x] Create `project/scripts/system/WeaponUnlockPool.cs` — tracks unlocked weapons
- [x] Modify `LootTable.cs` — only drop from unlocked pool
- [x] Modify `CollectionCodex.cs` — integrate weapon unlock tracking
- [x] Add map-specific weapon drop weights

### Task 1.7: Settlement Flow ⚠️ 部分完成
- [x] Modify `GameManager.cs` — unified settlement (win/lose), transfer glimmer to MetaProgression
- [x] Modify `GameOverUI.cs` (SettlementUI) — show engrams, decode interface, glimmer earned
- [ ] Wire settlement to MetaProgression.AddGlimmer()
- [ ] Implement "保留/分解" buttons for decoded weapons (currently auto-unlocked)

---

## Phase 2: Equipment Depth (Armor + Mods + Light Level) ⚠️ 部分完成

### Task 2.1: Armor System ⚠️
- [x] Create `project/scripts/armor/ArmorData.cs` — armor definition (slot, light level, mods, stats)
- [x] Create `project/scripts/armor/ArmorSlotManager.cs` — manages equipped armor
- [x] Create `project/scripts/armor/ArmorManager.cs` — manages all 5 armor slots
- [x] Modify `Player.cs` — integrate ArmorManager, apply armor stat bonuses
- [ ] Armor drops in gameplay (never drops from enemies)

### Task 2.2: Mod System ⚠️
- [x] Create `project/scripts/armor/ModData.cs` — mod definition (type, effect, slot restriction)
- [x] Create `project/scripts/armor/ModEffectProcessor.cs` — mod effect aggregation
- [x] Update `mods.json` — define all mods (weapon boost/survival/skill/element/economy/special)
- [ ] Wire mod effects into Player/Weapon/Combat calculations (GetEquippedMods returns empty)
- [ ] Implement mod equipping to armor slots

### Task 2.3: Light Level System ✅
- [x] Create `project/scripts/system/LightLevelCalculator.cs` — compute total light level from all gear
- [x] Modify `Weapon.cs` — weapon has light level, contributes to damage formula
- [x] Modify `ArmorData.cs` — armor has light level
- [x] Modify damage formula to incorporate light level

### Task 2.4: Equipment Screen (Pre-Game) ⚠️
- [x] Create `project/scripts/ui/EquipmentScreen.cs` — full-screen loadout UI
- [x] Show: weapon slot, 5 armor slots, subclass selector, star aspect selector
- [x] Show: total light level, mod slots
- [ ] Implement slot click → selection list popup (TODO stubs)
- [ ] Wire to MainMenu "出战" button

### Task 2.5: In-Game Equipment Screen ✅
- [x] Modify `EquipmentScreen.cs` — add Tab key binding to open during gameplay
- [x] Pause game when open
- [x] Show current loadout status (read-only during gameplay)

---

## Phase 3: Meta & Content (Shop + Maps + Classes) ⚠️ 部分完成

### Task 3.1: Meta Shop ⚠️
- [x] Modify `MetaProgression.cs` — remove old 5 upgrades, add shop categories
- [x] Create `project/scripts/ui/MetaShopUI.cs` — shop interface
- [x] Categories: Subclass unlock, Mod unlock, Light level modules
- [ ] Aspect unlock tab not populated
- [ ] Weapon unlock tab not populated
- [ ] MetaProgression stat bonuses return hardcoded defaults

### Task 3.2: Aspect System ⚠️
- [x] Create `project/scripts/player/AspectData.cs` — aspect definition
- [x] Update `aspects.json` — define aspects per subclass
- [x] Modify `EquipmentScreen.cs` — add aspect selection
- [ ] Wire aspect effects into subclass/fragment system (effects not applied)

### Task 3.3: Map Expansion ⚠️
- [x] Create `project/data/maps.json` — 5 map definitions with themes, enemies, weapon pools
- [x] Modify `RoomGenerator.cs` — load map config, apply theme-specific enemies
- [x] Modify `MapBackground.cs` — support 5 themes
- [ ] Only BugQueen boss implemented; RuinGuardian, CrystalWorm, VoidLord, FlameEmperor missing
- [ ] All maps use same enemies (per-map enemy selection not implemented)

### Task 3.4: Titan Class ✅
- [x] Create `project/scripts/player/Titan.cs` — base stats, skills
- [x] Implement 3 subclasses (Solar/Arc/Void) with unique skills and supers
- [x] Create `project/scenes/player/Titan.tscn`

### Task 3.5: Warlock Class ✅
- [x] Create `project/scripts/player/Warlock.cs` — base stats, skills
- [x] Implement 3 subclasses (Arc/Void/Solar) with unique skills and supers
- [x] Create `project/scenes/player/Warlock.tscn`

### Task 3.6: Content Population ⚠️
- [x] Populate `weapons.json` with all weapon types (white through exotic)
- [x] Populate `fragments.json` with all fragments per subclass
- [x] Populate `mods.json` with all mod types
- [x] Populate `aspects.json` with all aspects per subclass
- [ ] Balance pass on all numbers

---

## Phase 4: Lobby & Multiplayer ⚠️ 基础完成

### Task 4.1: Lobby Scene (Spaceship) ✅
- [x] Create `project/scenes/lobby/Lobby.tscn` — spaceship background with interactive facilities
- [x] Create `project/scripts/lobby/LobbyPlayer.cs` — player avatar in lobby (walk around)
- [x] Create interactive objects: EquipmentMachine, MapSandbox, MetaShop, Vault, FireteamCommunicator
- [x] Wire lobby as the main scene (replaces MainMenu)

### Task 4.2: Interactive Facilities ✅
- [x] Create `project/scripts/lobby/EquipmentMachine.cs` — opens EquipmentScreen
- [x] Create `project/scripts/lobby/MapSandbox.cs` — opens map selection, starts game
- [x] Create `project/scripts/lobby/MetaShopTerminal.cs` — opens MetaShopUI
- [x] Create `project/scripts/lobby/Vault.cs` — opens CollectionCodex UI
- [x] Implement IInteractable interface on all facilities

### Task 4.3: Multiplayer Infrastructure ✅
- [x] Create `project/scripts/network/NetworkManager.cs` — host/client management (ENet)
- [x] Create `project/scripts/network/NetworkSync.cs` — player state sync (position, health, actions)
- [x] Host controls map/room/enemy generation
- [x] Client connects to host IP

### Task 4.4: Multiplayer Game Sync ⚠️
- [x] Sync enemy HP/behavior from host to clients (EnemySync)
- [ ] Sync player actions (shoot/move/skills) to other players (all 6 TODO stubs in PlayerActionSync)
- [ ] Damage calculated by clients, sent to host for aggregation (currently client-side only)
- [ ] Loot calculated independently per player (currently shared drops)
- [x] Players are allies (no friendly fire, self-damage only from explosives)

### Task 4.5: Fireteam Communicator UI ⚠️
- [x] Create `project/scripts/ui/FireteamUI.cs` — multiplayer lobby UI
- [x] Host mode toggle (allow others to join)
- [x] Join game by IP input
- [x] Player list showing connected players
- [ ] Ready check before starting game (ready state not broadcast)

### Task 4.6: Lobby Integration ⚠️
- [x] Replace MainMenu with Lobby scene
- [x] Wire all interactive facilities
- [x] Implement lobby → game → settlement → lobby flow
- [ ] Multiplayer: all players in host's lobby, ready check, then start (untested)

---

## Summary

| Phase | Status | Focus |
|-------|--------|-------|
| 1 | ✅ 基本完成 | Core: Subclass + Fragments + Weapons + Engrams + Unlock Pool |
| 2 | ⚠️ 部分完成 | Depth: Armor + Mods + Light Level + Equipment UI |
| 3 | ⚠️ 部分完成 | Content: Meta Shop + Aspects + Maps + Titan/Warlock |
| 4 | ⚠️ 基础完成 | Lobby & Multiplayer: Spaceship lobby, networking, sync |

Each task produces working, testable code. Tasks within a phase can be executed sequentially.
