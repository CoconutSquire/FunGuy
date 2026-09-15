using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Explicit authoring tool. Runtime instantiates the saved prefabs, never this builder.
public static class PresentationAssets
{
    private const string Folder = "Assets/_Game/Resources/Presentation";
    private static Sprite white;
    private static readonly Color Ink = new(.06f, .08f, .065f, .97f), Gold = new(.88f, .72f, .39f), Paper = new(.97f, .91f, .75f);
    [MenuItem("FunGuy's/Rebuild battle presentation prefabs")]
    public static void Rebuild()
    {
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
        string whitePath = Folder + "/ui-white.png";
        if (!File.Exists(whitePath)) {
            var texture = new Texture2D(2, 2); texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); texture.Apply();
            File.WriteAllBytes(whitePath, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(whitePath);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(whitePath); importer.textureType = TextureImporterType.Sprite; importer.mipmapEnabled = false; importer.SaveAndReimport();
        white = AssetDatabase.LoadAssetAtPath<Sprite>(whitePath);
        var fighter = BuildFighter();
        var fighterAsset = PrefabUtility.SaveAsPrefabAsset(fighter.gameObject, Folder + "/BattleFighter.prefab").GetComponent<BattleFighterView>();
        Object.DestroyImmediate(fighter.gameObject);
        var root = Rect("BattleScreen", null, Vector2.zero, new(1600, 900));
        Stretch(root); var view = root.gameObject.AddComponent<BattleScreenView>(); view.layoutVersion = 1; view.fighterPrefab = fighterAsset;
        var backdrop = Rect("Backdrop", root, Vector2.zero, Vector2.zero); Stretch(backdrop);
        view.backdrop = backdrop.gameObject.AddComponent<RawImage>(); view.backdrop.color = new(.2f, .12f, .07f); view.backdrop.raycastTarget = false;
        var shade = Panel("Shade", root, Vector2.zero, Vector2.zero, new(0, 0, 0, .2f)); Stretch(shade);
        var safe = Rect("SafeArea", root, Vector2.zero, Vector2.zero); Stretch(safe);
        var content = Rect("Content", safe, Vector2.zero, new(1600, 900));
        safe.gameObject.AddComponent<LandscapeSafeArea>().content = content;
        Panel("Header", content, new(0, 400), new(1600, 100), Ink);
        Panel("Footer", content, new(0, -360), new(1600, 180), Ink);
        Panel("HeaderRule", content, new(0, 350), new(1560, 2), Gold);
        Panel("FooterRule", content, new(0, -270), new(1560, 2), Gold);
        view.home = Button("Btn_Back", content, "Home", new(-710, 405), new(130, 52), out _);
        view.team = Button("Btn_GoTeam", content, "Team", new(-560, 405), new(130, 52), out _);
        view.stageLabel = Text("StageTitle", content, "The Simmering Kitchen", new(-80, 407), new(700, 44), 30);
        view.waveLabel = Text("Wave", content, "Deployment", new(-80, 369), new(750, 26), 17);
        view.pause = Button("Btn_Pause", content, "Pause", new(420, 405), new(120, 52), out view.pauseLabel);
        view.speed = Button("Btn_Speed", content, "1×", new(555, 405), new(120, 52), out view.speedLabel);
        view.finish = Button("Btn_Finish", content, "Finish · Auto", new(705, 405), new(160, 52), out _);
        view.playerBonuses = Text("PlayerBonuses", content, "Your formation", new(-395, 331), new(740, 28), 16);
        view.enemyBonuses = Text("EnemyBonuses", content, "Enemy formation", new(395, 331), new(740, 28), 16);
        view.playerBonuses.enableAutoSizing = view.enemyBonuses.enableAutoSizing = true;
        view.playerBonuses.fontSizeMin = view.enemyBonuses.fontSizeMin = 12;
        var board = Rect("Board", content, Vector2.zero, new(1480, 570));
        view.cellLabels = new TMP_Text[24];
        for (int side = 0; side < 2; side++) for (int slot = 0; slot < 12; slot++) {
            var cell = Rect($"Hex_{side}_{slot}", board, BattleScreenView.CellPosition((TeamSide)side, slot), new(260, 115));
            var hex = cell.gameObject.AddComponent<BattleHexGraphic>(); hex.raycastTarget = false;
            hex.color = side == 0 ? new(.18f, .25f, .12f, .62f) : new(.30f, .14f, .08f, .62f);
            var cellLabel = Text("CellLabel", cell, FormationRules.Label(slot), new(0, -42), new(150, 20), 12);
            cellLabel.color = new(.83f, .74f, .5f, .65f); view.cellLabels[side * 12 + slot] = cellLabel;
        }
        view.fighterLayer = Rect("Fighters", content, Vector2.zero, new(1480, 570));
        view.feedLabel = Text("ActionFeed", content, "Ready for battle", new(0, -248), new(1430, 30), 20);
        view.signatures = new SignatureCardView[5];
        for (int i = 0; i < 5; i++) {
            var button = Button("Btn_Signature" + (i + 1), content, "Signature", new(-625 + i * 235, -345), new(222, 118), out var label);
            label.fontSize = 17; label.enableAutoSizing = true; label.fontSizeMin = 14; label.margin = new(10, 5, 10, 10);
            var bar = Panel("EnergyTrack", button.transform, new(0, -48), new(196, 6), new(.05f, .08f, .06f));
            var fill = Panel("Energy", bar, Vector2.zero, new(196, 6), new(.53f, .81f, .43f)).GetComponent<Image>();
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0;
            view.signatures[i] = new() { button = button, label = label, energy = fill };
        }
        view.auto = Button("Btn_Auto", content, "AUTO  OFF", new(650, -326), new(215, 64), out view.autoLabel);
        view.start = Button("Btn_RunBattle", content, "Start battle", new(650, -401), new(215, 64), out view.startLabel);
        view.motion = Button("Btn_ReducedMotion", content, "Motion: full", new(-625, -429), new(215, 32), out view.motionLabel); view.motionLabel.fontSize = 15;
        Text("ControlsHint", content, "Tap a fighter to inspect · Signatures wait for energy and cooldown", new(-50, -429), new(880, 30), 16);
        view.resultPanel = Modal("Result", content, out var result);
        view.resultTitle = Text("ResultTitle", result, "VICTORY", new(0, 120), new(680, 70), 42);
        view.resultBody = Text("ResultBody", result, "Rewards saved", new(0, 10), new(680, 150), 23);
        view.retry = Button("Btn_Retry", result, "Play again", new(-225, -150), new(200, 58), out _);
        view.next = Button("Btn_NextStage", result, "Next stage", new(0, -150), new(200, 58), out _);
        view.resultTeam = Button("Btn_ResultTeam", result, "Team", new(225, -150), new(200, 58), out _);
        view.resultPanel.SetActive(false);
        view.inspectorPanel = Modal("Inspector", content, out var inspector);
        view.inspectorTitle = Text("InspectorTitle", inspector, "Fighter", new(0, 165), new(680, 50), 27);
        var viewport = Panel("Viewport", inspector, new(0, -5), new(650, 255), new(0, 0, 0, .15f));
        viewport.gameObject.AddComponent<RectMask2D>();
        view.inspectorBody = Text("InspectorBody", viewport, "Details", Vector2.zero, new(620, 400), 21);
        view.inspectorBody.alignment = TextAlignmentOptions.TopLeft;
        var body = view.inspectorBody.rectTransform; body.anchorMin = body.anchorMax = new(.5f, 1); body.pivot = new(.5f, 1); body.anchoredPosition = new(0, -10);
        view.inspectorScroll = viewport.gameObject.AddComponent<ScrollRect>(); view.inspectorScroll.viewport = viewport; view.inspectorScroll.content = body;
        view.inspectorScroll.horizontal = false; view.inspectorScroll.movementType = ScrollRect.MovementType.Clamped;
        view.inspectorClose = Button("Btn_CloseInspector", inspector, "Close", new(0, -171), new(240, 50), out _);
        view.inspectorPanel.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, Folder + "/BattleScreen.prefab"); Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }
    private static BattleFighterView BuildFighter()
    {
        var root = Rect("BattleFighter", null, Vector2.zero, new(196, 112));
        var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        root.gameObject.AddComponent<Button>().targetGraphic = hit;
        var view = root.gameObject.AddComponent<BattleFighterView>(); view.group = root.gameObject.AddComponent<CanvasGroup>();
        var portrait = Rect("Portrait", root, new(0, 0), new(58, 60)); view.portrait = portrait.gameObject.AddComponent<FungusPortrait>(); view.portrait.raycastTarget = false;
        view.nameLabel = Text("Name", root, "Funguy", new(0, -34), new(190, 18), 16);
        view.nameLabel.enableAutoSizing = true; view.nameLabel.fontSizeMin = 14;
        Panel("HealthLabelBacking", root, new(0, 48), new(170, 22), new(0, 0, 0, .65f));
        view.hpLabel = Text("Health", root, "100 / 100", new(0, 48), new(170, 22), 16);
        var track = Panel("HealthTrack", root, new(0, 36), new(152, 8), new(.07f, .07f, .04f));
        view.hpFill = Panel("HealthFill", track, Vector2.zero, new(150, 6), Color.green).GetComponent<Image>();
        view.hpFill.type = Image.Type.Filled; view.hpFill.fillMethod = Image.FillMethod.Horizontal;
        var energy = Panel("EnergyTrack", root, new(0, 29), new(152, 4), new(.07f, .07f, .04f));
        view.energyFill = Panel("EnergyFill", energy, Vector2.zero, new(150, 3), new(.25f, .66f, .96f)).GetComponent<Image>();
        view.energyFill.type = Image.Type.Filled; view.energyFill.fillMethod = Image.FillMethod.Horizontal;
        view.shieldLabel = Text("Shield", root, "", new(66, 11), new(69, 36), 12); view.shieldLabel.color = new(.53f, .86f, 1);
        view.statusLabel = Text("Statuses", root, "", new(0, -48), new(194, 18), 12);
        view.floatingLabel = Text("Floating", root, "", new(0, 115), new(240, 36), 22);
        return view;
    }
    private static GameObject Modal(string name, Transform parent, out RectTransform panel)
    {
        var mask = Panel(name, parent, Vector2.zero, new(1600, 900), new(0, 0, 0, .63f)); mask.GetComponent<Image>().raycastTarget = true;
        panel = Panel("Card", mask, Vector2.zero, new(760, 440), Ink);
        var outline = panel.gameObject.AddComponent<Outline>(); outline.effectColor = Gold; outline.effectDistance = new(2, -2);
        return mask.gameObject;
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new(.5f, .5f); r.sizeDelta = size; r.anchoredPosition = position; return r;
    }
    private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var r = Rect(name, parent, position, size); var image = r.gameObject.AddComponent<Image>(); image.sprite = white; image.color = color; image.raycastTarget = false; return r;
    }
    private static TMP_Text Text(string name, Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var r = Rect(name, parent, position, size); var label = r.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF"); label.text = text;
        label.fontSize = fontSize; label.fontSizeMax = fontSize; label.fontSizeMin = fontSize;
        label.color = Paper; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.richText = false; label.overflowMode = TextOverflowModes.Ellipsis; return label;
    }
    private static Button Button(string name, Transform parent, string text, Vector2 position, Vector2 size, out TMP_Text label)
    {
        var r = Panel(name, parent, position, size, new(.13f, .18f, .12f)); r.GetComponent<Image>().raycastTarget = true;
        var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = r.GetComponent<Image>();
        var colors = button.colors; colors.highlightedColor = new(1.2f, 1.2f, 1.1f); colors.disabledColor = new(.55f, .55f, .55f, .65f); button.colors = colors;
        var outline = r.gameObject.AddComponent<Outline>(); outline.effectColor = new(.6f, .49f, .27f, .8f); outline.effectDistance = new(1, -1);
        label = Text("Label", r, text, Vector2.zero, size - new Vector2(12, 4), 21); return button;
    }
}
