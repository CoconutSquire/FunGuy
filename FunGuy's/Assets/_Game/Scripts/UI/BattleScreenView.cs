using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable] public sealed class SignatureCardView {
    public Button button;
    public TMP_Text label;
    public Image energy;
}

public sealed class BattleScreenView : MonoBehaviour
{
    public int layoutVersion;
    public RawImage backdrop;
    public RectTransform fighterLayer;
    public BattleFighterView fighterPrefab;
    public TMP_Text stageLabel, waveLabel, feedLabel, playerBonuses, enemyBonuses;
    public TMP_Text[] cellLabels;
    public Button start, home, team, pause, speed, auto, finish, motion;
    public TMP_Text startLabel, pauseLabel, speedLabel, autoLabel, motionLabel;
    public SignatureCardView[] signatures;
    public GameObject resultPanel, inspectorPanel;
    public TMP_Text resultTitle, resultBody, inspectorTitle, inspectorBody;
    public Button retry, next, resultTeam, inspectorClose;
    public ScrollRect inspectorScroll;
    private readonly Dictionary<string, BattleFighterView> fighters = new();
    private readonly List<string> playerIds = new();
    private GameData data;
    public Action<string> Inspect;
    public IReadOnlyDictionary<string, BattleFighterView> Fighters => fighters;
    public IReadOnlyList<string> PlayerIds => playerIds;
    public static Vector2 CellPosition(TeamSide side, int slot) => new(
        (side == TeamSide.Player ? -1 : 1) * (130 + FormationRules.Depth(slot) * 195),
        155 - FormationRules.Lane(slot) * 115 + (FormationRules.Depth(slot) % 2) * 57.5f);
    public static Color BiomeColor(string biome) => (biome ?? "").ToLowerInvariant() switch {
        "forest" => new(.43f, .64f, .2f), "wetlands" => new(.23f, .66f, .67f),
        "decay" => new(.67f, .35f, .63f), "tundra" => new(.4f, .72f, .86f),
        "kitchen" => new(.88f, .38f, .15f), "cosmic" => new(.58f, .42f, .9f), _ => new(.7f, .54f, .3f)
    };
    public void Initialize(GameData source)
    {
        data = source;
        backdrop.texture = Resources.Load<Texture2D>("Presentation/kitchen-battlefield-v1");
        if (backdrop.texture != null) backdrop.color = Color.white;
        resultPanel.SetActive(false); inspectorPanel.SetActive(false);
    }
    public void SetFighters(IReadOnlyList<BattleFighterState> states)
    {
        var incoming = states.Select(s => s.InstanceId).ToHashSet();
        foreach (string id in fighters.Keys.Where(id => !incoming.Contains(id)).ToArray()) {
            fighters[id].gameObject.SetActive(false); Destroy(fighters[id].gameObject); fighters.Remove(id);
        }
        foreach (var state in states) {
            if (!fighters.TryGetValue(state.InstanceId, out var view)) {
                view = Instantiate(fighterPrefab, fighterLayer); view.name = "Fighter_" + state.InstanceId;
                fighters.Add(state.InstanceId, view);
                string id = state.InstanceId;
                view.GetComponent<Button>().onClick.AddListener(() => Inspect?.Invoke(id));
            }
            string biome = null, role = null;
            if (data.Characters.TryGetValue(state.ContentId, out var c)) { biome = c.biome; role = c.classArchetype; }
            else if (data.Enemies.TryGetValue(state.ContentId, out var e)) { biome = e.biome; role = e.classArchetype; }
            view.Initialize(state, biome, role);
        }
        playerIds.Clear(); playerIds.AddRange(states.Where(s => s.Side == TeamSide.Player).OrderBy(s => s.Slot).Select(s => s.InstanceId));
        foreach (var unit in fighters.Values.OrderByDescending(v => CellPosition(v.State.Side, v.State.Slot).y)) unit.transform.SetAsLastSibling();
        RefreshCellLabels();
    }
    private void RefreshCellLabels()
    {
        for (int side = 0; side < 2; side++) for (int slot = 0; slot < FormationRules.SlotCount; slot++)
            cellLabels[side * FormationRules.SlotCount + slot].gameObject.SetActive(!fighters.Values.Any(f => (int)f.State.Side == side && f.State.Slot == slot));
    }
    public void Consume(BattleEvent e, bool reducedMotion)
    {
        if (e.Target != null && fighters.TryGetValue(e.Target.InstanceId, out var target)) {
            target.Apply(e.Target, reducedMotion); target.Animate(e, reducedMotion);
            if (e.Kind == BattleEventKind.Moved) RefreshCellLabels();
        }
        if (e.Kind == BattleEventKind.SkillUsed && e.ActorId != null && fighters.TryGetValue(e.ActorId, out var actor)) {
            string name = data.Skills.TryGetValue(e.Detail, out var skill) ? skill.name : e.Detail;
            feedLabel.text = actor.State.Name + "  ·  " + name;
        }
        if (e.Kind == BattleEventKind.Redirected && e.Target != null) feedLabel.text = e.Target.Name + " protects an ally";
        if (e.Kind == BattleEventKind.ActionSkipped && e.Target != null) feedLabel.text = e.Target.Name + " cannot act";
    }
    public void Tick(float delta, bool reducedMotion) { foreach (var fighter in fighters.Values) fighter.Tick(delta, reducedMotion); }
    public void RefreshControls(CampaignSession run, bool playing, bool paused, bool autoEnabled, float rate, bool reducedMotion, bool settlementPending)
    {
        pause.interactable = playing; finish.interactable = playing; start.interactable = !playing;
        startLabel.text = playing ? (paused ? "Paused" : "In battle") : settlementPending ? "Retry saving" : run == null ? "Start battle" : "Play again";
        pauseLabel.text = paused ? "Resume" : "Pause"; speedLabel.text = rate.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "×";
        autoLabel.text = autoEnabled ? "AUTO  ON" : "AUTO  OFF";
        motionLabel.text = reducedMotion ? "Motion: reduced" : "Motion: full";
        for (int i = 0; i < signatures.Length; i++) {
            var card = signatures[i]; bool exists = i < playerIds.Count;
            card.button.gameObject.SetActive(exists); if (!exists) continue;
            string id = playerIds[i]; var state = fighters[id].State;
            bool queued = run != null && run.Battle.IsSignatureQueued(id);
            data.Skills.TryGetValue(state.SignatureSkillId ?? "", out var skill);
            string availability = state.Hp <= 0 ? "DEFEATED" : queued ? "QUEUED · tap to cancel" :
                state.SignatureCooldown > 0 ? $"Cooldown {state.SignatureCooldown} · queue" :
                state.Energy >= (skill?.energyCost ?? 100) ? "READY · tap to cast" : "Tap to queue";
            card.label.text = $"{state.Name}\n{skill?.name ?? "No signature"}\n{state.Energy}/{state.MaxEnergy} energy  ·  {availability}";
            card.button.interactable = playing && run.Battle.Outcome == BattleOutcome.Running && state.Hp > 0 && skill != null && state.SignatureSkillId != state.BasicSkillId;
            card.energy.fillAmount = state.MaxEnergy > 0 ? state.Energy / (float)state.MaxEnergy : 0;
            card.button.GetComponent<Image>().color = queued ? new(.32f, .45f, .17f, .98f) : new(.12f, .17f, .14f, .98f);
        }
    }
    public void RefreshBonuses(BattleSession battle)
    {
        string Format(TeamSide side) => string.Join("  ·  ", battle.GetFormationBonuses(side).Select(s => s.Replace("biome:", "").Replace("class:", "").Replace("role:", "").Replace(":", " ×")));
        playerBonuses.text = "YOUR FORMATION  " + Format(TeamSide.Player);
        enemyBonuses.text = "ENEMY FORMATION  " + Format(TeamSide.Enemy);
    }
    public void ShowInspector(string id)
    {
        if (!fighters.TryGetValue(id, out var unit)) return;
        var state = unit.State;
        inspectorTitle.text = state.Name + " · " + FormationRules.Label(state.Slot);
        inspectorBody.text = BuildInspectorText(state);
        inspectorPanel.SetActive(true);
        inspectorBody.rectTransform.sizeDelta = new(inspectorBody.rectTransform.sizeDelta.x, Mathf.Max(300, inspectorBody.preferredHeight + 20));
        inspectorScroll.verticalNormalizedPosition = 1;
    }

