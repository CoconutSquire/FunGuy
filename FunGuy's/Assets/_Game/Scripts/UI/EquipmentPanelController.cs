using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentPanelController : MonoBehaviour
{
    private GameObject root;
    private Text characterLabel;
    private Text identityLabel;
    private Text statsLabel;
    private Text detailsLabel;
    private RectTransform characterContent;
    private Button[] characterButtons;
    private string selectedCharacterId;
    private Button[] slotButtons;
    private Text[] slotLabels;
    private Button upgradeButton;
    private Text upgradeLabel;
    private Button backButton;
    private Button[] choiceButtons;
    private Text[] choiceLabels;
    private int selected;
    private int selectedSlot = 0;

    public void Initialize(GameObject panel, Text character, Text identity, Text stats, Text details, RectTransform characterList, Button[] choices, Text[] choiceTexts, Button[] slots, Text[] labels, Button upgrade, Text upgradeText, Button back)
    {
        root=panel; characterLabel=character; identityLabel=identity; statsLabel=stats; detailsLabel=details; characterContent=characterList; choiceButtons=choices; choiceLabels=choiceTexts; slotButtons=slots; slotLabels=labels; upgradeButton=upgrade; upgradeLabel=upgradeText; backButton=back;
        Refresh();
    }
    public void Open() { root.SetActive(true); Refresh(); }
    public void Close() { root.SetActive(false); }
    public void BackToFormation() { Close(); }
    public void SelectCharacter(int index) { var owned=Owned(); if(index<0 || index>=owned.Count) return; selected=index; selectedCharacterId=owned[index].charId; Refresh(); }

    private System.Collections.Generic.List<OwnedUnit> Owned() => Game.Save.units.Where(u=>u!=null && Game.Data.Characters.ContainsKey(u.charId)).OrderBy(u=>Game.Data.Characters[u.charId].name,StringComparer.OrdinalIgnoreCase).ToList();

    public void EquipFromSlot(int slotIndex)
    {
        string charId=selectedCharacterId;
        if (string.IsNullOrEmpty(charId)) return;
        selectedSlot=Mathf.Clamp(slotIndex,0,EquipmentService.Slots.Length-1);
        string slot=EquipmentService.Slots[selectedSlot];
        var unit=Game.Save.units.First(u=>u.charId==charId);
        var equipped=unit.gearSlots?.FirstOrDefault(x=>x.slotId==slot);
        if (equipped != null) Game.Equipment.Unequip(charId,slot);
        else { var item=Game.Save.equipmentInventory.FirstOrDefault(x=>x.slotId==slot); if (item != null) Game.Equipment.Equip(charId,item.itemId); }
        Refresh();
    }

    public void UpgradeSelectedEquipment()
    {
        var charId=selectedCharacterId;
        if (string.IsNullOrEmpty(charId)) return;
        var unit=Game.Save.units.First(u=>u.charId==charId);
        var item=unit.gearSlots?.FirstOrDefault(x=>x.slotId==EquipmentService.Slots[selectedSlot]);
        if (item == null) { detailsLabel.text="Equip a piece first."; return; }

        bool changed = Game.Equipment.IsAtEvolutionGate(item)
            ? Game.Equipment.Evolve(item.itemId)
            : Game.Equipment.LevelUp(item.itemId);
        if (!changed) {
            detailsLabel.text = Game.Equipment.IsAtEvolutionGate(item)
                ? $"Evolution requires {Game.Equipment.EvolutionSporeCost} Spores."
                : $"Level up requires {Game.Equipment.LevelCost(item.level)} Gold.";
        }
        Refresh();
    }

    public void EquipChoice(int slotIndex) { EquipFromSlot(slotIndex); }

    public void Unequip(int slotIndex)
    {
        var charId=selectedCharacterId;
        if (string.IsNullOrEmpty(charId)) return;
        Game.Equipment.Unequip(charId, EquipmentService.Slots[Mathf.Clamp(slotIndex,0,3)]); Refresh();
    }

    private void Refresh()
    {
        Game.EnsureInitialized(); var save=Game.Save;
        var owned=Owned();
        if(characterContent==null) return;
        foreach(Transform child in characterContent) UnityEngine.Object.Destroy(child.gameObject);
        characterButtons=new Button[owned.Count];
        selectedCharacterId = string.IsNullOrEmpty(selectedCharacterId) || owned.All(u=>u.charId!=selectedCharacterId)
            ? (owned.Count>0 ? owned[0].charId : null) : selectedCharacterId;
        selected=owned.FindIndex(u=>u.charId==selectedCharacterId);
        for(int i=0;i<owned.Count;i++){
            var entry=owned[i]; var rosterDef=Game.Data.Characters[entry.charId];
            var go=new GameObject("EquipmentCharacterEntry",typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(characterContent,false);
            var rt=go.GetComponent<RectTransform>(); rt.anchorMin=new Vector2(0,.5f); rt.anchorMax=new Vector2(0,.5f); rt.pivot=new Vector2(.5f,.5f); rt.anchoredPosition=new Vector2(i*170f,0); rt.sizeDelta=new Vector2(155,60);
            var image=go.GetComponent<Image>(); image.color=entry.charId==selectedCharacterId?new Color(.76f,.58f,.16f,1f):new Color(.12f,.35f,.52f,1f);
            var button=go.GetComponent<Button>(); button.targetGraphic=image; int index=i; button.onClick.AddListener(()=>SelectCharacter(index)); characterButtons[i]=button;
            var label=new GameObject("Label",typeof(RectTransform),typeof(Text)).GetComponent<Text>(); label.transform.SetParent(go.transform,false); label.rectTransform.anchorMin=Vector2.zero; label.rectTransform.anchorMax=Vector2.one; label.rectTransform.offsetMin=Vector2.zero; label.rectTransform.offsetMax=Vector2.zero; label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=15; label.color=Color.white; label.alignment=TextAnchor.MiddleCenter; label.text=$"{rosterDef.name}\\n{rosterDef.rarityTier} • Lv.{entry.level}"; label.raycastTarget=false;
        }
        characterContent.sizeDelta=new Vector2(Mathf.Max(800f,owned.Count*170f),70f);
        if(owned.Count==0){characterLabel.text="No characters owned"; if(identityLabel!=null) identityLabel.text=""; if(statsLabel!=null) statsLabel.text=""; detailsLabel.text="Acquire a character to manage their equipment."; if(upgradeButton!=null) upgradeButton.interactable=false; return;}
        selected=Mathf.Clamp(selected,0,owned.Count-1); string charId=selectedCharacterId; var unit=save.units.First(u=>u.charId==charId);
        var def = Game.Data.Characters.TryGetValue(charId,out var characterDef) ? characterDef : null;
        characterLabel.text=def?.name ?? charId;
        if(identityLabel!=null) identityLabel.text=def==null ? "" : $"{def.classArchetype} • {def.role}\n{def.biome} • Rarity {def.rarity} • Lv.{unit.level}";
        if(statsLabel!=null) statsLabel.text=BuildStats(def, unit);
        if(choiceButtons!=null) for(int i=0;i<choiceButtons.Length;i++){
            string slot=EquipmentService.Slots[i]; var inv=save.equipmentInventory.FirstOrDefault(x=>x.slotId==slot);
            choiceButtons[i].interactable=inv!=null;
            if(choiceLabels!=null && i<choiceLabels.Length) choiceLabels[i].text=inv==null ? $"{slot.ToUpperInvariant()}\nNo equipment available" : $"{EquipmentName(slot)}\nRarity {inv.rarity} • Lv.{inv.level}";
        }
        for(int i=0;i<4;i++){
            string slot=EquipmentService.Slots[i]; var equipped=unit.gearSlots?.FirstOrDefault(x=>x.slotId==slot); var inv=save.equipmentInventory.FirstOrDefault(x=>x.slotId==slot);
            slotLabels[i].text=slot.ToUpperInvariant()+"\n"+(equipped==null?(inv==null?"Empty":"Equip "+EquipmentName(slot)):"Equipped "+EquipmentName(slot)+"\nLv."+equipped.level+(equipped.evolution>0?" • Evolved":""));
            slotButtons[i].interactable=equipped!=null || inv!=null;
        }
        detailsLabel.text="Tap an equipment slot to equip/unequip it.\n"+Describe(unit);
        var selectedItem=unit.gearSlots?.FirstOrDefault(x=>x.slotId==EquipmentService.Slots[selectedSlot]);
        if(upgradeButton!=null) {
            upgradeButton.interactable=selectedItem!=null;
            if(selectedItem==null) upgradeLabel.text="Equip Equipment";
            else if(selectedItem.evolution==0 && selectedItem.level>=EquipmentService.EvolutionLevel) upgradeLabel.text=$"EVOLVE • {Game.Equipment.EvolutionSporeCost} Spores";
            else if(selectedItem.level>=EquipmentService.MaxLevel) { upgradeButton.interactable=false; upgradeLabel.text="MAX LEVEL • 20"; }
            else upgradeLabel.text=$"LEVEL UP • {Game.Equipment.LevelCost(selectedItem.level)} Gold";
        }
    }
    private string BuildStats(CharacterDef def, OwnedUnit unit)
    {
        if (def == null || unit == null) return "Stats unavailable";

        // Use the same canonical stat calculator as battle/upgrade previews instead
        // of rebuilding level/evolution formulas locally. Equipment is then applied
        // through EquipmentService so this screen cannot drift from combat stats.
        var fighter = CombatUnitFactory.Create(def, Math.Max(1, unit.level), TeamSide.Player,
            Math.Max(1, unit.stars), Game.Data.StatRules);
        EquipmentService.ApplyTo(fighter, unit);

        return $"HP\n{fighter.maxHp}\n\nATK\n{fighter.atk}\n\nDEF\n{fighter.def}\n\nSPD\n{fighter.spd}\n\nPOT\n{fighter.pot}";
    }

    private string EquipmentName(string slot)=>Game.Equipment.Definition(slot)?.name ?? slot;
    private string Describe(OwnedUnit unit){
        var s=unit.gearSlots??new System.Collections.Generic.List<GearSlotState>();
        if(s.Count==0) return "Equipped: None";
        return "Equipped: "+string.Join(", ",s.Select(x=>$"{EquipmentName(x.slotId)} Lv.{x.level} {(x.evolution>0?"Evolved":"")}"));
    }
}
