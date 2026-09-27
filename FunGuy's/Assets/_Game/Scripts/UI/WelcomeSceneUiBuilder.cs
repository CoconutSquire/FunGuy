using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WelcomeSceneUiBuilder : RuntimeSceneUiBuilderBase
{
public void Build(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "TutorialRoot");
        var art = new GameObject("WelcomeArt", typeof(RectTransform), typeof(RawImage));
        art.transform.SetParent(root.transform, false);
        var rect = (RectTransform)art.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = art.GetComponent<RawImage>(); image.texture = Resources.Load<Texture2D>("Presentation/kitchen-battlefield-v1"); image.raycastTarget = false;
        EnsurePanel(root.transform, "WelcomeShade", new Color(0, 0, 0, .55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var title = EnsureLabel(root.transform, "WelcomeTitle", "FUNGUY'S", 72, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1, .85f, .5f));
        title.rectTransform.anchoredPosition = new Vector2(0, 80); title.rectTransform.sizeDelta = new Vector2(1000, 140);
        var detail = EnsureLabel(root.transform, "WelcomeDetail",
            "Build your team. Choose your formation.\nTime your skills.",
            32, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
        detail.rectTransform.anchoredPosition = new Vector2(0, -70);
        detail.rectTransform.sizeDelta = new Vector2(1100, 160);

        // The Tutorial scene is the first playable onboarding screen. Give it a
        // real entry point instead of leaving the player on static artwork/text.
        var begin = EnsureButton(root.transform, "Btn_BeginTutorial", "Begin Tutorial",
            new Vector2(0, -220), new Vector2(460, 88),
            new Color(.10f, .35f, .68f, 1f), out var beginLabel);
        SetAccessibleButton(begin, beginLabel, new Color(.10f, .35f, .68f, 1f));
        begin.onClick.RemoveAllListeners();
        begin.onClick.AddListener(() => TutorialManager.EnsureInstance().ContinueTutorial());
    }
}