using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class HomeSceneUiBuilder : RuntimeSceneUiBuilderBase
{
    public void Build(Scene scene, Canvas canvas)
    {
        if (canvas == null) return;

        var root = EnsureSceneRoot(scene, canvas.transform, "HomeRoot");

        // Reuse the authored HomeMenuController when one already exists. The
        // authored scene currently contains one, and creating a second controller
        // here would give both controllers a Start() lifecycle and compete for the
        // same Home UI. Only add one when a scene truly has none.
        var controller = root.GetComponent<HomeMenuController>();
        if (controller == null)
            controller = FindInScene<HomeMenuController>(scene).FirstOrDefault();
        if (controller == null)
            controller = EnsureSceneComponent<HomeMenuController>(scene, root.transform);
        else if (controller.transform != root.transform)
        {
            controller.transform.SetParent(root.transform, false);
            controller.transform.localPosition = Vector3.zero;
            controller.transform.localRotation = Quaternion.identity;
            controller.transform.localScale = Vector3.one;
        }

        // HomeRoot is the single owner of generated Home navigation. If the
        // complete generated menu is already present, leave it alone so the
        // bootstrap and controller lifecycle cannot rebuild it twice.
        if (IsCompleteHomeMenu(root.transform))
            return;

        // A partial/legacy build is unsafe: remove only generated children and
        // reconstruct the complete menu as one hierarchy.
        ClearGeneratedHomeChildren(root.transform, controller);

        // The old working Home was a runtime navigation menu: five clear,
        // colored buttons over the full-screen Home artwork. Make that layout
        // the single authoritative Home UI again.
        EnsureHomeBackground(canvas.transform);

        // Disable stale authored buttons. The scene file is retained for its
        // background/art, but navigation belongs exclusively to this builder.
        foreach (var button in FindInScene<Button>(scene))
        {
            if (button == null) continue;
            if (button.transform.IsChildOf(root.transform)) continue;
            button.gameObject.SetActive(false);
        }

        // The authored Home scene also contains an obsolete decorative Background
        // Image. It must not sit above or beside the authoritative runtime artwork.
        var authoredBackground = canvas.transform.Find("Background");
        if (authoredBackground != null)
            authoredBackground.gameObject.SetActive(false);

        var shell = EnsurePanel(root.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Home), 0.82f),
            CenterAnchor, CenterAnchor, new Vector2(-360f, 0f), new Vector2(760f, 760f));
        shell.GetComponent<Image>().raycastTarget = true;

        var title = EnsureLabel(shell.transform, "Lbl_Welcome",
            "Welcome, Commander. Build your squad and clear the frontier.",
            42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor,
            new Vector2(0f, 315f), new Vector2(700f, 90f));

        var stats = EnsureLabel(shell.transform, "Lbl_AccountStats",
            "", 30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(stats.rectTransform, CenterAnchor, CenterAnchor,
            new Vector2(0f, 245f), new Vector2(700f, 60f));

        var start = EnsureButton(shell.transform, "Btn_Start", "Start",
            new Vector2(0f, 155f), new Vector2(500f, 76f),
            IdleHuntressTheme.AccentFor(UiTone.Home), out var startLabel);

        var summon = EnsureButton(shell.transform, "Btn_Summon", "Summon",
            new Vector2(0f, 65f), new Vector2(500f, 76f),
            IdleHuntressTheme.AccentFor(UiTone.Summon), out var summonLabel);

        var funguy = EnsureButton(shell.transform, "Btn_Funguy", "Funguy",
            new Vector2(0f, -25f), new Vector2(500f, 76f),
            IdleHuntressTheme.AccentFor(UiTone.Team), out var funguyLabel);

        var campaign = EnsureButton(shell.transform, "Btn_Campaign", "Campaign",
            new Vector2(0f, -115f), new Vector2(500f, 76f),
            IdleHuntressTheme.AccentFor(UiTone.Battle), out var campaignLabel);

        var options = EnsureButton(shell.transform, "Btn_Options", "Options",
            new Vector2(0f, -205f), new Vector2(500f, 76f),
            new Color(.28f, .34f, .39f, 1f), out var optionsLabel);

        var hint = EnsureLabel(shell.transform, "Lbl_TutorialHint",
            "", 24, FontStyle.Italic, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(hint.rectTransform, CenterAnchor, CenterAnchor,
            new Vector2(0f, -315f), new Vector2(680f, 70f));

        ConfigureSkin(root, UiTone.Home,
            Array.Empty<Image>(),
            new[] { shell.GetComponent<Image>() },
            new[]
            {
                start.GetComponent<Image>(),
                summon.GetComponent<Image>(),
                funguy.GetComponent<Image>(),
                campaign.GetComponent<Image>(),
                options.GetComponent<Image>()
            },
            new[] { start, summon, funguy, campaign, options },
            new[] { title },
            new[] { stats, hint, startLabel, summonLabel, funguyLabel, campaignLabel, optionsLabel });

        start.onClick.RemoveAllListeners();
        start.onClick.AddListener(controller.OnStartPressed);

        summon.onClick.RemoveAllListeners();
        summon.onClick.AddListener(controller.OnSummonPressed);

        funguy.onClick.RemoveAllListeners();
        funguy.onClick.AddListener(controller.OnFunguyPressed);

        campaign.onClick.RemoveAllListeners();
        campaign.onClick.AddListener(controller.OnBattlePressed);

        options.onClick.RemoveAllListeners();
        options.onClick.AddListener(controller.OnOptionsPressed);

        controller.BindHomeView(stats, hint);
    }

    private static void EnsureHomeBackground(Transform canvasRoot)
    {
        if (canvasRoot == null) return;

        var existing = canvasRoot.Find("HomeArtBackground");
        GameObject background;

        if (existing != null)
        {
            background = existing.gameObject;
        }
        else
        {
            background = new GameObject("HomeArtBackground",
                typeof(RectTransform), typeof(RawImage));
            background.transform.SetParent(canvasRoot, false);
        }

        var rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.SetAsFirstSibling();

        var image = background.GetComponent<RawImage>();
        image.texture = Resources.Load<Texture2D>("Presentation/kitchen-battlefield-v1");
        image.color = Color.white;
        image.raycastTarget = false;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        background.SetActive(true);
        background.transform.SetAsFirstSibling();
    }

    private static bool IsCompleteHomeMenu(Transform root)
    {
        if (root == null) return false;
        var panel = root.Find("Panel_Main");
        if (panel == null) return false;

        string[] required = { "Btn_Start", "Btn_Summon", "Btn_Funguy", "Btn_Campaign", "Btn_Options" };
        foreach (var name in required)
        {
            var button = panel.Find(name);
            if (button == null || button.GetComponent<Button>() == null)
                return false;
        }

        return true;
    }

    private static void ClearGeneratedHomeChildren(Transform root, HomeMenuController controller)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (child == null) continue;
            if (child.GetComponent<HomeMenuController>() == controller) continue;
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }
}
