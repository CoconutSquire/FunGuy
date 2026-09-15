using System;
using System.Collections.Generic;
using System.Linq;

public interface ITeamService
{
    int Capacity { get; }
    IReadOnlyList<FormationPlacement> GetFormation();
    bool Place(string characterId, int slot);
    bool SwapSlots(int first, int second);
    bool Add(string characterId);
    bool Remove(string characterId);
    void Replace(IEnumerable<string> characterIds);
    void AutoFill();
    void Clear();
}

// Membership and placement commit together through the detached save boundary.
public sealed class LocalTeamService : ITeamService
{
    public int Capacity => FormationRules.Capacity;
    private readonly GameData data;
    private readonly IPlayerSaveStore store;

    public LocalTeamService(GameData data, IPlayerSaveStore store)
    { this.data = data; this.store = store; }

    public IReadOnlyList<FormationPlacement> GetFormation() => FormationRules.Resolve(store.Read());

    public bool Place(string characterId, int slot)
    {
        FormationRules.ValidateSlot(slot);
        var save = store.Read();
        if (!IsOwned(save, characterId)) return false;
        var positions = FormationRules.Resolve(save);
        var source = positions.FirstOrDefault(p => p.charId == characterId);
        var target = positions.FirstOrDefault(p => p.slot == slot);
        if (source?.slot == slot) return false;
        if (source != null)
        {
            if (target != null) target.slot = source.slot;
            source.slot = slot;
        }
        else
        {
            if (target == null && positions.Count >= Capacity) return false;
            if (target != null) { positions.Remove(target); save.activeTeam.Remove(target.charId); }
            save.activeTeam.Add(characterId);
            positions.Add(new FormationPlacement { charId = characterId, slot = slot });
        }
        save.formation = positions.OrderBy(p => p.slot).ToList();
        store.Write(save);
        return true;
    }

    public bool SwapSlots(int first, int second)
    {
        FormationRules.ValidateSlot(first); FormationRules.ValidateSlot(second);
        if (first == second) return false;
        var save = store.Read();
        var positions = FormationRules.Resolve(save);
        var a = positions.FirstOrDefault(p => p.slot == first);
        var b = positions.FirstOrDefault(p => p.slot == second);
        if (a == null && b == null) return false;
        if (a != null) a.slot = second;
        if (b != null) b.slot = first;
        save.formation = positions.OrderBy(p => p.slot).ToList();
        store.Write(save);
        return true;
    }

    private bool IsOwned(PlayerSave save, string id) =>
        !string.IsNullOrWhiteSpace(id) && data.Characters.ContainsKey(id) &&
        save.units.Any(u => u != null && u.charId == id);

    public bool Add(string characterId)
    {
        var save = store.Read();
        if (!IsOwned(save, characterId) || save.activeTeam.Contains(characterId) ||
            save.activeTeam.Count >= Capacity) return false;
        save.formation = FormationRules.Resolve(save);
        int emptySlot = FormationRules.PlacementOrder.First(s => save.formation.All(p => p.slot != s));
        save.activeTeam.Add(characterId);
        save.formation.Add(new FormationPlacement { charId = characterId, slot = emptySlot });
        store.Write(save);
        return true;
    }

    public bool Remove(string characterId)
    {
        var save = store.Read();
        save.formation = FormationRules.Resolve(save);
        if (!save.activeTeam.Remove(characterId)) return false;
        save.formation.RemoveAll(p => p.charId == characterId);
        store.Write(save);
        return true;
    }

    public void Replace(IEnumerable<string> characterIds)
    {
        if (characterIds == null) throw new ArgumentNullException(nameof(characterIds));
        var ids = characterIds.ToList();
        var save = store.Read();
        if (ids.Count > Capacity || ids.Distinct(StringComparer.Ordinal).Count() != ids.Count ||
            ids.Any(id => !IsOwned(save, id)))
            throw new ArgumentException("Select up to five distinct owned characters.", nameof(characterIds));
        CommitSelection(save, ids);
    }

    public void AutoFill()
    {
        var save = store.Read();
        var ids = save.units.Where(u => u != null && !string.IsNullOrWhiteSpace(u.charId) && data.Characters.ContainsKey(u.charId))
            .OrderByDescending(u => data.Characters[u.charId].rarity)
            .ThenByDescending(u => u.level)
            .ThenBy(u => u.charId, StringComparer.Ordinal)
            .Select(u => u.charId).Distinct(StringComparer.Ordinal).Take(Capacity).ToList();
        CommitSelection(save, ids);
    }

    public void Clear() => Replace(Array.Empty<string>());

    private void CommitSelection(PlayerSave save, List<string> ids)
    {
        if (save.activeTeam.SequenceEqual(ids)) return;
        save.activeTeam = ids;
        save.formation = ids.Select((id, index) => new FormationPlacement { charId = id, slot = FormationRules.PlacementOrder.ElementAt(index) }).ToList();
        store.Write(save);
    }
}
