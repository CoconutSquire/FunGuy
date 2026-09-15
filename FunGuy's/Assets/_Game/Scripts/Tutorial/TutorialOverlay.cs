using System;
using UnityEngine;
using UnityEngine.UI;

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
