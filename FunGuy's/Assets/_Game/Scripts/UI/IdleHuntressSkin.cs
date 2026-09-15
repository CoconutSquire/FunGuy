using UnityEngine;
using UnityEngine.UI;

public class IdleHuntressSkin : MonoBehaviour
{
    [SerializeField] private UiTone tone = UiTone.Home;
    [SerializeField] private Image[] backgroundLayers;
    [SerializeField] private Image[] panelLayers;
    [SerializeField] private Image[] accentLayers;
    [SerializeField] private Button[] primaryButtons;
    [SerializeField] private Text[] titleLabels;
    [SerializeField] private Text[] bodyLabels;

    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool pulseAccent = true;
    [SerializeField] private float pulseSpeed = 1.25f;
    [SerializeField] private float pulseRange = 0.08f;

    private Color _accentBase;

    private void OnEnable()
    {
        if (applyOnEnable) ApplyTheme();
    }

    private void Update()
    {
        if (!pulseAccent || accentLayers == null || accentLayers.Length == 0) return;
        float t = 1f + (Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseRange);
        var pulseColor = new Color(
            Mathf.Clamp01(_accentBase.r * t),
            Mathf.Clamp01(_accentBase.g * t),
            Mathf.Clamp01(_accentBase.b * t),
            _accentBase.a);

        foreach (var layer in accentLayers)
        {
            if (layer == null) continue;
            layer.color = pulseColor;
        }
    }

    public void ApplyTheme()
    {
        var bg = IdleHuntressTheme.BackgroundFor(tone);
        var panel = IdleHuntressTheme.PanelFor(tone);
        _accentBase = IdleHuntressTheme.AccentFor(tone);

        ApplyColor(backgroundLayers, bg);
        ApplyColor(panelLayers, panel);
        ApplyColor(accentLayers, _accentBase);
        ApplyText(titleLabels, Color.white);
        ApplyText(bodyLabels, new Color(0.92f, 0.92f, 0.92f, 1f));
        ApplyButtons();
    }

    private void ApplyButtons()
    {
        if (primaryButtons == null) return;
        foreach (var button in primaryButtons)
        {
            if (button == null) continue;
            var colors = button.colors;
            colors.normalColor = _accentBase;
            colors.highlightedColor = Color.Lerp(_accentBase, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(_accentBase, Color.black, 0.2f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(_accentBase.r, _accentBase.g, _accentBase.b, 0.45f);
            button.colors = colors;
            // The current palette uses bright buttons; pale body text is difficult to read on them.
            foreach (var label in button.GetComponentsInChildren<Text>(true))
                label.color = new Color(0.06f, 0.08f, 0.10f, 1f);
        }
    }

    private static void ApplyColor(Image[] images, Color color)
    {
        if (images == null) return;
        foreach (var img in images)
        {
            if (img == null) continue;
            img.color = color;
        }
    }

    private static void ApplyText(Text[] labels, Color color)
    {
        if (labels == null) return;
        foreach (var label in labels)
        {
            if (label == null) continue;
            label.color = color;
        }
    }
}
