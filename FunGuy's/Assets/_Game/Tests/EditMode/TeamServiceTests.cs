using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class TeamServiceTests
{
    private sealed class Store : IPlayerSaveStore
    {
        public PlayerSave state = new();
        public int writes;
        public bool failWrite;
        public PlayerSave Read() => JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(state));
        public void Write(PlayerSave save)
        {
            if (failWrite) throw new InvalidOperationException("Storage unavailable");
            state = JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(save));
            writes++;
        }
    }

    private static LocalTeamService Create(Store store)
    {
        var data = new GameData();
        foreach (var id in new[] { "f", "e", "d", "c", "b", "a" })
        {
            data.Characters[id] = new CharacterDef { id = id, rarity = 4 };
            store.state.units.Add(new OwnedUnit { charId = id, level = 1 });
        }
        return new LocalTeamService(data, store);
    }

    [Test]
    public void Replace_RejectsUnownedDuplicatesAndOverflowWithoutMutation()
    {
        var store = new Store(); var team = Create(store);
        store.state.activeTeam.Add("a");
        var before = JsonUtility.ToJson(store.state);
        Assert.Throws<ArgumentException>(() => team.Replace(new[] { "a", "missing" }));
        Assert.Throws<ArgumentException>(() => team.Replace(new[] { "a", "a" }));
        Assert.Throws<ArgumentException>(() => team.Replace(new[] { "a", "b", "c", "d", "e", "f" }));
        Assert.AreEqual(before, JsonUtility.ToJson(store.state));
        Assert.AreEqual(0, store.writes);
    }

    [Test]
    public void Selection_PreservesOrderAndAvoidsDuplicateOrNoOpWrites()
    {
        var store = new Store(); var team = Create(store);
        team.Replace(new[] { "b", "a" });
        team.Replace(new[] { "b", "a" });
        Assert.False(team.Add("a"));
        Assert.False(team.Add("missing"));
        Assert.False(team.Remove("missing"));
        Assert.AreEqual(1, store.writes);
        CollectionAssert.AreEqual(new[] { "b", "a" }, store.state.activeTeam);
        Assert.True(team.Remove("b"));
        Assert.True(team.Add("c"));
        CollectionAssert.AreEqual(new[] { "a", "c" }, store.state.activeTeam);
    }

    [Test]
    public void AutoFill_UsesStableTieBreakAndCapsAtFive()
    {
        var store = new Store(); var team = Create(store);
        store.state.units.Add(new OwnedUnit { charId = "retired", level = 999 });
        team.AutoFill();
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d", "e" }, store.state.activeTeam);
        Assert.False(team.Add("f"));
        Assert.AreEqual(1, store.writes);
        team.Clear();
        team.Clear();
        Assert.IsEmpty(store.state.activeTeam);
        Assert.AreEqual(2, store.writes);
    }

    [Test]
    public void StorageFailure_DoesNotPublishPartialSelection()
    {
        var store = new Store(); var team = Create(store);
        store.state.activeTeam.Add("a"); store.failWrite = true;
        Assert.Throws<InvalidOperationException>(() => team.Add("b"));
        CollectionAssert.AreEqual(new[] { "a" }, store.state.activeTeam);
        Assert.AreEqual(0, store.writes);
    }
}
