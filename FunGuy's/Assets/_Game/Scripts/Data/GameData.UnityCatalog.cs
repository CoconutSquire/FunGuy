using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameData
{
    // Shared finalization path for JSON and Unity-authored catalogs.
    public void LoadValidated(CharactersFile cfile, SkillsFile sfile, StagesFile stfile, BannersFile bfile,
        StatRulesCatalog statRules, LevelProgressionRules levelRules)
    {
        if (cfile == null || sfile == null || stfile == null || bfile == null)
            throw new InvalidOperationException("Unity content catalog is incomplete.");

        NormalizeCharacters(cfile.characters);
        NormalizeEnemies(cfile.enemies);
        NormalizeSkills(sfile.skills);
        NormalizeBanners(bfile.banners);

        StatRules = statRules;
        LevelRules = levelRules;
        Characters = (cfile.characters ?? new List<CharacterDef>()).ToDictionary(x => x.id, x => x);
        Enemies = (cfile.enemies ?? new List<EnemyDef>()).ToDictionary(x => x.id, x => x);
        Skills = (sfile.skills ?? new List<SkillDef>()).ToDictionary(x => x.id, x => x);
        Stages = (stfile.stages ?? new List<StageDef>()).ToDictionary(x => x.id, x => x);
        Banners = (bfile.banners ?? new List<BannerDef>()).ToDictionary(x => x.id, x => x);
        _ = new CombatExampleCatalog(this);
    }
}
