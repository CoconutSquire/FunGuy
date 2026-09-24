using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TeamMenuController : MonoBehaviour
{
    private int maxTeamSize => Team.Capacity;
    [SerializeField] private string battleSceneName = "Battle";
    [SerializeField] private Text teamStatusLabel;
    [SerializeField] private Text rosterLabel;
    [SerializeField] private Text hintLabel;
    [SerializeField] private bool autoSeedFromSaveIfTeamEmpty = true;

    private PlayerSave Save => Game.Save ??= SaveSystem.LoadOrNew();
    private ITeamService Team { get { Game.EnsureInitialized(); return Game.Team; } }
    private int selectedSlot = -1;
    private Text[] formationLabels;
    private Button[] formationButtons;
    private Button removeSlotButton;

    private static readonly Color FormationEmptyColor = new Color(0.16f, 0.19f, 0.23f, 1f);
    private static readonly Color FormationOccupiedColor = new Color(0.16f, 0.48f, 0.34f, 1f);
    private static readonly Color FormationSelectedColor = new Color(0.76f, 0.58f, 0.16f, 1f);

    public void ConfigureFormation(Button[] buttons, Text[] labels, Button remove)
    {
        formationButtons = buttons;
        formationLabels = labels;
        removeSlotButton = remove;
        for (int i = 0; i < buttons.Length; i++)
        {
            int slot = i;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => OnFormationSlotPressed(slot));
        }
        remove.onClick.RemoveAllListeners();
        remove.onClick.AddListener(OnRemoveSelectedPressed);
        RefreshStatus();
    }

    public void OnFormationSlotPressed(int slot)
    {
        FormationRules.ValidateSlot(slot);
        if (selectedSlot < 0) selectedSlot = slot;
        else
        {
            Team.SwapSlots(selectedSlot, slot);
            selectedSlot = -1;
        }
        RefreshStatus();
    }

    public void OnRemoveSelectedPressed()
    {
        var selected = Team.GetFormation().FirstOrDefault(p => p.slot == selectedSlot);
        if (selected != null) Team.Remove(selected.charId);
        selectedSlot = -1;
        RefreshStatus();
    }

    private void Start()
    {
        if (autoSeedFromSaveIfTeamEmpty && !Save.tutorialCompleted && Save.activeTeam.Count == 0 && Save.units.Count > 0)
        {
            AutoFillTeam();
        }
        RefreshStatus();
    }

    public void AddUnitToTeam(string charId)
    {
        if (!Team.Add(charId)) return;
        AdvanceTutorialOnTeamPlacement();
        RefreshStatus();
    }

    public void RemoveUnitFromTeam(string charId)
    {
        if (!Team.Remove(charId)) return;
        RefreshStatus();
    }

    public void ToggleUnitInTeam(string charId)
    {
        if (string.IsNullOrWhiteSpace(charId)) return;
        if (selectedSlot >= 0)
        {
            bool placed = Team.Place(charId, selectedSlot);
            if (placed) AdvanceTutorialOnTeamPlacement();
            selectedSlot = -1;
            RefreshStatus();
            if (!placed && !Save.activeTeam.Contains(charId) && Save.activeTeam.Count >= maxTeamSize && hintLabel != null)
                hintLabel.text = "Team is full. Choose an occupied hex to replace a fighter, or remove one first.";
            return;
        }
        if (Save.activeTeam.Contains(charId)) RemoveUnitFromTeam(charId);
        else AddUnitToTeam(charId);
    }

    public void AddUnitByRosterIndex(int rosterIndex)
    {
        var ranked = RankedOwnedUnits();
        if (rosterIndex < 0 || rosterIndex >= ranked.Count) return;
        AddUnitToTeam(ranked[rosterIndex].charId);
    }

    public void RemoveUnitByTeamIndex(int teamIndex)
    {
        if (teamIndex < 0 || teamIndex >= Save.activeTeam.Count) return;
        RemoveUnitFromTeam(Save.activeTeam[teamIndex]);
    }

    public void AutoFillTeam()
    {
        selectedSlot = -1;
        Team.AutoFill();
        AdvanceTutorialOnTeamPlacement();
        RefreshStatus();
    }

    public void OnClearTeamPressed()
    {
        selectedSlot = -1;
        Team.Clear();
        RefreshStatus();
    }

    public void OnStartBattlePressed()
    {
        if (Save.activeTeam.Count == 0)
        {
            if (hintLabel != null) hintLabel.text = "Place at least one fighter before starting a battle.";
            return;
        }

        if (TutorialManager.I != null && !Save.tutorialCompleted)
        {
            var step = (TutorialStep)Save.tutorialStep;
            if (step == TutorialStep.GoToTeamBuilder || step == TutorialStep.PlaceFirstUnit)
            {
                TutorialManager.I.GoToStep(TutorialStep.StartFirstBattle);
                return;
            }
        }

        SceneManager.LoadScene(battleSceneName);
    }

    public void OnBackPressed()
    {
        SceneManager.LoadScene("Home");
    }

    private void AdvanceTutorialOnTeamPlacement()
    {
        if (TutorialManager.I == null || Save.tutorialCompleted) return;
        if ((TutorialStep)Save.tutorialStep == TutorialStep.GoToTeamBuilder)
        {
            TutorialManager.I.GoToStep(TutorialStep.PlaceFirstUnit);
        }
    }

    private int GetRarity(string charId)
    {
        if (Game.Data == null || Game.Data.Characters == null) return 0;
        return Game.Data.Characters.TryGetValue(charId, out var c) ? c.rarity : 0;
    }

    private void EnsureData()
    {
        Game.EnsureInitialized();
    }

    private void RefreshStatus()
    {
        EnsureData();

        var formation = Team.GetFormation();
        if (formationLabels != null)
            for (int i = 0; i < formationLabels.Length; i++)
            {
                var placement = formation.FirstOrDefault(p => p.slot == i);
                string label = FormationRules.Label(i);
                if (selectedSlot == i) label = $"> {label} <";
                formationLabels[i].text = $"{label}\n{(placement == null ? "Empty" : ResolveName(placement.charId))}";
                ApplyFormationSlotColor(i, placement != null, selectedSlot == i);
            }
        if (removeSlotButton != null) removeSlotButton.interactable = formation.Any(p => p.slot == selectedSlot);

        if (teamStatusLabel != null)
        {
            teamStatusLabel.text = $"Team {formation.Count}/{maxTeamSize} · {FormationRules.SlotCount} hex spaces\nRear     /     Middle     /     Front → Enemy";
        }

        if (rosterLabel != null)
        {
            rosterLabel.text = "Choose any hex. Empty spaces stay empty.\nNormal attacks target the nearest occupied depth; rear attacks bypass it.";
        }

        if (hintLabel != null)
        {
            hintLabel.text = selectedSlot < 0
                ? "Tap a position, then a fighter to place them.\nTap two positions to swap or move."
                : $"Selected: {FormationRules.Label(selectedSlot)}.\nChoose a fighter, another position, or Remove.";
        }
    }


    private void ApplyFormationSlotColor(int slot, bool occupied, bool selected)
    {
        if (formationButtons == null || slot < 0 || slot >= formationButtons.Length || formationButtons[slot] == null) return;

        var button = formationButtons[slot];
        var colors = button.colors;
        Color baseColor = selected ? FormationSelectedColor : (occupied ? FormationOccupiedColor : FormationEmptyColor);
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.12f);
        colors.selectedColor = baseColor;
        colors.disabledColor = Color.Lerp(baseColor, Color.black, 0.35f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private List<OwnedUnit> RankedOwnedUnits()
    {
        EnsureData();
        return Save.units
            .OrderByDescending(u => GetRarity(u.charId))
            .ThenByDescending(u => u.level)
            .ThenBy(u => u.charId, System.StringComparer.Ordinal)
            .ToList();
    }

    private string ResolveName(string charId)
    {
        return Game.Data.Characters.TryGetValue(charId, out var c) ? c.name : charId;
    }
}
