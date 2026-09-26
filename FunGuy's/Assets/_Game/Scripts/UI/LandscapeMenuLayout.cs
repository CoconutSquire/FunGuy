using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Landscape adaptation for the existing menu adapter. Battle uses separately authored prefabs.
public static class LandscapeMenuLayout
{
    public static void Apply(Scene scene, Canvas canvas)
    {
        if (canvas == null) return;

        // RuntimeSceneUiBootstrap owns screen construction and control geometry.
        // This adapter only provides the safe-area parent and keeps the screen root
        // stretched to that content region. Repositioning individual controls here
        // created a second layout owner and could overwrite newer screen builders.
        var root = canvas.transform.Find(scene.name + "Root") as RectTransform;
        if (root != null && scene.name != "Battle")
        {
            var content = SafeContent(canvas.transform, "MenuSafeArea");
            root.SetParent(content, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localScale = Vector3.one;
        }

        var overlay = canvas.GetComponentsInChildren<TutorialOverlay>(true).FirstOrDefault();
        if (overlay == null) return;

        var tutorialContent = SafeContent(canvas.transform, "TutorialSafeArea");
        var r = (RectTransform)overlay.transform;
        r.SetParent(tutorialContent, false);
        r.anchorMin = r.anchorMax = r.pivot = new(.5f, .5f);
        r.anchoredPosition = new(0, 370);
        r.sizeDelta = new(1100, 130);

        var text = overlay.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.fontSize = 23;
            text.rectTransform.anchoredPosition = new(-140, 0);
            text.rectTransform.sizeDelta = new(760, 110);
        }

        var button = overlay.GetComponentInChildren<Button>(true);
        if (button != null)
        {
            var buttonRect = (RectTransform)button.transform;
            buttonRect.anchoredPosition = new(415, 0);
            buttonRect.sizeDelta = new(230, 70);
        }
    }

    private static RectTransform SafeContent(Transform parent, string name)
    {
        var existing = parent.Find(name); if (existing != null) return (RectTransform)existing.GetChild(0);
        var safe = new GameObject(name, typeof(RectTransform), typeof(LandscapeSafeArea)); safe.transform.SetParent(parent, false);
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(safe.transform, false);
        safe.GetComponent<LandscapeSafeArea>().content = content; safe.GetComponent<LandscapeSafeArea>().Refresh(); return content;
    }
}
