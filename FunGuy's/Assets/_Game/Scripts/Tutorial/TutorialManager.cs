using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Enum declared outside class for save-step serialization.
public enum TutorialStep
{
    Welcome = 0,
    ShowSummonPool = 1,
    GiveTicket = 2,
    DoFirstSummon = 3,
    GoToTeamBuilder = 4,
    PlaceFirstUnit = 5,
    StartFirstBattle = 6,
    BattleWinRewards = 7,
    ExplainTankDps = 8,
    PlaceTankFrontDpsBack = 9,
    Complete = 10
}

// Tutorial controller.
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager I { get; private set; }

    [SerializeField] private TutorialOverlay overlay;
    [SerializeField] private int minimumStarterUnits = 3;
    [SerializeField] private string[] preferredStarterIds =
    {
        "1",
        "2",
        "6",
    };

    private PlayerSave save => Game.Save ??= SaveSystem.LoadOrNew();
    private string currentMessage;
    private System.Action currentContinue;

    public static TutorialManager EnsureInstance()
    {
        if (I != null) return I;
        return new GameObject("TutorialManager").AddComponent<TutorialManager>();
    }

    public void AttachOverlay(TutorialOverlay value)
    {
        overlay = value;
        if (!string.IsNullOrEmpty(currentMessage)) overlay.Say(currentMessage, currentContinue);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (I == this) I = null;
    }

    public bool IsActive => save != null && !save.tutorialCompleted;

    public bool IsSceneAllowed(string sceneName)
    {
        if (!IsActive) return true;
        if (string.IsNullOrWhiteSpace(sceneName)) return false;

        switch ((TutorialStep)save.tutorialStep)
        {
            case TutorialStep.Welcome:
                return string.Equals(sceneName, "Tutorial", System.StringComparison.OrdinalIgnoreCase);
            case TutorialStep.ShowSummonPool:
            case TutorialStep.GiveTicket:
            case TutorialStep.DoFirstSummon:
                return string.Equals(sceneName, "Summon", System.StringComparison.OrdinalIgnoreCase);
            case TutorialStep.GoToTeamBuilder:
            case TutorialStep.PlaceFirstUnit:
            case TutorialStep.PlaceTankFrontDpsBack:
                return string.Equals(sceneName, "Team", System.StringComparison.OrdinalIgnoreCase);
            case TutorialStep.StartFirstBattle:
            case TutorialStep.BattleWinRewards:
            case TutorialStep.ExplainTankDps:
                return string.Equals(sceneName, "Battle", System.StringComparison.OrdinalIgnoreCase);
            case TutorialStep.Complete:
                return string.Equals(sceneName, "Home", System.StringComparison.OrdinalIgnoreCase);
            default:
                return string.Equals(sceneName, "Tutorial", System.StringComparison.OrdinalIgnoreCase);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsActive)
        {
            // A completed save can never re-enter the onboarding scene.
            if (string.Equals(scene.name, "Tutorial", System.StringComparison.OrdinalIgnoreCase))
                OpenScene("Home");
            return;
        }

        if (!IsSceneAllowed(scene.name))
        {
            var expected = ExpectedScene();
            if (!string.IsNullOrEmpty(expected) && !string.Equals(scene.name, expected, System.StringComparison.OrdinalIgnoreCase))
                OpenScene(expected);
        }
    }

    private string ExpectedScene()
    {
        switch ((TutorialStep)save.tutorialStep)
        {
            case TutorialStep.Welcome: return "Tutorial";
            case TutorialStep.ShowSummonPool:
            case TutorialStep.GiveTicket:
            case TutorialStep.DoFirstSummon: return "Summon";
            case TutorialStep.GoToTeamBuilder:
            case TutorialStep.PlaceFirstUnit:
            case TutorialStep.PlaceTankFrontDpsBack: return "Team";
            case TutorialStep.StartFirstBattle:
            case TutorialStep.BattleWinRewards:
            case TutorialStep.ExplainTankDps: return "Battle";
            default: return "Tutorial";
        }
    }

    private static void OpenScene(string name)
    {
        if (SceneManager.GetActiveScene().name != name) SceneManager.LoadScene(name);
    }

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Game.EnsureInitialized();
        EnsureData();
        EnsureStarterProfileForFirstRun();

        if (save.tutorialCompleted)
        {
            if (overlay != null) overlay.Hide();
            return;
        }

        if (save.tutorialStep < 0 || save.tutorialStep > (int)TutorialStep.Complete)
            save.tutorialStep = (int)TutorialStep.Welcome;

        var step = (TutorialStep)save.tutorialStep;
        GoToStep(step);
        if (step >= TutorialStep.ShowSummonPool && step <= TutorialStep.DoFirstSummon) OpenScene("Summon");
        else if (step == TutorialStep.PlaceFirstUnit) OpenScene("Team");
    }

    public void GoToStep(TutorialStep step)
    {
        save.tutorialStep = (int)step;
        SaveSystem.Save(save);

        if (overlay != null) overlay.Show();

        switch (step)
        {
            case TutorialStep.Welcome:
                Present(
                    "Welcome, sproutling. Let's walk through menus, summoning, and your first battle.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.ShowSummonPool:
                Present(
                    "This is the Summon menu. Pull your first unit to build your team.",
                    ContinueTutorial
                );
                OpenScene("Summon");
                break;

            case TutorialStep.GiveTicket:
                // The one-time ticket is created with the save, never replenished on resume.
                Present(
                    "You received a tutorial summon ticket. Use it now.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.DoFirstSummon:
                Present(
                    "Summon one fighter. After the pull, continue to Team setup.",
                    null
                );
                break;

            case TutorialStep.GoToTeamBuilder:
                Present(
                    "Open Team and place your first unit.",
                    ContinueTutorial
                );
                OpenScene("Team");
                break;

            case TutorialStep.PlaceFirstUnit:
                Present(
                    "Place durable fighters in front. Tap a position, then a fighter; tap two positions to swap or move.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.StartFirstBattle:
                Present(
                    "Start battle. Basic attacks are automatic; tap a signature to queue it, or turn Auto on.",
                    null
                );
                OpenScene("Battle");
                break;

            case TutorialStep.BattleWinRewards:
                Game.Campaign.ClaimTutorialBattleReward();
                Present(
                    "Rewards saved! First clears earn gold. Replaying a cleared stage is practice and gives no extra rewards.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.ExplainTankDps:
                Present(
                    "Use gold to level up your fighters. Compare their next-level stats and read their skills before spending.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.PlaceTankFrontDpsBack:
                Present(
                    "Open Upgrade fighters below. Choose a fighter, review the gold cost and level up. Continue when ready, or save your gold for later.",
                    ContinueTutorial
                );
                OpenScene("Team");
                break;

            case TutorialStep.Complete:
                save.tutorialCompleted = true;
                SaveSystem.Save(save);
                if (overlay != null) overlay.Hide();
                OpenScene("Home");
                break;
        }
    }

    public void ContinueTutorial()
    {
        if (save == null || save.tutorialCompleted) return;
        var current = (TutorialStep)save.tutorialStep;
        if (current == TutorialStep.Complete) return;
        GoToStep(current + 1);
    }

    public void OnFirstSummonCompleted()
    {
        if (save == null || save.tutorialCompleted) return;
        if ((TutorialStep)save.tutorialStep <= TutorialStep.DoFirstSummon)
            GoToStep(TutorialStep.GoToTeamBuilder);
    }

    public void OnFirstBattleCompleted(bool won)
    {
        if (!won || save == null || save.tutorialCompleted) return;
        if ((TutorialStep)save.tutorialStep <= TutorialStep.StartFirstBattle)
            GoToStep(TutorialStep.BattleWinRewards);
    }

    // Guidance never owns a purchase or requires spending to complete onboarding.
    public void SetWorkshopVisible(bool visible)
    {
        if (save.tutorialCompleted || save.tutorialStep != (int)TutorialStep.PlaceTankFrontDpsBack) return;
        if (visible) { if (overlay != null) overlay.Hide(); }
        else Present("Your upgrades save immediately. Return here between stages to strengthen your team. Continue to Home → Campaign.", ContinueTutorial);
    }

    private void Present(string message, System.Action onContinue)
    {
        currentMessage = message;
        currentContinue = onContinue;
        if (overlay != null) overlay.Say(message, onContinue);
        else Debug.Log($"[Tutorial] Waiting for overlay: {message}");
    }

    private void EnsureData()
    {
        if (Game.Data != null) return;
        Game.Data = new GameData();
        Game.Data.LoadAll();
    }

    private void EnsureStarterProfileForFirstRun()
    {
        if (save == null || save.tutorialCompleted || Game.Data == null) return;

        bool changed = false;


        int targetCount = Mathf.Clamp(minimumStarterUnits, 1, 5);
        var ownedIds = new HashSet<string>(
            save.units.Where(u => u != null && !string.IsNullOrWhiteSpace(u.charId)).Select(u => u.charId),
            System.StringComparer.OrdinalIgnoreCase);

        if (save.units.Count < targetCount)
        {
            foreach (var charId in ResolveStarterIds())
            {
                if (string.IsNullOrWhiteSpace(charId) || ownedIds.Contains(charId)) continue;
                save.units.Add(new OwnedUnit
                {
                    charId = charId,
                    level = 3,
                    copies = 1,
                    stars = 1,
                    coreLevel = 1,
                    xp = 0,
                    gearSlots = new List<GearSlotState>(),
                });
                ownedIds.Add(charId);
                changed = true;
                if (save.units.Count >= targetCount) break;
            }
        }

        save.activeTeam.RemoveAll(charId => string.IsNullOrWhiteSpace(charId) || !ownedIds.Contains(charId));
        if (save.activeTeam.Count == 0 && save.units.Count > 0)
        {
            save.activeTeam = save.units
                .Where(u => u != null && !string.IsNullOrWhiteSpace(u.charId))
                .Select(u => u.charId)
                .Take(targetCount)
                .ToList();
            changed = true;
        }

        if (!changed) return;
        SaveSystem.Save(save);
        Game.Save = save;
    }

    private IEnumerable<string> ResolveStarterIds()
    {
        var ordered = new List<string>();

        if (preferredStarterIds != null)
        {
            foreach (var id in preferredStarterIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!Game.Data.Characters.ContainsKey(id)) continue;
                ordered.Add(id);
            }
        }

        var roleFallback = new[] { "Tank", "DPS", "Healer" };
        foreach (var role in roleFallback)
        {
            var pick = Game.Data.Characters.Values
                .Where(c => c != null && string.Equals(c.role, role, System.StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.rarity)
                .ThenBy(c => c.id, System.StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (pick != null) ordered.Add(pick.id);
        }

        foreach (var candidate in Game.Data.Characters.Values
                     .Where(c => c != null)
                     .OrderByDescending(c => c.rarity)
                     .ThenBy(c => c.id, System.StringComparer.OrdinalIgnoreCase))
        {
            ordered.Add(candidate.id);
        }

        return ordered.Distinct(System.StringComparer.OrdinalIgnoreCase);
    }
}
