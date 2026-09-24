using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceSettings", menuName = "FunGuy/Game Balance Settings")]
public sealed class GameBalanceSettings : ScriptableObject
{
    [Header("Idle Generation")]
    [Min(0)] public double maxStoredHours = 24d;
    [Min(0)] public double baseGoldPerHour = 50d;
    [Min(0)] public double goldPerStagePerHour = 10d;
    [Min(0)] public double baseEquipmentPerHour = 0.10d;
    [Min(0)] public double equipmentPerStagePerHour = 0.02d;
    [Range(1, 6)] public int idleBaseRarity = 1;
    [Range(0, 100)] public int idleRarityStagesPerTier = 10;

    [Header("Equipment")] 
    public List<EquipmentDef> equipment = new();
    [Min(0)] public int symbioteRandomStatMin = 10;
    [Min(0)] public int symbioteRandomStatMax = 30;
    [Min(0)] public float symbioteTraitPercentPerRarity = 0.05f;
    [Min(0)] public float equipmentLevelStatGrowth = 0.10f;
    [Min(0)] public float equipmentRarityStatGrowth = 0.20f;
    [Min(0)] public int equipmentEvolutionSporeCost = 100;
    [Min(0)] public float equipmentEvolutionStatGrowth = 0.25f;

    public EquipmentDef GetEquipment(string id)
    {
        return equipment?.Find(x => x != null && x.id == id);
    }
}
