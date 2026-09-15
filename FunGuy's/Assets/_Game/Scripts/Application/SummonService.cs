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
        bool ticket = count == 1 && !save.tutorialCompleted && save.tutorialTickets > 0;
        int cost = ticket ? 0 : checked(banner.costPerPull * count);
        if (save.spores < cost) throw new InvalidOperationException($"You need {cost} spores for this summon.");
        var result = new SummonResult { usedTutorialTicket = ticket };
        for (int i = 0; i < count; i++) result.characterIds.Add(gacha.PullOne(save, bannerId, !ticket));
        if (ticket) save.tutorialTickets--;
        store.Write(save);
        return result;
    }
}
