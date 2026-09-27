using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TutorialSceneUiBuilder : RuntimeSceneUiBuilderBase
{
public void Build(Scene scene, Canvas canvas)
    {
        var manager = TutorialManager.EnsureInstance();
        if (canvas.transform.Find("TutorialOverlayRoot") != null) return;
        var overlayRoot = EnsurePanel(canvas.transform, "TutorialOverlayRoot", new Color(0.08f, 0.12f, 0.16f, 0.97f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(980f, 360f));
        var message = EnsureLabel(overlayRoot.transform, "Lbl_TutorialMessage", "", 27, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
        SetRect(message.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 50f), new Vector2(900f, 180f));
        var next = EnsureButton(overlayRoot.transform, "Btn_TutorialContinue", "Continue", new Vector2(0f, -100f),
            new Vector2(400f, 80f), IdleHuntressTheme.AccentFor(UiTone.Home), out _);
        var overlay = overlayRoot.AddComponent<TutorialOverlay>();
        overlay.Initialize(overlayRoot, message, next);
        manager.AttachOverlay(overlay);
    }
}