using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manual presentation library for combat keywords/statuses.
/// Each entry points at an editable scene GameObject template. The gameplay
/// keyword system remains the source of truth; this component only visualizes
/// status events already emitted by BattleSim.
/// </summary>
public sealed class KeywordVisualLibrary : MonoBehaviour
{
    public enum Placement
    {
        OnCharacter,
        OnBattleCell
    }

    [Serializable]
    public sealed class Entry
    {
        [Tooltip("Must match the combat keyword/status name, e.g. Burn, Freeze, Root.")]
        public string keyword;
        [Tooltip("An inactive, manually authored GameObject used as the visual template.")]
        public GameObject template;
        public Placement placement = Placement.OnCharacter;
        [Tooltip("Keep the visual alive until the keyword is removed/expired.")]
        public bool persistUntilExpired = true;
        [Tooltip("Allow multiple stacked applications to create multiple visuals.")]
        public bool allowStackedInstances;
    }

    private sealed class ActiveVisual
    {
        public string key;
        public string keyword;
        public GameObject instance;
        public Placement placement;
    }

    [SerializeField] private List<Entry> entries = new();
    [Tooltip("Use the same RectTransform as BattleScreenView.fighterLayer so grid visuals share the exact battle coordinates.")]
    public RectTransform battleLayer;
    private readonly List<ActiveVisual> active = new();

    public IReadOnlyList<Entry> Entries => entries;

    public void Apply(BattleFighterState target, string keyword, IReadOnlyDictionary<string, BattleFighterView> fighters)
    {
        if (target == null || string.IsNullOrWhiteSpace(keyword)) return;
        Entry entry = Find(keyword);
        if (entry == null || entry.template == null) return;
        if (!entry.allowStackedInstances)
            Remove(target.InstanceId, keyword);

        Transform parent = null;
        if (entry.placement == Placement.OnCharacter &&
            fighters != null && fighters.TryGetValue(target.InstanceId, out var fighter))
            parent = fighter.transform;
        else
            parent = battleLayer != null ? battleLayer : transform;

        GameObject visual = Instantiate(entry.template, parent);
        visual.name = "KeywordVisual_" + keyword;
        visual.SetActive(true);

        if (entry.placement == Placement.OnBattleCell)
            PositionOnCell(visual, target.Side, target.Slot);

        active.Add(new ActiveVisual {
            key = target.InstanceId + "|" + Normalize(keyword),
            keyword = keyword,
            instance = visual,
            placement = entry.placement
        });
    }

    public void Remove(string instanceId, string keyword)
    {
        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(keyword)) return;
        string normalized = Normalize(keyword);
        foreach (var visual in active.Where(v => v.key == instanceId + "|" + normalized).ToArray())
        {
            if (visual.instance != null) Destroy(visual.instance);
            active.Remove(visual);
        }
    }

    public void ClearFighter(string instanceId)
    {
        foreach (var visual in active.Where(v => v.key.StartsWith(instanceId + "|", StringComparison.Ordinal)).ToArray())
        {
            if (visual.instance != null) Destroy(visual.instance);
            active.Remove(visual);
        }
    }

    public void ClearAll()
    {
        foreach (var visual in active)
            if (visual.instance != null) Destroy(visual.instance);
        active.Clear();
    }

    public void RefreshCellVisuals(IReadOnlyDictionary<string, BattleFighterView> fighters)
    {
        foreach (var visual in active.Where(v => v.placement == Placement.OnBattleCell && v.instance != null).ToArray())
        {
            var state = fighters.Values.Select(f => f.State)
                .FirstOrDefault(s => s.InstanceId + "|" + Normalize(visual.keyword) == visual.key);
            if (state != null) PositionOnCell(visual.instance, state.Side, state.Slot);
        }
    }

    private Entry Find(string keyword) =>
        entries.FirstOrDefault(e => string.Equals(e.keyword?.Trim(), keyword.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    public static string StatusNameFromEvent(string detail)
    {
        if (string.IsNullOrWhiteSpace(detail)) return detail;
        int separator = detail.IndexOf(':');
        return separator >= 0 ? detail.Substring(separator + 1) : detail;
    }

    private static void PositionOnCell(GameObject visual, TeamSide side, int slot)
    {
        if (visual.transform is RectTransform rect)
            rect.anchoredPosition = BattleScreenView.CellPosition(side, slot);
        else
            visual.transform.localPosition = new Vector3(
                BattleScreenView.CellPosition(side, slot).x,
                BattleScreenView.CellPosition(side, slot).y,
                0);
    }

    private void OnDestroy() => ClearAll();
}
