using System;
using System.Collections.Generic;
using System.Linq;

// No Unity APIs: the same validation can gate editor builds and future server catalogs.
public static class GameDataValidator
{
    public static void Validate(CharactersFile characters, SkillsFile skills, StagesFile stages, BannersFile banners, StatRulesCatalog statRules = null)
    {
        Require(characters != null && skills != null && stages != null && banners != null, "Missing content file.");
        Require(stages.schemaVersion == 1, "stages.json requires schemaVersion 1 and wave objects with enemies lists.");
        var characterIds = Ids(characters.characters, x => x.id, "characters");
        var skillIds = Ids(skills.skills, x => x.id, "skills");
        Ids(stages.stages, x => x.id, "stages");
        Ids(banners.banners, x => x.id, "banners");

        foreach (var c in characters.characters)
        {
            ValidatePassives(c.id, c.passives);
            if (!string.IsNullOrEmpty(c.biome)) BiomeRules.Parse(c.biome);
            if (!string.IsNullOrEmpty(c.statModel))
            {
                Require(c.statModel == "class-growth-v1" && string.IsNullOrEmpty(c.statProfileId) && statRules != null,
                    $"Unit {c.id}: unsupported or conflicting stat model.");
                Require(!string.IsNullOrWhiteSpace(c.kitVersion) && !string.IsNullOrWhiteSpace(c.name) && !string.IsNullOrWhiteSpace(c.role),
                    $"Unit {c.id}: missing authored identity/version.");
                Require(c.rarityTier == "R" || c.rarityTier == "SR" || c.rarityTier == "UR", $"Unit {c.id}: missing acquisition tier.");
                Require(c.rarityTier != "R" || c.biome == "Biome-less", $"Unit {c.id}: R units are biome-less.");
                Require(c.growth == null || new[] { c.growth.hp, c.growth.atk, c.growth.def, c.growth.spd, c.growth.pot }.All(v => v == 0),
                    $"Unit {c.id}: growth must come from the class catalog.");
                BiomeRules.Parse(c.biome);
                statRules.CalculateAuthored(c.baseStats, c.classArchetype, 1, 1);
                Require(c.bst == c.baseStats.hp + c.baseStats.atk + c.baseStats.def + c.baseStats.spd + c.baseStats.pot, $"Unit {c.id}: incorrect BST.");
                Require(c.skills != null && skillIds.Contains(c.skills.basic) && skillIds.Contains(c.skills.ult), $"Unit {c.id}: missing skill reference.");
            }
            else if (string.IsNullOrEmpty(c.statProfileId)) ValidateUnit(c.id, c.baseStats, c.growth, c.skills, skillIds);
            else
            {
                Require(statRules != null, "Canonical units require a stat rules catalog.");
                var profile = statRules.Character(c.statProfileId);
                Require(c.name == profile.name && c.biome == profile.biome && c.classArchetype == profile.classArchetype &&
                    c.rarityTier == profile.rarityTier && c.role == profile.role, $"Unit {c.id}: identity conflicts with stat profile.");
                Require(HasNoStatOverrides(c), $"Unit {c.id}: canonical stats must come only from the profile.");
                Require(c.skills != null && skillIds.Contains(c.skills.basic) && skillIds.Contains(c.skills.ult), $"Unit {c.id}: missing skill reference.");
            }
        }

        foreach (var e in characters.enemies ?? new List<EnemyDef>())
        {
            ValidatePassives(e.id, e.passives);
            if (!string.IsNullOrEmpty(e.biome)) BiomeRules.Parse(e.biome);
            ValidateUnit(e.id, e.baseStats, e.growth, e.skills, skillIds);
        }

        foreach (var skill in skills.skills)
            ValidateSkill(skill);

        Require(stages.stages.All(s => s.order > 0) && stages.stages.Select(s => s.order).Distinct().Count() == stages.stages.Count, "Stages require unique positive order values.");

        foreach (var stage in stages.stages ?? new List<StageDef>())
        {
            Require(stage != null, "Stage entry is null.");
            Require(stage.waves != null && stage.waves.Count > 0, $"Stage {stage.id}: missing waves.");

            foreach (var wave in stage.waves ?? new List<WaveDef>())
            {
                Require(wave != null, $"Stage {stage.id}: wave entry is null.");
                Require(wave.enemies != null && wave.enemies.Count > 0, $"Stage {stage.id}: empty wave.");
                Require(wave.enemies.Count <= FormationRules.Capacity, $"Stage {stage.id}: wave exceeds formation capacity.");

                var slots = new HashSet<int>();
                foreach (var unit in wave.enemies)
                {
                    Require(unit != null && characterIds.Contains(unit.enemyId) && unit.level > 0, $"Stage {stage.id}: invalid roster opponent reference/level.");
                    if (!string.IsNullOrEmpty(unit.slotId))
                    {
                        int slot;
                        try
                        {
                            slot = FormationRules.ParseSlot(unit.slotId);
                        }
                        catch (Exception)
                        {
                            Require(false, $"Stage {stage.id}: invalid enemy slot '{unit.slotId}'.");
                            slot = -1; // unreachable, but keeps compiler happy
                        }
                        Require(slots.Add(slot), $"Stage {stage.id}: duplicate enemy slot.");
                    }
                }
            }

            Require(stage.rewards != null && stage.rewards.gold >= 0 && stage.rewards.spores >= 0 && stage.rewards.accountXp >= 0,
                $"Stage {stage.id}: invalid rewards.");
        }

        foreach (var banner in banners.banners)
        {                      
            Require(banner.currency == "spores" && banner.costPerPull > 0, $"Banner {banner.id}: unsupported currency/cost.");
            Require(banner.rates != null && banner.rates.Count > 0, $"Banner {banner.id}: missing rates.");
            Require(banner.rates.All(r => r != null && FiniteNonnegative(r.rate)), $"Banner {banner.id}: invalid rate.");
            Require(banner.rates.Select(r => r.rarity).Distinct().Count() == banner.rates.Count && Math.Abs(banner.rates.Sum(r => r.rate) - 1f) < 0.0001f,
                $"Banner {banner.id}: rates must be unique and sum to one.");
            Require(banner.rates.All(r => characters.characters.Any(c => c.rarity == r.rarity)), $"Banner {banner.id}: empty rarity pool.");
            Require(banner.pity != null && banner.pity.softPityStart > 0 && banner.pity.hardPity > banner.pity.softPityStart &&
                banner.rates.Any(r => r.rarity == banner.pity.guaranteeRarity), $"Banner {banner.id}: invalid pity.");
            Require(FiniteNonnegative(banner.featuredRateUp) && banner.featuredRateUp <= 1, $"Banner {banner.id}: invalid featured odds.");
            Require(banner.featuredCharacterIds != null && banner.featuredCharacterIds.All(characterIds.Contains), $"Banner {banner.id}: invalid featured IDs.");
        }
    }

