using System.Collections.Generic;

public enum TeamSide { Player, Enemy }

public class CombatUnit {
  public TeamSide side;
  public string id; // charId or enemyId
  public string name;
  public int level;
  public int formationSlot = -1; // -1 is legacy/unassigned; runtime fixes positions once before the battle.
  public string biome;
  public string classArchetype;
  public string role;

  public int maxHp;
  public int hp;
  public int atk;
  public int def;
  public int spd;
  public int pot;
  public float critChance;
  public float healingBonus;
  public float shieldBonus;
  public float burnChanceBonus;

  public string basicSkillId;
  public string ultSkillId;
  public string ultimateSkillId;

  public int ultCdRemaining;
  public int ultimateCdRemaining;
  public int energy;
  public int maxEnergy = 100;
  public float actionGauge;

  public List<StatusInstance> statuses = new();
  public int shield; // simple flat shield for MVP
  public List<PassiveDef> passives = new();
  public int turnsTaken;
  internal string instanceId;
  internal bool encounterStarted;
}

public class StatusInstance {
  public string status; // Poison, Burn, Freeze, Regen, Thorns
  public int remainingTurns;
  public float potency;
  public int stacks;
  public string sourceId;
  public string sourceRole;
  public float damageMultiplier = 1;
}
