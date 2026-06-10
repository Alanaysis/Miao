# Destiny-Inspired Looter Shooter — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a full Destiny 2-inspired loooter shooter with subclasses, fragments, armor+mods, weapon unlock pool, memory engrams, light level system, and meta shop.

**Architecture:** Extend the existing Godot 4.x C# codebase. Add subclass system, fragment-based upgrades, armor+mod equipment, weapon unlock pool, memory engram decode flow, light level system, and meta shop. Use JSON config files for all game data.

**Tech Stack:** Godot 4.x, C#, .NET, JSON config files

---

## Phase 1: Core Systems (Subclass + Fragments + Weapons)

### Task 1.1: JSON Data Infrastructure
- Create `project/data/` directory structure
- Create `DataLoader.cs` — static class to load/parse JSON configs
- Create initial JSON files: `weapons.json`, `fragments.json`, `subclasses.json`
- Wire DataLoader into GameManager._Ready()

### Task 1.2: Subclass System
- Create `project/scripts/player/SubclassType.cs` — enum (Void/Arc/Solar/Stasis)
- Create `project/scripts/player/SubclassData.cs` — data class for subclass config
- Create `project/scripts/player/SubclassManager.cs` — manages active subclass, skills
- Modify `Player.cs` — add SubclassManager, swap skills based on active subclass
- Modify `Hunter.cs` — implement 3 subclass variants (Void/Arc/Solar) with different skills

### Task 1.3: Fragment System (In-Run Upgrades)
- Create `project/scripts/player/FragmentData.cs` — fragment definition
- Create `project/scripts/player/FragmentManager.cs` — tracks collected fragments
- Modify `LevelUpManager.cs` — replace 5 upgrades with fragment selection
- Modify `LevelUpUI.cs` — show 3 random fragments from active subclass pool
- Implement fragment effects (stat bonuses, skill modifications, trigger effects)

### Task 1.4: Weapon Framework Expansion
- Update `WeaponData.cs` — add 7 new weapon types (Pulse/Scout/SMG/Sniper/Fusion/Rocket/Sword)
- Update `Weapon.cs` — implement new weapon behaviors (burst fire, charge, AOE, melee)
- Update `LootTable.cs` — support all weapon types
- Update `PerkSystem.cs` — per-weapon-type perk pools
- Update `Bullet.cs` — support new projectile types (explosive, piercing, burst)

### Task 1.5: Memory Engram System
- Create `project/scripts/pickup/MemoryEngram.cs` — implement IPickable
- Create `project/scripts/system/EngramDecoder.cs` — decode logic (single/batch)
- Modify `Enemy.cs` — add rare engram drop chance
- Modify `RoomGenerator.cs` — boss drops purple/gold engrams on clear
- Create `project/scripts/ui/DecodeUI.cs` — decode interface (single click + batch)

### Task 1.6: Weapon Unlock Pool
- Create `project/scripts/system/WeaponUnlockPool.cs` — tracks unlocked weapons
- Modify `LootTable.cs` — only drop from unlocked pool
- Modify `CollectionCodex.cs` — integrate weapon unlock tracking
- Add map-specific weapon drop weights

### Task 1.7: Settlement Flow
- Modify `GameManager.cs` — unified settlement (win/lose), transfer glimmer to MetaProgression
- Modify `GameOverUI.cs` (SettlementUI) — show engrams, decode interface, glimmer earned
- Wire settlement to MetaProgression.AddGlimmer()

---

## Phase 2: Equipment Depth (Armor + Mods + Light Level)

### Task 2.1: Armor System
- Create `project/scripts/armor/ArmorData.cs` — armor definition (slot, light level, mods, stats)
- Create `project/scripts/armor/ArmorSlot.cs` — manages equipped armor
- Create `project/scripts/armor/ArmorManager.cs` — manages all 5 armor slots
- Modify `Player.cs` — integrate ArmorManager, apply armor stat bonuses

### Task 2.2: Mod System
- Create `project/scripts/armor/ModData.cs` — mod definition (type, effect, slot restriction)
- Create `project/scripts/armor/ModEffect.cs` — mod effect application logic
- Update `mods.json` — define all mods (weapon boost/survival/skill/element/economy/special)
- Wire mod effects into Player/Weapon/Combat calculations

### Task 2.3: Light Level System
- Create `project/scripts/system/LightLevelCalculator.cs` — compute total light level from all gear
- Modify `Weapon.cs` — weapon has light level, contributes to damage formula
- Modify `ArmorData.cs` — armor has light level
- Modify damage formula to incorporate light level

### Task 2.4: Equipment Screen (Pre-Game)
- Create `project/scripts/ui/EquipmentScreen.cs` — full-screen loadout UI
- Show: weapon slot, 5 armor slots, subclass selector, star aspect selector
- Show: total light level, mod slots
- Implement slot click → selection list popup
- Wire to MainMenu "出战" button

### Task 2.5: In-Game Equipment Screen
- Modify `EquipmentScreen.cs` — add Tab key binding to open during gameplay
- Pause game when open
- Show current loadout status (read-only during gameplay)

---

## Phase 3: Meta & Content (Shop + Maps + Classes)

### Task 3.1: Meta Shop
- Modify `MetaProgression.cs` — remove old 5 upgrades, add shop categories
- Create `project/scripts/ui/MetaShopUI.cs` — shop interface
- Categories: Subclass unlock, Aspect unlock, Mod unlock, Light level modules, Weapon unlock
- Wire to MainMenu "Meta升级" button

### Task 3.2: Aspect System
- Create `project/scripts/player/AspectData.cs` — aspect definition
- Update `aspects.json` — define aspects per subclass
- Modify `EquipmentScreen.cs` — add aspect selection
- Wire aspect effects into subclass/fragment system

### Task 3.3: Map Expansion
- Create `project/data/maps.json` — 5 map definitions with themes, enemies, weapon pools
- Modify `RoomGenerator.cs` — load map config, apply theme-specific enemies
- Modify `MapBackground.cs` — support 5 themes
- Create 5 Boss AI classes (one per map)

### Task 3.4: Titan Class
- Create `project/scripts/player/Titan.cs` — base stats, skills
- Implement 3 subclasses (Solar/Arc/Void) with unique skills and supers
- Create `project/scenes/player/Titan.tscn`
- Add to MainMenu class selection

### Task 3.5: Warlock Class
- Create `project/scripts/player/Warlock.cs` — base stats, skills
- Implement 3 subclasses (Arc/Void/Solar) with unique skills and supers
- Create `project/scenes/player/Warlock.tscn`
- Add to MainMenu class selection

### Task 3.6: Content Population
- Populate `weapons.json` with all weapon types (white through legendary)
- Populate `fragments.json` with all fragments per subclass
- Populate `mods.json` with all mod types
- Populate `aspects.json` with all aspects per subclass
- Balance pass on all numbers

---

## Summary

| Phase | Tasks | Focus |
|-------|-------|-------|
| 1 | 7 tasks | Core: Subclass + Fragments + Weapons + Engrams + Unlock Pool |
| 2 | 5 tasks | Depth: Armor + Mods + Light Level + Equipment UI |
| 3 | 6 tasks | Content: Meta Shop + Aspects + Maps + Titan/Warlock |

Each task produces working, testable code. Tasks within a phase can be executed sequentially.
