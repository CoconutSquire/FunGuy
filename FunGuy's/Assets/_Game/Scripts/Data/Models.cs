using System;
using System.Collections.Generic;

[Serializable] public class StatBlock {
  public int hp;
  public int atk;
  public int def;
  public int spd;
  public int pot;
}

[Serializable] public class StatGrowth {
  public float hp;
  public float atk;
  public float def;
  public float spd;
  public float pot;
}

[Serializable] public class SkillRefs { public string basic; public string ult; }

[Serializable] public class CharacterDef {
  public string statModel; // class-growth-v1: authored bases with canonical class growth/evolution.
  public string kitVersion;
  public string statProfileId; // Empty retains legacy content; otherwise a stat-sheet-v1 reference.
  public string id;
  public string name;
  public int rarity;
  public string rarityTier;
  public string biome;
  public string classArchetype;
  public string role;
  public string element;
  public int bst;
  public List<string> synergyTags;
  public StatBlock baseStats;
  public StatGrowth growth;
  public SkillRefs skills;
  public List<PassiveDef> passives = new();
}

[Serializable] public class EnemyDef {
  public string id;
  public string name;
  public string biome;
  public string classArchetype;
  public string role;
  public StatBlock baseStats;
  public StatGrowth growth;
  public SkillRefs skills;
  public List<PassiveDef> passives = new();
}

[Serializable] public class EffectDef {
  public string type;      // Damage, Heal, ApplyStatus, Shield
  public float scale;      // damage/heal scaling vs ATK
  public string status;    // Poison, Burn, Freeze, Regen, Thorns
  public float chance;     // 0..1
  public int duration;     // turns
  public float potency;    // e.g. dot % of maxHP, regen %, thorns % of incoming
  public string target; // Optional per-effect target override.
  public string stat; // ATK/POT/MaxHP/TargetMaxHP.
  public float ignoreDefense;
  public bool ignoreShield;
  public bool sureHit;
  public int slot = -1;
}

[Serializable] public class PassiveDef {
  public string description;
  public string id;
  public string trigger;
  public string target;
  public int every = 1;
  public List<EffectDef> effects = new();
}

[Serializable] public class SkillDef {
  public string description;
  public string id;
  public string name;
  public string target;    // EnemyFront, AllEnemies, AllAllies, RandomEnemy2, Self
  public int cooldown;     // turns
  public int energyCost;   // 0 for basic, 100 for signature by default
  public List<EffectDef> effects;
}

[Serializable] public class RewardDef { public int gold; public int spores; public int accountXp; }
[Serializable] public class WaveUnit { public string enemyId; public int level; public string slotId; }
[Serializable] public class WaveDef { public List<WaveUnit> enemies; }
[Serializable] public class StageDef {
  public string description;
  public string encounterVersion;
  public bool boss;
  public string id;
  public string name;
  public int order;
  public int recommendedPower;
  public List<WaveDef> waves;
  public RewardDef rewards;
}

[Serializable] public class RateEntry {
  public int rarity;
  public float rate;
}

[Serializable] public class PityDef {
  public int guaranteeRarityAt; // legacy hard pity threshold
  public int guaranteeRarity;   // legacy guaranteed rarity
  public int softPityStart;     // e.g. 74
  public int hardPity;          // e.g. 90
  public int featuredRarity;    // rarity gated by 50/50 logic
}

[Serializable] public class BannerDef {
  public string id;
  public string name;
  public string bannerType; // standard_character, limited_character
  public string currency;
  public int costPerPull;
  public List<RateEntry> rates;
  public PityDef pity;
  public float featuredRateUp; // 0.5 for Genshin-like 50/50
  public bool carryFeaturedGuarantee;
  public List<string> featuredCharacterIds;
}

[Serializable] public class CharactersFile { public List<CharacterDef> characters; public List<EnemyDef> enemies; }
[Serializable] public class SkillsFile { public List<SkillDef> skills; }
[Serializable] public class StagesFile { public int schemaVersion; public List<StageDef> stages; }
[Serializable] public class BannersFile { public List<BannerDef> banners; }
