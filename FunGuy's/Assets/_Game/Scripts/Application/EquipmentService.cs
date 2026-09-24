using System;
using System.Collections.Generic;
using System.Linq;

public sealed class EquipmentService
{
    public static readonly string[] Slots = { "cap", "stipe", "mycelium", "symbiote" };
    private readonly Random rng = new();

    public EquipmentDef Definition(string id) => id switch {
        "cap" => new EquipmentDef { id="cap", name="Cap", slotId="cap", acquisition="Campaign rewards", hp=250, pot=20 },
        "stipe" => new EquipmentDef { id="stipe", name="Stipe", slotId="stipe", acquisition="Boss rewards", hp=300, def=35 },
        "mycelium" => new EquipmentDef { id="mycelium", name="Mycelium", slotId="mycelium", acquisition="Crafting / exploration", spd=25, critChance=.05f },
        "symbiote" => new EquipmentDef { id="symbiote", name="Symbiote", slotId="symbiote", acquisition="Rare drops", randomStat=true, uniqueTrait=true },
        _ => null
    };

    public GearSlotState Acquire(string equipmentId, int rarity = 1, int level = 1) {
        var def = Definition(equipmentId) ?? throw new ArgumentException("Unknown equipment: " + equipmentId);
        var item = new GearSlotState { slotId=def.slotId, itemId=Guid.NewGuid().ToString("N"), rarity=Math.Clamp(rarity,1,6), level=Math.Clamp(level,1,20) };
        if (def.randomStat) item.rolledStat = new[] { "HP", "ATK", "DEF", "SPD", "POT" }[rng.Next(5)];
        if (def.randomStat) item.rolledStatAmount = rng.Next(10, 31) * item.rarity;
        if (def.uniqueTrait) {
            item.uniqueTrait = new[] { "Healing", "Shield", "Burn" }[rng.Next(3)];
            item.traitAmount = item.uniqueTrait == "Burn" ? .05f * item.rarity : .05f * item.rarity;
        }
        return item;
    }

    public bool HasType(string slotId, PlayerSave save = null) { save ??= Game.Save ?? SaveSystem.LoadOrNew(); return (save.equipmentInventory ?? new List<GearSlotState>()).Any(x => x.slotId == slotId) || (save.units ?? new List<OwnedUnit>()).Any(u => (u.gearSlots ?? new List<GearSlotState>()).Any(x => x.slotId == slotId)); }\n\n    public void GrantIfMissing(string slotId, int rarity = 1, int level = 1) { if (!HasType(slotId)) AddToInventory(Acquire(slotId, rarity, level)); }\n\n    public bool Equip(string charId, string itemInstanceId) {
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

    public void AddToSave(PlayerSave save, GearSlotState item) { if (save == null || item == null) return; save.equipmentInventory ??= new List<GearSlotState>(); save.equipmentInventory.Add(item); }\n\n    public void AddToInventory(GearSlotState item) { if (item == null) return; var save=Game.Save ?? SaveSystem.LoadOrNew(); AddToSave(save, item); SaveSystem.Save(save); Game.Save=save; }

    public static void ApplyTo(CombatUnit fighter, OwnedUnit owned) {
        if (fighter == null || owned?.gearSlots == null) return;
        foreach (var item in owned.gearSlots) {
            if (item == null) continue;
            float scale = (1f + .1f * (Math.Max(1, item.level) - 1)) * (1f + .2f * (Math.Max(1, item.rarity) - 1));
            switch (item.slotId) {
                case "cap": fighter.maxHp += (int)Math.Round(250 * scale); fighter.hp += (int)Math.Round(250 * scale); fighter.pot += (int)Math.Round(20 * scale); break;
                case "stipe": fighter.maxHp += (int)Math.Round(300 * scale); fighter.hp += (int)Math.Round(300 * scale); fighter.def += (int)Math.Round(35 * scale); break;
                case "mycelium": fighter.spd += (int)Math.Round(25 * scale); fighter.critChance += .05f * Math.Max(1, item.rarity); break;
                case "symbiote": ApplySymbiote(fighter, item); break;
            }
        }
    }

    private static void ApplySymbiote(CombatUnit fighter, GearSlotState item) {
        int amount = (int)Math.Round(item.rolledStatAmount * (1f + .1f * (Math.Max(1, item.level) - 1)));
        switch (item.rolledStat) { case "HP": fighter.maxHp += amount; fighter.hp += amount; break; case "ATK": fighter.atk += amount; break; case "DEF": fighter.def += amount; break; case "SPD": fighter.spd += amount; break; case "POT": fighter.pot += amount; break; }
        switch (item.uniqueTrait) { case "Healing": fighter.healingBonus += item.traitAmount; break; case "Shield": fighter.shieldBonus += item.traitAmount; break; case "Burn": fighter.burnChanceBonus += item.traitAmount; break; }
    }
}
