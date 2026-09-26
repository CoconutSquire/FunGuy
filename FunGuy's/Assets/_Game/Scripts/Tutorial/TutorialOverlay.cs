using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialOverlay : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Text messageLabel;
    [SerializeField] private Button continueButton;

    private Action pendingContinue;

    public void Initialize(GameObject panel, Text label, Button next)
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(HandleContinuePressed);
        root = panel; messageLabel = label; continueButton = next;
        continueButton.onClick.RemoveListener(HandleContinuePressed);
        continueButton.onClick.AddListener(HandleContinuePressed);
    }

    public static void ApplyNavigationLock(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (string.Equals(scene.name, "Tutorial", StringComparison.OrdinalIgnoreCase)) return;

        var save = Game.Save ?? SaveSystem.LoadOrNew();
        if (save == null || save.tutorialCompleted) return;
        var step = (TutorialStep)Mathf.Clamp(save.tutorialStep, 0, (int)TutorialStep.Complete);

        foreach (var rootObject in scene.GetRootGameObjects())
        {
            foreach (var button in rootObject.GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;
                // During onboarding, gameplay input is deliberately whitelisted by
                // step. This prevents an accidental tap on another screen control
                // from bypassing the tutorial state machine.
                button.interactable = IsAllowedTutorialButton(button.gameObject.name, step);
            }
        }
    }

    private static bool IsAllowedTutorialButton(string objectName, TutorialStep step)
    {
        if (objectName == "Btn_TutorialContinue")
        {
            return step == TutorialStep.BattleWinRewards ||
                   step == TutorialStep.ExplainTankDps;
        }

        switch (step)
        {
            case TutorialStep.ShowSummonPool:
            case TutorialStep.GiveTicket:
                return false;
            case TutorialStep.DoFirstSummon:
                return objectName == "Btn_PullOne";
            case TutorialStep.GoToTeamBuilder:
                return false;
            case TutorialStep.PlaceFirstUnit:
                return objectName.StartsWith("Btn_FormationSlot", StringComparison.Ordinal) ||
                       objectName.StartsWith("Btn_RosterSlot", StringComparison.Ordinal);
            case TutorialStep.StartFirstBattle:
                return objectName == "Btn_StartBattle";
            case TutorialStep.PlaceTankFrontDpsBack:
                return objectName == "Btn_Upgrades" || objectName == "Btn_UpgradesBack" ||
                       objectName == "Btn_Upgrade" || objectName == "Btn_LevelUp" ||
                       objectName == "Btn_EquipmentUpgrade" || objectName == "Btn_EquipmentBack";
            default:
                return false;
        }
    }

    public void Show()
    {
        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    public void Say(string message, Action onContinue)
    {
        Show();
        pendingContinue = onContinue;
        if (continueButton != null) continueButton.gameObject.SetActive(onContinue != null);

        if (messageLabel != null) messageLabel.text = message;
        Debug.Log($"[TutorialOverlay] {message}");
    }

    public void ContinueTutorial()
    {
        HandleContinuePressed();
    }

    private void HandleContinuePressed()
    {
        var callback = pendingContinue;
        pendingContinue = null;
        callback?.Invoke();
    }
}
