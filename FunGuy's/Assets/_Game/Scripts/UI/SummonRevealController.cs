using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SummonRevealController : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Text titleLabel;
    [SerializeField] private Text nameLabel;
    [SerializeField] private Text rarityLabel;
    [SerializeField] private Image rarityFrame;
    [SerializeField] private Image rarityGlow;
    [SerializeField] private Button nextButton;
    [SerializeField] private float revealDelay = 0.2f;
    [SerializeField] private float pulseSpeed = 2f;

    private readonly Queue<CharacterDef> _queue = new();
    private Action _onFinished;
    private bool _isPlaying;
    private bool _waitingForNext;
    private float _pulseTimer;

    public void Initialize(GameObject panel, Text title, Text name, Text rarity, Image frame, Image glow, Button next)
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextPressed);
        root = panel; titleLabel = title; nameLabel = name; rarityLabel = rarity;
        rarityFrame = frame; rarityGlow = glow; nextButton = next;
        nextButton.onClick.RemoveListener(OnNextPressed);
        nextButton.onClick.AddListener(OnNextPressed);
    }

    private void Update()
    {
        if (!_isPlaying || rarityGlow == null) return;

        _pulseTimer += Time.unscaledDeltaTime * pulseSpeed;
        float p = 0.85f + (Mathf.Sin(_pulseTimer) * 0.15f);
        var c = rarityGlow.color;
        rarityGlow.color = new Color(
            Mathf.Clamp01(c.r * p),
            Mathf.Clamp01(c.g * p),
            Mathf.Clamp01(c.b * p),
            c.a);
    }

    public bool IsPlaying => _isPlaying;

    public void Play(List<CharacterDef> pulledCharacters, Action onFinished)
    {
        if (pulledCharacters == null || pulledCharacters.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        _queue.Clear();
        foreach (var c in pulledCharacters.Where(c => c != null))
        {
            _queue.Enqueue(c);
        }

        if (_queue.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        _onFinished = onFinished;
        _isPlaying = true;
        _waitingForNext = false;
        _pulseTimer = 0f;

        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        while (_queue.Count > 0)
        {
            var character = _queue.Dequeue();
            RenderCharacter(character);

            float t = 0f;
            while (t < revealDelay)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            _waitingForNext = true;
            while (_waitingForNext)
            {
                yield return null;
            }
        }

        _isPlaying = false;
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);
        _onFinished?.Invoke();
    }

    private void RenderCharacter(CharacterDef character)
    {
        if (titleLabel != null) titleLabel.text = "New Funguy Acquired";
        if (nameLabel != null) nameLabel.text = character.name;

        if (rarityLabel != null) rarityLabel.text = $"{character.rarityTier} · {character.biome} · {character.classArchetype}";

        var rarityColor = IdleHuntressTheme.RarityColor(character.rarity);
        if (rarityFrame != null) rarityFrame.color = rarityColor;
        if (rarityGlow != null) rarityGlow.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.75f);
    }

    private void OnNextPressed()
    {
        _waitingForNext = false;
    }
}
