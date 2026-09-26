using System;
using System.Collections.Generic;

public interface ISummonService
{
    SummonResult Pull(string bannerId, int count);
}

public sealed class SummonResult
{
    public List<string> characterIds = new();
    public bool usedTutorialTicket;
}

public sealed class LocalSummonService : ISummonService
{
    private readonly GameData data;
    private readonly IPlayerSaveStore store;
    private readonly GachaService gacha;
    public LocalSummonService(GameData data, IPlayerSaveStore store, GachaService gacha)
    { this.data = data; this.store = store; this.gacha = gacha; }

    public SummonResult Pull(string bannerId, int count)
    {
        if (count != 1 && count != 10) throw new ArgumentOutOfRangeException(nameof(count));
        if (!data.Banners.TryGetValue(bannerId, out var banner)) throw new InvalidOperationException("Banner unavailable.");
        var save = store.Read();
        int cost = checked(banner.costPerPull * count);
        if (save.summonTickets < cost) throw new InvalidOperationException($"You need {cost} Mycelial Tickets for this summon.");
        bool tutorialPull = count == 1 && !save.tutorialCompleted;
        var result = new SummonResult { usedTutorialTicket = tutorialPull };
        for (int i = 0; i < count; i++) result.characterIds.Add(gacha.PullOne(save, bannerId, true));
        store.Write(save);
        return result;
    }
}
