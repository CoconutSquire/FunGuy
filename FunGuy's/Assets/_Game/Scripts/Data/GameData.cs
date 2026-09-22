using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameData {
  public StatRulesCatalog StatRules { get; private set; }
  public LevelProgressionRules LevelRules { get; private set; }
  public Dictionary<string, CharacterDef> Characters = new();
  public Dictionary<string, EnemyDef> Enemies = new();
  public Dictionary<string, SkillDef> Skills = new();
  public Dictionary<string, StageDef> Stages = new();
  public Dictionary<string, BannerDef> Banners = new();

  public void LoadAll() {
    if (UnityContentCatalogLoader.TryLoad(out var authoredCatalog)) {
      UnityContentCatalogLoader.LoadInto(this, authoredCatalog);
      return;
    }

    var cfile = JsonLoader.LoadFromResources<CharactersFile>("GameData/characters");
    var sfile = JsonLoader.LoadFromResources<SkillsFile>("GameData/skills");
    var stfile = JsonLoader.LoadFromResources<StagesFile>("GameData/stages");
    var bfile = JsonLoader.LoadFromResources<BannersFile>("GameData/banners");

    var statRules = new StatRulesCatalog(JsonLoader.LoadFromResources<StatRulesFile>("GameData/stat_rules"));
    var levelRules = new LevelProgressionRules(JsonLoader.LoadFromResources<LevelProgressionFile>("GameData/level_progression"));
    GameDataValidator.Validate(cfile, sfile, stfile, bfile, statRules);
    LoadValidated(cfile, sfile, stfile, bfile, statRules, levelRules);
  }

  public IReadOnlyList<RateEntry> GetSortedRates(BannerDef banner) {
    if (banner?.rates == null || banner.rates.Count == 0) return Array.Empty<RateEntry>();
    return banner.rates.Where(r => r.rate > 0f).OrderByDescending(r => r.rarity).ToList();
  }

  private void NormalizeCharacters(List<CharacterDef> characters) {
    if (characters == null) return;
    foreach (var c in characters) {
      if (!string.IsNullOrEmpty(c.statProfileId) || !string.IsNullOrEmpty(c.statModel)) continue;
      if (c.baseStats == null) c.baseStats = new StatBlock();
      if (c.growth == null) c.growth = new StatGrowth();
      if (c.skills == null) c.skills = new SkillRefs();
      if (c.synergyTags == null) c.synergyTags = new List<string>();
      c.element ??= c.biome;
      c.biome = string.IsNullOrWhiteSpace(c.biome) ? MapElementToBiome(c.element) : c.biome;
      c.classArchetype ??= c.role;
      c.rarityTier ??= c.rarity >= 6 ? "UR" : (c.rarity >= 4 ? "SR" : "R");
      if (c.baseStats.pot <= 0) c.baseStats.pot = Math.Max(1, c.baseStats.atk);
      if (c.growth.pot <= 0f) c.growth.pot = c.growth.atk;
      c.passives ??= new List<PassiveDef>();
    }
  }

  private void NormalizeEnemies(List<EnemyDef> enemies) {
    if (enemies == null) return;
    foreach (var e in enemies) { e.baseStats ??= new StatBlock(); e.growth ??= new StatGrowth(); e.skills ??= new SkillRefs(); e.passives ??= new List<PassiveDef>(); }
  }
  private void NormalizeSkills(List<SkillDef> skills) { if (skills != null) foreach (var s in skills) s.effects ??= new List<EffectDef>(); }
  private void NormalizeBanners(List<BannerDef> banners) { if (banners != null) foreach (var b in banners) { b.rates ??= new List<RateEntry>(); b.featuredCharacterIds ??= new List<string>(); b.pity ??= new PityDef(); } }

  private string MapElementToBiome(string element) => string.IsNullOrWhiteSpace(element) ? "Kitchen" : element;
}
