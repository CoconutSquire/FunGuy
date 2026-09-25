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
    private Button[] characterButtons;
    private Button[] slotButtons;
    private Text[] slotLabels;
    private Button upgradeButton;
    private Text upgradeLabel;
    private Button backButton;
    private Button[] choiceButtons;
    private Text[] choiceLabels;
    private int selected;
    private int selectedSlot = 0;

    public void Initialize(GameObject panel, Text character, Text identity, Text stats, Text details, Button[] chars, Button[] choices, Text[] choiceTexts, Button[] slots, Text[] labels, Button upgrade, Text upgradeText, Button back)
    {
        root=panel; characterLabel=character; identityLabel=identity; statsLabel=stats; detailsLabel=details; characterButtons=chars; choiceButtons=choices; choiceLabels=choiceTexts; slotButtons=slots; slotLabels=labels; upgradeButton=upgrade; upgradeLabel=upgradeText; backButton=back;
        Refresh();
    }
    public void Open() { root.SetActive(true); Refresh(); }
    public void Close() { root.SetActive(false); }
    public void BackToFormation() { Close(); }
    public void SelectCharacter(int index) { selected=Mathf.Clamp(index,0,characterButtons.Length-1); Refresh(); }

    public void EquipFromSlot(int slotIndex)
    {
        if (selected < 0 || selected >= Game.Save.activeTeam.Count) return;
        string charId=Game.Save.activeTeam[selected];
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
        if (selected < 0 || selected >= Game.Save.activeTeam.Count) return;
        var charId=Game.Save.activeTeam[selected];
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
        if (selected < 0 || selected >= Game.Save.activeTeam.Count) return;
        Game.Equipment.Unequip(Game.Save.activeTeam[selected], EquipmentService.Slots[Mathf.Clamp(slotIndex,0,3)]); Refresh();
    }

    private void Refresh()
    {
        Game.EnsureInitialized(); var save=Game.Save;
        if (characterButtons==null) return;
        for(int i=0;i<characterButtons.Length;i++){
            bool exists=i<save.activeTeam.Count; characterButtons[i].interactable=exists;
            if(exists){ var id=save.activeTeam[i]; characterButtons[i].GetComponentInChildren<Text>().text=Game.Data.Characters.TryGetValue(id,out var d)?d.name:id; }
        }
        if(save.activeTeam.Count==0){characterLabel.text="No characters on the team"; if(identityLabel!=null) identityLabel.text=""; if(statsLabel!=null) statsLabel.text=""; detailsLabel.text="Add a character to the team first."; if(upgradeButton!=null) upgradeButton.interactable=false; return;}
        selected=Mathf.Clamp(selected,0,save.activeTeam.Count-1); string charId=save.activeTeam[selected]; var unit=save.units.First(u=>u.charId==charId);
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
        if(def?.baseStats==null) return "Stats unavailable";
        int level = Math.Max(1, unit.level);
        int hp=def.baseStats.hp + Mathf.RoundToInt(def.growth.hp * (level-1));
        int atk=def.baseStats.atk + Mathf.RoundToInt(def.growth.atk * (level-1));
        int defense=def.baseStats.def + Mathf.RoundToInt(def.growth.def * (level-1));
        int spd=def.baseStats.spd + Mathf.RoundToInt(def.growth.spd * (level-1));
        int pot=def.baseStats.pot + Mathf.RoundToInt(def.growth.pot * (level-1));
        foreach(var item in unit.gearSlots ?? new System.Collections.Generic.List<GearSlotState>())
        {
            float scale=(1f+0.1f*(Mathf.Max(1,item.level)-1))*(1f+0.2f*(Mathf.Max(1,item.rarity)-1))*(1f+0.25f*Mathf.Max(0,item.evolution));
            switch(item.slotId){
                case "cap": hp+=Mathf.RoundToInt(250*scale); pot+=Mathf.RoundToInt(20*scale); break;
                case "stipe": hp+=Mathf.RoundToInt(300*scale); defense+=Mathf.RoundToInt(35*scale); break;
                case "mycelium": spd+=Mathf.RoundToInt(25*scale); break;
                case "symbiote": var amount=Mathf.RoundToInt(item.rolledStatAmount*scale); switch(item.rolledStat){case "HP":hp+=amount;break;case "ATK":atk+=amount;break;case "DEF":defense+=amount;break;case "SPD":spd+=amount;break;case "POT":pot+=amount;break;} break;
            }
        }
        return $"HP\n{hp}\n\nATK\n{atk}\n\nDEF\n{defense}\n\nSPD\n{spd}\n\nPOT\n{pot}";
    }

    private string EquipmentName(string slot)=>Game.Equipment.Definition(slot)?.name ?? slot;
    private string Describe(OwnedUnit unit){
        var s=unit.gearSlots??new System.Collections.Generic.List<GearSlotState>();
        if(s.Count==0) return "Equipped: None";
        return "Equipped: "+string.Join(", ",s.Select(x=>$"{EquipmentName(x.slotId)} Lv.{x.level} {(x.evolution>0?"Evolved":"")}"));
    }
}
