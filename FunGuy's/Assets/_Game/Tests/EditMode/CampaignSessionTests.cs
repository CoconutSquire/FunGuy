using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class CampaignSessionTests
{
    private sealed class Store : IPlayerSaveStore {
        public PlayerSave state = new();
        public int writes;
        public bool fail;
        public PlayerSave Read() => JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(state));
        public void Write(PlayerSave value) {
            if (fail) throw new InvalidOperationException("Disk unavailable");
            state = value; writes++;
        }
    }
    private static (GameData data, Store store, LocalCampaignService service) Setup() {
        var data = new GameData(); data.LoadAll();
        var store = new Store();
        store.state.units.Add(new() { charId = "1", level = 100, stars = 1 });
        store.state.activeTeam.Add("1");
        return (data, store, new LocalCampaignService(data, store));
    }
    private static void Drain(CampaignSession run) {
        do { while (run.Battle.Outcome == BattleOutcome.Running) run.Step(); } while (run.AdvanceWave());
    }

    [Test] public void InteractiveWavesCarryStateAutoAndIdentityWithoutEarlyRewards() {
        var (data, store, service) = Setup();
        var run = service.Begin("s_1_1", seed: 19);
        Assert.False(run.Battle.AutoEnabled);
        Assert.Throws<InvalidOperationException>(() => run.Complete());
        Assert.False(run.AdvanceWave());
        run.Battle.SetAuto(true);
        while (run.Battle.Outcome == BattleOutcome.Running) run.Step();
        var first = run.Battle;
        var survivor = first.GetState().First(s => s.Side == TeamSide.Player);
        Assert.AreEqual(0, store.writes);
        Assert.True(run.AdvanceWave());
        var carried = run.Battle.InitialState.First(s => s.Side == TeamSide.Player);
        Assert.AreEqual(survivor.InstanceId, carried.InstanceId);
        Assert.AreEqual(survivor.Hp, carried.Hp); Assert.AreEqual(survivor.Energy, carried.Energy);
        Assert.AreEqual(survivor.Shield, carried.Shield); Assert.AreEqual(survivor.Gauge, carried.Gauge);
        Assert.AreEqual(survivor.SignatureCooldown, carried.SignatureCooldown);
        Assert.True(run.Battle.AutoEnabled);
        Assert.IsEmpty(first.Step());
        Drain(run); var result = run.Complete();
        Assert.True(result.won); Assert.AreEqual(run.WaveCount, result.battles.Count);
        Assert.IsNotEmpty(result.battles[0].Commands);
        Assert.AreEqual(BattleSession.RulesVersion, result.rulesVersion);
        Assert.AreSame(result, run.Complete()); Assert.AreEqual(1, store.writes);
    }

    [Test] public void CompletionMergesFreshSaveAndCompetingFirstClearsAwardOnce() {
        var (data, store, service) = Setup();
        var a = service.Begin("s_1_1", seed: 5, auto: true);
        var b = service.Begin("s_1_1", seed: 5, auto: true);
        Assert.AreNotEqual(a.EncounterId, b.EncounterId);
        Drain(a); Drain(b);
        store.state.gold = 1234; store.state.activeTeam.Clear();
        int reward = data.Stages["s_1_1"].rewards.gold;
        Assert.True(a.Complete().firstClearRewardGranted);
        Assert.False(b.Complete().firstClearRewardGranted);
        Assert.AreEqual(1234 + reward, store.state.gold);
        Assert.IsEmpty(store.state.activeTeam); Assert.AreEqual(1, store.writes);
    }

    [Test] public void FrozenContentAndRewardPayloadSurviveCatalogEditsAndFailedWriteRetry() {
        var (data, store, service) = Setup();
        int gold = store.state.gold, reward = data.Stages["s_1_1"].rewards.gold;
        var run = service.Begin("s_1_1", seed: 4, auto: true);
        data.Skills.Clear(); data.Enemies.Clear(); data.Stages["s_1_1"].waves.Clear();
        data.Stages["s_1_1"].rewards.gold = 99999;
        Drain(run); store.fail = true;
        Assert.Throws<InvalidOperationException>(() => run.Complete());
        Assert.AreEqual(gold, store.state.gold); Assert.AreEqual(0, store.writes);
        store.fail = false;
        Assert.True(run.Complete().firstClearRewardGranted);
        Assert.AreEqual(gold + reward, store.state.gold); Assert.AreEqual(1, store.writes);
    }

    [Test] public void TimeoutCancellationAndIncompleteWavesCannotClaim() {
        var (_, store, service) = Setup();
        var timeout = service.Begin("s_1_1", seed: 2, maxActionsPerWave: 0);
        Assert.AreEqual(BattleOutcome.Timeout, timeout.Complete().outcome);
        Assert.False(timeout.Complete().firstClearRewardGranted);
        var cancelled = service.Begin("s_1_1", seed: 2);
        cancelled.Cancel(); Assert.IsEmpty(cancelled.Step()); Assert.False(cancelled.AdvanceWave());
        Assert.Throws<InvalidOperationException>(() => cancelled.Complete());
        Assert.AreEqual(0, store.writes);
        Assert.Throws<InvalidOperationException>(() => service.Begin("s_1_2"));
    }
}
