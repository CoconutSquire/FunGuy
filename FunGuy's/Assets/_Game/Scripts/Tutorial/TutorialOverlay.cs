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

        foreach (var rootObject in scene.GetRootGameObjects())
        {
            foreach (var button in rootObject.GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;

                var name = button.gameObject.name;
                bool navigation = name == "Btn_Back" ||
                                  name == "Btn_Home" ||
                                  name == "Btn_Options" ||
                                  name == "Btn_Equipment" ||
                                  name == "Btn_Upgrades" ||
                                  name == "Btn_RemoveSelected" ||
                                  name == "Btn_RosterPrevious" ||
                                  name == "Btn_RosterNext" ||
                                  name == "Btn_UpgradesBack" ||
                                  name == "Btn_BackHome";

                if (navigation) button.interactable = false;
            }
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
