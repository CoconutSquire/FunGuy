using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class HomeScreenView
{
    private static readonly Color HomePanel = new(.08f, .12f, .10f, .82f);
    private static readonly Color Navigation = new(.12f, .35f, .52f, 1f);
    private static readonly Color Action = new(.12f, .52f, .25f, 1f);
    private static readonly Color Summon = new(.42f, .24f, .56f, 1f);
    private static readonly Color Back = new(.28f, .34f, .39f, 1f);

    public static void Build(Transform canvasRoot, HomeMenuController controller)
    {
        if (canvasRoot == null || controller == null) return;
        var existing = canvasRoot.Find("HomeRuntimeUI");
        if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

        // The authored Home scene is the source of truth for the background. Disable
        // legacy buttons so there is only one interactive Home UI.
        foreach (var button in canvasRoot.GetComponentsInChildren<Button>(true))
        {
            if (button == null) continue;
            button.gameObject.SetActive(false);
        }

        var root = new GameObject("HomeRuntimeUI", typeof(RectTransform));
        root.transform.SetParent(canvasRoot, false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero; rootRect.offsetMax = Vector2.zero;

        var shell = Panel(root.transform, "HomePanel", HomePanel, new Vector2(0,0), new Vector2(900,1520));
        Label(shell.transform, "Welcome", "Welcome, Commander. Build your squad and clear the frontier.",
            new Vector2(0,600), new Vector2(820,150), 42, FontStyle.Bold);
        var stats = Label(shell.transform, "AccountStats", "", new Vector2(0,500), new Vector2(820,100), 30, FontStyle.Normal);

        var start = Button(shell.transform, "Start", "Start", new Vector2(0,320), new Vector2(560,108), Action);
        var summon = Button(shell.transform, "Summon", "Summon", new Vector2(0,190), new Vector2(560,108), Summon);
        var funguy = Button(shell.transform, "Funguy", "Funguy", new Vector2(0,60), new Vector2(560,108), Navigation);
        var campaign = Button(shell.transform, "Campaign", "Campaign", new Vector2(0,-70), new Vector2(560,108), Action);
        var options = Button(shell.transform, "Options", "Options", new Vector2(0,-200), new Vector2(560,108), Back);
        var hint = Label(shell.transform, "Hint", "", new Vector2(0,-360), new Vector2(780,140), 24, FontStyle.Italic);

        start.onClick.AddListener(controller.OnStartPressed);
        summon.onClick.AddListener(controller.OnSummonPressed);
        funguy.onClick.AddListener(controller.OnFunguyPressed);
        options.onClick.AddListener(controller.OnOptionsPressed);

        var campaignPanel = CampaignPanelController.Create(root.transform);
        controller.OpenCampaign = campaignPanel.Open;
        campaign.onClick.AddListener(campaignPanel.Open);

        controller.BindHomeView(stats, hint);
        BuildFunguyOverlay(root.transform, controller);
    }

    private static void BuildFunguyOverlay(Transform parent, HomeMenuController controller)
    {
        var root = new GameObject("FunguyRosterRoot", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;

        var overlay = Panel(root.transform, "Overlay", new Color(.035f, .055f, .065f, .97f), Vector2.zero, new Vector2(1600, 900));
        Label(overlay.transform, "Title", "FUNGUY", new Vector2(0, 430), new Vector2(1600, 90), 44, FontStyle.Bold);

        var listPanel = Panel(overlay.transform, "RosterPanel", new Color(.07f, .11f, .13f, 1), new Vector2(-560, -10), new Vector2(520, 820));
        Label(listPanel.transform, "Header", "CHARACTERS", new Vector2(0, 370), new Vector2(460, 60), 26, FontStyle.Bold);

        // 1. Setup ScrollView Root
        var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(listPanel.transform, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = scrollRt.anchorMax = scrollRt.pivot = new Vector2(.5f, .5f);
        scrollRt.anchoredPosition = new Vector2(0, -20);
        scrollRt.sizeDelta = new Vector2(480, 700);
        scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, .12f);

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35f; // Ensures mouse wheel scrolling actually works

        // 2. Explicitly Create Viewport (Replaced Mask + Image with RectMask2D)
        var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollGo.transform, false);
        var vp = viewportObj.GetComponent<RectTransform>();
        vp.anchorMin = Vector2.zero; vp.anchorMax = Vector2.one;
        vp.offsetMin = vp.offsetMax = Vector2.zero;

        // 3. Explicitly Create Content
        var contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(viewportObj.transform, false);
        var cr = contentObj.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1);
        cr.pivot = new Vector2(.5f, 1); cr.anchoredPosition = Vector2.zero;
        cr.sizeDelta = new Vector2(0, 600); // Note: See scroll lock warning below

        scroll.viewport = vp;
        scroll.content = cr;

        // 4. Setup Details Panel
        var detail = Panel(overlay.transform, "DetailPanel", new Color(.07f, .11f, .13f, 1), new Vector2(430, -10), new Vector2(1050, 820));
        var name = Label(detail.transform, "CharacterName", "Select a Funguy", new Vector2(0, 350), new Vector2(900, 80), 42, FontStyle.Bold);
        var identity = Label(detail.transform, "Identity", "", new Vector2(0, 285), new Vector2(900, 90), 24, FontStyle.Normal);
        var stats = Label(detail.transform, "Stats", "", new Vector2(-210, 40), new Vector2(430, 360), 24, FontStyle.Normal);
        var gear = Label(detail.transform, "Equipment", "", new Vector2(230, 40), new Vector2(430, 360), 22, FontStyle.Normal);

        // Assumes 'Back' is a valid static method in this class scope
        var back = Button(detail.transform, "Back", "Back", new Vector2(0, -350), new Vector2(460, 80), Back);

        var roster = root.AddComponent<FunguyRosterController>();
        roster.Initialize(root, cr, name, identity, stats, gear, back);
        root.SetActive(false);
        controller.BindFunguyRoster(roster);
    }

    private static GameObject Child(Transform parent,string name)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); return go;
    }

    private static GameObject Panel(Transform parent,string name,Color color,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f); rt.anchoredPosition=pos; rt.sizeDelta=size;
        go.GetComponent<Image>().color=color; go.GetComponent<Image>().raycastTarget=true; return go;
    }

    private static Text Label(Transform parent,string name,string value,Vector2 pos,Vector2 size,int fontSize,FontStyle style)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f); rt.anchoredPosition=pos; rt.sizeDelta=size;
        var t=go.GetComponent<Text>(); t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.text=value; t.fontSize=fontSize; t.fontStyle=style; t.color=Color.white; t.alignment=TextAnchor.MiddleCenter; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Overflow; t.raycastTarget=false; return t;
    }

    private static Button Button(Transform parent,string name,string value,Vector2 pos,Vector2 size,Color color)
    {
        var go=new GameObject("Btn_"+name,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f); rt.anchoredPosition=pos; rt.sizeDelta=size;
        var image=go.GetComponent<Image>(); image.color=color; var b=go.GetComponent<Button>(); b.targetGraphic=image;
        var label=Label(go.transform,"Label",value,Vector2.zero,size,25,FontStyle.Bold); label.color=Color.white;
        var colors=b.colors; colors.normalColor=color; colors.highlightedColor=Color.Lerp(color,Color.white,.12f); colors.pressedColor=Color.Lerp(color,Color.black,.15f); colors.selectedColor=colors.highlightedColor; colors.disabledColor=new Color(color.r,color.g,color.b,.45f); b.colors=colors;
        return b;
    }
}
