using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class OnboardingTests
{
    private readonly string[] keys = { "FUNGI_SAVE_V2", "FUNGI_SAVE_V2_BAK", "FUNGI_SAVE_V1", "FUNGI_SAVE_V1_BAK" };
    private readonly Dictionary<string, string> backup = new();
    private int? motionBackup;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        motionBackup = PlayerPrefs.HasKey("FUNGUY_REDUCED_MOTION") ? PlayerPrefs.GetInt("FUNGUY_REDUCED_MOTION") : null;
        PlayerPrefs.DeleteKey("FUNGUY_REDUCED_MOTION");
        foreach (var key in keys) if (PlayerPrefs.HasKey(key)) backup[key] = PlayerPrefs.GetString(key);
        if (TutorialManager.I != null) Object.Destroy(TutorialManager.I.gameObject);
        yield return null;
        SaveSystem.DeleteSave();
        Game.Save = null; Game.Data = null; Game.Gacha = null; Game.Campaign = null; Game.Summons = null; Game.Team = null; Game.Upgrades = null; Game.SelectedStageId = null;
        Game.EnsureInitialized();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (TutorialManager.I != null) Object.Destroy(TutorialManager.I.gameObject);
        var oldScene = SceneManager.GetActiveScene();
        var cleanup = SceneManager.CreateScene("ValidationCleanup");
        SceneManager.SetActiveScene(cleanup);
        if (oldScene.IsValid() && oldScene.isLoaded) yield return SceneManager.UnloadSceneAsync(oldScene);
        foreach (var key in keys) PlayerPrefs.DeleteKey(key);
        foreach (var item in backup) PlayerPrefs.SetString(item.Key, item.Value);
        if (motionBackup.HasValue) PlayerPrefs.SetInt("FUNGUY_REDUCED_MOTION", motionBackup.Value);
        else PlayerPrefs.DeleteKey("FUNGUY_REDUCED_MOTION");
        PlayerPrefs.Save();
        backup.Clear();
        Game.Save = null; Game.Data = null; Game.Gacha = null; Game.Campaign = null; Game.Summons = null; Game.Team = null; Game.Upgrades = null; Game.SelectedStageId = null;
        yield return null;
    }

    private static Button FindButton(string name) => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(g => g.GetComponentsInChildren<Button>(true)).Single(b => b.name == name && b.gameObject.activeInHierarchy);

    private static IEnumerator Settle()
    {
        yield return null; yield return null; yield return null;
    }

    private static IEnumerator Press(string name)
    {
        var button = FindButton(name);
        Assert.True(button.interactable, name);
        button.onClick.Invoke();
        yield return Settle();
    }

    private static IEnumerator CaptureOptional(string name, int width = 2400, int height = 1080)
    {
        string directory = System.Environment.GetEnvironmentVariable("FUNGUY_UI_CAPTURES");
        if (string.IsNullOrEmpty(directory)) yield break;
        System.IO.Directory.CreateDirectory(directory);
        // Batch mode has no presented backbuffer; explicitly render the real canvases.
        var capture = new GameObject("ValidationCamera").AddComponent<Camera>();
        capture.enabled = false;
        capture.orthographic = true;
        capture.clearFlags = CameraClearFlags.SolidColor;
        capture.backgroundColor = Color.black;
        var target = new RenderTexture(width, height, 24);
        capture.targetTexture = target;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToArray();
        var modes = canvases.Select(c => c.renderMode).ToArray();
        var cameras = canvases.Select(c => c.worldCamera).ToArray();
        var distances = canvases.Select(c => c.planeDistance).ToArray();
        var previousTarget = RenderTexture.active;
        Texture2D pixels = null;
        try
        {
            foreach (var canvas in canvases)
            { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = capture; canvas.planeDistance = 1; }
            yield return Settle();
            Canvas.ForceUpdateCanvases();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(capture,
                new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            string path = System.IO.Path.Combine(directory, name + ".png");
            System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG());
            Assert.True(System.IO.File.Exists(path));
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i]; }
            RenderTexture.active = previousTarget;
            capture.targetTexture = null;
            target.Release(); Object.Destroy(target); Object.Destroy(pixels); Object.Destroy(capture.gameObject);
        }
    }

    [UnityTest]
    public IEnumerator CampaignPicker_ShowsLocksRestoresSelectionAndOpensBoss()
    {
        yield return SceneManager.LoadSceneAsync("Boot"); yield return Settle();
        Game.Save.tutorialCompleted = true; Game.Save.tutorialStep = (int)TutorialStep.Complete;
        SaveSystem.Save(Game.Save);
        yield return SceneManager.LoadSceneAsync("Home"); yield return Settle();
        yield return Press("Btn_Battle");
        Assert.False(FindButton("Btn_CampaignStage2").interactable);
        Assert.True(FindButton("Btn_CampaignStage1").interactable);
        yield return CaptureOptional("step4-campaign-locked");
        yield return Press("Btn_CloseCampaign");
        Game.Save.clearedStages.AddRange(Game.Data.Stages.Keys.Where(id => id != "s_1_7"));
        foreach (var unit in Game.Save.units) unit.level = 9;
        SaveSystem.Save(Game.Save); Game.SelectedStageId = null;
        yield return Press("Btn_Battle");
        var picker = Object.FindFirstObjectByType<CampaignPanelController>();
        Assert.AreEqual("s_1_7", picker.SelectedStageId);
        yield return CaptureOptional("step4-campaign-boss");
        yield return CaptureOptional("step4-campaign-tablet", 1600, 1200);
        int gold = Game.Save.gold; yield return Press("Btn_CampaignEnter");
        Assert.AreEqual("Battle", SceneManager.GetActiveScene().name);
        var battle = Object.FindFirstObjectByType<BattleSceneController>();
        StringAssert.Contains("Cauldron Keeper", battle.View.stageLabel.text);
        battle.OnRunBattlePressed(); battle.OnFinishPressed(); yield return FinishBattle(battle);
        Assert.AreEqual(BattleOutcome.Victory, battle.Session.Battle.Outcome);
        Assert.Contains("s_1_7", Game.Save.clearedStages); Assert.AreEqual(gold + 600, Game.Save.gold);
        yield return CaptureOptional("step4-boss-victory");
        battle.OnBackPressed(); yield return Settle(); yield return Press("Btn_Battle");
        Assert.AreEqual("s_1_7", Object.FindFirstObjectByType<CampaignPanelController>().SelectedStageId);
        yield return Press("Btn_CampaignStage1"); yield return Press("Btn_CampaignEnter");
        battle = Object.FindFirstObjectByType<BattleSceneController>();
        StringAssert.Contains("Practice", battle.View.waveLabel.text);
        Assert.AreEqual("s_1_1", Game.SelectedStageId);
    }

    [UnityTest]
    public IEnumerator UpgradeWorkshop_SpendsRewardGoldRestoresAndFeedsBattle()
    {
        yield return SceneManager.LoadSceneAsync("Boot"); yield return Settle();
        Game.Save.tutorialCompleted = true; Game.Save.tutorialStep = (int)TutorialStep.Complete;
        SaveSystem.Save(Game.Save);
        var clear = Game.Campaign.Run("s_1_1"); Assert.True(clear.won); Assert.True(clear.firstClearRewardGranted);
        int gold = Game.Save.gold;
        var formation = Game.Team.GetFormation().Select(p => p.charId + ":" + p.slot).ToArray();
        yield return SceneManager.LoadSceneAsync("Team"); yield return Settle();
        yield return Press("Btn_Upgrades");
        var workshop = Object.FindFirstObjectByType<UpgradePanelController>(); Assert.True(workshop.IsOpen);
        string id = "c_puffmage_orbi"; workshop.Select(id); yield return Settle();
        var preview = workshop.Preview; Assert.AreEqual(3, preview.Level); // Onboarding starters begin at level 3.
        yield return CaptureOptional("step4-upgrade-preview");
        yield return Press("Btn_ConfirmUpgrade");
        Assert.AreEqual(4, Game.Save.units.Single(u => u.charId == id).level);
        Assert.AreEqual(gold - preview.GoldCost, Game.Save.gold);
        Assert.AreEqual(preview.Next.HP, workshop.Preview.Current.HP);
        yield return CaptureOptional("step4-upgrade-saved");
        yield return CaptureOptional("step4-upgrade-tablet", 1600, 1200);
        yield return Press("Btn_CloseUpgrades"); Assert.False(workshop.IsOpen);
        CollectionAssert.AreEqual(formation, Game.Team.GetFormation().Select(p => p.charId + ":" + p.slot).ToArray());
        var persisted = SaveSystem.LoadOrNew();
        Assert.AreEqual(4, persisted.units.Single(u => u.charId == id).level);
        Assert.AreEqual(gold - preview.GoldCost, persisted.gold);
        Game.Save = null; Game.Data = null; Game.Gacha = null; Game.Campaign = null;
        Game.Summons = null; Game.Team = null; Game.Upgrades = null; Game.SelectedStageId = null; Game.EnsureInitialized();
        yield return SceneManager.LoadSceneAsync("Team"); yield return Settle();
        yield return Press("Btn_Upgrades");
        workshop = Object.FindFirstObjectByType<UpgradePanelController>(); workshop.Select(id);
        Assert.AreEqual(4, workshop.Preview.Level);
        yield return Press("Btn_CloseUpgrades");
        yield return Press("Btn_StartBattle");
        var battle = Object.FindFirstObjectByType<BattleSceneController>(); battle.OnRunBattlePressed();
        Assert.AreEqual(1, Game.Save.activeTeam.Count(x => Game.Data.Characters[x].biome == "Forest"));
        // R Orbi is biome-less; these three starters have no Forest HP tier.
        Assert.AreEqual(preview.Next.HP,
            battle.Session.Battle.GetState().Single(u => u.ContentId == id).MaxHp);
    }

    [UnityTest]
    public IEnumerator UpgradeWorkshop_PagesOwnedRosterAndExplainsUnavailablePurchases()
    {
        Game.Save.tutorialCompleted = true; Game.Save.tutorialStep = (int)TutorialStep.Complete; Game.Save.gold = 0;
        Game.Save.units = Game.Data.Characters.Keys.Select(id => new OwnedUnit { charId = id, level = 1, stars = 1, copies = 1 }).ToList();
        SaveSystem.Save(Game.Save);
        yield return SceneManager.LoadSceneAsync("Team"); yield return Settle();
        yield return Press("Btn_Upgrades");
        var workshop = Object.FindFirstObjectByType<UpgradePanelController>();
        Assert.False(FindButton("Btn_ConfirmUpgrade").interactable);
        string before = JsonUtility.ToJson(Game.Save); workshop.OnUpgradePressed();
        Assert.AreEqual(before, JsonUtility.ToJson(Game.Save));
        yield return Press("Btn_UpgradeNext"); yield return Press("Btn_UpgradeFighter5");
        Assert.AreEqual(Game.Data.Characters.Keys.OrderBy(x => x, System.StringComparer.Ordinal).Last(), workshop.SelectedCharacterId);
        yield return CaptureOptional("step4-upgrade-shortfall");
        yield return Press("Btn_CloseUpgrades");
        var owned = Game.Save.units.Single(u => u.charId == workshop.SelectedCharacterId); owned.level = 20;
        SaveSystem.Save(Game.Save); yield return Press("Btn_Upgrades");
        Assert.True(workshop.Preview.AtCap); Assert.False(FindButton("Btn_ConfirmUpgrade").interactable);
        yield return Press("Btn_CloseUpgrades");
        Game.Team.Clear(); Game.Save.units.Clear(); SaveSystem.Save(Game.Save);
        yield return Press("Btn_Upgrades"); Assert.IsNull(workshop.Preview);
        Assert.False(FindButton("Btn_ConfirmUpgrade").interactable);
        yield return Press("Btn_CloseUpgrades");
    }

    [UnityTest]
    public IEnumerator FreshAccount_CompletesSummonTeamBattleAndRestoresProgress()
    {
        yield return SceneManager.LoadSceneAsync("Boot");
        yield return Settle();
        Assert.NotNull(TutorialManager.I);
        yield return CaptureOptional("landscape-home-tutorial");
        Assert.GreaterOrEqual(Game.Save.units.Count, 3);
        yield return Press("Btn_TutorialContinue");
        Assert.AreEqual("Summon", SceneManager.GetActiveScene().name);
        yield return Press("Btn_TutorialContinue");
        yield return Press("Btn_TutorialContinue");
        Assert.AreEqual((int)TutorialStep.DoFirstSummon, Game.Save.tutorialStep);
        yield return CaptureOptional("landscape-summon");
        int copies = Game.Save.units.Sum(u => u.copies);
        yield return Press("Btn_PullOne");
        yield return new WaitForSecondsRealtime(.3f);
        Assert.AreEqual(copies + 1, Game.Save.units.Sum(u => u.copies));
        Assert.AreEqual(0, Game.Save.tutorialTickets);
        yield return CaptureOptional("landscape-reveal");
        yield return Press("Btn_RevealNext");
        Assert.AreEqual("Team", SceneManager.GetActiveScene().name);
        yield return Press("Btn_TutorialContinue");
        yield return Press("Btn_TutorialContinue");
        Assert.AreEqual("Battle", SceneManager.GetActiveScene().name);
        yield return CaptureOptional("hex-battle");
        yield return Press("Btn_RunBattle");
        var battle = Object.FindFirstObjectByType<BattleSceneController>();
        Assert.True(battle.IsPlaying);
        Assert.IsEmpty(Game.Save.clearedStages);
        battle.OnFinishPressed();
        yield return FinishBattle(battle);
        Assert.Contains("s_1_1", Game.Save.clearedStages);
        Assert.True(Game.Save.tutorialBattleRewardClaimed);
        yield return Press("Btn_TutorialContinue");
        yield return Press("Btn_TutorialContinue");
        Assert.AreEqual("Team", SceneManager.GetActiveScene().name);
        Assert.AreEqual((int)TutorialStep.PlaceTankFrontDpsBack, Game.Save.tutorialStep);
        yield return CaptureOptional("step4-upgrade-guidance");
        yield return Press("Btn_Upgrades");
        Assert.IsNull(Object.FindFirstObjectByType<TutorialOverlay>(), "Guide must not cover workshop controls.");
        var workshop = Object.FindFirstObjectByType<UpgradePanelController>();
        string upgradedId = workshop.SelectedCharacterId;
        int levelBefore = workshop.Preview.Level, goldBefore = Game.Save.gold, cost = workshop.Preview.GoldCost;
        yield return Press("Btn_ConfirmUpgrade");
        Assert.AreEqual(levelBefore + 1, SaveSystem.LoadOrNew().units.Find(u => u.charId == upgradedId).level);
        Assert.AreEqual(goldBefore - cost, Game.Save.gold);
        yield return CaptureOptional("step4-guided-upgrade-saved");
        yield return Press("Btn_CloseUpgrades");
        Assert.NotNull(Object.FindFirstObjectByType<TutorialOverlay>());
        yield return Press("Btn_TutorialContinue");
        Assert.True(Game.Save.tutorialCompleted);
        Assert.AreEqual("Home", SceneManager.GetActiveScene().name);
        var persisted = SaveSystem.LoadOrNew();
        Assert.True(persisted.tutorialCompleted);
        Assert.Contains("s_1_1", persisted.clearedStages);
        Assert.AreEqual(0, persisted.tutorialTickets);
        // Rebinding must not add another summon listener.
        yield return Press("Btn_Summon");
        foreach (var binder in Object.FindObjectsByType<UiPrefabBlueprintBinder>(FindObjectsSortMode.None))
        { binder.AutoBindCommonReferences(); binder.AutoBindCommonReferences(); }
        int spores = Game.Save.spores;
        yield return Press("Btn_PullOne");
        Assert.AreEqual(spores - 10, Game.Save.spores);
    }

    [UnityTest]
    public IEnumerator UpgradeGuidance_ResumesAndAllowsCompletionWithoutGold()
    {
        Game.EnsureInitialized();
        Game.Save.tutorialStep = (int)TutorialStep.PlaceTankFrontDpsBack;
        Game.Save.tutorialCompleted = false; Game.Save.gold = 0;
        SaveSystem.Save(Game.Save);
        yield return SceneManager.LoadSceneAsync("Boot"); yield return Settle();
        Assert.AreEqual("Team", SceneManager.GetActiveScene().name);
        yield return Press("Btn_Upgrades");
        Assert.False(FindButton("Btn_ConfirmUpgrade").interactable);
        Assert.IsNull(Object.FindFirstObjectByType<TutorialOverlay>());
        yield return Press("Btn_CloseUpgrades");
        yield return Press("Btn_TutorialContinue");
        Assert.True(SaveSystem.LoadOrNew().tutorialCompleted);
        Assert.AreEqual(0, Game.Save.gold);
        Assert.AreEqual("Home", SceneManager.GetActiveScene().name);
    }

    private static IEnumerator FinishBattle(BattleSceneController battle)
    {
        for (int frame = 0; frame < 600 && battle.IsPlaying; frame++) yield return null;
        Assert.False(battle.IsPlaying, "Battle playback must terminate.");
        Assert.True(battle.View.resultPanel.activeSelf);
    }

    [UnityTest]
    public IEnumerator LiveBattle_ControlsSnapshotsSettlementAndNavigation()
    {
        Game.Save.tutorialCompleted = true;
        Game.Save.units = Game.Data.Characters.Keys.OrderBy(id => id, System.StringComparer.Ordinal)
            .Take(5).Select(id => new OwnedUnit { charId = id, level = 20, copies = 1 }).ToList();
        Game.Save.activeTeam.Clear(); Game.Team.AutoFill(); SaveSystem.Save(Game.Save);
        // Crowded five-versus-five presentation fixture; bundled campaign content is unchanged.
        var wave = Game.Data.Stages["s_1_1"].waves[0];
        string enemyId = wave.enemies[0].enemyId;
        wave.enemies = Enumerable.Range(0, 5).Select(i => new WaveUnit { enemyId = enemyId, level = 1 }).ToList();
        yield return SceneManager.LoadSceneAsync("Battle"); yield return Settle();
        var battle = Object.FindFirstObjectByType<BattleSceneController>();
        Assert.AreEqual(24, battle.View.GetComponentsInChildren<BattleHexGraphic>().Length);
        foreach (var hex in battle.View.GetComponentsInChildren<BattleHexGraphic>()) Assert.NotNull(hex.canvasRenderer);
        foreach (var fighter in battle.View.Fighters.Values) Assert.NotNull(fighter.portrait.canvasRenderer);
        foreach (var fighter in battle.View.Fighters.Values) {
            fighter.hpLabel.ForceMeshUpdate();
            Assert.True(fighter.hpLabel.textInfo.characterInfo.Any(c => c.isVisible), "Numeric HP must render inside its label.");
        }
        Assert.AreEqual(5, battle.View.PlayerIds.Count);
        Assert.AreEqual(10, battle.View.Fighters.Count);
        Assert.NotNull(battle.View.backdrop.texture);
        Assert.IsNull(battle.Session);
        yield return CaptureOptional("landscape-battle-phone");
        yield return CaptureOptional("landscape-battle-tablet", 1600, 1200);
        yield return CaptureOptional("landscape-battle-desktop", 1920, 1080);
        battle.OnRunBattlePressed(); battle.OnPausePressed();
        Assert.True(battle.IsPlaying); Assert.True(battle.IsPaused); Assert.False(battle.Session.Battle.AutoEnabled);
        int count = battle.DisplayedEventCount;
        yield return new WaitForSecondsRealtime(.25f);
        Assert.AreEqual(count, battle.DisplayedEventCount); Assert.IsEmpty(Game.Save.clearedStages);
        string player = battle.View.PlayerIds[0];
        battle.View.signatures[0].button.onClick.Invoke();
        Assert.True(battle.Session.Battle.IsSignatureQueued(player));
        StringAssert.Contains("QUEUED", battle.View.signatures[0].label.text);
        battle.View.signatures[0].button.onClick.Invoke();
        Assert.False(battle.Session.Battle.IsSignatureQueued(player));
        battle.OnSpeedPressed(); Assert.AreEqual(1.5f, battle.PlaybackSpeed);
        battle.OnSpeedPressed(); Assert.AreEqual(2, battle.PlaybackSpeed);
        battle.OnSpeedPressed(); Assert.AreEqual(1, battle.PlaybackSpeed);
        battle.OnAutoPressed(); Assert.True(battle.Session.Battle.AutoEnabled);
        battle.SetReducedMotion(true); Assert.AreEqual(1, PlayerPrefs.GetInt("FUNGUY_REDUCED_MOTION"));
        battle.SetReducedMotion(false);
        battle.OnPausePressed();
        battle.View.Fighters[player].GetComponent<Button>().onClick.Invoke();
        Assert.True(battle.IsPaused); Assert.True(battle.View.inspectorPanel.activeSelf);
        yield return CaptureOptional("landscape-inspector");
        battle.View.inspectorClose.onClick.Invoke(); Assert.False(battle.IsPaused);
        for (int frame = 0; frame < 180 && battle.DisplayedEventCount == count; frame++) yield return null;
        Assert.Greater(battle.DisplayedEventCount, count);
        yield return new WaitForSecondsRealtime(1.2f);
        battle.OnPausePressed();
        yield return CaptureOptional("landscape-battle-live");
        battle.OnPausePressed();
        battle.SetReducedMotion(true);
        battle.SendMessage("OnApplicationFocus", false); Assert.True(battle.IsPaused);
        battle.OnPausePressed(); Assert.False(battle.IsPaused);
        battle.OnFinishPressed(); yield return FinishBattle(battle);
        Assert.AreEqual(BattleOutcome.Victory, battle.Session.Battle.Outcome);
        foreach (var final in battle.Session.Battle.GetState()) {
            var displayed = battle.View.Fighters[final.InstanceId].State;
            Assert.AreEqual(final.Hp, displayed.Hp); Assert.AreEqual(final.Shield, displayed.Shield);
            Assert.AreEqual(final.Energy, displayed.Energy); Assert.AreEqual(final.Slot, displayed.Slot);
        }
        Assert.Contains("s_1_1", Game.Save.clearedStages);
        yield return CaptureOptional("landscape-victory");
        int gold = Game.Save.gold;
        battle.View.retry.onClick.Invoke(); battle.OnFinishPressed(); yield return FinishBattle(battle);
        Assert.AreEqual(gold, Game.Save.gold, "Replay cannot duplicate first-clear rewards.");
        Assert.True(battle.View.next.interactable);
        battle.View.next.onClick.Invoke(); Assert.IsNull(battle.Session);
        battle.OnRunBattlePressed(); var cancelled = battle.Session;
        battle.OnGoTeamPressed(); yield return Settle();
        Assert.True(cancelled.Cancelled); Assert.AreEqual("Team", SceneManager.GetActiveScene().name);
        Assert.AreEqual(gold, Game.Save.gold);
        yield return SceneManager.LoadSceneAsync("Battle"); yield return Settle();
        battle = Object.FindFirstObjectByType<BattleSceneController>();
        Assert.True(battle.ReducedMotion);
        battle.OnRunBattlePressed(); Assert.False(battle.Session.Battle.AutoEnabled);
    }

    [UnityTest]
    public IEnumerator ResumeAtSummon_ReattachesOverlayWithoutReissuingTicket()
    {
        Game.Save.tutorialStep = (int)TutorialStep.DoFirstSummon;
        Game.Save.tutorialTickets = 0;
        SaveSystem.Save(Game.Save);
        yield return SceneManager.LoadSceneAsync("Boot");
        yield return Settle();
        Assert.AreEqual("Summon", SceneManager.GetActiveScene().name);
        Assert.AreEqual(0, Game.Save.tutorialTickets);
        Assert.NotNull(Object.FindFirstObjectByType<TutorialOverlay>());
        Assert.AreEqual("DontDestroyOnLoad", TutorialManager.I.gameObject.scene.name);
    }

    [UnityTest]
    public IEnumerator FormationControls_PlaceSwapPageRemoveAndResumeSparseBoard()
    {
        Game.Save.tutorialCompleted = true;
        Game.Save.units = Game.Data.Characters.Keys.OrderBy(id => id, System.StringComparer.Ordinal)
            .Take(6).Select(id => new OwnedUnit { charId = id, level = 1, copies = 1 }).ToList();
        Game.Save.activeTeam.Clear();
        SaveSystem.Save(Game.Save);
        yield return SceneManager.LoadSceneAsync("Team"); yield return Settle();
        Assert.IsEmpty(Game.Save.activeTeam);
        Assert.AreEqual(12, SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Button>(true)).Count(b => b.name.StartsWith("Btn_FormationSlot")));
        yield return Press("Btn_FormationSlot12");
        var placementHint = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Text>(true)).Single(t => t.name == "Lbl_Hint");
        StringAssert.Contains("Rear 4", placementHint.text);
        yield return Press("Btn_RosterSlot1");
        string first = Game.Save.activeTeam.Single();
        Assert.AreEqual(11, Game.Team.GetFormation().Single().slot);
        yield return Press("Btn_FormationSlot12");
        yield return Press("Btn_FormationSlot1");
        Assert.AreEqual(0, Game.Team.GetFormation().Single().slot);
        yield return Press("Btn_RosterNext");
        yield return Press("Btn_FormationSlot7");
        yield return Press("Btn_RosterSlot1");
        Assert.AreEqual(2, Game.Save.activeTeam.Count);
        Assert.True(Game.Team.GetFormation().Any(p => p.slot == 6 && p.charId != first));
        yield return Press("Btn_FormationSlot1");
        yield return Press("Btn_RemoveSelected");
        Assert.AreEqual(6, Game.Team.GetFormation().Single().slot);
        yield return SceneManager.LoadSceneAsync("Home"); yield return Settle();
        yield return SceneManager.LoadSceneAsync("Team"); yield return Settle();
        Assert.AreEqual(6, Game.Team.GetFormation().Single().slot);
        Assert.AreEqual(6, SaveSystem.LoadOrNew().formation.Single().slot);
        yield return Press("Btn_AutoFill");
        Assert.AreEqual(5, Game.Save.activeTeam.Count);
        yield return CaptureOptional("hex-team");
        yield return Press("Btn_ClearTeam");
        yield return SceneManager.LoadSceneAsync("Home"); yield return Settle();
        yield return SceneManager.LoadSceneAsync("Team"); yield return Settle();
        Assert.IsEmpty(Game.Save.activeTeam);
        yield return Press("Btn_StartBattle");
        Assert.AreEqual("Team", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator TeamControls_ClearSelectAndAutoFillPersistThroughService()
    {
        Game.Save.tutorialCompleted = true;
        Game.Save.units = Game.Data.Characters.Keys.OrderBy(id => id, System.StringComparer.Ordinal)
            .Take(6).Select(id => new OwnedUnit { charId = id, level = 1, copies = 1 }).ToList();
        Game.Save.activeTeam.Clear();
        SaveSystem.Save(Game.Save);
        yield return SceneManager.LoadSceneAsync("Team");
        yield return Settle();
        yield return Press("Btn_AutoFill");
        Assert.AreEqual(5, Game.Save.activeTeam.Count);
        yield return Press("Btn_ClearTeam");
        Assert.IsEmpty(Game.Save.activeTeam);
        yield return Press("Btn_RosterSlot1");
        Assert.AreEqual(1, Game.Save.activeTeam.Count);
        CollectionAssert.AreEqual(Game.Save.activeTeam, SaveSystem.LoadOrNew().activeTeam);
        yield return Press("Btn_RosterSlot1");
        Assert.IsEmpty(Game.Save.activeTeam);
        yield return Press("Btn_AutoFill");
        Assert.AreEqual(5, Game.Save.activeTeam.Count);
        Assert.AreEqual(5, Game.Save.activeTeam.Distinct().Count());
        CollectionAssert.AreEqual(Game.Save.activeTeam, SaveSystem.LoadOrNew().activeTeam);
    }
}
