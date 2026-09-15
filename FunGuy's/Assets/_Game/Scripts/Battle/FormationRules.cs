using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public sealed class FormationPlacement
{
    public string charId;
    public int slot;
}

// Team size is independent of available spaces. Slots are depth * 4 + lane.
public static class FormationRules
{
    // Odd-depth offset coordinates; six neighbors, clipped to the friendly 3 x 4 board.
    public static bool Adjacent(int a, int b)
    {
        if (a < 0 || b < 0 || a >= SlotCount || b >= SlotCount || a == b) return false;
        int aq = Depth(a), bq = Depth(b);
        int ar = Lane(a) - (aq - (aq & 1)) / 2, br = Lane(b) - (bq - (bq & 1)) / 2;
        int dq = aq - bq, dr = ar - br;
        return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2 == 1;
    }
    public const int Capacity = 5;
    public const int Depths = 3;
    public const int Lanes = 4;
    public const int SlotCount = Depths * Lanes;
    private static readonly int[] legacySlots = { 0, 2, 8, 9, 11 };
    public static IEnumerable<int> PlacementOrder => legacySlots.Concat(Enumerable.Range(0, SlotCount).Except(legacySlots));
    public static int MigrateLegacySlot(int slot)
    { if (slot < 0 || slot >= legacySlots.Length) return -1; return legacySlots[slot]; }
    public static void ValidateSlot(int slot)
    { if (slot < 0 || slot >= SlotCount) throw new ArgumentOutOfRangeException(nameof(slot)); }
    public static int Depth(int slot) { ValidateSlot(slot); return slot / Lanes; }
    public static int Lane(int slot) { ValidateSlot(slot); return slot % Lanes; }
    public static bool IsFront(int slot) => Depth(slot) == 0;
    public static string Label(int slot) => $"{new[] { "Front", "Middle", "Rear" }[Depth(slot)]} {Lane(slot) + 1}";
    public static string SlotId(int slot) => $"{new[] { "front", "middle", "rear" }[Depth(slot)]}_{Lane(slot) + 1}";
    public static int ParseSlot(string id)
    {
        string[] oldIds = { "front_left", "front_right", "back_left", "back_center", "back_right" };
        int old = Array.IndexOf(oldIds, id);
        if (old >= 0) return MigrateLegacySlot(old);
        for (int slot = 0; slot < SlotCount; slot++) if (SlotId(slot) == id) return slot;
        throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown formation space.");
    }

    // Recover legacy membership without moving valid existing assignments. Empty slots stay empty.
    public static List<FormationPlacement> Resolve(PlayerSave save)
    {
        var members = (save.activeTeam ?? new List<string>()).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().Take(Capacity).ToList();
        var result = new List<FormationPlacement>();
        foreach (var p in save.formation ?? new List<FormationPlacement>())
            if (p != null && p.slot >= 0 && p.slot < SlotCount && members.Contains(p.charId) &&
                !result.Any(x => x.slot == p.slot || x.charId == p.charId))
                result.Add(new FormationPlacement { charId = p.charId, slot = p.slot });
        foreach (var id in members.Where(id => !result.Any(p => p.charId == id)))
            result.Add(new FormationPlacement { charId = id, slot = PlacementOrder.First(s => result.All(p => p.slot != s)) });
        return result.OrderBy(p => p.slot).ToList();
    }

    public static void AssignBattleSlots(List<CombatUnit> units)
    {
        if (units == null || units.Count > Capacity || units.Any(u => u == null)) throw new ArgumentException("A battle side supports up to five units.");
        var occupied = new HashSet<int>();
        foreach (var unit in units.Where(u => u.formationSlot != -1))
        {
            ValidateSlot(unit.formationSlot);
            if (!occupied.Add(unit.formationSlot)) throw new ArgumentException("Duplicate battle formation slot.");
        }
        foreach (var unit in units.Where(u => u.formationSlot == -1))
        {
            unit.formationSlot = PlacementOrder.First(s => !occupied.Contains(s));
            occupied.Add(unit.formationSlot);
        }
    }

    public static List<CombatUnit> LivingRow(List<CombatUnit> units, bool front, bool fallback)
    {
        var alive = units.Where(u => u.hp > 0).OrderBy(u => u.formationSlot).ToList();
        int depth = front ? 0 : Depths - 1;
        var row = alive.Where(u => Depth(u.formationSlot) == depth).ToList();
        if (row.Count > 0 || !fallback || alive.Count == 0) return row;
        int occupiedDepth = front ? alive.Min(u => Depth(u.formationSlot)) : alive.Max(u => Depth(u.formationSlot));
        return alive.Where(u => Depth(u.formationSlot) == occupiedDepth).ToList();
    }
    public static List<CombatUnit> LivingSameRow(List<CombatUnit> units, int slot) =>
        units.Where(u => u.hp > 0 && Depth(u.formationSlot) == Depth(slot)).OrderBy(u => u.formationSlot).ToList();
}
