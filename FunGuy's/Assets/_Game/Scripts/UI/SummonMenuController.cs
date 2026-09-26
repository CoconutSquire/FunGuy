using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SummonMenuController : MonoBehaviour
{
    [SerializeField] private string defaultBannerId = "b_event_starspore";
    [SerializeField] private Text resultLabel;
    [SerializeField] private Text sporesLabel;
    [SerializeField] private Text bannerLabel;
    [SerializeField] private SummonRevealController revealController;
    [SerializeField] private Button pullOneButton;
    [SerializeField] private Button pullTenButton;

    private const int MultiPullCount = 10;
    private bool _busy;

    public void Initialize(
        Text result,
        Text spores,
        Text banner,
        SummonRevealController reveal,
        Button pullOne,
        Button pullTen,
        Button back)
    {
        resultLabel = result;
        sporesLabel = spores;
        bannerLabel = banner;
        revealController = reveal;
        pullOneButton = pullOne;
        pullTenButton = pullTen;

        pullOneButton.onClick.RemoveListener(OnPullOnePressed);
        pullOneButton.onClick.AddListener(OnPullOnePressed);
        pullTenButton.onClick.RemoveListener(OnPullTenPressed);
        pullTenButton.onClick.AddListener(OnPullTenPressed);

        if (back != null)
        {
            back.onClick.RemoveListener(OnBackPressed);
            back.onClick.AddListener(OnBackPressed);
        }
    }

    private PlayerSave Save => Game.Save ??= SaveSystem.LoadOrNew();

    private void Start()
    {
        EnsureServices();
        RefreshUi();
    }

    public void OnPullOnePressed()
    {
        ExecutePulls(1);
    }

    public void OnPullTenPressed()
    {
        ExecutePulls(MultiPullCount);
    }

    public void OnBackPressed()
    {
        SceneManager.LoadScene("Home");
    }

    private void ExecutePulls(int count)
    {
        if (_busy) return;
        EnsureServices();
        try
        {
            var result = Game.Summons.Pull(defaultBannerId, count);
            RefreshUi();
            PlayRevealOrFallback(result.characterIds, result.usedTutorialTicket);
        }
        catch (Exception error) { SetResult($"Summon failed: {error.Message}"); }
    }

    private void EnsureServices()
    {
        Game.EnsureInitialized();
    }

    private void NotifyTutorialOnFirstSummon()
    {
        if (TutorialManager.I == null) return;
        TutorialManager.I.OnFirstSummonCompleted();
    }

    private string BuildResultSummary(List<string> pulledIds, bool usedTicket)
    {
        var names = pulledIds
            .Select(id => Game.Data.Characters.TryGetValue(id, out var c) ? $"{c.name} ({c.rarityTier})" : id)
            .ToList();

        string prefix = usedTicket ? "Tutorial ticket used.\n" : string.Empty;
        return $"{prefix}Pulled {pulledIds.Count}:\n{string.Join(", ", names)}";
    }

    private void RefreshUi()
    {
        if (Game.Data != null && Game.Data.Banners.TryGetValue(defaultBannerId, out var banner) && bannerLabel != null)
        {
            bannerLabel.text = banner.name;
        }

        if (sporesLabel != null)
        {
            sporesLabel.text = $"Spores: {Save.spores}";
        }
    }

    private void SetResult(string message)
    {
        if (resultLabel != null) resultLabel.text = message;
        Debug.Log($"[Summon] {message}");
    }

    private void PlayRevealOrFallback(List<string> pulledIds, bool usedTicket)
    {
        var pulledCharacters = pulledIds
            .Where(id => Game.Data.Characters.ContainsKey(id))
            .Select(id => Game.Data.Characters[id])
            .ToList();

        if (revealController == null)
        {
            SetResult(BuildResultSummary(pulledIds, usedTicket));
            NotifyTutorialOnFirstSummon();
            return;
        }

        _busy = true;
        SetPullButtonsInteractable(false);
        revealController.Play(pulledCharacters, () =>
        {
            _busy = false;
            SetPullButtonsInteractable(true);
            SetResult(BuildResultSummary(pulledIds, usedTicket));
            NotifyTutorialOnFirstSummon();
        });
    }

    private void SetPullButtonsInteractable(bool interactable)
    {
        if (pullOneButton != null) pullOneButton.interactable = interactable;
        if (pullTenButton != null) pullTenButton.interactable = interactable;
    }
}
