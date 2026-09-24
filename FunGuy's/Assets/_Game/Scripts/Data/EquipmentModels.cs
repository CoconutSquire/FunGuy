using System;
using System.Collections.Generic;

[Serializable] public class EquipmentDef {
  public string id;
  public string name;
  public string slotId;
  public string acquisition;
  public int hp;
  public int def;
  public int pot;
  public int spd;
  public float critChance;
  public bool randomStat;
  public bool uniqueTrait;
}

[Serializable] public class EquipmentFile { public List<EquipmentDef> equipment = new(); }
