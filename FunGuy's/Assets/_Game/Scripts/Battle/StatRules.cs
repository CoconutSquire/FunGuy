using System;
using System.Collections.Generic;
using System.Linq;

[Serializable] public sealed class ClassStatRule
{
    public string id;
    public StatBlock baseStats;
    public StatGrowth growth; // SPD is retained as source evidence; v1 uses the page 13 replacement.
}

[Serializable] public sealed class CharacterStatRule
{
    public string id;
    public string name;
    public string rarityTier;
    public string biome;
    public string classArchetype;
    public string role;
    public int bst;
    public StatBlock baseStats;
}

[Serializable] public sealed class EvolutionStatRule
{
    public int stars;
    public int levelCap;
    public float multiplier;
}

[Serializable] public sealed class StatRulesFile
{
    public int schemaVersion;
    public string rulesVersion;
    public List<ClassStatRule> classes;
    public List<CharacterStatRule> characters;
    public List<EvolutionStatRule> evolution;
}

// Plain C# domain catalog. Copies inputs so callers cannot mutate a running rules version.
public sealed class StatRulesCatalog
{
    public const string Version = "stat-sheet-v1";
    private readonly Dictionary<string, ClassStatRule> classes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CharacterStatRule> characters = new(StringComparer.Ordinal);
    private readonly Dictionary<int, EvolutionStatRule> evolution = new();

    public StatRulesCatalog(StatRulesFile file)
    {
        Require(file != null && file.schemaVersion == 1 && file.rulesVersion == Version, "Unsupported stat rules version.");
        var expected = new HashSet<string> { "Tank", "DPS", "Mage", "Support", "Tactician", "Assassin" };
        Require(file.classes != null && file.classes.Count == 6, "Six class rules required.");
        foreach (var c in file.classes)
        {
            Require(c != null && expected.Remove(c.id), "Unknown or duplicate class.");
            ValidateStats(c.baseStats);
            Require(c.growth != null && new[] { c.growth.hp, c.growth.atk, c.growth.def, c.growth.spd, c.growth.pot }
                .All(x => !float.IsNaN(x) && !float.IsInfinity(x) && x >= 0 && x <= 100000), "Invalid class growth.");
            classes.Add(c.id, new ClassStatRule { id = c.id, baseStats = Copy(c.baseStats), growth = new StatGrowth
                { hp = c.growth.hp, atk = c.growth.atk, def = c.growth.def, spd = c.growth.spd, pot = c.growth.pot } });
        }
        Require(file.evolution != null && file.evolution.Count == 6, "Six evolution rules required.");
        decimal[] multipliers = { 1m, 1.2m, 1.5m, 2m, 2.5m, 3.5m };
        foreach (var e in file.evolution)
        {
            Require(e != null && e.stars >= 1 && e.stars <= 6 && !evolution.ContainsKey(e.stars), "Invalid evolution stars.");
            Require(!float.IsNaN(e.multiplier) && !float.IsInfinity(e.multiplier) &&
                (decimal)e.multiplier == multipliers[e.stars - 1] && e.levelCap == 80 + e.stars * 20, "Invalid v1 evolution curve.");
            evolution.Add(e.stars, new EvolutionStatRule { stars = e.stars, levelCap = e.levelCap, multiplier = e.multiplier });
        }
        Require(file.characters != null && file.characters.Count > 0, "Character stat references required.");
        foreach (var c in file.characters)
        {
            Require(c != null && !string.IsNullOrWhiteSpace(c.id) && !characters.ContainsKey(c.id), "Missing or duplicate stat profile ID.");
            Require(!string.IsNullOrWhiteSpace(c.name) && !string.IsNullOrWhiteSpace(c.role) && classes.ContainsKey(c.classArchetype ?? ""), "Invalid character identity/class.");
            Require(c.rarityTier == "R" || c.rarityTier == "SR" || c.rarityTier == "UR", "Invalid acquisition rarity.");
            BiomeRules.Parse(c.biome);
            ValidateStats(c.baseStats);
            Require(c.bst == Sum(c.baseStats), "Character BST does not equal its five base stats.");
            characters.Add(c.id, Copy(c));
        }
    }

