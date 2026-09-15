using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class UiButtonWireRule
{
    public string buttonName;
    public string methodName;
    public string targetTypeName;
}

[DefaultExecutionOrder(-900)]
public class UiPrefabBlueprintBinder : MonoBehaviour
{
    [SerializeField] private bool bindOnAwake = true;
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool caseInsensitiveNames = true;
    [SerializeField] private bool skipAlreadyAssigned = true;
    [SerializeField] private bool logSummary = true;
    [SerializeField] private bool logMissing = true;
    [SerializeField] private bool autoWireButtons = true;
    [SerializeField] private bool clearRuntimeListenersBeforeWire = false;
    [SerializeField] private bool logButtonWiring = true;
    [SerializeField] private List<UiButtonWireRule> customButtonRules = new();

    private int _boundCount;
    private int _wiredButtonCount;
    private readonly List<string> _missing = new();
    private readonly Dictionary<Button, UnityEngine.Events.UnityAction> _listeners = new();

    private void Awake()
    {
        if (bindOnAwake) AutoBindCommonReferences();
    }

    [ContextMenu("Auto Bind Common References")]
    public void AutoBindCommonReferences()
    {
        _boundCount = 0;
        _missing.Clear();

        BindHome();
        BindSummon();
        BindTeam();
        BindBattle();
        BindTutorial();
        BindSummonReveal();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        BindDebugPanels();
#endif
        BindTeamSlots();
        if (autoWireButtons) AutoWireButtonEvents();

        if (!logSummary) return;
        Debug.Log($"[UiPrefabBlueprintBinder] Bound {_boundCount} field reference(s), wired {_wiredButtonCount} button event(s). Missing: {_missing.Count}.");
        if (!logMissing) return;
        foreach (var miss in _missing)
        {
            Debug.LogWarning($"[UiPrefabBlueprintBinder] Missing: {miss}");
        }
    }

    [ContextMenu("Auto Wire Button Events")]
    public void AutoWireButtonEvents()
    {
        _wiredButtonCount = 0;
        var buttons = GetComponentsInChildren<Button>(includeInactive);
        var behaviours = GetComponentsInChildren<MonoBehaviour>(includeInactive).Where(b => b != null).ToList();

        foreach (var button in buttons)
        {
            if (button == null) continue;
            if (!TryWireButton(button, behaviours)) continue;
            _wiredButtonCount += 1;
        }

        if (logButtonWiring)
        {
            Debug.Log($"[UiPrefabBlueprintBinder] Auto-wired {_wiredButtonCount} button(s).");
        }
    }

    private void BindHome()
    {
        var controller = FindFirst<HomeMenuController>();
        if (controller == null) return;

        BindField(controller, "welcomeLabel",
            "Lbl_Welcome", "WelcomeLabel", "Text_Welcome", "HomeWelcomeLabel");
        BindField(controller, "accountStatsLabel",
            "Lbl_AccountStats", "AccountStatsLabel", "Text_AccountStats", "HomeAccountStatsLabel");
    }

    private void BindSummon()
    {
        var controller = FindFirst<SummonMenuController>();
        if (controller == null) return;

        BindField(controller, "resultLabel",
            "Lbl_Result", "SummonResultLabel", "Text_Result", "SummonResult");
        BindField(controller, "sporesLabel",
            "Lbl_Spores", "SporesLabel", "Text_Spores", "SummonSporesLabel");
        BindField(controller, "bannerLabel",
            "Lbl_Banner", "BannerLabel", "Text_BannerTitle", "SummonBannerLabel");
        BindField(controller, "revealController",
            "SummonReveal", "SummonRevealPanel", "SummonRevealController");
        BindField(controller, "pullOneButton",
            "Btn_PullOne", "PullOneButton", "ButtonPullOne");
        BindField(controller, "pullTenButton",
            "Btn_PullTen", "PullTenButton", "ButtonPullTen");
    }

