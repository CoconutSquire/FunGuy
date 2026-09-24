using System;
using System.Collections.Generic;

[Serializable] public sealed class LevelCostDef { public int fromLevel; public int gold; public int spores; }
[Serializable] public sealed class LevelProgressionFile
{
    public int schemaVersion;
    public string rulesVersion;
    public int levelCap;
    public List<LevelCostDef> costs;
}

// Slice economy tuning, separate from the stat sheet's evolution caps and growth rules.
public sealed class LevelProgressionRules
{
    public const string Version = "slice-levels-v2";
    public int LevelCap { get; }
    private readonly Dictionary<int, LevelCostDef> costs = new();
    public LevelProgressionRules(LevelProgressionFile file)
    {
        if (file == null || file.schemaVersion != 2 || file.rulesVersion != Version || file.levelCap != 180 ||
            file.costs == null || file.costs.Count != file.levelCap - 1)
            throw new InvalidOperationException("Invalid level progression catalog.");
        LevelCap = file.levelCap;
        foreach (var cost in file.costs)
        {
            if (cost == null || cost.fromLevel < 1 || cost.fromLevel >= LevelCap || cost.gold < 1 || cost.spores < 0 || costs.ContainsKey(cost.fromLevel))
                throw new InvalidOperationException("Invalid or duplicate level cost.");
            costs.Add(cost.fromLevel, new LevelCostDef { fromLevel = cost.fromLevel, gold = cost.gold, spores = cost.spores });
        }
    }
    public int Cost(int fromLevel) => costs.TryGetValue(fromLevel, out var cost) ? cost.gold :
        throw new ArgumentOutOfRangeException(nameof(fromLevel), "No upgrade at this level.");
    public int SporeCost(int fromLevel) => costs.TryGetValue(fromLevel, out var cost) ? cost.spores :
        throw new ArgumentOutOfRangeException(nameof(fromLevel), "No upgrade at this level.");
}
