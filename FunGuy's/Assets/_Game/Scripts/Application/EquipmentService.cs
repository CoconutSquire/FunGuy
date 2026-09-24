using System;
using System.Collections.Generic;
using System.Linq;

public sealed class EquipmentService
{
    public const int MaxLevel = 20;
    public const int EvolutionLevel = 10;
    private readonly GameBalanceSettings settings;
    public EquipmentService(GameBalanceSettings settings = null) { this.settings = settings; }
    public static readonly string[] Slots = { "cap", "stipe", "mycelium", "symbiote" };
    private readonly Random rng = new();

    public EquipmentDef Definition(string id) => settings?.GetEquipment(id) ?? id switch {
        "cap" => new EquipmentDef { id="cap", name="Cap", slotId="cap", acquisition="Campaign rewards", hp=250, pot=20 },
        "stipe" => new EquipmentDef { id="stipe", name="Stipe", slotId="stipe", acquisition="Boss rewards", hp=300, def=35 },
        "mycelium" => new EquipmentDef { id="mycelium", name="Mycelium", slotId="mycelium", acquisition="Crafting / exploration", spd=25, critChance=.05f },
        "symbiote" => new EquipmentDef { id="symbiote", name="Symbiote", slotId="symbiote", acquisition="Rare drops", randomStat=true, uniqueTrait=true },
        _ => null
    };

    public GearSlotState Acquire(string equipmentId, int rarity = 1, int level = 1) {
        var def = Definition(equipmentId) ?? throw new ArgumentException("Unknown equipment: " + equipmentId);
        var item = new GearSlotState {
            slotId=def.slotId,
            itemId=Guid.NewGuid().ToString("N"),
            rarity=Math.Clamp(rarity,1,6),
            level=Math.Clamp(level,1,MaxLevel),
            evolution=0
        };
        if (def.randomStat) item.rolledStat = new[] { "HP", "ATK", "DEF", "SPD", "POT" }[rng.Next(5)];
        if (def.randomStat) item.rolledStatAmount = rng.Next(settings?.symbioteRandomStatMin ?? 10, (settings?.symbioteRandomStatMax ?? 30) + 1) * item.rarity;
        if (def.uniqueTrait) {
            item.uniqueTrait = new[] { "Healing", "Shield", "Burn" }[rng.Next(3)];
            item.traitAmount = .05f * item.rarity;
        }
        return item;
    }

    public int LevelCost(int fromLevel) => Math.Max(0, 25 + 10 * (Math.Max(1, fromLevel) - 1));
    public int EvolutionSporeCost => settings?.equipmentEvolutionSporeCost ?? 100;
    public bool IsAtEvolutionGate(GearSlotState item) => item != null && item.evolution < 1 && item.level >= EvolutionLevel;
    public bool CanLevelUp(GearSlotState item, PlayerSave save = null) {
        if (item == null || item.level >= MaxLevel) return false;
        if (item.evolution < 1 && item.level >= EvolutionLevel) return false;
        save ??= Game.Save ?? SaveSystem.LoadOrNew();
        return save.gold >= LevelCost(item.level);
    }
    public bool CanEvolve(GearSlotState item, PlayerSave save = null) {
        if (item == null || item.evolution >= 1 || item.level < EvolutionLevel) return false;
        save ??= Game.Save ?? SaveSystem.LoadOrNew();
        return save.spores >= EvolutionSporeCost;
    }

    public bool LevelUp(string itemInstanceId) {
        var save = Game.Save ?? SaveSystem.LoadOrNew();
        var item = FindOwned(save, itemInstanceId);
        if (!CanLevelUp(item, save)) return false;
        save.gold -= LevelCost(item.level);
        item.level++;
        SaveSystem.Save(save); Game.Save = save;
        return true;
    }

    public bool Evolve(string itemInstanceId) {
        var save = Game.Save ?? SaveSystem.LoadOrNew();
        var item = FindOwned(save, itemInstanceId);
        if (!CanEvolve(item, save)) return false;
        save.spores -= EvolutionSporeCost;
        item.evolution = 1;
        SaveSystem.Save(save); Game.Save = save;
        return true;
    }

    private GearSlotState FindOwned(PlayerSave save, string itemInstanceId) {
        if (save == null || string.IsNullOrWhiteSpace(itemInstanceId)) return null;
        return (save.equipmentInventory ?? new List<GearSlotState>()).FirstOrDefault(i => i.itemId == itemInstanceId)
            ?? (save.units ?? new List<OwnedUnit>()).SelectMany(u => u.gearSlots ?? new List<GearSlotState>()).FirstOrDefault(i => i.itemId == itemInstanceId);
    }

