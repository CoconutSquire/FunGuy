public static class Game {
  public static GameData Data;
  public static PlayerSave Save;
  public static GachaService Gacha;
  public static ICampaignService Campaign;
  public static ISummonService Summons;
  public static ITeamService Team;
  public static IUpgradeService Upgrades;
  public static EquipmentService Equipment;
  public static IdleGenerationService Idle;
  public static string SelectedStageId;

  [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void Reset() { Data = null; Save = null; Gacha = null; Campaign = null; Summons = null; Team = null; Upgrades = null; Equipment = null; Idle = null; SelectedStageId = null; }

  public static void EnsureInitialized() {
    if (Data == null) { var data = new GameData(); data.LoadAll(); Data = data; }
    Save ??= SaveSystem.LoadOrNew();
    Gacha ??= new GachaService(Data);
    Campaign ??= new LocalCampaignService(Data, new LocalPlayerSaveStore());
    Summons ??= new LocalSummonService(Data, new LocalPlayerSaveStore(), Gacha);
    Team ??= new LocalTeamService(Data, new LocalPlayerSaveStore());
    Upgrades ??= new LocalUpgradeService(Data, new LocalPlayerSaveStore());
    var balanceSettings = UnityEngine.Resources.Load<GameBalanceSettings>("GameData/GameBalanceSettings");
    Equipment ??= new EquipmentService(balanceSettings);
    Idle ??= new IdleGenerationService(Data, Equipment);
  }
}
