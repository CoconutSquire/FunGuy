using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IdleGenerationPreview
{
    public int campaignDepth;
    public int gold;
    public int equipmentCount;
    public double goldPerHour;
    public double equipmentPerHour;
    public TimeSpan elapsed;
}

public sealed class IdleGenerationResult : IdleGenerationPreview
{
    public readonly List<GearSlotState> equipment = new();
}

/// <summary>
/// Offline/idle rewards are calculated from persisted UTC time, so they continue
/// accruing while the game is closed. Rewards are only committed when claimed.
/// </summary>
public sealed class IdleGenerationService
{
    public const double MaxStoredHours = 24d;
    private readonly GameData data;
    private readonly EquipmentService equipmentService;
    private readonly System.Random rng = new();
    private readonly GameBalanceSettings settings;

    public IdleGenerationService(GameData data, EquipmentService equipmentService)
    {
        this.data = data;
        this.equipmentService = equipmentService;
        this.settings = Resources.Load<GameBalanceSettings>("GameData/GameBalanceSettings");
    }

    public IdleGenerationPreview Preview(PlayerSave save = null)
    {
        save ??= Game.Save ?? SaveSystem.LoadOrNew();
        var elapsed = GetElapsed(save);
        var depth = CampaignDepth(save);
        var goldRate = GoldPerHour(depth);
        var equipmentRate = EquipmentPerHour(depth);
        var totalGoldProgress = save.idleGoldProgress + elapsed.TotalHours * goldRate;
        var totalEquipmentProgress = save.idleEquipmentProgress + elapsed.TotalHours * equipmentRate;
        return new IdleGenerationPreview
        {
            campaignDepth = depth,
            elapsed = elapsed,
            goldPerHour = goldRate,
            equipmentPerHour = equipmentRate,
            gold = (int)Math.Floor(totalGoldProgress),
            equipmentCount = (int)Math.Floor(totalEquipmentProgress),
        };
    }

    public IdleGenerationResult Claim(PlayerSave save = null)
    {
        save ??= Game.Save ?? SaveSystem.LoadOrNew();
        var preview = Preview(save);
        var result = new IdleGenerationResult
        {
            campaignDepth = preview.campaignDepth,
            elapsed = preview.elapsed,
            goldPerHour = preview.goldPerHour,
            equipmentPerHour = preview.equipmentPerHour,
            gold = preview.gold,
            equipmentCount = preview.equipmentCount,
        };

        // Convert the preview into a single claim transaction. Everything earned
        // up to this moment is granted first, then the fractional progress and
        // claim timestamp are reset so the same rewards cannot be claimed twice.
        save.gold = Math.Max(0, save.gold + result.gold);

        var totalGoldProgress = save.idleGoldProgress + preview.elapsed.TotalHours * preview.goldPerHour;
        var totalEquipmentProgress = save.idleEquipmentProgress + preview.elapsed.TotalHours * preview.equipmentPerHour;
        var itemCount = (int)Math.Floor(totalEquipmentProgress);

        save.idleGoldProgress = Math.Max(0d, totalGoldProgress - result.gold);
        save.idleEquipmentProgress = Math.Max(0d, totalEquipmentProgress - itemCount);

        if (itemCount > 0)
        {
            if (equipmentService == null)
                throw new InvalidOperationException("Idle equipment rewards cannot be claimed because EquipmentService is unavailable.");

            int rarity = Math.Clamp(
                (settings?.idleBaseRarity ?? 1) +
                preview.campaignDepth / Math.Max(1, settings?.idleRarityStagesPerTier ?? 10),
                1, 6);

            for (int i = 0; i < itemCount; i++)
            {
                string slot = new[] { "cap", "stipe", "mycelium", "symbiote" }[rng.Next(4)];
                var item = equipmentService.Acquire(slot, rarity, 1);
                if (item == null)
                    throw new InvalidOperationException("Idle equipment generation returned no item.");

                equipmentService.AddToSave(save, item);
                result.equipment.Add(item);
            }
        }

        // Reset the accumulation clock only after all rewards have been added
        // to the same PlayerSave that is persisted below.
        save.idleLastClaimedUtc = DateTime.UtcNow.ToString("o");
        SaveSystem.Save(save);
        Game.Save = save;
        return result;
    }

    private TimeSpan GetElapsed(PlayerSave save)
    {
        if (!DateTime.TryParse(save.idleLastClaimedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var last))
        {
            last = DateTime.UtcNow;
            save.idleLastClaimedUtc = last.ToString("o");
        }
        var elapsed = DateTime.UtcNow - last.ToUniversalTime();
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
        return elapsed > TimeSpan.FromHours(settings?.maxStoredHours ?? MaxStoredHours) ? TimeSpan.FromHours(MaxStoredHours) : elapsed;
    }

    private int CampaignDepth(PlayerSave save)
    {
        if (data?.Stages == null || data.Stages.Count == 0) return save.clearedStages?.Count ?? 0;
        // Orders restart at 1 for every chapter. Idle progression must therefore
        // use the same chapter-aware ordering as campaign unlocks.
        var stages = data.Stages.Values.OrderBy(s => s.chapter).ThenBy(s => s.order).ToList();
        int depth = 0;
        foreach (var stage in stages)
        {
            if (stage == null || string.IsNullOrWhiteSpace(stage.id) || !(save.clearedStages?.Contains(stage.id) ?? false)) break;
            depth++;
        }
        return depth;
    }

    private double GoldPerHour(int depth) => (settings?.baseGoldPerHour ?? 50d) + depth * (settings?.goldPerStagePerHour ?? 10d);
    private double EquipmentPerHour(int depth) => (settings?.baseEquipmentPerHour ?? 0.10d) + depth * (settings?.equipmentPerStagePerHour ?? 0.02d);
}