    public bool HasType(string slotId, PlayerSave save = null) { save ??= Game.Save ?? SaveSystem.LoadOrNew(); return (save.equipmentInventory ?? new List<GearSlotState>()).Any(x => x.slotId == slotId) || (save.units ?? new List<OwnedUnit>()).Any(u => (u.gearSlots ?? new List<GearSlotState>()).Any(x => x.slotId == slotId)); }

    public void GrantIfMissing(string slotId, int rarity = 1, int level = 1) { if (!HasType(slotId)) AddToInventory(Acquire(slotId, rarity, level)); }

    public bool Equip(string charId, string itemInstanceId) {
        var save = Game.Save ?? SaveSystem.LoadOrNew();
        var unit = save.units.FirstOrDefault(u => u.charId == charId);
        var item = save.equipmentInventory.FirstOrDefault(i => i.itemId == itemInstanceId);
        if (unit == null || item == null || string.IsNullOrWhiteSpace(item.slotId)) return false;
        unit.gearSlots ??= new List<GearSlotState>();
        var old = unit.gearSlots.FirstOrDefault(i => i.slotId == item.slotId);
        if (old != null) { unit.gearSlots.Remove(old); save.equipmentInventory.Add(old); }
        unit.gearSlots.RemoveAll(i => i.itemId == item.itemId);
        unit.gearSlots.Add(item); save.equipmentInventory.RemoveAll(i => i.itemId == item.itemId);
        SaveSystem.Save(save); Game.Save = save; return true;
    }

    public bool Unequip(string charId, string slotId) {
        var save = Game.Save ?? SaveSystem.LoadOrNew();
        var unit = save.units.FirstOrDefault(u => u.charId == charId); if (unit == null) return false;
        var item = unit.gearSlots?.FirstOrDefault(i => i.slotId == slotId); if (item == null) return false;
        unit.gearSlots.Remove(item); save.equipmentInventory.Add(item); SaveSystem.Save(save); Game.Save = save; return true;
    }

    public void AddToSave(PlayerSave save, GearSlotState item) { if (save == null || item == null) return; save.equipmentInventory ??= new List<GearSlotState>(); save.equipmentInventory.Add(item); }

    public void AddToInventory(GearSlotState item) { if (item == null) return; var save=Game.Save ?? SaveSystem.LoadOrNew(); AddToSave(save, item); SaveSystem.Save(save); Game.Save=save; }

    public static void ApplyTo(CombatUnit fighter, OwnedUnit owned) {
        if (fighter == null || owned?.gearSlots == null) return;
        var settings = UnityEngine.Resources.Load<GameBalanceSettings>("GameData/GameBalanceSettings");
        foreach (var item in owned.gearSlots) {
            if (item == null) continue;
            float scale =
                (1f + (settings?.equipmentLevelStatGrowth ?? .1f) * (Math.Max(1, item.level) - 1)) *
                (1f + (settings?.equipmentRarityStatGrowth ?? .2f) * (Math.Max(1, item.rarity) - 1)) *
                (1f + (settings?.equipmentEvolutionStatGrowth ?? .25f) * Math.Max(0, item.evolution));
            switch (item.slotId) {
                case "cap": fighter.maxHp += (int)Math.Round(250 * scale); fighter.hp += (int)Math.Round(250 * scale); fighter.pot += (int)Math.Round(20 * scale); break;
                case "stipe": fighter.maxHp += (int)Math.Round(300 * scale); fighter.hp += (int)Math.Round(300 * scale); fighter.def += (int)Math.Round(35 * scale); break;
                case "mycelium": fighter.spd += (int)Math.Round(25 * scale); fighter.critChance += .05f * Math.Max(1, item.rarity); break;
                case "symbiote": ApplySymbiote(fighter, item, settings); break;
            }
        }
    }

    private static void ApplySymbiote(CombatUnit fighter, GearSlotState item, GameBalanceSettings settings) {
        int amount = (int)Math.Round(item.rolledStatAmount *
            (1f + (settings?.equipmentLevelStatGrowth ?? .1f) * (Math.Max(1, item.level) - 1)) *
            (1f + (settings?.equipmentRarityStatGrowth ?? .2f) * (Math.Max(1, item.rarity) - 1)) *
            (1f + (settings?.equipmentEvolutionStatGrowth ?? .25f) * Math.Max(0, item.evolution)));
        switch (item.rolledStat) { case "HP": fighter.maxHp += amount; fighter.hp += amount; break; case "ATK": fighter.atk += amount; break; case "DEF": fighter.def += amount; break; case "SPD": fighter.spd += amount; break; case "POT": fighter.pot += amount; break; }
        switch (item.uniqueTrait) { case "Healing": fighter.healingBonus += item.traitAmount; break; case "Shield": fighter.shieldBonus += item.traitAmount; break; case "Burn": fighter.burnChanceBonus += item.traitAmount; break; }
    }
}
