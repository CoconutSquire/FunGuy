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
    var cfile = JsonLoader.LoadFromResources<CharactersFile>("GameData/characters");
    var sfile = JsonLoader.LoadFromResources<SkillsFile>("GameData/skills");
    var stfile = JsonLoader.LoadFromResources<StagesFile>("GameData/stages");
    var bfile = JsonLoader.LoadFromResources<BannersFile>("GameData/banners");

    var statRules = new StatRulesCatalog(JsonLoader.LoadFromResources<StatRulesFile>("GameData/stat_rules"));
    var levelRules = new LevelProgressionRules(JsonLoader.LoadFromResources<LevelProgressionFile>("GameData/level_progression"));
    GameDataValidator.Validate(cfile, sfile, stfile, bfile, statRules);

    NormalizeCharacters(cfile.characters);
    NormalizeEnemies(cfile.enemies);
    NormalizeSkills(sfile.skills);
    NormalizeBanners(bfile.banners);

    StatRules = statRules;
    LevelRules = levelRules;
    Characters = cfile.characters.ToDictionary(x => x.id, x => x);
    Enemies    = (cfile.enemies ?? new List<EnemyDef>()).ToDictionary(x => x.id, x => x);
    Skills     = sfile.skills.ToDictionary(x => x.id, x => x);
    Stages     = stfile.stages.ToDictionary(x => x.id, x => x);
    Banners    = bfile.banners.ToDictionary(x => x.id, x => x);
    _ = new CombatExampleCatalog(this); // Validate executable reference content without adding it to summon pools.
  }

  public IReadOnlyList<RateEntry> GetSortedRates(BannerDef banner) {
    if (banner?.rates == null || banner.rates.Count == 0) return Array.Empty<RateEntry>();
    return banner.rates
      .Where(r => r.rate > 0f)
      .OrderByDescending(r => r.rarity)
      .ToList();
  }

  private void NormalizeCharacters(List<CharacterDef> characters) {
    if (characters == null) return;

    foreach (var c in characters) {
      if (!string.IsNullOrEmpty(c.statProfileId) || !string.IsNullOrEmpty(c.statModel)) continue; // Authored content is explicit and validated.
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
      if (c.bst <= 0) c.bst = c.baseStats.hp + c.baseStats.atk + c.baseStats.def + c.baseStats.spd + c.baseStats.pot;
    }
  }

  private void NormalizeEnemies(List<EnemyDef> enemies) {
    if (enemies == null) return;

    foreach (var e in enemies) {
      if (e.baseStats == null) e.baseStats = new StatBlock();
      if (e.skills == null) e.skills = new SkillRefs();
      if (e.baseStats.pot <= 0) e.baseStats.pot = Math.Max(1, e.baseStats.atk);
      e.biome = string.IsNullOrWhiteSpace(e.biome) ? "Kitchen" : e.biome;
      e.classArchetype ??= "Enemy";
      e.role ??= "Enemy";
    }
  }

  private void NormalizeSkills(List<SkillDef> skills) {
    if (skills == null) return;

    foreach (var s in skills) {
      if (s.effects == null) s.effects = new List<EffectDef>();
      if (s.energyCost <= 0) s.energyCost = s.cooldown > 0 ? 100 : 0;
      if (s.cooldown < 0) s.cooldown = 0;
    }
  }

  private void NormalizeBanners(List<BannerDef> banners) {
    if (banners == null) return;

    foreach (var b in banners) {
      b.bannerType ??= "standard_character";
      if (b.rates == null) b.rates = new List<RateEntry>();
      if (b.pity == null) b.pity = new PityDef();
      if (b.featuredCharacterIds == null) b.featuredCharacterIds = new List<string>();
      if (b.featuredRateUp <= 0f) b.featuredRateUp = 0.5f;

      if (b.pity.guaranteeRarity <= 0) b.pity.guaranteeRarity = 5;
      if (b.pity.guaranteeRarityAt <= 0) b.pity.guaranteeRarityAt = 90;
      if (b.pity.hardPity <= 0) b.pity.hardPity = b.pity.guaranteeRarityAt;
      if (b.pity.softPityStart <= 0) b.pity.softPityStart = Math.Max(1, b.pity.hardPity - 15);
      if (b.pity.featuredRarity <= 0) b.pity.featuredRarity = b.pity.guaranteeRarity;

      float total = b.rates.Sum(r => r.rate);
      if (total <= 0f) {
        Debug.LogWarning($"[GameData] Banner {b.id} has no valid rates; applying fallback.");
        b.rates = new List<RateEntry> {
          new() { rarity = 3, rate = 0.8f },
          new() { rarity = 4, rate = 0.17f },
          new() { rarity = 5, rate = 0.03f },
        };
        total = 1f;
      }

      if (Math.Abs(total - 1f) > 0.0001f) {
        foreach (var entry in b.rates) {
          entry.rate = entry.rate / total;
        }
      }
    }
  }

  private static string MapElementToBiome(string element) {
    return (element ?? string.Empty).Trim().ToLowerInvariant() switch {
      "nature" => "Forest",
      "rot" => "Decay",
      "frost" => "Tundra",
      "ember" => "Wetlands",
      "astral" => "Kitchen",
      _ => "Kitchen",
    };
  }
}
