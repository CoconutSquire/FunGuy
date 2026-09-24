using System;
using System.Collections.Generic;
using System.Linq;

public class GachaService {
  private readonly GameData _data;
  private readonly System.Random _rng;

  public GachaService(GameData data) : this(data, rng: null) { }

  public GachaService(GameData data, int seed) : this(data, new System.Random(seed)) { }

  public GachaService(GameData data, System.Random rng) {
    _data = data;
    _rng = rng ?? new System.Random();
  }

  public string PullOne(PlayerSave save, string bannerId) {
    return PullOne(save, bannerId, consumeCurrency: true);
  }

  public string PullOne(PlayerSave save, string bannerId, bool consumeCurrency) {
    if (!_data.Banners.TryGetValue(bannerId, out var banner)) throw new Exception($"Unknown banner: {bannerId}");
    if (consumeCurrency && banner.currency == "spores" && save.spores < banner.costPerPull) throw new Exception("Not enough spores.");
    if (consumeCurrency && banner.currency == "spores") save.spores -= banner.costPerPull;

    int pityCount = SaveMapUtils.GetInt(save.bannerPity, bannerId, 0) + 1;
    SaveMapUtils.SetInt(save.bannerPity, bannerId, pityCount);

    int rolledRarity = RollRarity(banner, pityCount);
    int pityRarity = Math.Max(1, banner.pity.guaranteeRarity);
    if (rolledRarity >= pityRarity) SaveMapUtils.SetInt(save.bannerPity, bannerId, 0);

    bool wasFeatured = false;
    var chosen = PickCharacter(save, banner, rolledRarity, ref wasFeatured).id;

    AddUnitToInventory(save, chosen);
    save.bannerHistory.Add(new BannerHistoryEntry {
      bannerId = bannerId,
      pullNumber = pityCount,
      resultCharId = chosen,
      wasFeatured = wasFeatured,
      pulledAtUtc = DateTime.UtcNow.ToString("o"),
    });
    if (save.bannerHistory.Count > 200) save.bannerHistory.RemoveRange(0, save.bannerHistory.Count - 200);

    return chosen;
  }

  private int RollRarity(BannerDef banner, int pityCount) {
    var sortedRates = _data.GetSortedRates(banner);
    if (sortedRates.Count == 0) return 3;

    int hardPity = Math.Max(1, banner.pity.hardPity);
    int guaranteeRarity = Math.Max(1, banner.pity.guaranteeRarity);
    if (pityCount >= hardPity) return guaranteeRarity;

    var adjustedRates = BuildAdjustedRatesForPity(sortedRates, banner, pityCount);
    float r = (float)_rng.NextDouble();
    float cumulative = 0f;

    foreach (var entry in adjustedRates) {
      cumulative += entry.rate;
      if (r <= cumulative) return entry.rarity;
    }

    return adjustedRates.Last().rarity;
  }

  private List<RateEntry> BuildAdjustedRatesForPity(IReadOnlyList<RateEntry> rates, BannerDef banner, int pityCount) {
    int softPityStart = Math.Max(1, banner.pity.softPityStart);
    int hardPity = Math.Max(softPityStart + 1, banner.pity.hardPity);
    int topRarity = Math.Max(1, banner.pity.guaranteeRarity);

    var working = rates.Select(x => new RateEntry { rarity = x.rarity, rate = x.rate }).ToList();
    var topRate = working.FirstOrDefault(x => x.rarity == topRarity);
    if (topRate == null) return working;

    if (pityCount < softPityStart) return working;

    int span = Math.Max(1, hardPity - softPityStart);
    float progress = Math.Min(1f, (pityCount - softPityStart + 1) / (float)span);
    float boostedTopRate = topRate.rate + ((1f - topRate.rate) * progress);
    float otherTotal = 1f - topRate.rate;
    float remaining = Math.Max(0f, 1f - boostedTopRate);
    float scale = otherTotal > 0f ? remaining / otherTotal : 0f;

    for (int i = 0; i < working.Count; i++) {
      if (working[i].rarity == topRarity) working[i].rate = boostedTopRate;
      else working[i].rate *= scale;
    }

    return working;
  }

  private CharacterDef PickCharacter(PlayerSave save, BannerDef banner, int rarity, ref bool wasFeatured) {
    string type = (banner.bannerType ?? string.Empty).ToLowerInvariant();
    bool isLimited = type == "limited_character";
    int featuredRarity = Math.Max(1, banner.pity.featuredRarity);

    if (isLimited && rarity == featuredRarity && banner.featuredCharacterIds != null && banner.featuredCharacterIds.Count > 0) {
      string guaranteeKey = $"{banner.id}:featured";
      bool guaranteeActive = SaveMapUtils.GetBool(save.bannerFeaturedGuarantee, guaranteeKey, false);
      bool rollFeatured = guaranteeActive || _rng.NextDouble() <= Math.Max(0f, banner.featuredRateUp);

      if (rollFeatured) {
        var featuredPool = _data.Characters.Values
          .Where(c => c.rarity == rarity && banner.featuredCharacterIds.Contains(c.id))
          .ToList();
        if (featuredPool.Count > 0) {
          wasFeatured = true;
          SaveMapUtils.SetBool(save.bannerFeaturedGuarantee, guaranteeKey, false);
          return featuredPool[_rng.Next(featuredPool.Count)];
        }
      }

      if (banner.carryFeaturedGuarantee) SaveMapUtils.SetBool(save.bannerFeaturedGuarantee, guaranteeKey, true);
      var offBannerPool = _data.Characters.Values
        .Where(c => c.rarity == rarity && (banner.featuredCharacterIds == null || !banner.featuredCharacterIds.Contains(c.id)))
        .ToList();
      if (offBannerPool.Count > 0) return offBannerPool[_rng.Next(offBannerPool.Count)];
    }

    var pool = _data.Characters.Values.Where(c => c.rarity == rarity).ToList();
    if (pool.Count == 0) throw new Exception($"No characters found for rarity {rarity}.");
    return pool[_rng.Next(pool.Count)];
  }

  private void AddUnitToInventory(PlayerSave save, string charId) {
    var owned = save.units.FirstOrDefault(u => u.charId == charId);
    if (owned == null) {
      save.units.Add(new OwnedUnit {
        charId = charId,
        level = 1,
        copies = 1,
        stars = 0,
        coreLevel = 1,
        xp = 0,
      });
    }
    else owned.copies += 1;
  }
}
