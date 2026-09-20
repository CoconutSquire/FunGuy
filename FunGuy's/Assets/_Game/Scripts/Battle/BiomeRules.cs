using System;

public enum Biome { Forest, Wetlands, Decay, Tundra, Kitchen, Cosmic, BiomeLess }

public static class BiomeRules
{
    public static Biome Parse(string value) => (value ?? "").Trim().ToLowerInvariant() switch
    {
        "forest" => Biome.Forest, "wetlands" => Biome.Wetlands,
        "decay" => Biome.Decay, "tundra" => Biome.Tundra,
        "kitchen" => Biome.Kitchen, "cosmic" => Biome.Cosmic, "biome-less" => Biome.BiomeLess,
        _ => throw new ArgumentException("Unknown biome: " + value)
    };

    public static decimal DamageMultiplier(Biome attacker, Biome defender)
    {
        if (!Enum.IsDefined(typeof(Biome), attacker) || !Enum.IsDefined(typeof(Biome), defender)) throw new ArgumentOutOfRangeException("biome");
        if (attacker == Biome.Kitchen || defender == Biome.Kitchen || attacker == Biome.Cosmic || defender == Biome.Cosmic || attacker == Biome.BiomeLess || defender == Biome.BiomeLess) return 1m;
        if (((int)attacker + 1) % 4 == (int)defender) return 1.5m;
        if (((int)defender + 1) % 4 == (int)attacker) return .75m;
        return 1m;
    }
}

public static class DamageCalculator
{
    public const int DefenseConstant = 3000;

    public static int Calculate(int attack, int defense, decimal skillScale, Biome attacker, Biome defender)
    {
        if (attack < 0 || skillScale < 0) throw new ArgumentOutOfRangeException("attack/skillScale");
        decimal value = attack * skillScale * DefenseConstant / (DefenseConstant + (decimal)Math.Max(0, defense));
        return Math.Max(1, StatCalculator.Round(value * BiomeRules.DamageMultiplier(attacker, defender)));
    }
}