    private string BuildInspectorText(BattleFighterState state)
    {
        data.Skills.TryGetValue(state.BasicSkillId ?? "", out var basic);
        data.Skills.TryGetValue(state.SignatureSkillId ?? "", out var signature);
        data.Skills.TryGetValue(state.UltimateSkillId ?? "", out var ultimate);

        var sections = new List<string>
        {
            $"Health  {state.Hp} / {state.MaxHp}\nShield  {state.Shield}     Energy  {state.Energy} / {state.MaxEnergy}",
            BuildBattleSkill("BASIC", basic),
            BuildBattleSkill("SIGNATURE", signature),
            BuildBattleSkill("ULTIMATE", ultimate)
        };

        if (data.Characters.TryGetValue(state.ContentId, out var character))
        {
            var passives = character.passives == null
                ? new List<PassiveDef>()
                : character.passives.Where(p => p != null && !string.IsNullOrWhiteSpace(p.description)).ToList();
            if (passives.Count > 0)
                sections.Add("PASSIVES\n" + string.Join("\n\n", passives.Select(p => p.description)));
            else if (!string.IsNullOrWhiteSpace(character.passiveDescription))
                sections.Add("PASSIVE · " + (string.IsNullOrWhiteSpace(character.passiveName) ? "Passive" : character.passiveName) + "\n" + character.passiveDescription);
            else
                sections.Add("PASSIVES\nNo passive abilities listed for this character.");
        }

        sections.Add("ACTIVE STATUS EFFECTS\n" +
            (state.Statuses.Count == 0 ? "None." : string.Join("\n", state.Statuses.GroupBy(s => s.Name).Select(g =>
                g.Key + (g.Count() > 1 ? " ×" + g.Count() : "") + "  ·  " + string.Join(", ", g.Select(s => s.RemainingTurns < 0 ? "permanent" : s.RemainingTurns + " turns").Distinct())))));

        return string.Join("\n\n", sections);
    }

