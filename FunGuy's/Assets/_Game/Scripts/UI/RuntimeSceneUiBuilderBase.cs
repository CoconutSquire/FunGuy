using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public abstract class RuntimeSceneUiBuilderBase
{
    protected static readonly Vector2 CenterAnchor = new(0.5f, 0.5f);
    protected static readonly Color SoftWhite = new(0.92f, 0.92f, 0.92f, 1f);
    private static object _cachedFont;

    protected static GameObject EnsureSceneRoot(Scene scene, Transform canvas, string rootName)
    {
        var content = EnsureMenuSafeContent(canvas);
        var root = EnsureChild(content, rootName);
        root.SetActive(true);
        StretchToParent(EnsureRectTransform(root));
        EnsureComponent<UiPrefabBlueprintBinder>(root);
        return root;
    }

    protected static RectTransform EnsureMenuSafeContent(Transform canvas)
    {
        if (canvas == null) return null;
        var existing = canvas.Find("MenuSafeArea");
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
        safe.transform.SetParent(canvas, false);
        StretchToParent((RectTransform)safe.transform);

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(safe.transform, false);
        var content = (RectTransform)contentGo.transform;
        StretchToParent(content);

        var safeArea = safe.GetComponent<LandscapeSafeArea>();
        safeArea.content = content;
        safe.SetActive(true);
        content.gameObject.SetActive(true);
        safeArea.Refresh();
        return content;
    }

    protected static T EnsureSceneComponent<T>(Scene scene, Transform parent) where T : Component
    {
        if (parent == null) return null;
        var existing = parent.GetComponent<T>();
        if (existing != null) return existing;
        return parent.gameObject.AddComponent<T>();
    }

    protected static void ConfigureSkin(GameObject root, UiTone tone, Image[] backgrounds, Image[] panels, Image[] accents, Button[] buttons, Text[] titles, Text[] body)
    {
        var skin = EnsureComponent<IdleHuntressSkin>(root);
        SetPrivateField(skin, "applyOnEnable", false);
        SetPrivateField(skin, "tone", tone);
        SetPrivateField(skin, "backgroundLayers", backgrounds.Where(x => x != null).ToArray());
        SetPrivateField(skin, "panelLayers", panels.Where(x => x != null).ToArray());
        SetPrivateField(skin, "accentLayers", accents.Where(x => x != null).ToArray());
        SetPrivateField(skin, "primaryButtons", buttons.Where(x => x != null).ToArray());
        SetPrivateField(skin, "titleLabels", titles.Where(x => x != null).ToArray());
        SetPrivateField(skin, "bodyLabels", body.Where(x => x != null).ToArray());
        skin.ApplyTheme();
    }

    protected static void SetAccessibleButton(Button button, Text label, Color color)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null) image.color = color;
        if (label != null) label.color = Color.white;
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.16f);
        colors.selectedColor = Color.Lerp(color, Color.white, 0.08f);
        colors.disabledColor = Color.Lerp(color, Color.black, 0.35f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    protected static TutorialSpotlightTarget Target(TutorialStep step, Button button, string hint)
    {
        var graphic = button == null ? null : (button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>());
        return new TutorialSpotlightTarget { step = step, target = graphic, hint = hint };
    }

    protected static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        var found = new List<T>();
        foreach (var root in scene.GetRootGameObjects())
            found.AddRange(root.GetComponentsInChildren<T>(true));
        return found;
    }

    protected static GameObject EnsureChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    protected static GameObject EnsurePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = EnsureChild(parent, name);
        var rt = EnsureRectTransform(go);
        SetRect(rt, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        var image = EnsureComponent<Image>(go);
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    protected static Dropdown EnsureDropdown(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, out Text labelText)
    {
        var go = EnsureChild(parent, name);
        var rt = EnsureRectTransform(go);
        SetRect(rt, CenterAnchor, CenterAnchor, anchoredPosition, sizeDelta);
        var image = EnsureComponent<Image>(go);
        image.color = color;
        image.raycastTarget = true;
        var dropdown = EnsureComponent<Dropdown>(go);
        dropdown.targetGraphic = image;
        dropdown.options.Clear();
        labelText = EnsureLabel(go.transform, "Label", label, 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(labelText.rectTransform);
        labelText.rectTransform.offsetMin = new Vector2(14f, 0f);
        labelText.rectTransform.offsetMax = new Vector2(-14f, 0f);

        var template = EnsureChild(go.transform, "Template");
        var templateRt = EnsureRectTransform(template);
        templateRt.anchorMin = new Vector2(0f, 0f);
        templateRt.anchorMax = new Vector2(1f, 0f);
        templateRt.pivot = new Vector2(.5f, 1f);
        templateRt.anchoredPosition = Vector2.zero;
        templateRt.sizeDelta = new Vector2(0f, 320f);
        var templateImage = EnsureComponent<Image>(template);
        templateImage.color = new Color(.07f,.11f,.14f,.98f);
        var scroll = EnsureComponent<ScrollRect>(template);
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = EnsureChild(template.transform, "Viewport");
        var viewportRt = EnsureRectTransform(viewport);
        StretchToParent(viewportRt);
        var mask = EnsureComponent<Mask>(viewport);
        mask.showMaskGraphic = false;
        EnsureComponent<Image>(viewport).color = Color.white;

        var content = EnsureChild(viewport.transform, "Content");
        var contentRt = EnsureRectTransform(content);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);
        var layout = EnsureComponent<VerticalLayoutGroup>(content);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 2f;
        var fitter = EnsureComponent<ContentSizeFitter>(content);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewportRt;
        scroll.content = contentRt;

        var item = EnsureChild(content.transform, "Item");
        var itemRt = EnsureRectTransform(item);
        itemRt.anchorMin = new Vector2(0f, 1f);
        itemRt.anchorMax = new Vector2(1f, 1f);
        itemRt.pivot = new Vector2(.5f, 1f);
        itemRt.anchoredPosition = Vector2.zero;
        itemRt.sizeDelta = new Vector2(0f, 64f);
        var itemImage = EnsureComponent<Image>(item);
        itemImage.color = new Color(.12f,.25f,.32f,1f);
        var toggle = EnsureComponent<Toggle>(item);
        toggle.targetGraphic = itemImage;
        var itemLabel = EnsureLabel(item.transform, "Item Label", "Equipment", 20, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        itemLabel.rectTransform.offsetMin = new Vector2(16f, 0f);
        itemLabel.rectTransform.offsetMax = new Vector2(-16f, 0f);
        dropdown.template = templateRt;
        dropdown.captionText = labelText;
        dropdown.itemText = itemLabel;
        template.SetActive(false);
        return dropdown;
    }

    protected static Button EnsureButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, out Text labelText, string labelName = null)
    {
        var go = EnsureChild(parent, name);
        var rt = EnsureRectTransform(go);
        SetRect(rt, CenterAnchor, CenterAnchor, anchoredPosition, sizeDelta);
        var image = EnsureComponent<Image>(go);
        image.color = color;
        image.raycastTarget = true;
        var button = EnsureComponent<Button>(go);
        button.targetGraphic = image;
        labelText = EnsureLabel(go.transform, string.IsNullOrWhiteSpace(labelName) ? "Label" : labelName, label, 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
        StretchToParent(labelText.rectTransform);
        return button;
    }

    protected static Text EnsureLabel(Transform parent, string name, string text, int fontSize, FontStyle style, TextAnchor anchor, Color color)
    {
        var go = EnsureChild(parent, name);
        var label = EnsureComponent<Text>(go);
        label.font = ResolveFont();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = anchor;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.supportRichText = true;
        label.raycastTarget = false;
        return label;
    }

    protected static RectTransform EnsureRectTransform(GameObject go) => EnsureComponent<RectTransform>(go);

    protected static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = CenterAnchor;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    protected static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = CenterAnchor;
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    protected static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing ?? go.AddComponent<T>();
    }

    protected static Font ResolveFont()
    {
        if (_cachedFont != null) return (Font)_cachedFont;
        _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_cachedFont == null) _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return (Font)_cachedFont;
    }

    protected static Color WithAlpha(Color color, float alpha) => new(color.r, color.g, color.b, Mathf.Clamp01(alpha));

    protected static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null || string.IsNullOrWhiteSpace(fieldName)) return;
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        var field = target.GetType().GetField(fieldName, flags);
        if (field == null) return;
        field.SetValue(target, value);
    }
}