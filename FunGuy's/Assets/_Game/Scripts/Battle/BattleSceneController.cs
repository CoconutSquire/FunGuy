using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Presentation orchestrator. Damage, targeting and wallet transactions stay behind application contracts.
public sealed class BattleSceneController : MonoBehaviour
{
    [SerializeField] private string stageId = "s_1_1";
    private BattleScreenView view;
    private CampaignSession run;
    private int cursor;
    private float wait;
    private bool playing, manualPause, inspectPause, focusPause, autoEnabled, fastForward, settlementPending;
    private string rewardDescription;
    public bool IsPlaying => playing;
    public bool IsPaused => manualPause || inspectPause || focusPause;
    public bool ReducedMotion { get; private set; }
    public float PlaybackSpeed { get; private set; } = 1;
    public int DisplayedEventCount { get; private set; }
    public CampaignSession Session => run;
    public BattleScreenView View => view;

    public void Configure(BattleScreenView screen)
    {
        view = screen; Game.EnsureInitialized(); view.Initialize(Game.Data);
        if (Game.Campaign.IsUnlocked(Game.SelectedStageId)) stageId = Game.SelectedStageId;
        ReducedMotion = PlayerPrefs.GetInt("FUNGUY_REDUCED_MOTION", 0) == 1;
        view.start.onClick.AddListener(OnRunBattlePressed); view.home.onClick.AddListener(OnBackPressed);
        view.team.onClick.AddListener(OnGoTeamPressed); view.pause.onClick.AddListener(OnPausePressed);
        view.speed.onClick.AddListener(OnSpeedPressed); view.auto.onClick.AddListener(OnAutoPressed);
        view.finish.onClick.AddListener(OnFinishPressed); view.motion.onClick.AddListener(() => SetReducedMotion(!ReducedMotion));
        view.retry.onClick.AddListener(OnRunBattlePressed); view.next.onClick.AddListener(OnNextStagePressed); view.resultTeam.onClick.AddListener(OnGoTeamPressed);
        view.Inspect = id => { inspectPause = playing; view.ShowInspector(id); RefreshControls(); };
        view.inspectorClose.onClick.AddListener(() => { inspectPause = false; view.inspectorPanel.SetActive(false); RefreshControls(); });
        for (int i = 0; i < view.signatures.Length; i++) { int index = i; view.signatures[i].button.onClick.AddListener(() => OnSignaturePressed(index)); }
        ShowPreview();
    }
    private void ShowPreview()
    {
        if (!Game.Data.Stages.TryGetValue(stageId, out var stage)) { view.feedLabel.text = "Stage unavailable."; return; }
        view.stageLabel.text = (stage.boss ? "BOSS · " : "") + stage.name;
        rewardDescription = $"+{stage.rewards.gold} gold   +{stage.rewards.spores} spores   +{stage.rewards.accountXp} XP";
        view.waveLabel.text = $"{stageId.Replace("s_", "STAGE ").Replace('_', '-')}   ·   {stage.waves.Count} waves   ·   " +
            (Game.Save.clearedStages.Contains(stageId) ? "Practice · rewards already claimed" : rewardDescription);
        var states = new List<BattleFighterState>();
        foreach (var placement in FormationRules.Resolve(Game.Save)) {
            var owned = Game.Save.units.First(u => u.charId == placement.charId);
            var fighter = CombatUnitFactory.Create(Game.Data.Characters[placement.charId], owned.level, TeamSide.Player, owned.stars, Game.Data.StatRules);
            fighter.formationSlot = placement.slot;
            states.Add(new BattleFighterState("preview/player/" + placement.slot, fighter));
        }
        var enemies = stage.waves[0].enemies.Select(e => CombatUnitFactory.Create(Game.Data.Enemies[e.enemyId], e, TeamSide.Enemy)).ToList();
        FormationRules.AssignBattleSlots(enemies);
        states.AddRange(enemies.Select(e => new BattleFighterState("preview/enemy/" + e.formationSlot, e)));
        view.SetFighters(states); view.resultPanel.SetActive(false);
        view.playerBonuses.text = "YOUR DEPLOYMENT  ·  " + states.Count(s => s.Side == TeamSide.Player) + " / 5 fighters";
        view.enemyBonuses.text = "ENEMY DEPLOYMENT  ·  wave 1";
        view.feedLabel.text = stage.description ?? "Choose your formation in Team. Basics are automatic; tap a signature to queue it.";
        RefreshControls();
    }
    public void OnRunBattlePressed()
    {
        if (playing) return;
        if (settlementPending) { Settle(); return; }
        try {
            run = Game.Campaign.Begin(stageId, auto: autoEnabled); playing = true;
            manualPause = inspectPause = focusPause = fastForward = false; cursor = 0; wait = .35f; DisplayedEventCount = 0;
            view.resultPanel.SetActive(false); view.inspectorPanel.SetActive(false);
            view.retry.GetComponentInChildren<TMPro.TMP_Text>().text = "Play again";
            StartWave();
            if (!Game.Save.tutorialCompleted && Game.Save.tutorialStep == (int)TutorialStep.StartFirstBattle)
                FindFirstObjectByType<TutorialOverlay>()?.Hide();
        } catch (Exception error) { run?.Cancel(); playing = false; view.feedLabel.text = error.Message; }
        RefreshControls();
    }
    private void StartWave()
    {
        cursor = 0; view.SetFighters(run.Battle.InitialState); view.RefreshBonuses(run.Battle);
        view.waveLabel.text = $"WAVE {run.Wave} / {run.WaveCount}  ·  {Game.Data.Stages[stageId].name}";
        view.feedLabel.text = $"Wave {run.Wave} begins";
    }
    private void Update()
    {
        if (view == null || !playing || IsPaused) return;
        float delta = Time.unscaledDeltaTime * PlaybackSpeed;
        view.Tick(delta, ReducedMotion || fastForward);
        wait -= delta;
        for (int budget = 0; budget < 96 && playing && wait <= 0; budget++) {
            if (cursor < run.Battle.Events.Count) {
                var e = run.Battle.Events[cursor++]; DisplayedEventCount++;
                view.Consume(e, ReducedMotion || fastForward);
                if (e.Kind == BattleEventKind.ActionCompleted) view.RefreshBonuses(run.Battle);
                wait += EventDuration(e.Kind);
            } else if (run.Battle.Outcome == BattleOutcome.Running) {
                try { run.Step(); }
                catch (Exception error) { run.Cancel(); playing = false; view.feedLabel.text = "Battle stopped: " + error.Message; }
            } else if (run.AdvanceWave()) { StartWave(); wait = fastForward ? 0 : .55f; }
            else { playing = false; Settle(); }
        }
        RefreshControls();
    }
    private float EventDuration(BattleEventKind kind)
    {
        if (fastForward) return 0;
        if (ReducedMotion) return kind == BattleEventKind.ActionCompleted ? .18f : 0;
        return kind switch {
            BattleEventKind.SkillUsed => .24f, BattleEventKind.Damage => .16f,
            BattleEventKind.Heal or BattleEventKind.Shield => .1f,
            BattleEventKind.UnitDied => .25f, BattleEventKind.ActionCompleted => .18f, _ => 0
        };
    }
    private void Settle()
    {
        CampaignResult result;
        try {
            result = run.Complete(); settlementPending = false;
        } catch (Exception error) {
            settlementPending = true;
            view.ShowResult("REWARDS NOT SAVED", "The battle is complete. Retry saving to claim safely.\n" + error.Message, false);
            view.retry.GetComponentInChildren<TMPro.TMP_Text>().text = "Retry saving";
            RefreshControls(); return;
        }
            view.retry.GetComponentInChildren<TMPro.TMP_Text>().text = "Play again";
            string title = result.outcome switch { BattleOutcome.Victory => "VICTORY", BattleOutcome.Draw => "DRAW", BattleOutcome.Timeout => "TIME LIMIT", _ => "DEFEAT" };
            string detail = result.won ? $"All {result.waveCount} waves cleared.\n" + (result.firstClearRewardGranted ? rewardDescription + "\nFirst-clear rewards saved." : "Practice clear. First-clear rewards were already claimed.") :
                $"Reached wave {result.wavesReached} / {result.waveCount}.\n" + (result.outcome == BattleOutcome.Timeout ? "Try another formation or signature timing." : "Adjust your formation and try again.");
            view.ShowResult(title, detail, NextStageId() != null && Game.Save.tutorialCompleted);
            view.feedLabel.text = title;
            TutorialManager.I?.OnFirstBattleCompleted(result.won);
        RefreshControls();
    }
    public void OnSignaturePressed(int index)
    {
        if (!playing || index < 0 || index >= view.PlayerIds.Count) return;
        string id = view.PlayerIds[index];
        if (run.Battle.IsSignatureQueued(id)) run.Battle.CancelSignature(id); else run.Battle.QueueSignature(id);
        RefreshControls();
    }
    public void OnPausePressed() { if (playing) { manualPause = focusPause ? false : !manualPause; focusPause = false; RefreshControls(); } }
    public void OnAutoPressed() { autoEnabled = !autoEnabled; if (playing) run.Battle.SetAuto(autoEnabled); RefreshControls(); }
    public void OnSpeedPressed() { PlaybackSpeed = PlaybackSpeed == 1 ? 1.5f : PlaybackSpeed == 1.5f ? 2 : 1; RefreshControls(); }
    public void SetReducedMotion(bool enabled) { ReducedMotion = enabled; PlayerPrefs.SetInt("FUNGUY_REDUCED_MOTION", enabled ? 1 : 0); PlayerPrefs.Save(); RefreshControls(); }
    public void OnFinishPressed()
    {
        if (!playing) return;
        fastForward = true; autoEnabled = true; manualPause = inspectPause = focusPause = false;
        view.inspectorPanel.SetActive(false); run.Battle.SetAuto(true); wait = 0; RefreshControls();
    }
    private void RefreshControls() { if (view != null) view.RefreshControls(run, playing, IsPaused, autoEnabled, PlaybackSpeed, ReducedMotion, settlementPending); }
    private string NextStageId()
    {
        var ordered = Game.Data.Stages.Values.OrderBy(s => s.order).Select(s => s.id).ToList(); int index = ordered.IndexOf(stageId);
        return index >= 0 && index + 1 < ordered.Count && Game.Campaign.IsUnlocked(ordered[index + 1]) ? ordered[index + 1] : null;
    }
    public void OnNextStagePressed() { if (playing || settlementPending) return; string next = NextStageId(); if (next == null) return; stageId = Game.SelectedStageId = next; run = null; ShowPreview(); }
    public void OnSelectStage(string id) { if (playing || settlementPending || !Game.Campaign.IsUnlocked(id)) return; stageId = Game.SelectedStageId = id; run = null; ShowPreview(); }
    public void OnRetryPressed() => OnRunBattlePressed();
    public void OnBackPressed() { Cancel(); SceneManager.LoadScene("Home"); }
    public void OnGoTeamPressed() { Cancel(); SceneManager.LoadScene("Team"); }
    private void Cancel() { if (playing || settlementPending) run?.Cancel(); playing = false; }
    private void OnDisable() => Cancel();
    private void OnApplicationPause(bool paused) { if (playing && paused) { focusPause = true; RefreshControls(); } }
    private void OnApplicationFocus(bool focused) { if (playing && !focused) { focusPause = true; RefreshControls(); } }
}