    public CharacterStatRule Character(string id)
    {
        if (id == null || !characters.TryGetValue(id, out var value)) throw new ArgumentException("Unknown stat profile: " + id);
        return Copy(value);
    }

    public StatBlock CalculateCharacter(string id, int level, int stars) => Calculate(Character(id).baseStats, characters[id].classArchetype, level, stars);
    public StatBlock CalculateAuthored(StatBlock stats, string classId, int level, int stars)
    {
        ValidateStats(stats);
        if (classId == null || !classes.ContainsKey(classId)) throw new ArgumentException("Unknown class: " + classId);
        return Calculate(stats, classId, level, stars);
    }
    public StatBlock CalculateClass(string classId, int level, int stars)
    {
        if (classId == null || !classes.TryGetValue(classId, out var c)) throw new ArgumentException("Unknown class: " + classId);
        return Calculate(c.baseStats, classId, level, stars);
    }

    public int LevelCap(int stars)
    {
        if (!evolution.TryGetValue(stars, out var rule)) throw new ArgumentOutOfRangeException(nameof(stars));
        return rule.levelCap;
    }

    private StatBlock Calculate(StatBlock b, string classId, int level, int stars)
    {
        if (level < 1 || level > LevelCap(stars)) throw new ArgumentOutOfRangeException(nameof(level), "Level exceeds evolution cap.");
        var g = classes[classId].growth;
        decimal multiplier = (decimal)evolution[stars].multiplier;
        return new StatBlock {
            hp = StatCalculator.Round((b.hp + (decimal)g.hp * (level - 1)) * multiplier),
            atk = StatCalculator.Round((b.atk + (decimal)g.atk * (level - 1)) * multiplier),
            def = StatCalculator.Round((b.def + (decimal)g.def * (level - 1)) * multiplier),
            pot = StatCalculator.Round((b.pot + (decimal)g.pot * (level - 1)) * multiplier),
            spd = checked(b.spd + 15 * stars)
        };
    }

    private static int Sum(StatBlock b) => checked(b.hp + b.atk + b.def + b.spd + b.pot);
    private static void ValidateStats(StatBlock b) => Require(b != null && b.hp > 0 && b.atk > 0 && b.def >= 0 && b.spd > 0 && b.pot >= 0 &&
        new[] { b.hp, b.atk, b.def, b.spd, b.pot }.All(x => x <= 1000000), "Invalid base stats.");
    private static StatBlock Copy(StatBlock b) => new() { hp = b.hp, atk = b.atk, def = b.def, spd = b.spd, pot = b.pot };
    private static CharacterStatRule Copy(CharacterStatRule c) => new() { id = c.id, name = c.name, rarityTier = c.rarityTier,
        biome = c.biome, classArchetype = c.classArchetype, role = c.role, bst = c.bst, baseStats = Copy(c.baseStats) };
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("Invalid stat rules: " + message); }
}

public static class StatCalculator
{
    // Explicit rounding contract, once after composition. Valid zero DEF/POT remain zero.
    public static int Round(decimal value) => checked((int)Math.Round(value, 0, MidpointRounding.ToEven));

    // Existing content remains at its tested scale until complete canonical kits are introduced.
    public static StatBlock CalculateLegacy(StatBlock b, StatGrowth g, int level)
    {
        if (b == null || g == null) throw new ArgumentNullException("Legacy stats/growth");
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
        int Scale(int value, float growth) => Math.Max(1, checked((int)Math.Round(value + growth * (level - 1), MidpointRounding.ToEven)));
        return new StatBlock { hp = Scale(b.hp, g.hp), atk = Scale(b.atk, g.atk), def = Scale(b.def, g.def), spd = Scale(b.spd, g.spd), pot = Scale(b.pot, g.pot) };
    }
}
