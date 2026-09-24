using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// The workshop owns selection and feedback only. Prices, stats and save commits belong to IUpgradeService.
public sealed class UpgradePanelController : MonoBehaviour
{
    private GameObject panel;
    private Text wallet, identity, stats, skills, feedback, pageLabel, upgradeLabel;
    private Button upgrade, previous, next;
    private Button[] roster;
    private Text[] rosterLabels;
    private FungusPortrait portrait;
    private Action onClosed;
    private int page;
    private string selected;
    private UnitUpgradePreview preview;
    private bool submitting;
    private string message = "";
    public string SelectedCharacterId => selected;
    public UnitUpgradePreview Preview => preview;
    public bool IsOpen => panel != null && panel.activeSelf;

    public void Configure(GameObject panel, Text wallet, Text identity, Text stats, Text skills, Text feedback,
        Text pageLabel, Button upgrade, Button previous, Button next, Button close, Button[] roster,
        FungusPortrait portrait, Action onClosed)
    {
        this.panel = panel; this.wallet = wallet; this.identity = identity; this.stats = stats;
        this.skills = skills; this.feedback = feedback; this.pageLabel = pageLabel; this.upgrade = upgrade;
        this.previous = previous; this.next = next; this.roster = roster; this.portrait = portrait; this.onClosed = onClosed;
        rosterLabels = roster.Select(b => b.GetComponentInChildren<Text>()).ToArray();
        upgradeLabel = upgrade.GetComponentInChildren<Text>();
        upgrade.onClick.AddListener(OnUpgradePressed);
        previous.onClick.AddListener(() => { page--; Refresh(); });
        next.onClick.AddListener(() => { page++; Refresh(); });
        close.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    public void Open()
    {
        Game.EnsureInitialized(); panel.SetActive(true); panel.transform.SetAsLastSibling();
        message = "Spend gold to level up; Spores unlock the major ascension milestones.";
        Refresh();
        TutorialManager.I?.SetWorkshopVisible(true);
    }
    public void Close() { panel.SetActive(false); onClosed?.Invoke(); TutorialManager.I?.SetWorkshopVisible(false); }

    private List<OwnedUnit> Owned() => Game.Save.units.Where(u => u != null && Game.Data.Characters.ContainsKey(u.charId))
        .OrderBy(u => u.charId, StringComparer.Ordinal).ToList(); // Levels never reshuffle the selection under a tap.

    public void Select(string id)
    {
        if (!Owned().Any(u => u.charId == id)) return;
        selected = id; message = "Preview the next level before upgrading. Scroll the kit panel for all abilities."; Refresh();
        skills.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1;
    }

    public void OnUpgradePressed()
    {
        if (submitting || preview == null || !preview.CanAfford) return;
        submitting = true; upgrade.interactable = false;
        try
        {
            var result = Game.Upgrades.LevelUp(preview.CharacterId, preview.Level, preview.RulesVersion);
            message = $"Saved: {Game.Data.Characters[result.CharacterId].name} is now level {result.Level}.";
        }
        catch (Exception)
        {
            message = "Upgrade could not be saved or the preview changed. Review the current values and try again.";
        }
        finally { submitting = false; Refresh(); }
    }

    private void Refresh()
    {
        var owned = Owned(); int pages = Math.Max(1, (owned.Count + roster.Length - 1) / roster.Length);
        page = Mathf.Clamp(page, 0, pages - 1);
        if (!owned.Any(u => u.charId == selected)) selected = owned.FirstOrDefault()?.charId;
        wallet.text = $"Gold  {Game.Save.gold}    •    Spores  {Game.Save.spores}";
        feedback.text = message;
        pageLabel.text = $"{page + 1} / {pages}   •   {owned.Count} owned";
        previous.interactable = page > 0; next.interactable = page + 1 < pages;
        for (int i = 0; i < roster.Length; i++)
        {
            var unit = owned.ElementAtOrDefault(page * roster.Length + i);
            var button = roster[i]; button.onClick.RemoveAllListeners(); button.interactable = unit != null;
            rosterLabels[i].text = unit == null ? "—" : $"{(unit.charId == selected ? "> " : "")}{Game.Data.Characters[unit.charId].name}\nLevel {unit.level}";
            if (unit != null) { string id = unit.charId; button.onClick.AddListener(() => Select(id)); }
        }
        preview = null; upgrade.interactable = false; portrait.gameObject.SetActive(selected != null);
        if (selected == null)
        {
            identity.text = "No fighters yet"; stats.text = "Visit Summon to obtain your first fighter.";
            skills.text = ""; upgradeLabel.text = "Select a fighter"; return;
        }
        var definition = Game.Data.Characters[selected];
        try { preview = Game.Upgrades.Preview(selected); }
        catch (Exception)
        {
            identity.text = definition.name; stats.text = "This fighter's progression is unavailable.";
            skills.text = ""; upgradeLabel.text = "Unavailable"; return;
        }
        identity.text = $"{definition.name}\n{definition.rarityTier}  •  {definition.biome}  •  {definition.classArchetype} / {definition.role}\nEvolution {preview.EvolutionStars}/6  •  Level {preview.Level}  •  Current cap {preview.LevelCap}";
        var now = preview.Current; var after = preview.Next;
        string Row(string label, int value, int? nextValue) => nextValue.HasValue ? $"{label}    {value}  →  {nextValue}" : $"{label}    {value}";
        stats.text = (after == null ? "CURRENT STATS" : "CURRENT  →  NEXT LEVEL") + "\nBefore formation bonuses\n\n" +
            string.Join("\n", new[] { Row("HP", now.HP, after?.HP), Row("ATK", now.ATK, after?.ATK),
                Row("DEF", now.DEF, after?.DEF), Row("SPD", now.SPD, after?.SPD), Row("POT", now.POT, after?.POT) });
        var basic = Game.Data.Skills[definition.skills.basic]; var signature = Game.Data.Skills[definition.skills.ult];
        skills.text = $"BASIC · {basic.name}\n{basic.description}\n\nSIGNATURE · {signature.name}\n{signature.energyCost} energy  •  {signature.cooldown} turn cooldown\n{signature.description}\n\nPASSIVE\n" +
            string.Join("\n", definition.passives.Select(p => p.description)) + "\n\nStatus chances are base chances, modified by POT and immunity.";
        portrait.variant = definition.classArchetype == "Tank" ? 0 :
            definition.classArchetype == "Support" || definition.classArchetype == "Mage" || definition.role == "Healer" ? 1 : 2;
        portrait.cap = definition.biome switch {
            "Forest" => new Color(.38f, .68f, .26f), "Wetlands" => new Color(.28f, .68f, .65f),
            "Decay" => new Color(.62f, .38f, .73f), "Tundra" => new Color(.62f, .82f, .95f),
            _ => new Color(.88f, .54f, .25f) };
        portrait.SetVerticesDirty();
        upgradeLabel.text = preview.AtCap ? "Current level cap reached" : preview.RequiresAscension ? $"Ascension  •  {preview.GoldCost} gold + {preview.SporeCost} Spores" : $"Level up  •  {preview.GoldCost} gold";
        upgrade.interactable = preview.CanAfford && !submitting;
        if (!preview.AtCap && !preview.CanAfford)
            if (preview.Gold < preview.GoldCost) feedback.text += $"\nNeed {preview.GoldCost - preview.Gold} more gold.";
        if (!preview.AtCap && preview.Spores < preview.SporeCost) feedback.text += $"\nNeed {preview.SporeCost - preview.Spores} more Spores for this ascension milestone.";
    }
}
