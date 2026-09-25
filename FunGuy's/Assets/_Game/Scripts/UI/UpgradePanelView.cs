using System;
using UnityEngine;
using UnityEngine.UI;

// Explicit view factory for the existing menu adapter. All positions are in the landscape safe surface.
public static class UpgradePanelView
{
    public static UpgradePanelController Create(Transform parent, Action onClosed)
    {
        var gold = new Color(1, .82f, .42f);
        var foreground = new Color(.96f, .93f, .83f);
        var background = new Color(.08f, .12f, .10f, 1);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform Rect(string name, Transform owner, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(owner, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new(.5f, .5f);
            rect.anchoredPosition = new(x, y); rect.sizeDelta = new(w, h); return rect;
        }
        Image Panel(string name, Transform owner, float x, float y, float w, float h, Color color)
        { var image = Rect(name, owner, x, y, w, h).gameObject.AddComponent<Image>(); image.color = color; return image; }
        Text Label(string name, Transform owner, string value, float x, float y, float w, float h, int size, Color color)
        {
            var label = Rect(name, owner, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = font; label.fontSize = size; label.color = color; label.text = value;
            label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; return label;
        }
        Button Button(string name, Transform owner, string text, float x, float y, float w, float h, int size = 24, Color? color = null)
        {
            var buttonColor = color ?? new Color(.22f, .32f, .23f);
            var image = Panel(name, owner, x, y, w, h, buttonColor);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = Color.Lerp(buttonColor, Color.white, .18f);
            colors.pressedColor = Color.Lerp(buttonColor, Color.black, .16f);
            colors.selectedColor = Color.Lerp(buttonColor, Color.white, .08f);
            colors.disabledColor = Color.Lerp(buttonColor, Color.black, .35f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            var label = Label(name + "Label", image.transform, text, 0, 0, w - 24, h - 8, size, Color.white); label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        var rosterColor = new Color(.10f, .25f, .34f, 1f);
        var navigationColor = new Color(.12f, .35f, .52f, 1f);
        var upgradeColor = new Color(.12f, .52f, .25f, 1f);
        var equipmentColor = new Color(.10f, .35f, .68f, 1f);
        var backColor = new Color(.28f, .34f, .39f, 1f);

        var overlay = Panel("Panel_Upgrades", parent, 0, 0, 1600, 900, new Color(0, 0, 0, .94f));
        var shell = Panel("UpgradeShell", overlay.transform, 0, 0, 1480, 800, background);
        Label("UpgradeTitle", shell.transform, "GROW YOUR FIGHTERS", -360, 337, 650, 60, 34, gold);
        var wallet = Label("UpgradeGold", shell.transform, "", 430, 337, 370, 60, 30, gold);
        var roster = new Button[5];
        for (int i = 0; i < roster.Length; i++) roster[i] = Button("Btn_UpgradeFighter" + (i + 1), shell.transform, "", -465, 230 - i * 102, 465, 88, 23, rosterColor);
        var previous = Button("Btn_UpgradePrevious", shell.transform, "Previous", -590, -310, 205, 64, 22, navigationColor);
        var next = Button("Btn_UpgradeNext", shell.transform, "Next", -350, -310, 205, 64, 22, navigationColor);
        var page = Label("UpgradePage", shell.transform, "", -470, -369, 465, 45, 21, foreground); page.alignment = TextAnchor.MiddleCenter;
        var identity = Label("UpgradeIdentity", shell.transform, "", 155, 222, 730, 135, 25, foreground);
        var portrait = Rect("UpgradePortrait", shell.transform, 635, 222, 104, 116).gameObject.AddComponent<FungusPortrait>(); portrait.raycastTarget = false;
        var stats = Label("UpgradeStats", shell.transform, "", 25, -16, 410, 300, 25, foreground);
        var viewport = Panel("KitViewport", shell.transform, 450, -25, 430, 328, new Color(.04f, .07f, .05f, .7f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        var skills = Label("UpgradeSkills", viewport.transform, "", 0, 0, 398, 800, 22, foreground);
        skills.alignment = TextAnchor.UpperLeft; skills.rectTransform.anchorMin = new(0, 1); skills.rectTransform.anchorMax = new(1, 1);
        skills.rectTransform.pivot = new(.5f, 1); skills.rectTransform.sizeDelta = new(-32, 800); skills.rectTransform.anchoredPosition = new(0, -8);
        var fit = skills.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = (RectTransform)viewport.transform; scroll.content = skills.rectTransform;
        var feedback = Label("UpgradeFeedback", shell.transform, "", 240, -248, 875, 86, 22, gold);
        var upgrade = Button("Btn_ConfirmUpgrade", shell.transform, "Level up", -200, -345, 330, 60, 22, upgradeColor);
        var equipment = Button("Btn_UpgradeEquipment", shell.transform, "Equipment", 150, -345, 300, 60, 22, equipmentColor);
        var close = Button("Btn_CloseUpgrades", shell.transform, "Back to team", 480, -345, 300, 60, 22, backColor);

        equipment.onClick.AddListener(() => {
            var equipmentController = UnityEngine.Object.FindFirstObjectByType<EquipmentPanelController>(FindObjectsInactive.Include);
            if (equipmentController != null) equipmentController.Open();
        });

        var controller = overlay.gameObject.AddComponent<UpgradePanelController>();
        controller.Configure(overlay.gameObject, wallet, identity, stats, skills, feedback, page, upgrade,
            previous, next, close, roster, portrait, onClosed);
        return controller;
    }
}
