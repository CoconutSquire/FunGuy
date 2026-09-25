using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class FunguyRosterController : MonoBehaviour
{
    private GameObject root;
    private RectTransform content;
    private Text characterLabel;
    private Text identityLabel;
    private Text statsLabel;
    private Text equipmentLabel;
    private Button backButton;
    private int selectedIndex;

    public void Initialize(GameObject panel, RectTransform scrollContent, Text character, Text identity, Text stats, Text equipment, Button back)
    {
        root = panel;
        content = scrollContent;
        characterLabel = character;
        identityLabel = identity;
        statsLabel = stats;
        equipmentLabel = equipment;
        backButton = back;
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(Close);
        Refresh();
    }

    public void Open()
    {
        Game.EnsureInitialized();
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close() => root.SetActive(false);

    private List<OwnedUnit> Owned()
    {
        return Game.Save.units
            .Where(u => u != null && Game.Data.Characters.ContainsKey(u.charId))
            .OrderBy(u => Game.Data.Characters[u.charId].name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void Refresh()
    {
        Game.EnsureInitialized();
        var owned = Owned();
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, owned.Count - 1));
        BuildRoster(owned);

        if (owned.Count == 0)
        {
            characterLabel.text = "No Funguy acquired";
            identityLabel.text = "Summon your first Funguy to build your roster.";
            statsLabel.text = "";
            equipmentLabel.text = "";
            return;
        }

        ShowCharacter(owned[selectedIndex], owned);
    }

    private void BuildRoster(List<OwnedUnit> owned)
    {
        foreach (Transform child in content) Destroy(child.gameObject);
        var viewport = content.parent as RectTransform;
        float rowHeight = 104f;
        float spacing = 12f;
        float totalHeight = Mathf.Max(600f, owned.Count * (rowHeight + spacing));
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, totalHeight);

        for (int i = 0; i < owned.Count; i++)
        {
            var unit = owned[i];
            var def = Game.Data.Characters[unit.charId];
            var go = new GameObject("FunguyEntry", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(content, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -i * (rowHeight + spacing));
            rt.sizeDelta = new Vector2(-20f, rowHeight);

            var image = go.GetComponent<Image>();
            image.color = i == selectedIndex ? new Color(.76f,.58f,.16f,1f) : new Color(.10f,.18f,.20f,1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            int index = i;
            button.onClick.AddListener(() => { selectedIndex = index; Refresh(); });

            var portraitGo = new GameObject("Icon", typeof(RectTransform), typeof(FungusPortrait));
            portraitGo.transform.SetParent(go.transform, false);
            var portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.anchorMin = new Vector2(0f,.5f); portraitRt.anchorMax = new Vector2(0f,.5f); portraitRt.pivot = new Vector2(.5f,.5f);
            portraitRt.anchoredPosition = new Vector2(58f,0f); portraitRt.sizeDelta = new Vector2(76f,76f);
            var portrait = portraitGo.GetComponent<FungusPortrait>();
            portrait.variant = def.classArchetype == "Tank" ? 0 : (def.classArchetype == "Support" || def.classArchetype == "Mage" || def.role == "Healer" ? 1 : 2);
            portrait.cap = BiomeColor(def.biome);
            portrait.SetVerticesDirty();

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = $"{def.name}\n{def.rarityTier} • Lv.{unit.level} • {def.classArchetype}\n{def.biome} • {def.role}";
            label.fontSize = 22; label.color = Color.white; label.alignment = TextAnchor.MiddleLeft; label.supportRichText = true; label.raycastTarget = false;
            var labelRt = labelGo.GetComponent<RectTransform>(); labelRt.anchorMin = new Vector2(0f,.5f); labelRt.anchorMax = new Vector2(1f,.5f); labelRt.offsetMin = new Vector2(110f,-42f); labelRt.offsetMax = new Vector2(-14f,42f);
        }
    }

    private void ShowCharacter(OwnedUnit unit, List<OwnedUnit> owned)
    {
        var def = Game.Data.Characters[unit.charId];
        characterLabel.text = def.name;
        identityLabel.text = $"{def.classArchetype} • {def.role}\n{def.biome} • {def.rarityTier} • Lv.{unit.level}";
        statsLabel.text = BuildStats(def, unit);
        equipmentLabel.text = BuildEquipment(unit);
    }

    private static string BuildStats(CharacterDef def, OwnedUnit unit)
    {
        int level = Math.Max(1, unit.level);
        int hp = def.baseStats.hp + Mathf.RoundToInt(def.growth.hp * (level - 1));
        int atk = def.baseStats.atk + Mathf.RoundToInt(def.growth.atk * (level - 1));
        int defense = def.baseStats.def + Mathf.RoundToInt(def.growth.def * (level - 1));
        int spd = def.baseStats.spd + Mathf.RoundToInt(def.growth.spd * (level - 1));
        int pot = def.baseStats.pot + Mathf.RoundToInt(def.growth.pot * (level - 1));
        foreach (var item in unit.gearSlots ?? new List<GearSlotState>())
        {
            float scale = (1f + .1f * (Mathf.Max(1,item.level)-1)) * (1f + .2f * (Mathf.Max(1,item.rarity)-1)) * (1f + .25f * Mathf.Max(0,item.evolution));
            switch (item.slotId)
            {
                case "cap": hp += Mathf.RoundToInt(250*scale); pot += Mathf.RoundToInt(20*scale); break;
                case "stipe": hp += Mathf.RoundToInt(300*scale); defense += Mathf.RoundToInt(35*scale); break;
                case "mycelium": spd += Mathf.RoundToInt(25*scale); break;
                case "symbiote":
                    int amount = Mathf.RoundToInt(item.rolledStatAmount * scale);
                    switch (item.rolledStat) { case "HP": hp += amount; break; case "ATK": atk += amount; break; case "DEF": defense += amount; break; case "SPD": spd += amount; break; case "POT": pot += amount; break; }
                    break;
            }
        }
        return $"HP   {hp}\nATK  {atk}\nDEF  {defense}\nSPD  {spd}\nPOT  {pot}";
    }

    private string BuildEquipment(OwnedUnit unit)
    {
        var gear = unit.gearSlots ?? new List<GearSlotState>();
        if (gear.Count == 0) return "No equipment equipped.";
        return string.Join("\n\n", EquipmentService.Slots.Select(slot =>
        {
            var item = gear.FirstOrDefault(x => x.slotId == slot);
            if (item == null) return $"{slot.ToUpperInvariant()}\nEmpty";
            var name = Game.Equipment.Definition(slot)?.name ?? slot;
            return $"{slot.ToUpperInvariant()}\n{name} • Rarity {item.rarity} • Lv.{item.level}" + (item.evolution > 0 ? " • Evolved" : "");
        }));
    }

    private static Color BiomeColor(string biome) => biome switch
    {
        "Forest" => new Color(.38f,.68f,.26f), "Wetlands" => new Color(.28f,.68f,.65f),
        "Decay" => new Color(.62f,.38f,.73f), "Tundra" => new Color(.62f,.82f,.95f),
        _ => new Color(.88f,.54f,.25f)
    };
}
