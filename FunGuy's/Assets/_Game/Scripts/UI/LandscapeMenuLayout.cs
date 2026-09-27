using UnityEngine;
using UnityEngine.SceneManagement;

public static class LandscapeMenuLayout
{
    // Compatibility shim for older callers. Screen builders now own their
    // hierarchy and parent directly to MenuSafeArea/Content, so this method
    // must never reparent or reposition a completed screen.
    public static void Apply(Scene scene, Canvas canvas)
    {
        if (canvas == null) return;
        EnsureSafeContent(canvas.transform);
    }

    private static RectTransform EnsureSafeContent(Transform parent)
    {
        var existing = parent.Find("MenuSafeArea");
        if (existing != null)
        {
            var existingContent = existing.Find("Content") as RectTransform;
            if (existingContent != null)
            {
                existing.gameObject.SetActive(true);
                existingContent.gameObject.SetActive(true);
                return existingContent;
            }
        }

        var safe = new GameObject("MenuSafeArea", typeof(RectTransform), typeof(LandscapeSafeArea));
        safe.transform.SetParent(parent, false);
        var safeRect = (RectTransform)safe.transform;
        safeRect.anchorMin = Vector2.zero;
        safeRect.anchorMax = Vector2.one;
        safeRect.offsetMin = Vector2.zero;
        safeRect.offsetMax = Vector2.zero;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(safe.transform, false);
        var content = (RectTransform)contentGo.transform;
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        var safeArea = safe.GetComponent<LandscapeSafeArea>();
        safeArea.content = content;
        safe.SetActive(true);
        content.gameObject.SetActive(true);
        safeArea.Refresh();
        return content;
    }
}