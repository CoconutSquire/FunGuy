using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class OptionsSceneUiBuilder : RuntimeSceneUiBuilderBase
{
public void Build(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "OptionsRoot");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var debugRoot = EnsureChild(root.transform, "DebugRoot");
        StretchToParent(EnsureRectTransform(debugRoot));

        var debugController = EnsureSceneComponent<DebugProgressionController>(scene, debugRoot.transform);
        var smokeController = EnsureSceneComponent<GameplaySmokeTestController>(scene, debugRoot.transform);

        var bg = EnsurePanel(debugRoot.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Home),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shell = EnsurePanel(debugRoot.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Home), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(940f, 1540f));

        var title = EnsureLabel(shell.transform, "Lbl_OptionsTitle", "Debug / QA Controls",
            40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 630f), new Vector2(860f, 120f));

        var status = EnsureLabel(shell.transform, "Lbl_DebugStatus", "Ready.",
            26, FontStyle.Normal, TextAnchor.MiddleLeft, SoftWhite);
        SetRect(status.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 535f), new Vector2(860f, 90f));

        var reset = EnsureButton(shell.transform, "Btn_ResetSave", "Reset Save", new Vector2(-230f, 380f), new Vector2(300f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var resetLabel);
        var grant = EnsureButton(shell.transform, "Btn_GrantStarterResources", "Grant Resources", new Vector2(120f, 380f), new Vector2(330f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var grantLabel);
        var seed = EnsureButton(shell.transform, "Btn_SeedStarterRoster", "Seed Roster", new Vector2(-230f, 275f), new Vector2(300f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var seedLabel);
        var skip = EnsureButton(shell.transform, "Btn_SkipTutorial", "Skip Tutorial", new Vector2(120f, 275f), new Vector2(330f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var skipLabel);
        var openSummon = EnsureButton(shell.transform, "Btn_OpenSummon", "Open Summon", new Vector2(-230f, 170f), new Vector2(300f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var openSummonLabel);
        var runSmoke = EnsureButton(shell.transform, "Btn_RunSmokeTests", "Run Smoke Tests", new Vector2(120f, 170f), new Vector2(330f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var smokeLabel);
        var backHome = EnsureButton(shell.transform, "Btn_BackHome", "Back Home", new Vector2(0f, 60f), new Vector2(640f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var backLabel);

        var smokeOutput = EnsureLabel(shell.transform, "Lbl_SmokeOutput",
            "Smoke Test Result: not run.", 24, FontStyle.Normal, TextAnchor.UpperLeft, SoftWhite);
        SetRect(smokeOutput.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -370f), new Vector2(860f, 760f));

        ConfigureSkin(debugRoot, UiTone.Home,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>() },
            new[] { reset.GetComponent<Image>(), grant.GetComponent<Image>(), seed.GetComponent<Image>(), skip.GetComponent<Image>(), openSummon.GetComponent<Image>(), runSmoke.GetComponent<Image>(), backHome.GetComponent<Image>() },
            new[] { reset, grant, seed, skip, openSummon, runSmoke, backHome },
            new[] { title },
            new[] { status, smokeOutput, resetLabel, grantLabel, seedLabel, skipLabel, openSummonLabel, smokeLabel, backLabel });

#else
        var title = EnsureLabel(root.transform, "Lbl_OptionsTitle", "Options", 40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 240f), new Vector2(800f, 100f));
        var audio = EnsureButton(root.transform, "Btn_Audio", AudioListener.pause ? "Enable audio" : "Mute audio", Vector2.zero,
            new Vector2(560f, 100f), IdleHuntressTheme.AccentFor(UiTone.Home), out var audioLabel);
        audio.onClick.AddListener(() => {
            AudioListener.pause = !AudioListener.pause;
            audioLabel.text = AudioListener.pause ? "Enable audio" : "Mute audio";
        });
        var back = EnsureButton(root.transform, "Btn_Back", "Back", new Vector2(0f, -150f), new Vector2(560f, 100f),
            IdleHuntressTheme.AccentFor(UiTone.Home), out _);
        back.onClick.AddListener(() => SceneManager.LoadScene("Home"));
#endif
    }
}