    private void BindTeam()
    {
        var controller = FindFirst<TeamMenuController>();
        if (controller == null) return;

        BindField(controller, "teamStatusLabel",
            "Lbl_TeamStatus", "TeamStatusLabel", "Text_TeamStatus");
        BindField(controller, "rosterLabel",
            "Lbl_Roster", "RosterLabel", "Text_Roster");
        BindField(controller, "hintLabel",
            "Lbl_Hint", "HintLabel", "Text_Hint");
    }

    private void BindBattle()
    {
        var controller = FindFirst<BattleSceneController>();
        if (controller == null) return;
        if (controller.View != null) return; // Authored battle view is explicitly bound once.

        BindField(controller, "battleResultLabel",
            "Lbl_BattleResult", "BattleResultLabel", "Text_BattleResult");
        BindField(controller, "stageInfoLabel",
            "Lbl_StageInfo", "StageInfoLabel", "Text_StageInfo");
        BindField(controller, "teamPreviewLabel",
            "Lbl_TeamPreview", "TeamPreviewLabel", "Text_TeamPreview");
    }

    private void BindTutorial()
    {
        var manager = FindFirst<TutorialManager>();
        if (manager != null)
        {
            BindField(manager, "overlay",
                "TutorialOverlay", "TutorialOverlayRoot", "Panel_TutorialOverlay");
        }

        var overlay = FindFirst<TutorialOverlay>();
        if (overlay == null) return;

        BindField(overlay, "root",
            "TutorialOverlayRoot", "Panel_TutorialOverlay", "TutorialOverlay");
        BindField(overlay, "messageLabel",
            "Lbl_TutorialMessage", "TutorialMessageLabel", "Text_TutorialMessage");
        BindField(overlay, "continueButton",
            "Btn_TutorialContinue", "ContinueButton", "Button_TutorialContinue");
    }

    private void BindSummonReveal()
    {
        var reveal = FindFirst<SummonRevealController>();
        if (reveal == null) return;

        BindField(reveal, "root",
            "SummonReveal", "SummonRevealPanel", "Panel_SummonReveal");
        BindField(reveal, "titleLabel",
            "Lbl_RevealTitle", "RevealTitleLabel", "Text_RevealTitle");
        BindField(reveal, "nameLabel",
            "Lbl_RevealName", "RevealNameLabel", "Text_RevealName");
        BindField(reveal, "rarityLabel",
            "Lbl_RevealRarity", "RevealRarityLabel", "Text_RevealRarity");
        BindField(reveal, "rarityFrame",
            "Img_RarityFrame", "RarityFrame", "RevealRarityFrame");
        BindField(reveal, "rarityGlow",
            "Img_RarityGlow", "RarityGlow", "RevealRarityGlow");
        BindField(reveal, "nextButton",
            "Btn_RevealNext", "RevealNextButton", "Button_RevealNext");
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void BindDebugPanels()
    {
        var debug = FindFirst<DebugProgressionController>();
        if (debug != null)
        {
            BindField(debug, "statusLabel",
                "Lbl_DebugStatus", "DebugStatusLabel", "Text_DebugStatus");
        }

        var smoke = FindFirst<GameplaySmokeTestController>();
        if (smoke != null)
        {
            BindField(smoke, "outputLabel",
                "Lbl_SmokeOutput", "SmokeOutputLabel", "Text_SmokeOutput");
        }
    }

#endif

    private void BindTeamSlots()
    {
        var slots = FindFirst<TeamRosterSlotBinder>();
        if (slots == null) return;

        BindField(slots, "teamController",
            "TeamRoot", "TeamMenuController", "Canvas/TeamRoot");
    }

    private void BindField(Component target, string fieldName, params string[] candidates)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            _missing.Add($"{target.GetType().Name}.{fieldName} (field not found)");
            return;
        }

        if (skipAlreadyAssigned && field.GetValue(target) != null) return;

        object resolved = ResolveByCandidates(field.FieldType, candidates);
        if (resolved == null)
        {
            _missing.Add($"{target.GetType().Name}.{fieldName} <- [{string.Join(", ", candidates)}]");
            return;
        }