    // JsonUtility materializes omitted nested serializable objects as zero-filled values.
    // Those are absence, not competing canonical stats. Any authored nonzero/NaN value is rejected.
    public static bool HasNoStatOverrides(CharacterDef c) =>
        (c.baseStats == null || new[] { c.baseStats.hp, c.baseStats.atk, c.baseStats.def, c.baseStats.spd, c.baseStats.pot }.All(v => v == 0)) &&
        (c.growth == null || new[] { c.growth.hp, c.growth.atk, c.growth.def, c.growth.spd, c.growth.pot }.All(v => v == 0));

    private static void ValidateUnit(string id, StatBlock stats, StatGrowth growth, SkillRefs refs, HashSet<string> skills)
    {
        Require(stats != null && stats.hp > 0 && stats.atk > 0 && stats.def >= 0 && stats.spd > 0, $"Unit {id}: invalid base stats.");
        Require(growth != null && new[] { growth.hp, growth.atk, growth.def, growth.spd, growth.pot }.All(FiniteNonnegative), $"Unit {id}: invalid growth.");
        Require(refs != null && skills.Contains(refs.basic) && skills.Contains(refs.ult), $"Unit {id}: missing skill reference.");
    }

    public static void ValidateSkill(SkillDef skill)
    {
        Require(skill != null && !string.IsNullOrWhiteSpace(skill.id), "Missing skill ID.");
        Require(CombatEffectRules.Targets.Contains(skill.target) && skill.target != "Attacker", $"Skill {skill.id}: unsupported target.");
        Require(skill.cooldown >= 0 && skill.energyCost >= 0 && skill.energyCost <= 100, $"Skill {skill.id}: invalid cooldown/energy.");
        ValidateEffects(skill.id, skill.effects, false);
    }

