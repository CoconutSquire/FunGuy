using System;
using System.Linq;

public interface IUpgradeService
{
    UnitUpgradePreview Preview(string characterId);
    UnitUpgradePreview LevelUp(string characterId, int expectedLevel, string rulesVersion);
}

public sealed class UnitStatPreview
{
    public int HP { get; }
    public int ATK { get; }
    public int DEF { get; }
    public int SPD { get; }
    public int POT { get; }
    internal UnitStatPreview(CombatUnit unit)
    { HP = unit.maxHp; ATK = unit.atk; DEF = unit.def; SPD = unit.spd; POT = unit.pot; }
}

// Detached read model; the client never supplies the price or resulting stats.
public sealed class UnitUpgradePreview
{
    public string CharacterId { get; }
    public string RulesVersion { get; }
    public int Level { get; }
    public int LevelCap { get; }
    public int EvolutionStars { get; }
    public int Gold { get; }
    public int GoldCost { get; }
    public int Spores { get; }
    public int SporeCost { get; }
    public bool RequiresAscension => SporeCost > 0;
    public bool AtCap => Level >= LevelCap;
    public bool CanAfford => !AtCap && Gold >= GoldCost && Spores >= SporeCost;
    public UnitStatPreview Current { get; }
    public UnitStatPreview Next { get; }
    internal UnitUpgradePreview(OwnedUnit unit, int cap, int gold, int cost, string version,
        UnitStatPreview current, UnitStatPreview next)
    {
        CharacterId = unit.charId; Level = unit.level; EvolutionStars = unit.stars; LevelCap = cap;
        Gold = gold; GoldCost = cost; Spores = spores; SporeCost = sporeCost; RulesVersion = version; Current = current; Next = next;
    }
}

// One synchronous local transaction. A server adapter must own the same calculation and commit.
public sealed class LocalUpgradeService : IUpgradeService
{
    private readonly GameData data;
    private readonly IPlayerSaveStore store;
    public LocalUpgradeService(GameData data, IPlayerSaveStore store)
    { this.data = data ?? throw new ArgumentNullException(nameof(data)); this.store = store ?? throw new ArgumentNullException(nameof(store)); }

    public UnitUpgradePreview Preview(string characterId) => Preview(store.Read(), characterId);

    public UnitUpgradePreview LevelUp(string characterId, int expectedLevel, string rulesVersion)
    {
        var save = store.Read();
        var preview = Preview(save, characterId);
        if (preview.Level != expectedLevel || preview.RulesVersion != rulesVersion)
            throw new InvalidOperationException("This upgrade has changed. Review the refreshed preview.");
        if (preview.AtCap) throw new InvalidOperationException("This fighter has reached the current level cap.");
        if (!preview.CanAfford) throw new InvalidOperationException($"You need {preview.GoldCost} gold and {preview.SporeCost} spores for this level.");
        var owned = save.units.Single(u => u.charId == characterId);
        save.gold = checked(save.gold - preview.GoldCost);
        save.spores = checked(save.spores - preview.SporeCost);
        owned.level = checked(owned.level + 1);
        var result = Preview(save, characterId); // Validate the complete result before publishing it.
        store.Write(save);
        return result;
    }

    private UnitUpgradePreview Preview(PlayerSave save, string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !data.Characters.TryGetValue(id, out var definition))
            throw new InvalidOperationException("This fighter is unavailable.");
        var matches = save.units.Where(u => u != null && u.charId == id).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException("Select one owned fighter.");
        var owned = matches[0];
        if (owned.level < 1 || owned.stars < 1 || owned.stars > 6 || save.gold < 0)
            throw new InvalidOperationException("This fighter's progression could not be loaded.");
        int cap = Math.Min(data.LevelRules.LevelCap, data.StatRules.LevelCap(owned.stars));
        UnitStatPreview Stats(int level) => new(CombatUnitFactory.Create(definition, level, TeamSide.Player, owned.stars, data.StatRules));
        var current = Stats(owned.level);
        // Older saves above the slice cap retain their level; the workshop does not downgrade them.
        return new UnitUpgradePreview(owned, cap, save.gold, owned.level < cap ? data.LevelRules.Cost(owned.level) : 0,
            save.spores, owned.level < cap ? data.LevelRules.SporeCost(owned.level) : 0, LevelProgressionRules.Version, current, owned.level < cap ? Stats(owned.level + 1) : null);
    }
}
