using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Landscape adaptation for the existing menu adapter. Battle uses separately authored prefabs.
public static class LandscapeMenuLayout
{
    public static void Apply(Scene scene, Canvas canvas)
    {
        var root = canvas.transform.Find(scene.name + "Root") as RectTransform;
        if (root != null && scene.name != "Battle") {
            var content = SafeContent(canvas.transform, "MenuSafeArea"); root.SetParent(content, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            RectTransform Find(string name) => root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == name);
            void Place(string name, float x, float y, float w, float h, int font = 0) {
                var r = Find(name); if (r == null) return;
                r.anchorMin = r.anchorMax = r.pivot = new(.5f, .5f); r.anchoredPosition = new(x, y); r.sizeDelta = new(w, h);
                if (font > 0) foreach (var text in r.GetComponentsInChildren<Text>(true)) text.fontSize = font;
            }
            Place("Panel_Main", 0, -10, 1500, 800);
            if (scene.name == "Home") {
                Place("Lbl_Welcome", -320, 205, 700, 160, 36); Place("Lbl_AccountStats", -320, 65, 680, 90, 28);
                string[] buttons = { "Btn_Start", "Btn_Summon", "Btn_Team", "Btn_Battle", "Btn_Options" };
                for (int i = 0; i < buttons.Length; i++) Place(buttons[i], 460, 230 - i * 110, 380, 88);
                Place("Lbl_TutorialHint", -320, -145, 680, 160, 25);
            } else if (scene.name == "Summon") {
                Place("Lbl_Banner", -330, 235, 690, 120, 33); Place("Lbl_Spores", -330, 135, 650, 80, 27);
                Place("Btn_PullOne", -330, 20, 440, 86); Place("Btn_PullTen", -330, -85, 440, 86); Place("Btn_Back", -330, -190, 440, 86);
                Place("Lbl_Result", 370, 0, 620, 560, 25); Place("Lbl_TutorialHint", -330, -310, 670, 80, 22);
                Place("Panel_RevealCard", 0, 0, 1060, 660);
                Place("Lbl_RevealTitle", 180, 230, 520, 90, 33); Place("Img_RarityGlow", -290, 15, 340, 340); Place("Img_RarityFrame", -290, 15, 280, 280);
                Place("Lbl_RevealName", 180, 60, 520, 120, 32); Place("Lbl_RevealRarity", 180, -40, 500, 60); Place("Btn_RevealNext", 180, -175, 340, 80);
            } else if (scene.name == "Team") {
                Place("Panel_Main", 0, 0, 1540, 860); Place("Lbl_TeamStatus", -350, 340, 820, 65, 26);
                for (int slot = 0; slot < 12; slot++) {
                    int depth = FormationRules.Depth(slot);
                    Place("Btn_FormationSlot" + (slot + 1), -140 - depth * 210, 185 - FormationRules.Lane(slot) * 122 + (depth % 2) * 61, 270, 115, 18);
                }
                Place("Lbl_Hint", -350, -305, 790, 80, 24);
                Place("Panel_RosterSlots", 470, 130, 570, 365);
                for (int i = 0; i < 5; i++) Place("Btn_RosterSlot" + (i + 1), -135 + (i % 2) * 270, 115 - (i / 2) * 116, 248, 102, 20);
                Place("Btn_RosterPrevious", 280, -110, 165, 58, 23); Place("Lbl_RosterPage", 470, -110, 200, 58, 19); Place("Btn_RosterNext", 660, -110, 165, 58, 23);
                Place("Lbl_Roster", 470, -217, 555, 125, 22);
                Place("Btn_AutoFill", 275, -325, 180, 68, 23); Place("Btn_ClearTeam", 470, -325, 180, 68, 23); Place("Btn_StartBattle", 665, -325, 180, 68, 23);
                Place("Btn_Back", -650, -390, 180, 55, 23); Place("Btn_RemoveSelected", -300, -390, 440, 55, 23);
                Place("Btn_Upgrades", 470, -390, 565, 55, 23);
            } else if (scene.name == "Options") {
                Place("Lbl_OptionsTitle", -350, 300, 680, 80, 34); Place("Lbl_DebugStatus", -350, 222, 680, 75, 22);
                string[] buttons = { "Btn_ResetSave", "Btn_GrantStarterResources", "Btn_SeedStarterRoster", "Btn_SkipTutorial", "Btn_OpenSummon", "Btn_RunSmokeTests", "Btn_BackHome" };
                for (int i = 0; i < buttons.Length; i++) Place(buttons[i], -525 + (i % 2) * 350, 110 - (i / 2) * 110, 325, 80, 23);
                Place("Lbl_SmokeOutput", 380, -20, 620, 680, 20);
            }
            foreach (var text in root.GetComponentsInChildren<Text>(true)) text.verticalOverflow = VerticalWrapMode.Truncate;
        }
        var overlay = canvas.GetComponentsInChildren<TutorialOverlay>(true).FirstOrDefault();
        if (overlay != null) {
            var content = SafeContent(canvas.transform, "TutorialSafeArea"); var r = (RectTransform)overlay.transform; r.SetParent(content, false);
            r.anchorMin = r.anchorMax = r.pivot = new(.5f, .5f); r.anchoredPosition = new(0, 370); r.sizeDelta = new(1100, 130);
            var text = overlay.GetComponentInChildren<Text>(true); text.fontSize = 23;
            text.rectTransform.anchoredPosition = new(-140, 0); text.rectTransform.sizeDelta = new(760, 110);
            var button = overlay.GetComponentInChildren<Button>(true); ((RectTransform)button.transform).anchoredPosition = new(415, 0); ((RectTransform)button.transform).sizeDelta = new(230, 70);
        }
    }
    private static RectTransform SafeContent(Transform parent, string name)
    {
        var existing = parent.Find(name); if (existing != null) return (RectTransform)existing.GetChild(0);
        var safe = new GameObject(name, typeof(RectTransform), typeof(LandscapeSafeArea)); safe.transform.SetParent(parent, false);
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(safe.transform, false);
        safe.GetComponent<LandscapeSafeArea>().content = content; safe.GetComponent<LandscapeSafeArea>().Refresh(); return content;
    }
}