        field.SetValue(target, resolved);
        _boundCount += 1;
    }

    private bool TryWireButton(Button button, List<MonoBehaviour> behaviours)
    {
        var candidates = BuildButtonMethodCandidates(button.name);
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.methodName)) continue;
            if (!TryFindTargetMethod(behaviours, candidate.methodName, candidate.targetTypeName, out var target, out var method)) continue;

            if (clearRuntimeListenersBeforeWire) button.onClick.RemoveAllListeners();

            var localTarget = target;
            var localMethod = method;
            if (_listeners.TryGetValue(button, out var previous)) button.onClick.RemoveListener(previous);
            UnityEngine.Events.UnityAction listener = () => localMethod.Invoke(localTarget, null);
            _listeners[button] = listener;
            button.onClick.AddListener(listener);
            return true;
        }

        return false;
    }

    private List<UiButtonWireRule> BuildButtonMethodCandidates(string buttonName)
    {
        var list = new List<UiButtonWireRule>();
        if (string.IsNullOrWhiteSpace(buttonName)) return list;

        foreach (var rule in customButtonRules)
        {
            if (rule == null) continue;
            if (!NamesMatch(rule.buttonName, buttonName)) continue;
            list.Add(new UiButtonWireRule
            {
                buttonName = rule.buttonName,
                methodName = rule.methodName,
                targetTypeName = rule.targetTypeName
            });
        }

        string normalized = NormalizeToken(buttonName);
        if (normalized.Contains("pullone")) list.Add(new UiButtonWireRule { methodName = "OnPullOnePressed" });
        if (normalized.Contains("pullten") || normalized.Contains("pull10")) list.Add(new UiButtonWireRule { methodName = "OnPullTenPressed" });
        if (normalized.Contains("autofill")) list.Add(new UiButtonWireRule { methodName = "AutoFillTeam" });
        if (normalized.Contains("startbattle")) list.Add(new UiButtonWireRule { methodName = "OnStartBattlePressed" });
        if (normalized.Contains("runbattle")) list.Add(new UiButtonWireRule { methodName = "OnRunBattlePressed" });
        if (normalized.Contains("runsmoketests") || normalized.Contains("smoketest")) list.Add(new UiButtonWireRule { methodName = "OnRunSmokeTestsPressed" });
        if (normalized.Contains("goteam")) list.Add(new UiButtonWireRule { methodName = "OnGoTeamPressed" });
        if (normalized.Contains("back")) list.Add(new UiButtonWireRule { methodName = "OnBackPressed" });
        if (normalized.Contains("options")) list.Add(new UiButtonWireRule { methodName = "OnOptionsPressed" });
        if (normalized.Contains("summon")) list.Add(new UiButtonWireRule { methodName = "OnSummonPressed" });
        if (normalized.Contains("team")) list.Add(new UiButtonWireRule { methodName = "OnTeamPressed" });
        if (normalized.Contains("battle")) list.Add(new UiButtonWireRule { methodName = "OnBattlePressed" });
        if (normalized.Contains("start")) list.Add(new UiButtonWireRule { methodName = "OnStartPressed" });
        if (normalized.Contains("tutorialcontinue")) list.Add(new UiButtonWireRule { methodName = "ContinueTutorial", targetTypeName = "TutorialOverlay" });
        if (normalized.Contains("resetsave")) list.Add(new UiButtonWireRule { methodName = "OnResetSavePressed" });
        if (normalized.Contains("grantstarterresources")) list.Add(new UiButtonWireRule { methodName = "OnGrantStarterResourcesPressed" });
        if (normalized.Contains("skiptutorial")) list.Add(new UiButtonWireRule { methodName = "OnSkipTutorialPressed" });
        if (normalized.Contains("seedstarterroster")) list.Add(new UiButtonWireRule { methodName = "OnSeedStarterRosterPressed" });
        if (normalized.Contains("opensummon")) list.Add(new UiButtonWireRule { methodName = "OnOpenSummonPressed" });
        if (normalized.Contains("backhome")) list.Add(new UiButtonWireRule { methodName = "OnBackHomePressed" });
        if (normalized.Contains("clearteam")) list.Add(new UiButtonWireRule { methodName = "OnClearTeamPressed" });
        if (normalized.Contains("retry")) list.Add(new UiButtonWireRule { methodName = "OnRetryPressed" });
        if (normalized.Contains("nextstage")) list.Add(new UiButtonWireRule { methodName = "OnNextStagePressed" });

        string cleaned = StripButtonAffixes(buttonName);
        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            string conventional = $"On{cleaned}Pressed";
            list.Add(new UiButtonWireRule { methodName = conventional });
        }

        var unique = new HashSet<string>(StringComparer.Ordinal);
        var deduped = new List<UiButtonWireRule>();
        foreach (var item in list)
        {
            string key = $"{item.targetTypeName}|{item.methodName}";
            if (unique.Add(key)) deduped.Add(item);
        }

        return deduped;
    }

    private bool TryFindTargetMethod(
        List<MonoBehaviour> behaviours,
        string methodName,
        string targetTypeName,
        out MonoBehaviour target,
        out MethodInfo method)
    {
        target = null;
        method = null;
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var behaviour in behaviours)
        {
            if (behaviour == null) continue;
            var type = behaviour.GetType();
            if (!string.IsNullOrWhiteSpace(targetTypeName))
            {
                bool typeMatch = NamesMatch(type.Name, targetTypeName) || NamesMatch(type.FullName, targetTypeName);
                if (!typeMatch) continue;
            }

            var candidate = type.GetMethod(methodName, flags);
            if (candidate == null) continue;
            if (candidate.ReturnType != typeof(void)) continue;
            if (candidate.GetParameters().Length != 0) continue;

            target = behaviour;
            method = candidate;
            return true;
        }

        return false;
    }

    private object ResolveByCandidates(Type fieldType, IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            var transform = FindByNameOrPath(candidate);
            if (transform == null) continue;

            if (fieldType == typeof(GameObject)) return transform.gameObject;
            if (fieldType == typeof(Transform)) return transform;

            if (typeof(Component).IsAssignableFrom(fieldType))
            {
                var direct = transform.GetComponent(fieldType);
                if (direct != null) return direct;

                var child = transform.GetComponentInChildren(fieldType, includeInactive);
                if (child != null) return child;
            }
        }

        return null;
    }

    private T FindFirst<T>() where T : Component
    {
        var all = GetComponentsInChildren<T>(includeInactive);
        return all.FirstOrDefault();
    }

    private Transform FindByNameOrPath(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return null;

        if (candidate.Contains("/"))
        {
            var byPath = FindByPath(candidate);
            if (byPath != null) return byPath;
        }

        foreach (var t in GetComponentsInChildren<Transform>(includeInactive))
        {
            if (NamesMatch(t.name, candidate)) return t;
        }

        return null;
    }

    private Transform FindByPath(string path)
    {
        var segments = path.Split('/');
        IEnumerable<Transform> current = new[] { transform };

        foreach (var segment in segments)
        {
            var next = new List<Transform>();
            foreach (var node in current)
            {
                foreach (Transform child in node)
                {
                    if (NamesMatch(child.name, segment))
                    {
                        next.Add(child);
                    }
                }
            }

            if (next.Count == 0) return null;
            current = next;
        }

        return current.FirstOrDefault();
    }

    private bool NamesMatch(string a, string b)
    {
        if (caseInsensitiveNames) return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        return string.Equals(a, b, StringComparison.Ordinal);
    }

    private static string NormalizeToken(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var chars = raw.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToLowerInvariant();
    }

    private static string StripButtonAffixes(string buttonName)
    {
        if (string.IsNullOrWhiteSpace(buttonName)) return string.Empty;
        string name = buttonName.Trim();

        string[] prefixes = { "Btn_", "Btn", "Button_", "Button" };
        foreach (var prefix in prefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(prefix.Length);
                break;
            }
        }

        var alnum = new string(name.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(alnum)) return string.Empty;
        return char.ToUpperInvariant(alnum[0]) + alnum.Substring(1);
    }
}
