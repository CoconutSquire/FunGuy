using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RuntimeSceneUiBootstrap
{
    private static Font _cachedFont;
    private static bool _registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { _registered = false; _cachedFont = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (_registered) return;
        _registered = true;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (scene.name != "Boot" && scene.name != "Home" && scene.name != "Summon" && scene.name != "Team" &&
            scene.name != "Battle" && scene.name != "Tutorial" && scene.name != "Options") return;

        EnsureEventSystem(scene);

        if (string.Equals(scene.name, "Boot", StringComparison.OrdinalIgnoreCase)) return;

        Game.EnsureInitialized();
        var canvas = EnsureCanvas(scene);

        switch (scene.name)
        {
            case "Home":
                EnsureHomeScene(scene, canvas);
                break;
            case "Summon":
                EnsureSummonScene(scene, canvas);
                break;
            case "Team":
                EnsureTeamScene(scene, canvas);
                break;
            case "Battle":
                EnsureBattleScene(scene, canvas);
                break;
            case "Tutorial":
                EnsureWelcomeScene(scene, canvas);
                break;
            case "Options":
                EnsureOptionsScene(scene, canvas);
                break;
        }

        if (!Game.Save.tutorialCompleted) EnsureTutorialScene(scene, canvas);
        LandscapeMenuLayout.Apply(scene, canvas);

        foreach (var binder in FindInScene<UiPrefabBlueprintBinder>(scene))
        {
            if (binder == null) continue;
            binder.AutoBindCommonReferences();
        }
    }

    private static void EnsureWelcomeScene(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "TutorialRoot");
        var art = new GameObject("WelcomeArt", typeof(RectTransform), typeof(RawImage));
        art.transform.SetParent(root.transform, false);
        var rect = (RectTransform)art.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = art.GetComponent<RawImage>(); image.texture = Resources.Load<Texture2D>("Presentation/kitchen-battlefield-v1"); image.raycastTarget = false;
        EnsurePanel(root.transform, "WelcomeShade", new Color(0, 0, 0, .55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var title = EnsureLabel(root.transform, "WelcomeTitle", "FUNGUY'S", 72, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1, .85f, .5f));
        title.rectTransform.anchoredPosition = new Vector2(0, 80); title.rectTransform.sizeDelta = new Vector2(1000, 140);
        var detail = EnsureLabel(root.transform, "WelcomeDetail", "Build your team. Choose your formation.\nTime your skills.", 32, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
        detail.rectTransform.anchoredPosition = new Vector2(0, -70); detail.rectTransform.sizeDelta = new Vector2(1100, 160);
    }

    private static void EnsureHomeScene(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "HomeRoot");
        var controller = EnsureSceneComponent<HomeMenuController>(scene, root.transform);
        var spotlight = EnsureComponent<TutorialSpotlightController>(root);

        // The authored Home scene already contains the intended home artwork.
        // Keep that scene background visible instead of covering it with a runtime-generated color panel.
        var bg = FindInScene<Image>(scene).FirstOrDefault(x => x != null && x.gameObject.name == "Background");
        if (bg != null)
        {
            // The authored artwork is the visual background. Do not run it through the
            // theme skin: that would replace its sprite color with the flat Home palette.
            bg.raycastTarget = false;
            bg.transform.SetAsFirstSibling();
        }
        else
        {
            // Fallback for scenes created without the authored background.
            bg = EnsurePanel(root.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Home),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).GetComponent<Image>();
        }
        var shell = EnsurePanel(root.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Home), 0.82f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(900f, 1520f));

        var title = EnsureLabel(shell.transform, "Lbl_Welcome",
            "Welcome, Commander. Build your squad and clear the frontier.",
            42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 600f), new Vector2(820f, 150f));

        var stats = EnsureLabel(shell.transform, "Lbl_AccountStats",
            "Lv.1  Gold:100  Spores:50", 30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(stats.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 500f), new Vector2(820f, 100f));

        var start = EnsureButton(shell.transform, "Btn_Start", "Start", new Vector2(0f, 320f), new Vector2(560f, 108f), IdleHuntressTheme.AccentFor(UiTone.Home), out var startLabel);
        var summon = EnsureButton(shell.transform, "Btn_Summon", "Summon", new Vector2(0f, 190f), new Vector2(560f, 108f), IdleHuntressTheme.AccentFor(UiTone.Home), out var summonLabel);
        var team = EnsureButton(shell.transform, "Btn_Team", "Funguy", new Vector2(0f, 60f), new Vector2(560f, 108f), IdleHuntressTheme.AccentFor(UiTone.Home), out var teamLabel);
        var battle = EnsureButton(shell.transform, "Btn_Battle", "Battle", new Vector2(0f, -70f), new Vector2(560f, 108f), IdleHuntressTheme.AccentFor(UiTone.Home), out var battleLabel);
        var options = EnsureButton(shell.transform, "Btn_Options", "Options", new Vector2(0f, -200f), new Vector2(560f, 108f), IdleHuntressTheme.AccentFor(UiTone.Home), out var optionsLabel);

        var hint = EnsureLabel(shell.transform, "Lbl_TutorialHint", string.Empty, 24, FontStyle.Italic, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(hint.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -360f), new Vector2(780f, 140f));

        ConfigureSkin(root, UiTone.Home,
            Array.Empty<Image>(),
            new[] { shell.GetComponent<Image>() },
            new[] { start.GetComponent<Image>(), summon.GetComponent<Image>(), team.GetComponent<Image>(), battle.GetComponent<Image>(), options.GetComponent<Image>() },
            new[] { start, summon, team, battle, options },
            new[] { title },
            new[] { stats, hint, startLabel, summonLabel, teamLabel, battleLabel, optionsLabel });

        spotlight.Configure(
            hint,
            new[]
            {
                Target(TutorialStep.Welcome, start, "Tutorial begins automatically for new players."),
            });

        // Keep the home controller on root so binder can wire all local controls.
        var campaign = CampaignPanelController.Create(root.transform);
        controller.OpenCampaign = campaign.Open;
        battleLabel.text = "Campaign";
        team.onClick.RemoveAllListeners();
        team.onClick.AddListener(controller.OnFunguyPressed);
        BuildFunguyRosterOverlay(scene, canvas);
    }

    private static void BuildFunguyRosterOverlay(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "FunguyRosterRoot");
        var overlay = EnsurePanel(root.transform, "FunguyRosterOverlay", new Color(.035f,.055f,.065f,.97f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var controller = EnsureSceneComponent<FunguyRosterController>(scene, root.transform);
        var title = EnsureLabel(overlay.transform, "Title", "FUNGUY", 44, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, 430), new Vector2(1600, 90));
        var listPanel = EnsurePanel(overlay.transform, "RosterPanel", new Color(.07f,.11f,.13f,1f), CenterAnchor, CenterAnchor, new Vector2(-560,-10), new Vector2(520,820));
        var listHeader = EnsureLabel(listPanel.transform, "RosterHeader", "CHARACTERS", 26, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(listHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0,370), new Vector2(460,60));

        var scrollGo = EnsureChild(listPanel.transform, "ScrollView");
        var scrollRectTransform = EnsureRectTransform(scrollGo);
        SetRect(scrollRectTransform, CenterAnchor, CenterAnchor, new Vector2(0,-20), new Vector2(480,700));
        var scrollImage = EnsureComponent<Image>(scrollGo); scrollImage.color = new Color(0,0,0,.12f); scrollImage.raycastTarget = true;
        var scroll = EnsureComponent<ScrollRect>(scrollGo); scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = true; scroll.scrollSensitivity = 35f;
        var viewport = EnsureChild(scrollGo, "Viewport");
        var viewportRt = EnsureRectTransform(viewport); viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one; viewportRt.offsetMin = Vector2.zero; viewportRt.offsetMax = Vector2.zero;
        var maskImage = EnsureComponent<Image>(viewport); maskImage.color = new Color(1,1,1,1); maskImage.raycastTarget = true;
        var mask = EnsureComponent<Mask>(viewport); mask.showMaskGraphic = false;
        var contentGo = EnsureChild(viewport, "Content");
        var contentRt = EnsureRectTransform(contentGo); contentRt.anchorMin = new Vector2(0,1); contentRt.anchorMax = new Vector2(1,1); contentRt.pivot = new Vector2(.5f,1); contentRt.anchoredPosition = Vector2.zero; contentRt.sizeDelta = new Vector2(0,600);
        scroll.viewport = viewportRt; scroll.content = contentRt;

        var detail = EnsurePanel(overlay.transform, "DetailPanel", new Color(.07f,.11f,.13f,1f), CenterAnchor, CenterAnchor, new Vector2(430,-10), new Vector2(1050,820));
        var name = EnsureLabel(detail.transform, "CharacterName", "Select a Funguy", 42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(name.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0,350), new Vector2(900,80));
        var identity = EnsureLabel(detail.transform, "Identity", "", 24, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(identity.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0,270), new Vector2(900,100));
        var statsHeader = EnsureLabel(detail.transform, "StatsHeader", "STATS", 25, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(statsHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(-250,160), new Vector2(380,60));
        var stats = EnsureLabel(detail.transform, "Stats", "", 26, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        SetRect(stats.rectTransform, CenterAnchor, CenterAnchor, new Vector2(-250,-30), new Vector2(380,350));
        var gearHeader = EnsureLabel(detail.transform, "EquipmentHeader", "EQUIPMENT", 25, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(gearHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(260,160), new Vector2(480,60));
        var equipment = EnsureLabel(detail.transform, "Equipment", "", 22, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        SetRect(equipment.rectTransform, CenterAnchor, CenterAnchor, new Vector2(260,-30), new Vector2(480,350));
        var back = EnsureButton(overlay.transform, "Back", "Back", new Vector2(0,-445), new Vector2(440,80), new Color(.28f,.32f,.35f,1f), out var backLabel);
        backLabel.color = Color.white;
        controller.Initialize(overlay, contentRt, name, identity, stats, equipment, back);
        overlay.SetActive(false);
    }

    private static GameObject EnsureChild(GameObject scrollGo, string v)
    {
        throw new NotImplementedException();
    }

    private static void EnsureSummonScene(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "SummonRoot");
        var controller = EnsureSceneComponent<SummonMenuController>(scene, root.transform);
        var spotlight = EnsureComponent<TutorialSpotlightController>(root);

        var bg = EnsurePanel(root.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Summon),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shell = EnsurePanel(root.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Summon), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(920f, 1540f));

        var bannerLabel = EnsureLabel(shell.transform, "Lbl_Banner", "Starseed Bloom Event Banner",
            40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(bannerLabel.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 610f), new Vector2(840f, 120f));

        var sporesLabel = EnsureLabel(shell.transform, "Lbl_Spores", "Spores: 50",
            30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(sporesLabel.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 520f), new Vector2(840f, 90f));

        var pullOne = EnsureButton(shell.transform, "Btn_PullOne", "Pull x1", new Vector2(0f, 350f), new Vector2(540f, 108f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var pullOneLabel);
        var pullTen = EnsureButton(shell.transform, "Btn_PullTen", "Pull x10", new Vector2(0f, 220f), new Vector2(540f, 108f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var pullTenLabel);
        var back = EnsureButton(shell.transform, "Btn_Back", "Back", new Vector2(0f, 90f), new Vector2(540f, 108f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var backLabel);

        var result = EnsureLabel(shell.transform, "Lbl_Result",
            "Summon results appear here.", 28, FontStyle.Normal, TextAnchor.UpperLeft, SoftWhite);
        SetRect(result.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -280f), new Vector2(820f, 640f));

        var hint = EnsureLabel(shell.transform, "Lbl_TutorialHint", string.Empty, 24, FontStyle.Italic, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(hint.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -610f), new Vector2(820f, 120f));

        var revealRoot = EnsurePanel(root.transform, "SummonReveal", new Color(0f, 0f, 0f, 0.85f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var revealCard = EnsurePanel(revealRoot.transform, "Panel_RevealCard",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Summon), 0.98f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(740f, 980f));

        var revealTitle = EnsureLabel(revealCard.transform, "Lbl_RevealTitle", "New Funguy Acquired",
            40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(revealTitle.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 360f), new Vector2(640f, 120f));

        var revealGlow = EnsurePanel(revealCard.transform, "Img_RarityGlow",
            WithAlpha(IdleHuntressTheme.AccentFor(UiTone.Summon), 0.35f),
            CenterAnchor, CenterAnchor, new Vector2(0f, 80f), new Vector2(460f, 460f));
        var revealFrame = EnsurePanel(revealCard.transform, "Img_RarityFrame",
            IdleHuntressTheme.RarityColor(4),
            CenterAnchor, CenterAnchor, new Vector2(0f, 80f), new Vector2(360f, 360f));
        var revealName = EnsureLabel(revealCard.transform, "Lbl_RevealName", "Unknown Funguy",
            36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(revealName.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -180f), new Vector2(640f, 100f));

        var revealRarity = EnsureLabel(revealCard.transform, "Lbl_RevealRarity", "★★★",
            30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(revealRarity.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -250f), new Vector2(640f, 90f));

        var revealNext = EnsureButton(revealCard.transform, "Btn_RevealNext", "Next", new Vector2(0f, -360f), new Vector2(420f, 100f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var revealNextLabel);

        var revealController = EnsureSceneComponent<SummonRevealController>(scene, revealRoot.transform);
        revealController.Initialize(revealRoot, revealTitle, revealName, revealRarity,
            revealFrame.GetComponent<Image>(), revealGlow.GetComponent<Image>(), revealNext);
        revealRoot.SetActive(false);

        ConfigureSkin(root, UiTone.Summon,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>(), revealCard.GetComponent<Image>() },
            new[] { pullOne.GetComponent<Image>(), pullTen.GetComponent<Image>(), back.GetComponent<Image>(), revealNext.GetComponent<Image>(), revealGlow.GetComponent<Image>(), revealFrame.GetComponent<Image>() },
            new[] { pullOne, pullTen, back, revealNext },
            new[] { bannerLabel, revealTitle, revealName },
            new[] { sporesLabel, result, hint, pullOneLabel, pullTenLabel, backLabel, revealRarity, revealNextLabel });

        spotlight.Configure(
            hint,
            new[]
            {
                Target(TutorialStep.ShowSummonPool, pullOne, "Your first stop is this banner. Continue and pull once."),
                Target(TutorialStep.GiveTicket, pullOne, "Use the tutorial ticket to perform your first summon."),
                Target(TutorialStep.DoFirstSummon, pullOne, "Pull one unit to continue onboarding."),
            });

    }

    private static void EnsureTeamScene(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "TeamRoot");
        var controller = EnsureSceneComponent<TeamMenuController>(scene, root.transform);
        var slotBinder = EnsureSceneComponent<TeamRosterSlotBinder>(scene, root.transform);
        var spotlight = EnsureComponent<TutorialSpotlightController>(root);

        var bg = EnsurePanel(root.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Team),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shell = EnsurePanel(root.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Team), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(940f, 1560f));

        var teamStatus = EnsureLabel(shell.transform, "Lbl_TeamStatus", "Team (0/5): No units selected",
            29, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
        SetRect(teamStatus.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 620f), new Vector2(860f, 80f));

        var formationButtons = new List<Button>();
        var formationLabels = new List<Text>();
        for (int i = 0; i < FormationRules.SlotCount; i++)
        {
            int depth = FormationRules.Depth(i);
            float x = 250f - depth * 250f;
            var button = EnsureButton(shell.transform, $"Btn_FormationSlot{i + 1}", FormationRules.Label(i),
                new Vector2(x, 440f - FormationRules.Lane(i) * 125f + (depth % 2) * 62.5f), new Vector2(320f, 120f), IdleHuntressTheme.AccentFor(UiTone.Team), out var label);
            HexBoardVisual.Apply(button.GetComponent<Image>());
            label.fontSize = 19;
            SetRect(label.rectTransform, CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(190f, 100f));
            formationButtons.Add(button); formationLabels.Add(label);
        }

        var hint = EnsureLabel(shell.transform, "Lbl_Hint", "Tip: build at least 3 units for smoother stage clears.",
            24, FontStyle.Italic, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(hint.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -50f), new Vector2(860f, 90f));

        var slotStrip = EnsurePanel(shell.transform, "Panel_RosterSlots",
            WithAlpha(IdleHuntressTheme.BackgroundFor(UiTone.Team), 0.55f),
            CenterAnchor, CenterAnchor, new Vector2(0f, -200f), new Vector2(860f, 160f));

        var slotButtons = new List<Button>();
        var slotLabels = new List<Text>();
        float firstX = -320f;
        for (int i = 0; i < 5; i++)
        {
            float x = firstX + (i * 160f);
            var slot = EnsureButton(slotStrip.transform, $"Btn_RosterSlot{i + 1}", $"Slot {i + 1}",
                new Vector2(x, 0f), new Vector2(150f, 140f), IdleHuntressTheme.AccentFor(UiTone.Team), out var slotLabel, $"Lbl_RosterSlot{i + 1}");
            slotLabel.fontSize = 20;
            slotButtons.Add(slot);
            slotLabels.Add(slotLabel);
        }

        var roster = EnsureLabel(shell.transform, "Lbl_Roster", "No units owned yet. Visit Summon.",
            25, FontStyle.Normal, TextAnchor.UpperLeft, SoftWhite);
        SetRect(roster.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -425f), new Vector2(860f, 95f));
        var previous = EnsureButton(shell.transform, "Btn_RosterPrevious", "Previous", new Vector2(-300f, -330f), new Vector2(240f, 70f), IdleHuntressTheme.AccentFor(UiTone.Team), out var previousLabel);
        var next = EnsureButton(shell.transform, "Btn_RosterNext", "Next", new Vector2(300f, -330f), new Vector2(240f, 70f), IdleHuntressTheme.AccentFor(UiTone.Team), out var nextLabel);
        var pageLabel = EnsureLabel(shell.transform, "Lbl_RosterPage", "Roster", 22, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
        SetRect(pageLabel.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -330f), new Vector2(340f, 70f));
        var remove = EnsureButton(shell.transform, "Btn_RemoveSelected", "Remove selected fighter", new Vector2(0f, -440f), new Vector2(500f, 70f), IdleHuntressTheme.AccentFor(UiTone.Team), out var removeLabel);
        controller.ConfigureFormation(formationButtons.ToArray(), formationLabels.ToArray(), remove);
        slotBinder.ConfigurePaging(previous, next, pageLabel);

        var upgrades = EnsureButton(shell.transform, "Btn_Upgrades", "Upgrade fighters", Vector2.zero,
            new Vector2(400, 55), IdleHuntressTheme.AccentFor(UiTone.Team), out _);
        var workshop = UpgradePanelView.Create(root.transform, slotBinder.RebindSlots);
        upgrades.onClick.RemoveAllListeners(); upgrades.onClick.AddListener(workshop.Open);

        var equipmentButton = EnsureButton(shell.transform, "Btn_Equipment", "Equipment", new Vector2(0f, -610f),
            new Vector2(300f, 76f), new Color(0.10f, 0.35f, 0.68f, 1f), out var equipmentButtonLabel);
        equipmentButtonLabel.color = Color.white;
        var equipmentRoot = EnsurePanel(root.transform, "EquipmentPanel", new Color(.05f,.08f,.1f,.98f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var equipmentCard = EnsurePanel(equipmentRoot.transform, "Panel_EquipmentCard", WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Team), .99f), CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(900f, 1450f));
        var equipmentTitle = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentTitle", "Equipment", 42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(equipmentTitle.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, 620), new Vector2(820, 70));
        var equipmentCharacter = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentCharacter", "Character", 40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(equipmentCharacter.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, 555), new Vector2(820, 80));
        var equipmentIdentity = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentIdentity", "Class • Role\nBiome", 24, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(equipmentIdentity.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, 495), new Vector2(820, 70));

        var choiceHeader = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentChoices", "AVAILABLE EQUIPMENT", 22, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(choiceHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(-290, 390), new Vector2(250, 55));
        var choiceButtons=new Button[4]; var choiceLabels=new Text[4];
        for(int i=0;i<4;i++){ var b=EnsureButton(equipmentCard.transform,$"Btn_EquipmentChoice{i+1}","Equipment",new Vector2(-290,300-i*125),new Vector2(250,105),new Color(.10f,.25f,.34f,1f),out var l); l.fontSize=17; choiceButtons[i]=b; choiceLabels[i]=l; }

        var centerHeader = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentSlots", "EQUIPMENT SLOTS", 22, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(centerHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, 390), new Vector2(260, 55));
        var gearButtons=new Button[4]; var gearLabels=new Text[4];
        for(int i=0;i<4;i++){ var b=EnsureButton(equipmentCard.transform,$"Btn_EquipmentSlot{i+1}",EquipmentService.Slots[i].ToUpperInvariant(),new Vector2(0,300-i*125),new Vector2(250,105),new Color(.16f,.34f,.22f,1f),out var l); l.fontSize=17; gearButtons[i]=b; gearLabels[i]=l; }

        var statsHeader = EnsureLabel(equipmentCard.transform, "Lbl_CharacterStats", "CHARACTER STATS", 22, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(statsHeader.rectTransform, CenterAnchor, CenterAnchor, new Vector2(290, 390), new Vector2(250, 55));
        var statsLabel = EnsureLabel(equipmentCard.transform, "Lbl_CharacterStatsValues", "HP\n0\n\nATK\n0\n\nDEF\n0\n\nSPD\n0\n\nPOT\n0", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(statsLabel.rectTransform, CenterAnchor, CenterAnchor, new Vector2(290, 80), new Vector2(250, 560));

        var equipmentDetails = EnsureLabel(equipmentCard.transform, "Lbl_EquipmentDetails", "", 20, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(equipmentDetails.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0, -280), new Vector2(820, 120));
        var equipmentUpgrade = EnsureButton(equipmentCard.transform, "Btn_EquipmentUpgrade", "Equip Equipment", new Vector2(0, -440), new Vector2(420, 80), IdleHuntressTheme.AccentFor(UiTone.Team), out var equipmentUpgradeLabel);
        var charButtons=new Button[5];
        for(int i=0;i<5;i++){ var b=EnsureButton(equipmentCard.transform,$"Btn_EquipmentCharacter{i+1}","Character",new Vector2(-250+i*125,410),new Vector2(115,60),IdleHuntressTheme.AccentFor(UiTone.Team),out _); charButtons[i]=b; }
        var equipmentBack=EnsureButton(equipmentCard.transform,"Btn_EquipmentBack","Back to Formation",new Vector2(0,-570),new Vector2(460,80),new Color(.28f,.34f,.39f,1f),out var equipmentBackLabel);
        choiceLabels.ToList().ForEach(x => x.color = Color.white);
        gearLabels.ToList().ForEach(x => x.color = Color.white);
        equipmentUpgradeLabel.color = Color.white;
        equipmentBackLabel.color = Color.white;
        var equipmentController=EnsureSceneComponent<EquipmentPanelController>(scene,equipmentRoot.transform);
        equipmentController.Initialize(equipmentRoot,equipmentCharacter,equipmentIdentity,statsLabel,equipmentDetails,charButtons,choiceButtons,choiceLabels,gearButtons,gearLabels,equipmentUpgrade,equipmentUpgradeLabel,equipmentBack);
        equipmentUpgrade.onClick.AddListener(equipmentController.UpgradeSelectedEquipment);
        for(int i=0;i<5;i++){ int index=i; charButtons[i].onClick.AddListener(()=>equipmentController.SelectCharacter(index)); }
        for(int i=0;i<4;i++){ int index=i; gearButtons[i].onClick.AddListener(()=>equipmentController.EquipFromSlot(index)); choiceButtons[i].onClick.AddListener(()=>equipmentController.EquipChoice(index)); }
        equipmentBack.onClick.AddListener(equipmentController.BackToFormation);
        equipmentRoot.SetActive(false);
        equipmentButton.onClick.AddListener(equipmentController.Open);

        var autoFill = EnsureButton(shell.transform, "Btn_AutoFill", "Auto Fill", new Vector2(-310f, -520f), new Vector2(210f, 76f), new Color(0.00f, 0.47f, 0.45f, 1f), out var autoFillLabel);
        var clearTeam = EnsureButton(shell.transform, "Btn_ClearTeam", "Clear Team", new Vector2(-80f, -520f), new Vector2(210f, 76f), new Color(0.78f, 0.34f, 0.05f, 1f), out var clearLabel);
        var startBattle = EnsureButton(shell.transform, "Btn_StartBattle", "Start Battle", new Vector2(180f, -520f), new Vector2(210f, 76f), new Color(0.12f, 0.52f, 0.25f, 1f), out var startBattleLabel);
        var back = EnsureButton(shell.transform, "Btn_Back", "Back", new Vector2(0f, -720f), new Vector2(500f, 76f), new Color(0.28f, 0.34f, 0.39f, 1f), out var backLabel);

        ConfigureSkin(root, UiTone.Team,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>(), slotStrip.GetComponent<Image>() },
            new[] { autoFill.GetComponent<Image>(), clearTeam.GetComponent<Image>(), startBattle.GetComponent<Image>(), back.GetComponent<Image>(), equipmentButton.GetComponent<Image>() }
                .Concat(slotButtons.Concat(formationButtons).Concat(new[] { previous, next, remove }).Select(b => b.GetComponent<Image>())).ToArray(),
            new[] { autoFill, clearTeam, startBattle, back, previous, next, remove }.Concat(slotButtons).Concat(formationButtons).ToArray(),
            new[] { teamStatus },
            new[] { hint, roster, autoFillLabel, clearLabel, startBattleLabel, backLabel, equipmentButtonLabel }
                .Concat(slotLabels).Concat(formationLabels).Concat(new[] { previousLabel, nextLabel, pageLabel, removeLabel }).ToArray());

        SetAccessibleButton(autoFill, autoFillLabel, new Color(0.00f, 0.47f, 0.45f, 1f));
        SetAccessibleButton(clearTeam, clearLabel, new Color(0.78f, 0.34f, 0.05f, 1f));
        SetAccessibleButton(startBattle, startBattleLabel, new Color(0.12f, 0.52f, 0.25f, 1f));
        SetAccessibleButton(back, backLabel, new Color(0.28f, 0.34f, 0.39f, 1f));
        SetAccessibleButton(equipmentButton, equipmentButtonLabel, new Color(0.10f, 0.35f, 0.68f, 1f));

        SetAccessibleButton(remove, removeLabel, new Color(0.70f, 0.10f, 0.12f, 1f));

        spotlight.Configure(
            null, // Team controller owns persistent placement instructions; tutorial uses its overlay.
            new[]
            {
                Target(TutorialStep.GoToTeamBuilder, autoFill, "Auto-fill to place your first formation."),
                Target(TutorialStep.PlaceFirstUnit, formationButtons.FirstOrDefault(), "Tap a position, then a fighter. Two positions swap or move."),
                Target(TutorialStep.StartFirstBattle, startBattle, "When ready, start your first battle."),
                Target(TutorialStep.PlaceTankFrontDpsBack, upgrades, "Preview a fighter's next level and gold cost in the workshop."),
            });

    }

    private static void EnsureBattleScene(Scene scene, Canvas canvas)
    {
        var asset = Resources.Load<BattleScreenView>("Presentation/BattleScreen");
        if (asset == null) throw new InvalidOperationException("Battle presentation prefab is missing. Run the Presentation authoring task.");
        var screen = UnityEngine.Object.Instantiate(asset, canvas.transform);
        screen.name = "BattleRoot";
        var controller = EnsureSceneComponent<BattleSceneController>(scene, screen.transform);
        controller.Configure(screen);
    }
    private static void EnsureTutorialScene(Scene scene, Canvas canvas)
    {
        var manager = TutorialManager.EnsureInstance();
        if (canvas.transform.Find("TutorialOverlayRoot") != null) return;
        var overlayRoot = EnsurePanel(canvas.transform, "TutorialOverlayRoot", new Color(0.08f, 0.12f, 0.16f, 0.97f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(980f, 360f));
        var message = EnsureLabel(overlayRoot.transform, "Lbl_TutorialMessage", "", 27, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
        SetRect(message.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 50f), new Vector2(900f, 180f));
        var next = EnsureButton(overlayRoot.transform, "Btn_TutorialContinue", "Continue", new Vector2(0f, -100f),
            new Vector2(400f, 80f), IdleHuntressTheme.AccentFor(UiTone.Home), out _);
        var overlay = overlayRoot.AddComponent<TutorialOverlay>();
        overlay.Initialize(overlayRoot, message, next);
        manager.AttachOverlay(overlay);
    }

    private static void EnsureOptionsScene(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "OptionsRoot");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var debugRoot = EnsureChild(root.transform, "DebugRoot");
        StretchToParent(EnsureRectTransform(debugRoot));

        var debugController = EnsureSceneComponent<DebugProgressionController>(scene, debugRoot.transform);
        var smokeController = EnsureSceneComponent<GameplaySmokeTestController>(scene, debugRoot.transform);

        var bg = EnsurePanel(debugRoot.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Home),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shell = EnsurePanel(debugRoot.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Home), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(940f, 1540f));

        var title = EnsureLabel(shell.transform, "Lbl_OptionsTitle", "Debug / QA Controls",
            40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 630f), new Vector2(860f, 120f));

        var status = EnsureLabel(shell.transform, "Lbl_DebugStatus", "Ready.",
            26, FontStyle.Normal, TextAnchor.MiddleLeft, SoftWhite);
        SetRect(status.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 535f), new Vector2(860f, 90f));

        var reset = EnsureButton(shell.transform, "Btn_ResetSave", "Reset Save", new Vector2(-230f, 380f), new Vector2(300f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var resetLabel);
        var grant = EnsureButton(shell.transform, "Btn_GrantStarterResources", "Grant Resources", new Vector2(120f, 380f), new Vector2(330f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var grantLabel);
        var seed = EnsureButton(shell.transform, "Btn_SeedStarterRoster", "Seed Roster", new Vector2(-230f, 275f), new Vector2(300f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var seedLabel);
        var skip = EnsureButton(shell.transform, "Btn_SkipTutorial", "Skip Tutorial", new Vector2(120f, 275f), new Vector2(330f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var skipLabel);
        var openSummon = EnsureButton(shell.transform, "Btn_OpenSummon", "Open Summon", new Vector2(-230f, 170f), new Vector2(300f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var openSummonLabel);
        var runSmoke = EnsureButton(shell.transform, "Btn_RunSmokeTests", "Run Smoke Tests", new Vector2(120f, 170f), new Vector2(330f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var smokeLabel);
        var backHome = EnsureButton(shell.transform, "Btn_BackHome", "Back Home", new Vector2(0f, 60f), new Vector2(640f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var backLabel);

        var smokeOutput = EnsureLabel(shell.transform, "Lbl_SmokeOutput",
            "Smoke Test Result: not run.", 24, FontStyle.Normal, TextAnchor.UpperLeft, SoftWhite);
        SetRect(smokeOutput.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -370f), new Vector2(860f, 760f));

        ConfigureSkin(debugRoot, UiTone.Home,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>() },
            new[] { reset.GetComponent<Image>(), grant.GetComponent<Image>(), seed.GetComponent<Image>(), skip.GetComponent<Image>(), openSummon.GetComponent<Image>(), runSmoke.GetComponent<Image>(), backHome.GetComponent<Image>() },
            new[] { reset, grant, seed, skip, openSummon, runSmoke, backHome },
            new[] { title },
            new[] { status, smokeOutput, resetLabel, grantLabel, seedLabel, skipLabel, openSummonLabel, smokeLabel, backLabel });

#else
        var title = EnsureLabel(root.transform, "Lbl_OptionsTitle", "Options", 40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 240f), new Vector2(800f, 100f));
        var audio = EnsureButton(root.transform, "Btn_Audio", AudioListener.pause ? "Enable audio" : "Mute audio", Vector2.zero,
            new Vector2(560f, 100f), IdleHuntressTheme.AccentFor(UiTone.Home), out var audioLabel);
        audio.onClick.AddListener(() => {
            AudioListener.pause = !AudioListener.pause;
            audioLabel.text = AudioListener.pause ? "Enable audio" : "Mute audio";
        });
        var back = EnsureButton(root.transform, "Btn_Back", "Back", new Vector2(0f, -150f), new Vector2(560f, 100f),
            IdleHuntressTheme.AccentFor(UiTone.Home), out _);
        back.onClick.AddListener(() => SceneManager.LoadScene("Home"));
#endif
    }

    private static Canvas EnsureCanvas(Scene scene)
    {
        var existing = FindInScene<Canvas>(scene).FirstOrDefault();
        if (existing != null) { ConfigureLandscapeCanvas(existing); return existing; }

        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasGo, scene);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 1f;

        return canvas;
    }

    private static void ConfigureLandscapeCanvas(Canvas canvas)
    {
        var scaler = EnsureComponent<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 1;
    }
    private static void EnsureEventSystem(Scene scene)
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(es, scene);
    }

    private static GameObject EnsureSceneRoot(Scene scene, Transform canvas, string rootName)
    {
        var root = EnsureChild(canvas, rootName);
        StretchToParent(EnsureRectTransform(root));
        EnsureComponent<UiPrefabBlueprintBinder>(root);
        return root;
    }

    private static T EnsureSceneComponent<T>(Scene scene, Transform parent) where T : Component
    {
        var existing = FindInScene<T>(scene).FirstOrDefault();
        if (existing != null)
        {
            if (parent != null && existing.transform != parent && existing.transform.parent != parent)
            {
                existing.transform.SetParent(parent, false);
            }
            return existing;
        }

        return parent.gameObject.AddComponent<T>();
    }

    private static void ConfigureSkin(
        GameObject root,
        UiTone tone,
        Image[] backgrounds,
        Image[] panels,
        Image[] accents,
        Button[] buttons,
        Text[] titles,
        Text[] body)
    {
        var skin = EnsureComponent<IdleHuntressSkin>(root);
        SetPrivateField(skin, "tone", tone);
        SetPrivateField(skin, "backgroundLayers", backgrounds.Where(x => x != null).ToArray());
        SetPrivateField(skin, "panelLayers", panels.Where(x => x != null).ToArray());
        SetPrivateField(skin, "accentLayers", accents.Where(x => x != null).ToArray());
        SetPrivateField(skin, "primaryButtons", buttons.Where(x => x != null).ToArray());
        SetPrivateField(skin, "titleLabels", titles.Where(x => x != null).ToArray());
        SetPrivateField(skin, "bodyLabels", body.Where(x => x != null).ToArray());
        skin.ApplyTheme();
    }

    private static void SetAccessibleButton(Button button, Text label, Color color)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null) image.color = color;
        if (label != null) label.color = Color.white;

        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.16f);
        colors.selectedColor = Color.Lerp(color, Color.white, 0.08f);
        colors.disabledColor = Color.Lerp(color, Color.black, 0.35f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static TutorialSpotlightTarget Target(TutorialStep step, Button button, string hint)
    {
        var graphic = button == null ? null : (button.targetGraphic ?? button.GetComponent<Graphic>());
        return new TutorialSpotlightTarget
        {
            step = step,
            target = graphic,
            hint = hint
        };
    }

    private static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        var found = new List<T>();
        foreach (var root in scene.GetRootGameObjects())
        {
            found.AddRange(root.GetComponentsInChildren<T>(true));
        }
        return found;
    }

    private static GameObject EnsureChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject EnsurePanel(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        var go = EnsureChild(parent, name);
        var rt = EnsureRectTransform(go);
        SetRect(rt, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        var image = EnsureComponent<Image>(go);
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    private static Button EnsureButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color,
        out Text labelText,
        string labelName = null)
    {
        var go = EnsureChild(parent, name);
        var rt = EnsureRectTransform(go);
        SetRect(rt, CenterAnchor, CenterAnchor, anchoredPosition, sizeDelta);

        var image = EnsureComponent<Image>(go);
        image.color = color;
        image.raycastTarget = true;

        var button = EnsureComponent<Button>(go);
        button.targetGraphic = image;

        labelText = EnsureLabel(
            go.transform,
            string.IsNullOrWhiteSpace(labelName) ? "Label" : labelName,
            label,
            30,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.black);
        StretchToParent(labelText.rectTransform);

        return button;
    }

    private static Text EnsureLabel(
        Transform parent,
        string name,
        string text,
        int fontSize,
        FontStyle style,
        TextAnchor anchor,
        Color color)
    {
        var go = EnsureChild(parent, name);
        var label = EnsureComponent<Text>(go);
        label.font = ResolveFont();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = anchor;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.supportRichText = true;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform EnsureRectTransform(GameObject go)
    {
        return EnsureComponent<RectTransform>(go);
    }

    private static void SetRect(
        RectTransform rt,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = CenterAnchor;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = CenterAnchor;
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }

    private static Font ResolveFont()
    {
        if (_cachedFont != null) return _cachedFont;

        _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_cachedFont == null)
        {
            _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return _cachedFont;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null || string.IsNullOrWhiteSpace(fieldName)) return;
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        var field = target.GetType().GetField(fieldName, flags);
        if (field == null) return;
        field.SetValue(target, value);
    }

    private static readonly Vector2 CenterAnchor = new(0.5f, 0.5f);
    private static readonly Color SoftWhite = new(0.92f, 0.92f, 0.92f, 1f);
}
