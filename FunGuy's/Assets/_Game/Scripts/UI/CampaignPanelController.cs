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
    private readonly Dictionary<int, Button> chapterButtons = new();
    private string selected;
    private int selectedChapter;
    private readonly Dictionary<string, Button> buttons = new();

    public string SelectedStageId => selected;

    public static CampaignPanelController Create(Transform parent)
    {
        var root = new GameObject("CampaignPanel", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        var r = (RectTransform)root.transform;
        r.anchorMin = r.anchorMax = r.pivot = new(.5f, .5f);
        r.sizeDelta = new(1600, 900);
        root.GetComponent<Image>().color = new Color(.06f, .10f, .08f, 1);

        var view = root.AddComponent<CampaignPanelController>();

        Text Label(string name, string value, float x, float y, float w, float h, int size)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(root.transform, false);
            text.rectTransform.anchoredPosition = new(x, y);
            text.rectTransform.sizeDelta = new(w, h);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = new(1, .91f, .70f);
            text.alignment = TextAnchor.MiddleLeft;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        Button Button(string name, string value, float x, float y, float w, float h)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(root.transform, false);
            var rect = (RectTransform)obj.transform;
            rect.anchoredPosition = new(x, y);
            rect.sizeDelta = new(w, h);

            var image = obj.GetComponent<Image>();
            image.color = new(.22f, .32f, .23f);

            var button = obj.GetComponent<Button>();
            button.targetGraphic = image;

            var label = Label(name + "Label", value, 0, 0, w - 35, h - 10, 22);
            label.transform.SetParent(obj.transform, false);
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        Label("CampaignTitle", "CAMPAIGN", 0, 368, 1440, 65, 38);
        Label("ChapterLabel", "CHAPTERS", -660, 300, 260, 45, 24);
        Label("StageLabel", "STAGES", -660, 250, 650, 40, 24);
        Label("StageHint", "Select a chapter above, then choose one of its stages.", -660, 218, 650, 35, 18);

        var stages = Game.Data.Stages.Values
            .OrderBy(s => s.chapter)
            .ThenBy(s => s.order)
            .ToArray();

        var chapters = stages
            .GroupBy(s => s.chapter)
            .OrderBy(g => g.Key)
            .ToArray();

        const float chapterStartX = -470f;
        const float chapterWidth = 300f;
        const float chapterGap = 12f;
        for (int i = 0; i < chapters.Length; i++)
        {
            var chapter = chapters[i];
            int chapterNumber = chapter.Key;
            var chapterButton = Button(
                "Btn_CampaignChapter" + chapterNumber,
                chapter.First().chapterName ?? $"Chapter {chapterNumber}",
                chapterStartX + i * (chapterWidth + chapterGap),
                300,
                chapterWidth,
                58);
            chapterButton.onClick.AddListener(() => view.SelectChapter(chapterNumber));
            view.chapterButtons.Add(chapterNumber, chapterButton);
        }

        for (int n = 0; n < stages.Length; n++)
        {
            string id = stages[n].id;
            var button = Button("Btn_CampaignStage" + (n + 1), "", -400, 0, 125, 58);
            button.onClick.AddListener(() =>
            {
                view.selected = id;
                view.selectedChapter = Game.Data.Stages[id].chapter;
                view.Refresh();
            });
            view.buttons.Add(id, button);
        }

        view.details = Label("CampaignDetails", "", 400, 70, 620, 475, 25);
        view.details.alignment = TextAnchor.UpperLeft;

        view.enter = Button("Btn_CampaignEnter", "Prepare battle", 400, -265, 620, 78);
        view.enter.onClick.AddListener(view.Enter);

        var close = Button("Btn_CloseCampaign", "Back", 400, -360, 620, 65);
        close.onClick.AddListener(() => root.SetActive(false));

        root.SetActive(false);
        return view;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        var ordered = Game.Data.Stages.Values.OrderBy(s => s.chapter).ThenBy(s => s.order).ToArray();
        if (ordered.Length == 0) return;

        var preferred = Game.Data.Stages.ContainsKey(Game.SelectedStageId) &&
                        Game.Campaign.IsUnlocked(Game.SelectedStageId)
            ? Game.SelectedStageId
            : ordered.FirstOrDefault(s =>
                Game.Campaign.IsUnlocked(s.id) &&
                !Game.Save.clearedStages.Contains(s.id))?.id
              ?? ordered.Last().id;

        selected = preferred;
        selectedChapter = Game.Data.Stages[selected].chapter;
        Refresh();
    }

    private void SelectChapter(int chapterNumber)
    {
        var chapterStages = Game.Data.Stages.Values
            .Where(s => s.chapter == chapterNumber)
            .OrderBy(s => s.order)
            .ToArray();

        if (chapterStages.Length == 0) return;

        selectedChapter = chapterNumber;
        selected = chapterStages
            .FirstOrDefault(s => Game.Campaign.IsUnlocked(s.id) &&
                                 !Game.Save.clearedStages.Contains(s.id))?.id
            ?? chapterStages.First().id;
        Refresh();
    }

    private void Refresh()
    {
        if (!Game.Data.Stages.ContainsKey(selected)) return;

        var chapterStages = Game.Data.Stages.Values
            .Where(s => s.chapter == selectedChapter)
            .OrderBy(s => s.order)
            .ToArray();

        const int columns = 5;
        const float startX = -660f;
        const float startY = 175f;
        const float cellWidth = 128f;
        const float cellHeight = 62f;
        const float gapX = 10f;
        const float gapY = 8f;

        foreach (var chapterPair in chapterButtons)
        {
            bool active = chapterPair.Key == selectedChapter;
            chapterPair.Value.interactable = true;
            chapterPair.Value.GetComponent<Image>().color = active
                ? new(.32f, .48f, .30f)
                : new(.22f, .32f, .23f);
        }

        var visibleIds = new HashSet<string>(chapterStages.Select(s => s.id));
        foreach (var pair in buttons)
        {
            var stage = Game.Data.Stages[pair.Key];
            bool inChapter = visibleIds.Contains(stage.id);
            pair.Value.gameObject.SetActive(inChapter);

            if (!inChapter) continue;

            int index = System.Array.IndexOf(chapterStages, stage);
            int column = index % columns;
            int row = index / columns;

            var rect = (RectTransform)pair.Value.transform;
            rect.anchoredPosition = new(
                startX + column * (cellWidth + gapX),
                startY - row * (cellHeight + gapY));
            rect.sizeDelta = new(cellWidth, cellHeight);

            bool unlocked = Game.Campaign.IsUnlocked(stage.id);
            pair.Value.interactable = unlocked;

            string state = !unlocked
                ? "Locked"
                : Game.Save.clearedStages.Contains(stage.id)
                    ? "Cleared"
                    : "New";

            pair.Value.GetComponentInChildren<Text>().text =
                $"{(selected == stage.id ? "▶ " : "")}Stage {stage.order}\n{state}";
        }

        var chosen = Game.Data.Stages[selected];
        bool cleared = Game.Save.clearedStages.Contains(selected);

        details.text =
            $"Chapter {chosen.chapter}: {chosen.chapterName}\n\n" +
            $"Stage {chosen.order} — {chosen.name}\n\n" +
            (chosen.boss ? "BOSS ENCOUNTER\n\n" : "") +
            chosen.description + "\n\n" +
            $"Recommended Power: {chosen.recommendedPower}\n" +
            $"{chosen.waves.Count} waves\n\n" +
            (cleared
                ? "Practice: first-clear rewards already claimed."
                : $"First clear: {chosen.rewards.gold} gold, {chosen.rewards.spores} spores, {chosen.rewards.accountXp} XP") +
            "\n\nUse gold in Team → Upgrade fighters.\nBring up to five fighters; empty hexes are allowed.";

        enter.interactable = Game.Campaign.IsUnlocked(selected);
    }

    private void Enter()
    {
        if (!Game.Campaign.IsUnlocked(selected)) return;
        Game.SelectedStageId = selected;
        SceneManager.LoadScene("Battle");
    }
}
