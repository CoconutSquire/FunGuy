using System;
using System.Collections.Generic;

// Application-owned encounter. Presentation advances one action or one wave after playback.
// Persistence is only touched by Complete, never by damage events or UI callbacks.
public sealed class CampaignSession
{
    private readonly GameData data;
    private readonly List<CombatUnit> player;
    private readonly List<List<CombatUnit>> waves;
    private readonly Random rng;
    private readonly int maxActions;
    private readonly Func<bool> commit;
    private readonly List<CampaignWaveResult> history = new();
    private CampaignResult result;
    public string EncounterId { get; } = Guid.NewGuid().ToString("N");
    public int Seed { get; }
    public string ContentVersion { get; }
    public int Wave { get; private set; } = 1;
    public int WaveCount => waves.Count;
    public bool Cancelled { get; private set; }
    public BattleSession Battle { get; private set; }

    internal CampaignSession(GameData data, List<CombatUnit> player, List<List<CombatUnit>> waves,
        int seed, bool auto, int maxActions, Func<bool> commit, string contentVersion)
    {
        if (waves.Count == 0 || maxActions < 0) throw new ArgumentException("Invalid campaign limits.");
        this.data = BattleSession.SnapshotSkills(data); this.player = player; this.waves = waves;
        this.maxActions = maxActions; this.commit = commit; Seed = seed; rng = new Random(seed);
        ContentVersion = contentVersion;
        StartWave(auto);
    }

    private void StartWave(bool auto) => Battle = new BattleSession(data, player, waves[Wave - 1],
        rng, maxActions, auto, EncounterId, Wave, false);

    public IReadOnlyList<BattleEvent> Step() => Cancelled || result != null ? Array.Empty<BattleEvent>() : Battle.Step();

    public bool AdvanceWave()
    {
        if (Cancelled || result != null || Battle.Outcome != BattleOutcome.Victory || Wave == WaveCount) return false;
        history.Add(new CampaignWaveResult(Wave, Battle));
        bool auto = Battle.AutoEnabled;
        Wave++; StartWave(auto);
        return true;
    }

    public void Cancel() { if (result == null) Cancelled = true; }

    public CampaignResult Complete()
    {
        if (Cancelled) throw new InvalidOperationException("Campaign was cancelled.");
        if (result != null) return result;
        if (Battle.Outcome == BattleOutcome.Running || (Battle.Outcome == BattleOutcome.Victory && Wave < WaveCount))
            throw new InvalidOperationException("Finish all campaign waves before settlement.");
        bool won = Battle.Outcome == BattleOutcome.Victory && Wave == WaveCount;
        // Publish completion only after a successful atomic write. A storage failure is safely retryable.
        bool rewarded = won && commit();
        var completed = new List<CampaignWaveResult>(history) { new(Wave, Battle) };
        result = new CampaignResult { won = won, firstClearRewardGranted = rewarded, wavesReached = Wave,
            waveCount = WaveCount, outcome = Battle.Outcome, seed = Seed, contentVersion = ContentVersion, battles = completed.AsReadOnly() };
        return result;
    }
}
