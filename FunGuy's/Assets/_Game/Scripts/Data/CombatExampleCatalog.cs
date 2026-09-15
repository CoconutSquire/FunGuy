using System;
using System.Collections.Generic;
using System.Linq;

[Serializable] public sealed class CombatExamplesFile {
    public int schemaVersion;
    public string rulesVersion;
    public List<SkillDef> skills;
    public List<CharacterDef> characters;
}

// Isolated executable content examples, deliberately outside saved ownership and summon pools.
public sealed class CombatExampleCatalog
{
    private readonly CombatExamplesFile file;
    public GameData Data { get; }
    public CombatExampleCatalog(GameData source)
    {
        file = JsonLoader.LoadFromResources<CombatExamplesFile>("GameData/combat_examples");
        if (file == null || file.schemaVersion != 1 || file.rulesVersion != BattleSession.RulesVersion ||
            file.skills == null || file.characters == null || file.characters.Count != FormationRules.Capacity)
            throw new InvalidOperationException("Invalid combat example catalog version or team.");
        Data = BattleSession.SnapshotSkills(source);
        foreach (var skill in file.skills) {
            GameDataValidator.ValidateSkill(skill);
            if (!Data.Skills.TryAdd(skill.id, skill)) throw new InvalidOperationException("Duplicate example skill.");
        }
        var ids = new HashSet<string>();
        foreach (var c in file.characters) {
            if (c == null || string.IsNullOrWhiteSpace(c.id) || !ids.Add(c.id)) throw new InvalidOperationException("Invalid example identity.");
            var profile = source.StatRules.Character(c.statProfileId);
            if (c.name != profile.name || c.biome != profile.biome || c.classArchetype != profile.classArchetype ||
                c.role != profile.role || c.rarityTier != profile.rarityTier || !GameDataValidator.HasNoStatOverrides(c) ||
                c.skills == null || !Data.Skills.ContainsKey(c.skills.basic) || !Data.Skills.ContainsKey(c.skills.ult))
                throw new InvalidOperationException($"Example {c.id} conflicts with its canonical profile or skills.");
            GameDataValidator.ValidatePassives(c.id, c.passives);
        }
        rules = source.StatRules;
    }
    private readonly StatRulesCatalog rules;
    public List<CombatUnit> CreateTeam(int level = 1, TeamSide side = TeamSide.Player) => file.characters.Select((c, index) => {
        var fighter = CombatUnitFactory.Create(c, level, side, rules: rules);
        fighter.formationSlot = new[] { 0, 2, 8, 9, 11 }[index];
        return fighter;
    }).ToList();
}
