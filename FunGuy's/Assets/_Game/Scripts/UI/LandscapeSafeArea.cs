using UnityEngine;

// Safe-area adapter for the shared 1600x900 landscape CanvasScaler surface.
// The CanvasScaler owns screen scaling; this component only clips the UI to the
// device safe area so the two systems do not scale the same hierarchy twice.
[ExecuteAlways]
public sealed class LandscapeSafeArea : MonoBehaviour
{
    public RectTransform content;
    public Vector2 designSize = new(1600, 900);

    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (content == null || transform.parent is not RectTransform parent) return;

        var rect = (RectTransform)transform;
        Rect safe = Screen.safeArea;
        float width = Screen.width;
        float height = Screen.height;
        Vector2 min = width > 0f && height > 0f
            ? new Vector2(safe.xMin / width, safe.yMin / height)
            : Vector2.zero;
        Vector2 max = width > 0f && height > 0f
            ? new Vector2(safe.xMax / width, safe.yMax / height)
            : Vector2.one;

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // CanvasScaler already converts the 1600x900 design surface to the
        // device resolution. Keep this content at identity scale and stretch it
        // across the safe-area rectangle; applying another FitScale here can
        // double-scale the hierarchy and make runtime UI effectively disappear
        // or render far outside the visible area on some resolutions.
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, .5f);
        content.anchoredPosition = Vector2.zero;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        content.localScale = Vector3.one;
    }
}
