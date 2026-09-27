using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TeamSceneUiBuilder : RuntimeSceneUiBuilderBase
{
public void Build(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "TeamRoot");
        var controller = EnsureSceneComponent<TeamMenuController>(scene, root.transform);
        var slotBinder = EnsureSceneComponent<TeamRosterSlotBinder>(scene, root.transform);
        var spotlight = EnsureComponent<TutorialSpotlightController>(root);

        var bg = EnsurePanel(root.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Team),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Team is a landscape management screen. Keep the formation and roster as
        // separate columns so neither side has to compete for the same vertical space.
        var shell = EnsurePanel(root.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Team), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(1500f, 820f));

        var title = EnsureLabel(shell.transform, "Lbl_TeamStatus", "Funguy Team",
            34, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor, new Vector2(-655f, 360f), new Vector2(520f, 58f));

        var teamStatus = EnsureLabel(shell.transform, "Lbl_TeamCount", "Team 0/5",
            25, FontStyle.Normal, TextAnchor.MiddleRight, SoftWhite);
        SetRect(teamStatus.rectTransform, CenterAnchor, CenterAnchor, new Vector2(655f, 360f), new Vector2(420f, 58f));

        // LEFT: formation board.
        var formationPanel = EnsurePanel(shell.transform, "Panel_Formation",
            WithAlpha(IdleHuntressTheme.BackgroundFor(UiTone.Team), 0.48f),
            CenterAnchor, CenterAnchor, new Vector2(-385f, 35f), new Vector2(690f, 570f));

        var formationHeader = EnsureLabel(formationPanel.transform, "Lbl_FormationHeader", "FORMATION",
            24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(formationHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 245f), new Vector2(600f, 45f));

        var formationButtons = new List<Button>();
        var formationLabels = new List<Text>();
        for (int i = 0; i < FormationRules.SlotCount; i++)
        {
            int depth = FormationRules.Depth(i);
            float x = 185f - depth * 185f;
            float y = 130f - FormationRules.Lane(i) * 105f + (depth % 2) * 52.5f;
            var button = EnsureButton(formationPanel.transform, $"Btn_FormationSlot{i + 1}", FormationRules.Label(i),
                new Vector2(x, y), new Vector2(205f, 92f),
                IdleHuntressTheme.AccentFor(UiTone.Team), out var label);
            HexBoardVisual.Apply(button.GetComponent<Image>());
            label.fontSize = 17;
            SetRect(label.rectTransform, CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(185f, 78f));
            formationButtons.Add(button);
            formationLabels.Add(label);
        }

        var remove = EnsureButton(formationPanel.transform, "Btn_RemoveSelected", "Remove Selected",
            new Vector2(0f, -235f), new Vector2(250f, 58f),
            new Color(0.70f, 0.10f, 0.12f, 1f), out var removeLabel);
        controller.ConfigureFormation(formationButtons.ToArray(), formationLabels.ToArray(), remove);

        var hint = EnsureLabel(formationPanel.transform, "Lbl_Hint",
            "Select a position, then choose a fighter. Select two positions to swap or move.",
            18, FontStyle.Italic, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(hint.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -155f), new Vector2(610f, 70f));

        // RIGHT: owned character selection.
        var rosterPanel = EnsurePanel(shell.transform, "Panel_Roster",
            WithAlpha(IdleHuntressTheme.BackgroundFor(UiTone.Team), 0.42f),
            CenterAnchor, CenterAnchor, new Vector2(385f, 35f), new Vector2(690f, 570f));

        var rosterHeader = EnsureLabel(rosterPanel.transform, "Lbl_RosterHeader", "CHARACTER SELECTION",
            24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(rosterHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 245f), new Vector2(620f, 45f));

        var roster = EnsureLabel(rosterPanel.transform, "Lbl_Roster",
            "Choose a fighter to place in the selected position.",
            18, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(roster.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 205f), new Vector2(620f, 50f));

        var slotStrip = EnsurePanel(rosterPanel.transform, "Panel_RosterSlots",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Team), 0.72f),
            CenterAnchor, CenterAnchor, new Vector2(0f, 78f), new Vector2(640f, 205f));

        var slotButtons = new List<Button>();
        var slotLabels = new List<Text>();
        for (int i = 0; i < 5; i++)
        {
            float x = -256f + i * 128f;
            var slot = EnsureButton(slotStrip.transform, $"Btn_RosterSlot{i + 1}", $"Fighter {i + 1}",
                new Vector2(x, 0f), new Vector2(116f, 174f),
                IdleHuntressTheme.AccentFor(UiTone.Team), out var slotLabel, $"Lbl_RosterSlot{i + 1}");
            slotLabel.fontSize = 15;
            slotButtons.Add(slot);
            slotLabels.Add(slotLabel);
        }

        var previous = EnsureButton(rosterPanel.transform, "Btn_RosterPrevious", "Previous",
            new Vector2(-165f, -85f), new Vector2(230f, 58f),
            IdleHuntressTheme.AccentFor(UiTone.Team), out var previousLabel);
        var next = EnsureButton(rosterPanel.transform, "Btn_RosterNext", "Next",
            new Vector2(165f, -85f), new Vector2(230f, 58f),
            IdleHuntressTheme.AccentFor(UiTone.Team), out var nextLabel);
        var pageLabel = EnsureLabel(rosterPanel.transform, "Lbl_RosterPage", "Roster",
            17, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
        SetRect(pageLabel.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -85f), new Vector2(100f, 58f));
        slotBinder.ConfigurePaging(previous, next, pageLabel);

        // BOTTOM-RIGHT: primary team actions are kept in one evenly spaced row.
        var actions = EnsurePanel(shell.transform, "Panel_TeamActions",
            WithAlpha(IdleHuntressTheme.BackgroundFor(UiTone.Team), 0.50f),
            CenterAnchor, CenterAnchor, new Vector2(385f, -310f), new Vector2(690f, 115f));

        var autoFill = EnsureButton(actions.transform, "Btn_AutoFill", "Auto Fill",
            new Vector2(-255f, 0f), new Vector2(145f, 68f),
            new Color(0.00f, 0.47f, 0.45f, 1f), out var autoFillLabel);
        var clearTeam = EnsureButton(actions.transform, "Btn_ClearTeam", "Clear Team",
            new Vector2(-85f, 0f), new Vector2(145f, 68f),
            new Color(0.78f, 0.34f, 0.05f, 1f), out var clearLabel);
        var equipmentButton = EnsureButton(actions.transform, "Btn_Equipment", "Equipment",
            new Vector2(85f, 0f), new Vector2(145f, 68f),
            new Color(0.10f, 0.35f, 0.68f, 1f), out var equipmentButtonLabel);
        var startBattle = EnsureButton(actions.transform, "Btn_StartBattle", "Start Battle",
            new Vector2(255f, 0f), new Vector2(145f, 68f),
            new Color(0.12f, 0.52f, 0.25f, 1f), out var startBattleLabel);

        autoFill.onClick.RemoveAllListeners();
        autoFill.onClick.AddListener(controller.AutoFillTeam);
        clearTeam.onClick.RemoveAllListeners();
        clearTeam.onClick.AddListener(controller.OnClearTeamPressed);
        startBattle.onClick.RemoveAllListeners();
        startBattle.onClick.AddListener(controller.OnStartBattlePressed);

        var back = EnsureButton(shell.transform, "Btn_Back", "Back",
            new Vector2(-385f, -310f), new Vector2(170f, 68f),
            new Color(0.28f, 0.34f, 0.39f, 1f), out var backLabel);
        back.onClick.RemoveAllListeners();
        back.onClick.AddListener(controller.OnBackPressed);

        var upgrades = EnsureButton(shell.transform, "Btn_Upgrades", "Upgrade Fighters",
            new Vector2(-90f, -310f), new Vector2(220f, 68f),
            IdleHuntressTheme.AccentFor(UiTone.Team), out var upgradesLabel);
        var workshop = UpgradePanelView.Create(root.transform, slotBinder.RebindSlots);
        upgrades.onClick.RemoveAllListeners();
        upgrades.onClick.AddListener(workshop.Open);

        // Equipment remains an overlay owned by the existing EquipmentPanelController.
        var equipmentRoot = EnsurePanel(root.transform, "EquipmentPanel",
            new Color(.05f,.08f,.1f,.98f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var equipmentCanvas = EnsureComponent<Canvas>(equipmentRoot);
        equipmentCanvas.enabled = true;
        equipmentCanvas.overrideSorting = true;
        equipmentCanvas.sortingOrder = 100;
        EnsureComponent<GraphicRaycaster>(equipmentRoot).enabled = true;

        var equipmentCard = EnsurePanel(equipmentRoot.transform, "Panel_EquipmentCard",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Team), .99f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(900f, 1450f));
        var equipmentTitle = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentTitle", "Equipment",
            42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(equipmentTitle.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, 620), new Vector2(820, 70));
        var equipmentCharacter = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentCharacter", "Character",
            40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(equipmentCharacter.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, 555), new Vector2(820, 80));
        var equipmentIdentity = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentIdentity", "Class • Role\nBiome",
            24, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(equipmentIdentity.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, 495), new Vector2(820, 70));

        var choiceHeader = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentChoices", "AVAILABLE EQUIPMENT",
            22, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(choiceHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(-290, 300), new Vector2(250, 55));
        var choiceDropdowns=new Dropdown[4];
        for(int i=0;i<4;i++){
            var dropdown=EnsureDropdown(equipmentCard.transform,$"Dropdown_EquipmentChoice{i+1}",
                $"{EquipmentService.Slots[i].ToUpperInvariant()} • No equipment available",
                new Vector2(-290,210-i*125),new Vector2(250,105),
                new Color(.10f,.25f,.34f,1f),out var dropdownLabel);
            dropdownLabel.fontSize=17;
            choiceDropdowns[i]=dropdown;
        }

        var centerHeader = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentSlots", "EQUIPMENT SLOTS",
            22, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(centerHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, 300), new Vector2(260, 55));
        var gearButtons=new Button[4]; var gearLabels=new Text[4];
        for(int i=0;i<4;i++){ var b=EnsureButton(equipmentCard.transform,$"Btn_EquipmentSlot{i+1}",EquipmentService.Slots[i].ToUpperInvariant(),new Vector2(0,210-i*125),new Vector2(250,105),new Color(.16f,.34f,.22f,1f),out var l); l.fontSize=17; gearButtons[i]=b; gearLabels[i]=l; }

        var statsHeader = EnsureLabel(equipmentCard.transform, "Lbl_CharacterStats", "CHARACTER STATS",
            22, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(statsHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(290, 300), new Vector2(250, 55));
        var statsLabel = EnsureLabel(equipmentCard.transform, "Lbl_CharacterStatsValues",
            "HP\n0\n\nATK\n0\n\nDEF\n0\n\nSPD\n0\n\nPOT\n0",
            22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(statsLabel.rectTransform, CenterAnchor, CenterAnchor, new Vector2(290, 0), new Vector2(250, 560));

        var equipmentDetails = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentDetails", "",
            20, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(equipmentDetails.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, -320), new Vector2(820, 120));
        var equipmentUpgrade = EnsureButton(equipmentCard.transform, "Btn_EquipmentUpgrade", "Equip Equipment",
            new Vector2(190, -390), new Vector2(300, 80), new Color(.12f, .52f, .25f, 1f), out var equipmentUpgradeLabel);

        var characterStrip = EnsurePanel(equipmentCard.transform, "Panel_EquipmentCharacters",
            new Color(.05f,.08f,.10f,.88f), CenterAnchor, CenterAnchor, new Vector2(0, 410), new Vector2(820, 82));
        var characterViewport = EnsureChild(characterStrip.transform, "Viewport");
        var characterViewportRt = EnsureRectTransform(characterViewport);
        SetRect(characterViewportRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        EnsureComponent<RectMask2D>(characterViewport);
        var characterContent = EnsureChild(characterViewport.transform, "Content");
        var characterContentRt = EnsureRectTransform(characterContent);
        characterContentRt.anchorMin = new Vector2(0, .5f); characterContentRt.anchorMax = new Vector2(0, .5f);
        characterContentRt.pivot = new Vector2(0, .5f); characterContentRt.anchoredPosition = Vector2.zero;
        var characterScroll = EnsureComponent<ScrollRect>(characterStrip);
        characterScroll.horizontal = true; characterScroll.vertical = false; characterScroll.movementType = ScrollRect.MovementType.Clamped;
        characterScroll.viewport = characterViewportRt; characterScroll.content = characterContentRt;
        var characterHeader = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentCharacterList", "SELECT CHARACTER",
            18, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(characterHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(-360, 458), new Vector2(260, 45));
        var equipmentBack=EnsureButton(equipmentCard.transform,"Btn_EquipmentBack","Back to Formation",
            new Vector2(-190,-390),new Vector2(300,80),new Color(.28f,.34f,.39f,1f),out var equipmentBackLabel);

        gearLabels.ToList().ForEach(x => x.color = Color.white);
        equipmentUpgradeLabel.color = Color.white;
        equipmentBackLabel.color = Color.white;
        var equipmentController=EnsureSceneComponent<EquipmentPanelController>(scene,equipmentRoot.transform);
        equipmentController.Initialize(equipmentRoot,equipmentCharacter,equipmentIdentity,statsLabel,equipmentDetails,characterContentRt,choiceDropdowns,gearButtons,gearLabels,equipmentUpgrade,equipmentUpgradeLabel,equipmentBack);
        equipmentUpgrade.onClick.AddListener(equipmentController.UpgradeSelectedEquipment);
        for(int i=0;i<4;i++){ int index=i; gearButtons[i].onClick.AddListener(()=>equipmentController.EquipFromSlot(index)); }
        equipmentBack.onClick.AddListener(equipmentController.BackToFormation);
        equipmentRoot.SetActive(false);
        equipmentButton.onClick.RemoveAllListeners();
        equipmentButton.onClick.AddListener(equipmentController.Open);

        ConfigureSkin(root, UiTone.Team,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>(), formationPanel.GetComponent<Image>(), rosterPanel.GetComponent<Image>(), slotStrip.GetComponent<Image>(), actions.GetComponent<Image>() },
            new[] { autoFill.GetComponent<Image>(), clearTeam.GetComponent<Image>(), equipmentButton.GetComponent<Image>(), startBattle.GetComponent<Image>(), back.GetComponent<Image>(), upgrades.GetComponent<Image>(), remove.GetComponent<Image>() }
                .Concat(slotButtons.Concat(formationButtons).Concat(new[] { previous, next }).Select(b => b.GetComponent<Image>())).ToArray(),
            new[] { autoFill, clearTeam, equipmentButton, startBattle, back, upgrades, remove, previous, next }
                .Concat(slotButtons).Concat(formationButtons).ToArray(),
            new[] { title, formationHeader, rosterHeader },
            new[] { teamStatus, roster, hint, autoFillLabel, clearLabel, equipmentButtonLabel, startBattleLabel, backLabel, upgradesLabel, removeLabel }
                .Concat(slotLabels).Concat(formationLabels).Concat(new[] { previousLabel, nextLabel, pageLabel }).ToArray());

        SetAccessibleButton(autoFill, autoFillLabel, new Color(0.00f, 0.47f, 0.45f, 1f));
        SetAccessibleButton(clearTeam, clearLabel, new Color(0.78f, 0.34f, 0.05f, 1f));
        SetAccessibleButton(equipmentButton, equipmentButtonLabel, new Color(0.10f, 0.35f, 0.68f, 1f));
        SetAccessibleButton(startBattle, startBattleLabel, new Color(0.12f, 0.52f, 0.25f, 1f));
        SetAccessibleButton(back, backLabel, new Color(0.28f, 0.34f, 0.39f, 1f));
        SetAccessibleButton(upgrades, upgradesLabel, IdleHuntressTheme.AccentFor(UiTone.Team));
        SetAccessibleButton(remove, removeLabel, new Color(0.70f, 0.10f, 0.12f, 1f));

        spotlight.Configure(
            null,
            new[]
            {
                Target(TutorialStep.GoToTeamBuilder, autoFill, "Auto-fill to place your first formation."),
                Target(TutorialStep.PlaceFirstUnit, formationButtons.FirstOrDefault(), "Tap a position, then a fighter. Two positions swap or move."),
                Target(TutorialStep.StartFirstBattle, startBattle, "When ready, start your first battle."),
                Target(TutorialStep.PlaceTankFrontDpsBack, upgrades, "Preview a fighter's next level and gold cost in the workshop."),
            });
    }
}