using System;
using System.Collections.Generic;
using System.Linq;

// Imports the legacy funguy_characters.json roster into the current runtime schema.
// The presentation already renders every CharacterDef with FungusPortrait, so imported
// units intentionally point at the shared executable placeholder skills and do not need
// individual sprite assets.
public static class LegacyCharacterImporter
{
    [Serializable]
    private sealed class LegacyCharactersFile
    {
        public List<LegacyCharacter> characters;
    }

    [Serializable]
    private sealed class LegacyCharacter
    {
        public string id;
        public string name;
        public string rarity;
        public string biome;
        public string @class;
        public string role;
        public string[] keySynergies;
        public int hp;
        public int atk;
        public int def;
        public int spd;
        public int pot;
        public int bst;
        public string passive;
        public string passiveDescription;
        public string signatureSkill;
        public string skillDescription;
        public string cooldown;
    }

    public static void AppendMissingCharacters(CharactersFile destination)
    {
        if (destination == null || destination.characters == null) return;

        var source = JsonLoader.LoadFromResources<LegacyCharactersFile>("GameData/funguy_characters");
        if (source?.characters == null || source.characters.Count == 0) return;

        var existing = new HashSet<string>(destination.characters
            .Where(c => c != null && !string.IsNullOrWhiteSpace(c.id))
            .Select(c => c.id), StringComparer.Ordinal);

        foreach (var legacy in source.characters)
        {
            if (legacy == null || string.IsNullOrWhiteSpace(legacy.id) || !existing.Add(legacy.id))
                continue;

            var stats = new StatBlock
            {
                hp = legacy.hp,
                atk = legacy.atk,
                def = legacy.def,
                spd = legacy.spd,
                pot = legacy.pot
            };

            destination.characters.Add(new CharacterDef
            {
                statModel = "class-growth-v1",
                kitVersion = "legacy-placeholder-v1",
                id = legacy.id,
                name = legacy.name,
                rarity = ParseRarity(legacy.rarity),
                rarityTier = NormalizeRarity(legacy.rarity),
                biome = string.IsNullOrWhiteSpace(legacy.biome) ? "Kitchen" : legacy.biome,
                classArchetype = string.IsNullOrWhiteSpace(legacy.@class) ? "DPS" : legacy.@class,
                role = string.IsNullOrWhiteSpace(legacy.role) ? "DPS" : legacy.role,
                bst = legacy.bst > 0 ? legacy.bst : stats.hp + stats.atk + stats.def + stats.spd + stats.pot,
                synergyTags = legacy.keySynergies == null ? new List<string>() : legacy.keySynergies.ToList(),
                passiveName = legacy.passive,
                passiveDescription = legacy.passiveDescription,
                signatureSkillName = legacy.signatureSkill,
                signatureSkillDescription = legacy.skillDescription,
                signatureCooldown = legacy.cooldown,
                baseStats = stats,
                skills = new SkillRefs { basic = "imported_character_basic", ult = "imported_character_signature" },
                passives = new List<PassiveDef>()
            });
        }
    }

    private static int ParseRarity(string rarity)
    {
        switch ((rarity ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "UR": return 5;
            case "SR": return 4;
            case "R": return 3;
            default: return 4;
        }
    }

    private static string NormalizeRarity(string rarity)
    {
        var value = (rarity ?? string.Empty).Trim().ToUpperInvariant();
        return value == "R" || value == "SR" || value == "UR" ? value : "SR";
    }
}