    public static void ValidatePassives(string unitId, List<PassiveDef> passives)
    {
        if (passives == null) return; // Legacy content has none.
        var ids = new HashSet<string>();
        foreach (var p in passives)
        {
            Require(p != null && !string.IsNullOrWhiteSpace(p.id) && ids.Add(p.id), $"Unit {unitId}: invalid passive ID.");
            Require(CombatEffectRules.Triggers.Contains(p.trigger) && CombatEffectRules.Targets.Contains(p.target) && p.every > 0,
                $"Passive {p.id}: invalid trigger/target/interval.");
            Require(p.every == 1 || p.trigger == "TurnStart", $"Passive {p.id}: intervals require TurnStart.");
            Require(p.target != "Attacker" || p.trigger == "DamageTaken" || p.trigger == "BasicHit" || p.trigger == "DamageDealt" || p.trigger == "EnergyGranted",
                $"Passive {p.id}: no trigger counterpart.");
            ValidateEffects(p.id, p.effects, p.trigger == "DamageTaken" || p.trigger == "BasicHit" || p.trigger == "DamageDealt" || p.trigger == "EnergyGranted", p.trigger == "EnergyGranted");
        }
    }

    private static void ValidateEffects(string id, List<EffectDef> effects, bool counterpart, bool energyGrant = false)
    {
        Require(effects != null && effects.Count > 0, $"Skill/passive {id}: empty effects.");
        foreach (var e in effects)
        {
            Require(e != null && CombatEffectRules.Effects.Contains(e.type), $"Skill/passive {id}: unsupported effect.");
            Require(FiniteNonnegative(e.scale) && !float.IsNaN(e.potency) && !float.IsInfinity(e.potency) &&
                (e.potency >= 0 || e.type == "Gauge"), $"Skill/passive {id}: invalid scaling.");

            Require(
                string.IsNullOrWhiteSpace(e.target) ||
                (CombatEffectRules.Targets.Contains(e.target) &&
                 (e.target != "Attacker" || counterpart)),
                $"Skill/passive {id}: invalid effect target.");

            Require(
                string.IsNullOrWhiteSpace(e.stat) ||
                new[] { "ATK", "POT", "MaxHP", "TargetMaxHP", "GrantedEnergy" }.Contains(e.stat),
                $"Skill/passive {id}: invalid scaling stat.");

            Require(
                e.stat != "GrantedEnergy" || energyGrant,
                $"Skill/passive {id}: GrantedEnergy needs its trigger context.");

            Require(
                e.type != "Damage" ||
                string.IsNullOrWhiteSpace(e.stat) ||
                e.stat == "ATK" ||
                e.stat == "POT",
                $"Skill/passive {id}: damage requires ATK or POT.");

            Require(FiniteNonnegative(e.ignoreDefense) && e.ignoreDefense <= 1, $"Skill/passive {id}: invalid defense bypass.");
            Require(e.slot >= -1 && e.slot < FormationRules.SlotCount, $"Skill/passive {id}: invalid movement slot.");
            if (e.type == "ApplyStatus")
                Require(CombatEffectRules.IsStatus(e.status) && (e.duration > 0 || e.duration == -1) &&
                    FiniteNonnegative(e.chance) && e.chance <= 1, $"Skill/passive {id}: invalid status/chance/duration.");
        }
    }

    private static HashSet<string> Ids<T>(List<T> rows, Func<T, string> id, string label) where T : class
    {
        Require(rows != null && rows.Count > 0, $"{label}: missing definitions.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
            Require(row != null && !string.IsNullOrWhiteSpace(id(row)) && ids.Add(id(row)), $"{label}: null definition or missing/duplicate ID.");
        return ids;
    }

    private static bool FiniteNonnegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Invalid game content: " + message);
    }
}