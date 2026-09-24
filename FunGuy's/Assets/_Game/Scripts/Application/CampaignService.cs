using System;
using System.Collections.Generic;
using System.Linq;

public interface ICampaignService
{
    bool IsUnlocked(string stageId);
    CampaignSession Begin(string stageId, int? seed = null, bool auto = false, int maxActionsPerWave = 200);
    CampaignResult Run(string stageId);
    bool ClaimTutorialBattleReward();
}

public sealed class CampaignResult
{
    public string contentVersion;
    public bool won;
    public bool firstClearRewardGranted;
    public int wavesReached;
    public int waveCount;
    public BattleOutcome outcome;
    public int seed;
    public string rulesVersion = BattleSession.RulesVersion;
    public IReadOnlyList<CampaignWaveResult> battles { get; internal set; }
}

public sealed class CampaignWaveResult
{
    public int Wave { get; }
    public BattleOutcome Outcome { get; }
    public IReadOnlyList<BattleFighterState> InitialState { get; }
    public IReadOnlyList<BattleEvent> Events { get; }
    public IReadOnlyList<BattleCommandRecord> Commands { get; }
    public IReadOnlyList<BattleFighterState> FinalState { get; }
    internal CampaignWaveResult(int wave, BattleSession session) {
        Wave = wave; Outcome = session.Outcome; InitialState = session.InitialState; Events = session.Events;
        Commands = session.Commands; FinalState = session.GetState();
    }
}

// Local adapter for the playable slice. Production must execute this operation on the server.
public sealed class LocalCampaignService : ICampaignService
{
    private readonly GameData data;
    private readonly IPlayerSaveStore store;
    public LocalCampaignService(GameData data, IPlayerSaveStore store) { this.data = data; this.store = store; }

    public bool IsUnlocked(string stageId) => IsUnlocked(store.Read(), stageId);

    private bool IsUnlocked(PlayerSave save, string stageId)
    {
        var stages = data.Stages.Values.OrderBy(s => s.order).ToList();
        int index = stages.FindIndex(s => s.id == stageId);
        return index >= 0 && (index == 0 || save.clearedStages.Contains(stages[index - 1].id));
    }

    public CampaignResult Run(string stageId)
    {
        var session = Begin(stageId, auto: true);
        do { while (session.Battle.Outcome == BattleOutcome.Running) session.Step(); }
        while (session.AdvanceWave());
        return session.Complete();
    }

    public CampaignSession Begin(string stageId, int? seed = null, bool auto = false, int maxActionsPerWave = 200)
    {
        var save = store.Read();
        if (!IsUnlocked(save, stageId)) throw new InvalidOperationException("Complete the previous stage first.");
        var stage = data.Stages[stageId];
        var selected = save.activeTeam.Distinct().ToList();
        if (selected.Count == 0 || selected.Count > 5) throw new InvalidOperationException("Select between one and five units in Team.");
        var player = new List<CombatUnit>();
        foreach (var placement in FormationRules.Resolve(save))
        {
            string id = placement.charId;
            var owned = save.units.FirstOrDefault(u => u.charId == id);
            if (owned == null || !data.Characters.TryGetValue(id, out var definition)) throw new InvalidOperationException("Your team contains an unavailable unit.");
            var fighter = CombatUnitFactory.Create(definition, owned.level, TeamSide.Player, owned.stars, data.StatRules);
            EquipmentService.ApplyTo(fighter, owned);
            fighter.formationSlot = placement.slot;
            player.Add(fighter);
        }
        if (player.Count != selected.Count) throw new InvalidOperationException("Your team contains an unavailable unit.");
        var waves = stage.waves.Select(w => w.enemies.Select(u =>
            CombatUnitFactory.Create(data.Characters[u.enemyId], u, TeamSide.Enemy, data.StatRules)).ToList()).ToList();
        var reward = new RewardDef { gold = stage.rewards.gold, spores = stage.rewards.spores, accountXp = stage.rewards.accountXp };
        return new CampaignSession(data, player, waves, seed ?? new Random().Next(), auto, maxActionsPerWave,
            () => CommitFirstClear(stageId, reward, stage.boss), stage.encounterVersion ?? "legacy");
    }

    private bool CommitFirstClear(string stageId, RewardDef reward, bool boss)
    {
        // Re-read at settlement so a long-running battle cannot overwrite newer team/summon/claim writes.
        var save = store.Read();
        if (!save.clearedStages.Contains(stageId))
        {
            checked {
                save.gold += reward.gold;
                save.spores += reward.spores;
                save.accountXp += reward.accountXp;
            }
            save.clearedStages.Add(stageId);
            if (Game.Equipment != null) {
                if (boss) Game.Equipment.GrantIfMissing("stipe", 2, 1);
                else if (save.clearedStages.Count == 1) Game.Equipment.GrantIfMissing("cap", 1, 1);
                if (new Random().NextDouble() < .05) Game.Equipment.AddToInventory(Game.Equipment.Acquire("symbiote", 3, 1));
            }
            store.Write(save);
            return true;
        }
        return false;
    }

    public bool ClaimTutorialBattleReward()
    {
        var save = store.Read();
        if (save.tutorialBattleRewardClaimed || save.clearedStages.Count == 0) return false;
        checked { save.gold += 100; save.spores += 10; }
        save.tutorialBattleRewardClaimed = true;
        store.Write(save);
        return true;
    }
}
