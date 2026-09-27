using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SummonSceneUiBuilder : RuntimeSceneUiBuilderBase
{
public void Build(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "SummonRoot");
        var controller = EnsureSceneComponent<SummonMenuController>(scene, root.transform);
        var spotlight = EnsureComponent<TutorialSpotlightController>(root);

        var bg = EnsurePanel(root.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Summon),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shell = EnsurePanel(root.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Summon), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(920f, 1540f));

        var bannerLabel = EnsureLabel(shell.transform, "Lbl_Banner", "Starseed Bloom Event Banner",
            40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(bannerLabel.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 610f), new Vector2(840f, 120f));

        var sporesLabel = EnsureLabel(shell.transform, "Lbl_Spores", "Basidiospores: 1",
            30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(sporesLabel.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 520f), new Vector2(840f, 90f));

        var pullOne = EnsureButton(shell.transform, "Btn_PullOne", "Summon x1  ·  1 Basidiospore", new Vector2(0f, 350f), new Vector2(540f, 108f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var pullOneLabel);
        var pullTen = EnsureButton(shell.transform, "Btn_PullTen", "Summon x10  ·  10 Basidiospores", new Vector2(0f, 220f), new Vector2(540f, 108f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var pullTenLabel);
        var back = EnsureButton(shell.transform, "Btn_Back", "Back", new Vector2(0f, 90f), new Vector2(540f, 108f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var backLabel);

        var result = EnsureLabel(shell.transform, "Lbl_Result",
            "Summon results appear here.", 28, FontStyle.Normal, TextAnchor.UpperLeft, SoftWhite);
        SetRect(result.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -280f), new Vector2(820f, 640f));

        var hint = EnsureLabel(shell.transform, "Lbl_TutorialHint", string.Empty, 24, FontStyle.Italic, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(hint.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -610f), new Vector2(820f, 120f));

        var revealRoot = EnsurePanel(root.transform, "SummonReveal", new Color(0f, 0f, 0f, 0.85f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var revealCard = EnsurePanel(revealRoot.transform, "Panel_RevealCard",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Summon), 0.98f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(740f, 980f));

        var revealTitle = EnsureLabel(revealCard.transform, "Lbl_RevealTitle", "New Funguy Acquired",
            40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(revealTitle.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 360f), new Vector2(640f, 120f));

        var revealGlow = EnsurePanel(revealCard.transform, "Img_RarityGlow",
            WithAlpha(IdleHuntressTheme.AccentFor(UiTone.Summon), 0.35f),
            CenterAnchor, CenterAnchor, new Vector2(0f, 80f), new Vector2(460f, 460f));
        var revealFrame = EnsurePanel(revealCard.transform, "Img_RarityFrame",
            IdleHuntressTheme.RarityColor(4),
            CenterAnchor, CenterAnchor, new Vector2(0f, 80f), new Vector2(360f, 360f));
        var revealName = EnsureLabel(revealCard.transform, "Lbl_RevealName", "Unknown Funguy",
            36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(revealName.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -180f), new Vector2(640f, 100f));

        var revealRarity = EnsureLabel(revealCard.transform, "Lbl_RevealRarity", "★★★",
            30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(revealRarity.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -250f), new Vector2(640f, 90f));

        var revealNext = EnsureButton(revealCard.transform, "Btn_RevealNext", "Next", new Vector2(0f, -360f), new Vector2(420f, 100f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var revealNextLabel);

        var revealController = EnsureSceneComponent<SummonRevealController>(scene, revealRoot.transform);
        revealController.Initialize(revealRoot, revealTitle, revealName, revealRarity,
            revealFrame.GetComponent<Image>(), revealGlow.GetComponent<Image>(), revealNext);
        revealRoot.SetActive(false);

        // Runtime-created controls must be explicitly handed to the existing
        // controller. Without this binding the screen renders correctly but
        // Pull/Back buttons have no controller listeners.
        controller.Initialize(
            result,
            sporesLabel,
            bannerLabel,
            revealController,
            pullOne,
            pullTen,
            back);

        ConfigureSkin(root, UiTone.Summon,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>(), revealCard.GetComponent<Image>() },
            new[] { pullOne.GetComponent<Image>(), pullTen.GetComponent<Image>(), back.GetComponent<Image>(), revealNext.GetComponent<Image>(), revealGlow.GetComponent<Image>(), revealFrame.GetComponent<Image>() },
            new[] { pullOne, pullTen, back, revealNext },
            new[] { bannerLabel, revealTitle, revealName },
            new[] { sporesLabel, result, hint, pullOneLabel, pullTenLabel, backLabel, revealRarity, revealNextLabel });

        spotlight.Configure(
            hint,
            new[]
            {
                Target(TutorialStep.ShowSummonPool, pullOne, "Your first stop is this banner. Continue and pull once."),
                Target(TutorialStep.GiveTicket, pullOne, "Use the tutorial ticket to perform your first summon."),
                Target(TutorialStep.DoFirstSummon, pullOne, "Pull one unit to continue onboarding."),
            });

    }
}