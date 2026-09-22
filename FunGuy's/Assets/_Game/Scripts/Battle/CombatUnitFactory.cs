using System;
using System.Collections.Generic;

// Shared by gameplay and QA. Profile-backed units use versioned canonical rules.
public static class CombatUnitFactory
{
    public static CombatUnit Create(CharacterDef definition, int level, TeamSide side, int stars = 1, StatRulesCatalog rules = null)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (!string.IsNullOrEmpty(definition.statModel))
        {
            if (definition.statModel != "class-growth-v1" || rules == null || !string.IsNullOrEmpty(definition.statProfileId))
                throw new ArgumentException("Unsupported or conflicting authored stat model.");
            return Create(definition.id, definition.name, definition.biome, definition.classArchetype, definition.role,
                rules.CalculateAuthored(definition.baseStats, definition.classArchetype, level, stars), definition.skills, level, side, definition.passives);
        }
        if (!string.IsNullOrEmpty(definition.statProfileId))
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules), "Canonical character requires stat rules.");
            var profile = rules.Character(definition.statProfileId);
            return Create(definition.id, profile.name, profile.biome, profile.classArchetype, profile.role,
                rules.CalculateCharacter(profile.id, level, stars), definition.skills, level, side, definition.passives);
        }
        return Create(definition.id, definition.name, definition.biome, definition.classArchetype,
            definition.role, StatCalculator.CalculateLegacy(definition.baseStats, definition.growth, level), definition.skills, level, side, definition.passives);
    }

    public static CombatUnit Create(EnemyDef definition, int level, TeamSide side)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        return Create(definition.id, definition.name, definition.biome, definition.classArchetype,
            definition.role, StatCalculator.CalculateLegacy(definition.baseStats, definition.growth, level), definition.skills, level, side, definition.passives);
    }

    public static CombatUnit Create(EnemyDef definition, WaveUnit entry, TeamSide side)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        var fighter = Create(definition, entry.level, side);
        if (!string.IsNullOrEmpty(entry.slotId)) fighter.formationSlot = FormationRules.ParseSlot(entry.slotId);
        return fighter;
    }

    // Campaign opponents now use the same authored roster definitions as collectible fighters.
    public static CombatUnit Create(CharacterDef definition, WaveUnit entry, TeamSide side, StatRulesCatalog rules)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        var fighter = Create(definition, entry.level, side, 1, rules);
        if (!string.IsNullOrEmpty(entry.slotId)) fighter.formationSlot = FormationRules.ParseSlot(entry.slotId);
        return fighter;
    }

    private static CombatUnit Create(string id, string name, string biome, string archetype,
        string role, StatBlock stats, SkillRefs skills, int level, TeamSide side, List<PassiveDef> passives)
    {
        if (stats == null || skills == null)
            throw new ArgumentException($"Unit {id} is missing stats or skills.");
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
        int hp = stats.hp;
        return new CombatUnit
        {
            id = id, name = name, biome = biome, classArchetype = archetype, role = role,
            side = side, level = level, maxHp = hp, hp = hp,
            atk = stats.atk, def = stats.def, spd = stats.spd, pot = stats.pot,
            basicSkillId = skills.basic, ultSkillId = skills.ult, maxEnergy = 100,
            statuses = new List<StatusInstance>(), passives = CombatEffectRules.ClonePassives(passives)
        };
    }

}
