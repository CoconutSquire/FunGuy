using UnityEngine;

// A fixed design surface fitted inside the actual safe area. Art can bleed outside it.
[ExecuteAlways]
public sealed class LandscapeSafeArea : MonoBehaviour
{
    public RectTransform content;
    public Vector2 designSize = new(1600, 900);
    public static float FitScale(Vector2 available, Vector2 design) =>
        Mathf.Max(.001f, Mathf.Min(available.x / design.x, available.y / design.y));
    private void LateUpdate() => Refresh();
    public void Refresh()
    {
        if (content == null || transform.parent is not RectTransform parent) return;
        var rect = (RectTransform)transform;
        Rect safe = Screen.safeArea;
        Vector2 min = Screen.width > 0 && Screen.height > 0 ? new(safe.xMin / Screen.width, safe.yMin / Screen.height) : Vector2.zero;
        Vector2 max = Screen.width > 0 && Screen.height > 0 ? new(safe.xMax / Screen.width, safe.yMax / Screen.height) : Vector2.one;
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        content.anchorMin = content.anchorMax = content.pivot = new(.5f, .5f);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = designSize;
        var available = Vector2.Scale(parent.rect.size, max - min);
        content.localScale = Vector3.one * FitScale(available, designSize);
    }
}
