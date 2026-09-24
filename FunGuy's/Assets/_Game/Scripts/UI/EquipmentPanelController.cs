using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentPanelController : MonoBehaviour
{
    private GameObject root;
    private Text characterLabel;
    private Text detailsLabel;
    private Button[] characterButtons;
    private Button[] slotButtons;
    private Text[] slotLabels;
    private Button upgradeButton;
    private Text upgradeLabel;
    private int selected;

    public void Initialize(GameObject panel, Text character, Text details, Button[] chars, Button[] slots, Text[] labels, Button upgrade, Text upgradeText)
    {
        root=panel; characterLabel=character; detailsLabel=details; characterButtons=chars; slotButtons=slots; slotLabels=labels; upgradeButton=upgrade; upgradeLabel=upgradeText;
        Refresh();
    }
    public void Open() { root.SetActive(true); Refresh(); }
    public void Close() { root.SetActive(false); }
    public void SelectCharacter(int index) { selected=Mathf.Clamp(index,0,characterButtons.Length-1); Refresh(); }

    public void EquipFromSlot(int slotIndex)
    {
        if (selected < 0 || selected >= Game.Save.activeTeam.Count) return;
        string charId=Game.Save.activeTeam[selected];
        string slot=EquipmentService.Slots[Mathf.Clamp(slotIndex,0,EquipmentService.Slots.Length-1)];
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
        var item=unit.gearSlots?.FirstOrDefault();
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
        if(save.activeTeam.Count==0){characterLabel.text="No characters on the team"; detailsLabel.text="Add a character to the team first."; if(upgradeButton!=null) upgradeButton.interactable=false; return;}
        selected=Mathf.Clamp(selected,0,save.activeTeam.Count-1); string charId=save.activeTeam[selected]; var unit=save.units.First(u=>u.charId==charId);
        characterLabel.text=Game.Data.Characters.TryGetValue(charId,out var def)?def.name:charId;
        for(int i=0;i<4;i++){
            string slot=EquipmentService.Slots[i]; var equipped=unit.gearSlots?.FirstOrDefault(x=>x.slotId==slot); var inv=save.equipmentInventory.FirstOrDefault(x=>x.slotId==slot);
            slotLabels[i].text=slot.ToUpperInvariant()+"\n"+(equipped==null?(inv==null?"Empty":"Equip "+EquipmentName(slot)):"Equipped "+EquipmentName(slot)+"\nLv."+equipped.level+(equipped.evolution>0?" • Evolved":""));
            slotButtons[i].interactable=equipped!=null || inv!=null;
        }
        detailsLabel.text="Tap an equipment slot to equip/unequip it.\n"+Describe(unit);
        var selectedItem=unit.gearSlots?.FirstOrDefault();
        if(upgradeButton!=null) {
            upgradeButton.interactable=selectedItem!=null;
            if(selectedItem==null) upgradeLabel.text="Equip Equipment";
            else if(selectedItem.evolution==0 && selectedItem.level>=EquipmentService.EvolutionLevel) upgradeLabel.text=$"EVOLVE • {Game.Equipment.EvolutionSporeCost} Spores";
            else if(selectedItem.level>=EquipmentService.MaxLevel) { upgradeButton.interactable=false; upgradeLabel.text="MAX LEVEL • 20"; }
            else upgradeLabel.text=$"LEVEL UP • {Game.Equipment.LevelCost(selectedItem.level)} Gold";
        }
    }
    private string EquipmentName(string slot)=>Game.Equipment.Definition(slot)?.name ?? slot;
    private string Describe(OwnedUnit unit){
        var s=unit.gearSlots??new System.Collections.Generic.List<GearSlotState>();
        if(s.Count==0) return "Equipped: None";
        return "Equipped: "+string.Join(", ",s.Select(x=>$"{EquipmentName(x.slotId)} Lv.{x.level} {(x.evolution>0?"Evolved":"")}"));
    }
}
