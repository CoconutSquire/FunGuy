using NUnit.Framework;

public class KeywordRulesTests
{
    [Test]
    public void CsvBackedStatuses_AreRecognized()
    {
        string[] expected = {
            "Vantage", "Stealth", "Intangible", "Luminescence", "FrostShield", "Regen",
            "Reflect", "Immortality", "Cloak", "Poison", "Burn", "Bleed", "Vulnerability",
            "Brittle", "Silence", "Charm", "Confusion", "Freeze", "Root", "Stun"
        };

        foreach (var keyword in expected)
            Assert.IsTrue(KeywordRules.IsCsvBackedStatus(keyword), keyword);
    }

    [Test]
    public void CsvDoTDescriptions_ResolveAliases()
    {
        Assert.IsNotNull(KeywordRules.Description("Poison"));
        Assert.IsNotNull(KeywordRules.Description("Burn"));
        Assert.IsNotNull(KeywordRules.Description("Bleed"));
    }

    [Test]
    public void CsvDefaults_MatchGlossary()
    {
        Assert.AreEqual(.10f, KeywordRules.LuminescenceCritBonus, .0001f);
        Assert.AreEqual(.20f, KeywordRules.VantageAccuracyBonus, .0001f);
        Assert.AreEqual(.15f, KeywordRules.VulnerabilityDamageTakenBonus, .0001f);
        Assert.AreEqual(.10f, KeywordRules.BurnDefenseReductionPerStack, .0001f);
        Assert.AreEqual(.25f, KeywordRules.BrittleCritTakenBonus, .0001f);
        Assert.AreEqual(3, KeywordRules.RegenDefaultTurns);
    }
}
