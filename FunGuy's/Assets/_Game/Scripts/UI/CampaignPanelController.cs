using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Displays catalog content and application-owned unlocks. Selecting an encounter never claims its rewards.
public sealed class CampaignPanelController : MonoBehaviour
{
    private Text details;
    private Button enter;
    private string selected;
    private readonly Dictionary<string, Button> buttons = new();
    public string SelectedStageId => selected;

    public static CampaignPanelController Create(Transform parent)
    {
        var root = new GameObject("CampaignPanel", typeof(RectTransform), typeof(Image)); root.transform.SetParent(parent, false);
        var r = (RectTransform)root.transform; r.anchorMin = r.anchorMax = r.pivot = new(.5f, .5f); r.sizeDelta = new(1600, 900);
        root.GetComponent<Image>().color = new Color(.06f, .10f, .08f, 1);
        var view = root.AddComponent<CampaignPanelController>();
        Text Label(string name, string value, float x, float y, float w, float h, int size)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>(); text.transform.SetParent(root.transform, false);
            text.rectTransform.anchoredPosition = new(x, y); text.rectTransform.sizeDelta = new(w, h);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.color = new(1, .91f, .70f);
            text.alignment = TextAnchor.MiddleLeft; text.text = value; text.raycastTarget = false; return text;
        }
        Button Button(string name, string value, float x, float y, float w, float h)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(root.transform, false);
            var rect = (RectTransform)obj.transform; rect.anchoredPosition = new(x, y); rect.sizeDelta = new(w, h);
            var image = obj.GetComponent<Image>(); image.color = new(.22f, .32f, .23f);
            var button = obj.GetComponent<Button>(); button.targetGraphic = image;
            var label = Label(name + "Label", value, 0, 0, w - 35, h - 10, 25); label.transform.SetParent(obj.transform, false);
            label.alignment = TextAnchor.MiddleCenter; return button;
        }
        Label("CampaignTitle", "CHAPTER 1 · THE SIMMERING KITCHEN", 0, 368, 1440, 80, 38);
        var stages = Game.Data.Stages.Values.OrderBy(s => s.order).ToArray();
        for (int n = 0; n < stages.Length; n++)
        {
            string id = stages[n].id;
            var button = Button("Btn_CampaignStage" + (n + 1), "", -400, 257 - n * 84, 670, 72);
            button.onClick.AddListener(() => { view.selected = id; view.Refresh(); }); view.buttons.Add(id, button);
        }
        view.details = Label("CampaignDetails", "", 360, 65, 660, 490, 27);
        view.enter = Button("Btn_CampaignEnter", "Prepare battle", 350, -260, 660, 84);
        view.enter.onClick.AddListener(view.Enter);
        var close = Button("Btn_CloseCampaign", "Back", 350, -365, 660, 70);
        close.onClick.AddListener(() => root.SetActive(false)); root.SetActive(false); return view;
    }
    public void Open()
    {
        gameObject.SetActive(true); transform.SetAsLastSibling();
        selected = Game.Campaign.IsUnlocked(Game.SelectedStageId) ? Game.SelectedStageId :
            Game.Data.Stages.Values.OrderBy(s => s.order).FirstOrDefault(s => Game.Campaign.IsUnlocked(s.id) && !Game.Save.clearedStages.Contains(s.id))?.id ??
            Game.Data.Stages.Values.OrderBy(s => s.order).Last().id;
        Refresh();
    }
    private void Refresh()
    {
        foreach (var pair in buttons)
        {
            var stage = Game.Data.Stages[pair.Key]; bool unlocked = Game.Campaign.IsUnlocked(stage.id);
            pair.Value.interactable = unlocked;
            string state = !unlocked ? "Locked" : Game.Save.clearedStages.Contains(stage.id) ? "Cleared" : "New";
            pair.Value.GetComponentInChildren<Text>().text = $"{(selected == stage.id ? "> " : "")}{stage.order}. {stage.name}  ·  {state}";
        }
        var chosen = Game.Data.Stages[selected]; bool cleared = Game.Save.clearedStages.Contains(selected);
        details.text = (chosen.boss ? "BOSS ENCOUNTER\n" : "") + chosen.name + "\n\n" + chosen.description + "\n\n" +
            $"{chosen.waves.Count} waves\n" + (cleared ? "Practice: first-clear rewards already claimed." :
            $"First clear: {chosen.rewards.gold} gold, {chosen.rewards.spores} spores, {chosen.rewards.accountXp} XP") +
            "\n\nUse gold in Team → Upgrade fighters.\nBring up to five fighters; empty hexes are allowed.";
        enter.interactable = Game.Campaign.IsUnlocked(selected);
    }
    private void Enter()
    {
        if (!Game.Campaign.IsUnlocked(selected)) return;
        Game.SelectedStageId = selected; SceneManager.LoadScene("Battle");
    }
}
