#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DebugProgressionController : MonoBehaviour
{
    [SerializeField] private Text statusLabel;
    [SerializeField] private string summonScene = "Summon";
    [SerializeField] private string homeScene = "Home";
    [SerializeField] private string defaultBannerId = "b_standard";

    public void OnResetSavePressed()
    {
        var save = SaveSystem.ResetToNewSave();
        Game.Save = save;
        SetStatus("Save reset to fresh profile.");
    }

    public void OnGrantStarterResourcesPressed()
    {
        var save = SaveSystem.GrantDebugResources(gold: 10000, spores: 500, sporeEssence: 1000, coreFragments: 500, primeSpores: 100);
        Game.Save = save;
        SetStatus("Granted starter debug resources.");
    }

    public void OnSkipTutorialPressed()
    {
        var save = SaveSystem.SetTutorialStateForDebug((int)TutorialStep.Complete, completed: true);
        Game.Save = save;
        SetStatus("Tutorial flagged complete.");
    }

    public void OnSeedStarterRosterPressed()
    {
        EnsureServices();
        var save = Game.Save ?? SaveSystem.LoadOrNew();

        int pulls = 10;
        int seeded = 0;
        for (int i = 0; i < pulls; i++)
        {
            try
            {
                _ = Game.Gacha.PullOne(save, defaultBannerId, consumeCurrency: false);
                seeded += 1;
            }
            catch (System.Exception ex)
            {
                SetStatus($"Seed stopped: {ex.Message}");
                break;
            }
        }

        SaveSystem.Save(save);
        Game.Save = save;
        SetStatus($"Seeded roster with {seeded} pulls from {defaultBannerId}.");
    }

    public void OnOpenSummonPressed()
    {
        SceneManager.LoadScene(summonScene);
    }

    public void OnBackHomePressed()
    {
        SceneManager.LoadScene(homeScene);
    }

    private void EnsureServices()
    {
        if (Game.Data == null)
        {
            Game.Data = new GameData();
            Game.Data.LoadAll();
        }

        if (Game.Gacha == null)
        {
            Game.Gacha = new GachaService(Game.Data);
        }
    }

    private void SetStatus(string message)
    {
        if (statusLabel != null) statusLabel.text = message;
        Debug.Log($"[DebugProgression] {message}");
    }
}

#endif
