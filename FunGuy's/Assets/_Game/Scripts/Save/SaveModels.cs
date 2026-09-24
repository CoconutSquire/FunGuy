using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class StringIntEntry {
  public string key;
  public int value;
}

[Serializable]
public class StringBoolEntry {
  public string key;
  public bool value;
}

[Serializable]
public class GearSlotState {
  public string slotId;   // cap, stipe, mycelium, symbiotic
  public string itemId;
  public int rarity;      // 1-6
  public int level;       // 1-20
  public string rolledStat;       // Symbiote random stat: HP/ATK/DEF/SPD/POT
  public int rolledStatAmount;
  public string uniqueTrait;      // Symbiote unique skill trait
  public float traitAmount;
}

[Serializable]
public class OwnedUnit {
  public string charId;
  public int level;
  public int copies; // for future ascension
  public int stars;
  public int coreLevel;
  public int xp;
  public List<GearSlotState> gearSlots = new();
}

[Serializable]
public class BannerHistoryEntry {
  public string bannerId;
  public int pullNumber;
  public string resultCharId;
  public bool wasFeatured;
  public string pulledAtUtc;
}

[Serializable]
public class PlayerSave {
  public int version = 6;
  public string createdUtc;
  public string lastSavedUtc;
  public string idleLastClaimedUtc;
  public double idleEquipmentProgress;

  public int gold;
  public int spores;
  public int accountLevel;
  public int accountXp;
  public List<string> clearedStages = new();
  public bool tutorialBattleRewardClaimed;
  public int sporeEssence;
  public int coreFragments;
  public int primeSpores;

  public bool tutorialCompleted;
  public int tutorialStep;     // 0..N
  public int tutorialTickets;  // scripted first-time summon pacing

  public List<OwnedUnit> units = new();
  public List<GearSlotState> equipmentInventory = new();
  public List<string> activeTeam = new(); // list of charIds
  public List<FormationPlacement> formation = new(); // Positions for activeTeam members; removed units leave holes.
  public List<StringIntEntry> bannerPity = new();
  public List<StringBoolEntry> bannerFeaturedGuarantee = new();
  public List<BannerHistoryEntry> bannerHistory = new();
}

public static class SaveMapUtils {
  public static int GetInt(List<StringIntEntry> entries, string key, int defaultValue = 0) {
    if (entries == null) return defaultValue;
    var found = entries.FirstOrDefault(x => x.key == key);
    return found == null ? defaultValue : found.value;
  }

  public static void SetInt(List<StringIntEntry> entries, string key, int value) {
    if (entries == null) return;
    var found = entries.FirstOrDefault(x => x.key == key);
    if (found == null) entries.Add(new StringIntEntry { key = key, value = value });
    else found.value = value;
  }

  public static bool GetBool(List<StringBoolEntry> entries, string key, bool defaultValue = false) {
    if (entries == null) return defaultValue;
    var found = entries.FirstOrDefault(x => x.key == key);
    return found == null ? defaultValue : found.value;
  }

  public static void SetBool(List<StringBoolEntry> entries, string key, bool value) {
    if (entries == null) return;
    var found = entries.FirstOrDefault(x => x.key == key);
    if (found == null) entries.Add(new StringBoolEntry { key = key, value = value });
    else found.value = value;
  }
}
