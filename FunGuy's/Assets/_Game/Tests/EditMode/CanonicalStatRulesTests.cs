using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CanonicalStatRulesTests
{
    private static StatRulesFile File() => JsonLoader.LoadFromResources<StatRulesFile>("GameData/stat_rules");
    private static StatRulesCatalog Rules() => new(File());
    private static int[] Values(StatBlock s) => new[] { s.hp, s.atk, s.def, s.spd, s.pot };

    // Independently worked reference values, not computed by the implementation under test.
    [TestCase("Tank", 1, 1, 150, 90, 150, 105, 120)]
    [TestCase("Tank", 20, 1, 1385, 185, 378, 105, 215)]
    [TestCase("Tank", 100, 1, 6585, 585, 1338, 105, 615)]
    [TestCase("Tank", 200, 6, 45798, 3798, 8883, 180, 3902)]
    [TestCase("DPS", 1, 1, 90, 180, 90, 165, 90)]
    [TestCase("DPS", 20, 1, 565, 408, 185, 165, 185)]
    [TestCase("DPS", 100, 1, 2565, 1368, 585, 165, 585)]
    [TestCase("DPS", 200, 6, 17728, 8988, 3798, 240, 3798)]
    [TestCase("Support", 1, 1, 120, 80, 120, 135, 160)]
    [TestCase("Support", 20, 1, 690, 175, 215, 135, 388)]
    [TestCase("Support", 100, 1, 3090, 575, 615, 135, 1348)]
    [TestCase("Support", 200, 6, 21315, 3762, 3902, 210, 8918)]
    [TestCase("Assassin", 1, 1, 80, 160, 80, 195, 100)]
    [TestCase("Assassin", 20, 1, 365, 388, 137, 195, 195)]
    [TestCase("Assassin", 100, 1, 1565, 1348, 377, 195, 595)]
    [TestCase("Assassin", 200, 6, 10728, 8918, 2370, 270, 3832)]
    [TestCase("Mage", 20, 1, 442, 340, 128, 145, 320)]
    [TestCase("Tactician", 20, 1, 518, 252, 176, 165, 340)]
    public void ClassReferences_MatchWorkedExamples(string c, int level, int stars, int hp, int atk, int def, int spd, int pot) =>
        CollectionAssert.AreEqual(new[] { hp, atk, def, spd, pot }, Values(Rules().CalculateClass(c, level, stars)));

    [Test]
    public void NamedRows_PreserveSourceStatsAndNeutralityTaxWithoutApplyingItTwice()
    {
        var rules = Rules();
        Assert.AreEqual(14, File().characters.Count);
        CollectionAssert.AreEqual(new[] { 190, 70, 155, 70, 130 }, Values(rules.Character("sheet_01").baseStats));
        CollectionAssert.AreEqual(new[] { 1425, 165, 383, 85, 225 }, Values(rules.CalculateCharacter("sheet_01", 20, 1)));
        CollectionAssert.AreEqual(new[] { 45938, 3728, 8900, 160, 3938 }, Values(rules.CalculateCharacter("sheet_01", 200, 6)));
        CollectionAssert.AreEqual(new[] { 160, 60, 160, 105, 105 }, Values(rules.CalculateCharacter("sheet_42", 1, 1)));
        Assert.AreEqual(575, rules.Character("sheet_42").bst);
        Assert.AreEqual(500, rules.Character("sheet_R09").bst);
        Assert.AreEqual("Biome-less", rules.Character("sheet_R09").biome);
        Assert.AreEqual("Tank", rules.Character("sheet_14").classArchetype);
        Assert.AreEqual("Battery", rules.Character("sheet_14").role);
    }

    [Test]
    public void Evolution_EnforcesEveryCapAndUsesFlatSpeedRatherThanLevelGrowth()
    {
        var rules = Rules();
        int[] caps = { 100, 120, 140, 160, 180, 200 };
        int[] tankHpAtOne = { 150, 180, 225, 300, 375, 525 };
        for (int stars = 1; stars <= 6; stars++)
        {
            int s = stars;
            Assert.AreEqual(caps[s - 1], rules.LevelCap(s));
            Assert.AreEqual(tankHpAtOne[s - 1], rules.CalculateClass("Tank", 1, s).hp);
            Assert.AreEqual(90 + 15 * s, rules.CalculateClass("Tank", caps[s - 1], s).spd);
            Assert.Throws<ArgumentOutOfRangeException>(() => rules.CalculateClass("Tank", caps[s - 1] + 1, s));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => rules.CalculateClass("Tank", 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => rules.CalculateClass("Tank", 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rules.CalculateClass("Tank", 1, 7));
    }

    [Test]
    public void Biomes_AllThirtySixMatchupsUseDirectedAdvantageAndExplicitNeutrality()
    {
        // Forest, Wetlands, Decay, Tundra, Kitchen, Biome-less; rows attack columns.
        int[,] expected = { {100,150,100,75,100,100}, {75,100,150,100,100,100},
            {100,75,100,150,100,100}, {150,100,75,100,100,100},
            {100,100,100,100,100,100}, {100,100,100,100,100,100} };
        for (int a = 0; a < 6; a++) for (int d = 0; d < 6; d++)
            Assert.AreEqual(expected[a,d], DamageCalculator.Calculate(100, 0, 1m, (Biome)a, (Biome)d), $"{(Biome)a} -> {(Biome)d}");
        Assert.AreEqual(Biome.Forest, BiomeRules.Parse(" FOREST "));
        Assert.Throws<ArgumentException>(() => BiomeRules.Parse("Ember"));
        Assert.Throws<ArgumentException>(() => BiomeRules.Parse(null));
        Assert.Throws<ArgumentOutOfRangeException>(() => BiomeRules.DamageMultiplier((Biome)99, Biome.Kitchen));
    }

    [TestCase(3000, 3000, 1, 1500)]
    [TestCase(3000, 3000, 2, 3000)]
    [TestCase(5, 3000, 1, 2)]
    [TestCase(7, 3000, 1, 4)]
    [TestCase(1, 3000, 1, 1)]
    [TestCase(100, -10, 1, 100)]
    public void Damage_UsesRevisedConstantSingleFinalRoundingAndMinimum(int atk, int def, int scale, int expected) =>
        Assert.AreEqual(expected, DamageCalculator.Calculate(atk, def, scale, Biome.Kitchen, Biome.Forest));

    [Test]
    public void Catalog_RejectsInconsistentDataAndDefensivelyCopiesItsInputs()
    {
        var f = File(); f.schemaVersion = 99;
        Assert.Throws<InvalidOperationException>(() => new StatRulesCatalog(f));
        f = File(); f.characters[0].bst++;
        Assert.Throws<InvalidOperationException>(() => new StatRulesCatalog(f));
        f = File(); f.characters[0].biome = "Nature";
        Assert.Throws<ArgumentException>(() => new StatRulesCatalog(f));
        f = File(); f.classes[0].growth.hp = float.NaN;
        Assert.Throws<InvalidOperationException>(() => new StatRulesCatalog(f));
        f = File(); f.evolution[0].levelCap = 200;
        Assert.Throws<InvalidOperationException>(() => new StatRulesCatalog(f));
        f = File(); var rules = new StatRulesCatalog(f);
        f.characters[0].baseStats.hp = 1; f.classes[0].growth.hp = 0; f.evolution[0].multiplier = 100;
        rules.Character("sheet_01").baseStats.hp = 2;
        Assert.AreEqual(1425, rules.CalculateCharacter("sheet_01", 20, 1).hp);
    }

    private sealed class MemoryStore : IPlayerSaveStore
    {
        public PlayerSave state = new();
        public int writes;
        public PlayerSave Read() => JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(state));
        public void Write(PlayerSave value) { state = value; writes++; }
    }

    [Test]
    public void CanonicalProfile_IsUsedByFactoryAndCampaignWithOwnedEvolution()
    {
        var data = new GameData(); data.LoadAll();
        var definition = new CharacterDef { id = "canonical_test", statProfileId = "sheet_01",
            skills = new SkillRefs { basic = "atk_basic", ult = "atk_basic" } };
        var fighter = CombatUnitFactory.Create(definition, 200, TeamSide.Player, 6, data.StatRules);
        Assert.AreEqual(45938, fighter.maxHp);
        Assert.AreEqual(3728, fighter.atk);
        Assert.AreEqual(160, fighter.spd);
        Assert.AreEqual("Golden Chanterelle", fighter.name);
        Assert.AreEqual("Forest", fighter.biome);
        Assert.Throws<ArgumentNullException>(() => CombatUnitFactory.Create(definition, 1, TeamSide.Player));
        data.Characters.Add(definition.id, definition); // Test-only kit; no invented production skill import.
        var store = new MemoryStore();
        store.state.units.Add(new OwnedUnit { charId = definition.id, level = 200, stars = 6 });
        store.state.activeTeam.Add(definition.id);
        var service = new LocalCampaignService(data, store);
        Assert.True(service.Run("s_1_1").won);
        Assert.AreEqual(1, store.writes);
        store.state.units[0].stars = 1;
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Run("s_1_1"));
        Assert.AreEqual(1, store.writes, "Invalid evolution must not grant rewards or write a save.");
    }

    [Test]
    public void CanonicalContent_RejectsDuplicateStatsAndLegacyBiomeAliases()
    {
        var c = JsonLoader.LoadFromResources<CharactersFile>("GameData/characters");
        var s = JsonLoader.LoadFromResources<SkillsFile>("GameData/skills");
        var st = JsonLoader.LoadFromResources<StagesFile>("GameData/stages");
        var b = JsonLoader.LoadFromResources<BannersFile>("GameData/banners");
        var p = Rules().Character("sheet_01");
        var unit = c.characters[0]; unit.statProfileId = p.id; unit.name = p.name; unit.rarityTier = p.rarityTier;
        unit.biome = p.biome; unit.classArchetype = p.classArchetype; unit.role = p.role;
        Assert.Throws<InvalidOperationException>(() => GameDataValidator.Validate(c, s, st, b, Rules()));
        unit.baseStats = null; unit.growth = null; unit.statModel = null;
        Assert.DoesNotThrow(() => GameDataValidator.Validate(c, s, st, b, Rules()));
        unit.biome = "Nature";
        Assert.Throws<ArgumentException>(() => GameDataValidator.Validate(c, s, st, b, Rules()));
    }
}
