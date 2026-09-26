using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Runtime bridge for the gameplay glossary in Resources/GameData/Keywords.csv.
// The CSV remains the source-of-truth glossary; this class supplies the parsed
// keyword names plus the numeric defaults that the glossary explicitly defines.
public static class KeywordRules
{
    public sealed class Rule
    {
        public string Name;
        public string Description;
    }

    private static Dictionary<string, Rule> rules;

    public static IReadOnlyDictionary<string, Rule> All
    {
        get { EnsureLoaded(); return rules; }
    }

    public static bool IsDefined(string name)
    {
        EnsureLoaded();
        return !string.IsNullOrWhiteSpace(name) && rules.ContainsKey(Normalize(name));
    }

    public static string Description(string name)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = Normalize(name);
        if (rules.TryGetValue(key, out var rule)) return rule.Description;
        if ((key == "poison" || key == "burn" || key == "bleed") &&
            rules.TryGetValue(key + " (dot)", out rule)) return rule.Description;
        return null;
    }

    public static float LuminescenceCritBonus => .10f;
    public static float VantageAccuracyBonus => .20f;
    public static float VulnerabilityDamageTakenBonus => .15f;
    public static float BurnDefenseReductionPerStack => .10f;
    public static float BrittleCritTakenBonus => .25f;
    public static int RegenDefaultTurns => 3;

    public static bool IsCsvBackedStatus(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && CsvStatuses.Contains(Normalize(name));
    }

    private static readonly HashSet<string> CsvStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vantage", "Stealth", "Intangible", "Luminescence", "FrostShield", "Regen", "Reflect",
        "Immortality", "Cloak", "Poison", "Burn", "Bleed", "Vulnerability", "Brittle", "Silence",
        "Charm", "Confusion", "Freeze", "Move", "Root", "Stun"
    };

    private static void EnsureLoaded()
    {
        if (rules != null) return;

        rules = new Dictionary<string, Rule>(StringComparer.OrdinalIgnoreCase);
        var asset = Resources.Load<TextAsset>("GameData/Keywords");
        if (asset == null)
        {
            Debug.LogWarning("KeywordRules: GameData/Keywords.csv could not be loaded.");
            return;
        }

        foreach (var row in ParseCsv(asset.text))
        {
            if (row.Count < 2) continue;
            var name = row[0].Trim();
            var description = row[1].Trim();
            if (string.IsNullOrWhiteSpace(name) || string.Equals(name, "Name", StringComparison.OrdinalIgnoreCase)) continue;
            if (!LooksLikeKeyword(name)) continue;
            rules[Normalize(name)] = new Rule { Name = name, Description = description };
        }
    }

    private static bool LooksLikeKeyword(string value)
    {
        if (value.Length == 0 || value.Length > 64) return false;
        if (value.Contains("What it is", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Gameplay Mechanic", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Role:", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Mechanics:", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Class", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Biome", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Elemental", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Neutral", StringComparison.OrdinalIgnoreCase)) return false;
        return value.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '/' || c == '(' || c == ')' || c == '%');
    }

    private static string Normalize(string value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static IEnumerable<List<string>> ParseCsv(string text)
    {
        var row = new List<string>();
        var cell = new System.Text.StringBuilder();
        bool quoted = false;

        for (int i = 0; i < (text ?? string.Empty).Length; i++)
        {
            char c = text[i];
            if (c == '"')
            {
                if (quoted && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else quoted = !quoted;
                continue;
            }

            if (!quoted && c == ',')
            {
                row.Add(cell.ToString());
                cell.Clear();
                continue;
            }

            if (!quoted && (c == '\n' || c == '\r'))
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString());
                cell.Clear();
                yield return row;
                row = new List<string>();
                continue;
            }

            cell.Append(c);
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            yield return row;
        }
    }
}