    private static string BuildBattleSkill(string category, SkillDef skill)
    {
        if (skill == null) return category + "\nNot available for this fighter.";
        var usage = skill.energyCost > 0 ? $"{skill.energyCost} energy" :
            skill.cooldown > 0 ? $"{skill.cooldown} turn cooldown" : "No cooldown";
        var target = string.IsNullOrWhiteSpace(skill.target) ? "" : $"  •  {skill.target}";

        string description = skill.description;
        if (string.IsNullOrWhiteSpace(description) ||
            description.StartsWith("Ultimate for ", StringComparison.OrdinalIgnoreCase))
            description = BuildEffectDescription(skill);
        if (string.IsNullOrWhiteSpace(description))
            description = "Skill mechanics are defined by this fighter's combat effects.";

        return $"{category} · {skill.name}\n{usage}{target}\n{description}";
    }

    private static string BuildEffectDescription(SkillDef skill)
    {
        if (skill?.effects == null || skill.effects.Count == 0) return null;
        var parts = new List<string>();
        foreach (var effect in skill.effects)
        {
            if (effect == null || string.IsNullOrWhiteSpace(effect.type)) continue;
            string target = string.IsNullOrWhiteSpace(effect.target) ? skill.target : effect.target;
            string targetLabel = string.IsNullOrWhiteSpace(target) ? "the target" : target;
            switch (effect.type)
            {
                case "Damage":
                    parts.Add($"Deals {effect.scale * 100f:0.#}% damage to {targetLabel}.");
                    if (effect.ignoreDefense > 0) parts.Add($"Ignores {effect.ignoreDefense * 100f:0.#}% defense.");
                    if (effect.ignoreShield) parts.Add("Ignores shields.");
                    if (effect.sureHit) parts.Add("Cannot miss.");
                    break;
                case "Heal":
                    parts.Add($"Heals {targetLabel} for {effect.scale * 100f:0.#}% of {string.IsNullOrWhiteSpace(effect.stat) ? "POT" : effect.stat}.");
                    break;
                case "Shield":
                    parts.Add($"Grants {targetLabel} a shield for {effect.scale * 100f:0.#}% of {string.IsNullOrWhiteSpace(effect.stat) ? "POT" : effect.stat}.");
                    break;
                case "ApplyStatus":
                    string status = string.IsNullOrWhiteSpace(effect.status) ? "a status effect" : effect.status;
                    string chance = effect.chance > 0 && effect.chance < 1 ? $" ({effect.chance * 100f:0.#}% chance)" : "";
                    string duration = effect.duration > 0 ? $" for {effect.duration} turns" : "";
                    parts.Add($"Applies {status} to {targetLabel}{duration}{chance}.");
                    if (effect.potency > 0) parts.Add($"Potency {effect.potency * 100f:0.#}%.");
                    break;
                case "Cleanse": parts.Add($"Cleanses negative effects from {targetLabel}."); break;
                case "Dispel": parts.Add($"Dispels positive effects from {targetLabel}."); break;
                case "Energy": parts.Add($"Restores {effect.scale:0.#} energy to {targetLabel}."); break;
                case "Gauge": parts.Add($"Changes turn gauge for {targetLabel} by {effect.scale * 100f:0.#}%."); break;
                case "Move": parts.Add($"Moves {targetLabel} by {effect.slot} slot{(Math.Abs(effect.slot) == 1 ? "" : "s")}."); break;
                default: parts.Add(effect.type + "."); break;
            }
        }
        return parts.Count == 0 ? null : string.Join(" ", parts);
    }
    public void ShowResult(string title, string body, bool nextUnlocked)
    {
        resultTitle.text = title; resultBody.text = body; next.interactable = nextUnlocked; resultPanel.SetActive(true);
    }
}
