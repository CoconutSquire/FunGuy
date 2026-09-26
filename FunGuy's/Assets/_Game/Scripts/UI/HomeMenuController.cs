using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeMenuController : MonoBehaviour {
  public System.Action OpenCampaign;
  [SerializeField] private Text welcomeLabel = null;
  [SerializeField] private Text accountStatsLabel = null;
  [SerializeField] private bool autoLaunchTutorialOnFirstOpen = true;

  private PlayerSave Save => Game.Save ??= SaveSystem.LoadOrNew();
  private Text idleLabel;
  private Button idleClaimButton;
  private Text homeStatsLabel;
  private Text homeHintLabel;
  private FunguyRosterController funguyRoster;

  private void Start() {
    Game.EnsureInitialized();
    var canvas = GetComponentInParent<Canvas>();
    if (canvas != null) HomeScreenView.Build(canvas.transform, this);
    if (autoLaunchTutorialOnFirstOpen && !Save.tutorialCompleted && Save.tutorialStep <= 0) {
      SceneManager.LoadScene("Tutorial");
      return;
    }

    RefreshHomeStats();
    BuildIdlePanel();
  }

  public void OnStartPressed() {
    var save = Save;
    if (!save.tutorialCompleted) SceneManager.LoadScene("Tutorial");
    else SceneManager.LoadScene("Team");
  }

  public void OnSummonPressed() { SceneManager.LoadScene("Summon"); }
    public void OnTeamPressed()
    {
        OnFunguyPressed();
    }
        
  public void OnFunguyPressed() { funguyRoster?.Open(); }

    public void OnBattlePressed() { if (OpenCampaign != null) OpenCampaign(); else SceneManager.LoadScene("Battle"); }
  public void OnOptionsPressed() { SceneManager.LoadScene("Options"); }

  public void BindHomeView(Text stats, Text hint) { homeStatsLabel = stats; homeHintLabel = hint; RefreshHomeStats(); }

  public void BindFunguyRoster(FunguyRosterController roster) { funguyRoster = roster; }

  private void RefreshHomeStats() {
    if (welcomeLabel != null) {
      welcomeLabel.text = Save.tutorialCompleted
        ? "Welcome back, Commander."
        : "Welcome, new Commander. Begin onboarding to claim your first unit.";
    }
    var stats = $"Lv.{Save.accountLevel}  Gold:{Save.gold}  Spores:{Save.spores}";
    if (accountStatsLabel != null) accountStatsLabel.text = stats;
    if (homeStatsLabel != null) homeStatsLabel.text = stats;
    if (homeHintLabel != null) homeHintLabel.text = Save.tutorialCompleted ? "Choose an activity to continue." : "Start will continue your onboarding.";
  }

  private void BuildIdlePanel() {
    var host = transform.parent != null ? transform.parent : transform;
    if (host.Find("IdleGenerationPanel") != null) return;
    var panelGo = new GameObject("IdleGenerationPanel", typeof(RectTransform), typeof(Image));
    panelGo.transform.SetParent(homeUi, false);
    var panel = panelGo.GetComponent<Image>();
    panel.color = new Color(.08f, .12f, .16f, .98f);
    var panelRect = panelGo.GetComponent<RectTransform>();
    panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
    panelRect.pivot = new Vector2(.5f, .5f);
    panelRect.anchoredPosition = new Vector2(0f, -430f);
    panelRect.sizeDelta = new Vector2(820f, 300f);

    var title = MakeLabel(panelGo.transform, "Idle Generation", 30, new Vector2(0, 105), new Vector2(760, 55), FontStyle.Bold);
    idleLabel = MakeLabel(panelGo.transform, "", 22, new Vector2(0, 25), new Vector2(760, 105), FontStyle.Normal);
    idleClaimButton = MakeButton(panelGo.transform, "Claim Idle Rewards", new Vector2(0, -100), new Vector2(430, 70));
    idleClaimButton.onClick.AddListener(ClaimIdleRewards);
    RefreshIdlePanel();
  }

  private void RefreshIdlePanel() {
    if (idleLabel == null) return;
    var preview = Game.Idle?.Preview(Save);
    if (preview == null) { idleLabel.text = "Idle rewards unavailable."; return; }
    idleLabel.text = $"Campaign depth: {preview.campaignDepth} stages\\n" +
                     $"Stored: {preview.gold} Gold + {preview.equipmentCount} equipment\\n" +
                     $"Accumulating: {preview.goldPerHour}/hr Gold • {preview.equipmentPerHour:0.##}/hr equipment\\n" +
                     $"Time: {FormatDuration(preview.elapsed)}";
    idleClaimButton.interactable = preview.gold > 0 || preview.equipmentCount > 0;
  }

  private void ClaimIdleRewards() {
    Game.EnsureInitialized();

    var save = Game.Save ?? SaveSystem.LoadOrNew();
    var idle = Game.Idle;
    if (idle == null) {
      RefreshIdlePanel();
      return;
    }

    var preview = idle.Preview(save);
    if (preview == null || (preview.gold <= 0 && preview.equipmentCount <= 0)) {
      RefreshIdlePanel();
      return;
    }

    var result = idle.Claim(save);
    if (result == null) {
      RefreshIdlePanel();
      return;
    }

    // Claim() persists the same save instance and updates Game.Save. Re-read it
    // so the account display and idle panel both reflect the committed balances.
    Game.Save = SaveSystem.LoadOrNew();
    RefreshHomeStats();
    RefreshIdlePanel();
  }

  private static string FormatDuration(TimeSpan span) {
    if (span.TotalDays >= 1) return $"{(int)span.TotalDays}d {span.Hours:00}h";
    if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h {span.Minutes:00}m";
    return $"{Math.Max(0, span.Minutes):00}m {Math.Max(0, span.Seconds):00}s";
  }

  private static Text MakeLabel(Transform parent, string text, int size, Vector2 pos, Vector2 dimensions, FontStyle style) {
    var go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
    var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f); rt.pivot = new Vector2(.5f,.5f); rt.anchoredPosition = pos; rt.sizeDelta = dimensions;
    var label = go.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.text = text; label.fontSize = size; label.fontStyle = style; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Overflow;
    return label;
  }

  private static Button MakeButton(Transform parent, string text, Vector2 pos, Vector2 dimensions) {
    var go = new GameObject("ClaimButton", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
    var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f); rt.pivot = new Vector2(.5f,.5f); rt.anchoredPosition = pos; rt.sizeDelta = dimensions;
    var image = go.GetComponent<Image>(); image.color = new Color(.55f,.75f,.35f,1f);
    var button = go.GetComponent<Button>(); button.targetGraphic = image;
    var label = MakeLabel(go.transform, text, 25, Vector2.zero, dimensions, FontStyle.Bold); label.color = Color.black;
    return button;
  }
}
