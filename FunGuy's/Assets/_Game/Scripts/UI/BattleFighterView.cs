using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleFighterView : MonoBehaviour
{
    public TMP_Text nameLabel, hpLabel, shieldLabel, statusLabel, floatingLabel;
    public Image hpFill, energyFill;
    public FungusPortrait portrait;
    public CanvasGroup group;
    public BattleFighterState State { get; private set; }
    private Vector2 home, moveFrom;
    private float moveTime, pulse, hit, popup;
    private Color baseColor;
    public void Initialize(BattleFighterState state, string biome, string role)
    {
        moveTime = pulse = hit = popup = 0; floatingLabel.alpha = 0;
        portrait.cap = BattleScreenView.BiomeColor(biome);
        portrait.enemy = state.Side == TeamSide.Enemy;
        portrait.variant = role == "Tank" || role == "Wall" || role == "Taunt" ? 0 : role == "Support" || role == "Mage" || role == "Healer" ? 1 : 2;
        baseColor = Color.white; portrait.color = baseColor; portrait.SetVerticesDirty();
        nameLabel.text = state.Name; hpFill.color = state.Side == TeamSide.Player ? new(.48f, .8f, .24f) : new(.92f, .34f, .23f);
        Apply(state, true);
    }
    public void Apply(BattleFighterState state, bool instant = false)
    {
        State = state;
        Vector2 next = BattleScreenView.CellPosition(state.Side, state.Slot);
        if (next != home) { moveFrom = ((RectTransform)transform).anchoredPosition; home = next; moveTime = instant ? 0 : .3f; }
        if (instant) ((RectTransform)transform).anchoredPosition = home;
        hpLabel.text = state.Hp <= 0 ? "DEFEATED" : $"{state.Hp:N0} / {state.MaxHp:N0}";
        hpFill.fillAmount = state.MaxHp <= 0 ? 0 : state.Hp / (float)state.MaxHp;
        energyFill.fillAmount = state.MaxEnergy <= 0 ? 0 : state.Energy / (float)state.MaxEnergy;
        shieldLabel.text = state.Shield > 0 ? $"SHIELD {state.Shield}" : "";
        statusLabel.text = string.Join("  ", state.Statuses.Select(s => s.Name + (s.RemainingTurns < 0 ? "" : " " + s.RemainingTurns)).Distinct().Take(2));
        if (state.Statuses.Count > 2) statusLabel.text += $" +{state.Statuses.Count - 2}";
        group.alpha = state.Hp > 0 ? 1 : .3f;
    }
    public void Animate(BattleEvent e, bool reduced)
    {
        if (e.Kind == BattleEventKind.SkillUsed) pulse = reduced ? 0 : .3f;
        if (e.Kind == BattleEventKind.Damage) { hit = reduced ? 0 : .25f; Pop("−" + e.Amount, new(1, .6f, .42f)); }
        if (e.Kind == BattleEventKind.Heal && e.Amount > 0) Pop("+" + e.Amount, new(.6f, 1, .45f));
        if (e.Kind == BattleEventKind.Shield) Pop("SHIELD +" + e.Amount, new(.4f, .85f, 1));
        if (e.Kind == BattleEventKind.Miss) Pop("MISS", Color.white);
        if (e.Kind == BattleEventKind.Critical) Pop("CRITICAL", new(1, .85f, .35f));
        if (e.Kind == BattleEventKind.UnitDied) Pop("DEFEATED", new(1, .5f, .4f));
    }
    private void Pop(string text, Color color) { floatingLabel.text = text; floatingLabel.color = color; popup = .65f; }
    public void Tick(float delta, bool reduced)
    {
        moveTime = Mathf.Max(0, moveTime - delta); pulse = Mathf.Max(0, pulse - delta); hit = Mathf.Max(0, hit - delta); popup = Mathf.Max(0, popup - delta);
        var p = moveTime > 0 && !reduced ? Vector2.Lerp(moveFrom, home, 1 - moveTime / .3f) : home;
        if (!reduced) p.x += Mathf.Sin(pulse / .3f * Mathf.PI) * (State.Side == TeamSide.Player ? 18 : -18) + Mathf.Sin(hit * 100) * hit * 16;
        ((RectTransform)transform).anchoredPosition = p;
        portrait.color = hit > 0 ? Color.Lerp(baseColor, new Color(1, .45f, .25f), hit * 2) : baseColor;
        floatingLabel.alpha = popup > .2f ? 1 : popup * 5;
        floatingLabel.rectTransform.anchoredPosition = new(0, 115 + (reduced ? 0 : (1 - popup / .65f) * 18));
    }
}
