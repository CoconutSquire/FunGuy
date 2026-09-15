using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeMenuController : MonoBehaviour {
  public System.Action OpenCampaign;
  [SerializeField] private Text welcomeLabel = null;
  [SerializeField] private Text accountStatsLabel = null;
  [SerializeField] private bool autoLaunchTutorialOnFirstOpen = true;

  private PlayerSave Save => Game.Save ??= SaveSystem.LoadOrNew();

  private void Start() {
    if (autoLaunchTutorialOnFirstOpen && !Save.tutorialCompleted && Save.tutorialStep <= 0) {
      SceneManager.LoadScene("Tutorial");
      return;
    }

    RefreshHomeStats();
  }

  // Hook this to Start button OnClick().
  // First-time players are routed to onboarding, returning players to Team.
  public void OnStartPressed() {
    var save = Save;
    if (!save.tutorialCompleted) SceneManager.LoadScene("Tutorial");
    else SceneManager.LoadScene("Team");
  }

  public void OnSummonPressed() {
    SceneManager.LoadScene("Summon");
  }

  public void OnTeamPressed() {
    SceneManager.LoadScene("Team");
  }

  public void OnBattlePressed() {
    if (OpenCampaign != null) OpenCampaign(); else SceneManager.LoadScene("Battle");
  }

  public void OnOptionsPressed() {
    SceneManager.LoadScene("Options");
  }

  private void RefreshHomeStats() {
    if (welcomeLabel != null) {
      welcomeLabel.text = Save.tutorialCompleted
        ? "Welcome back, Commander."
        : "Welcome, new Commander. Begin onboarding to claim your first unit.";
    }

    if (accountStatsLabel != null) {
      accountStatsLabel.text = $"Lv.{Save.accountLevel}  Gold:{Save.gold}  Spores:{Save.spores}";
    }
  }
}
