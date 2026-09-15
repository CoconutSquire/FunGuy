using UnityEngine;
using UnityEngine.UI;

// Editable code-native pixel silhouette; an illustrated sprite can replace this component per roster kit.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class FungusPortrait : MaskableGraphic
{
    public int variant;
    public bool enemy;
    public Color cap = new(.35f, .58f, .22f);
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); var r = rectTransform.rect;
        void Pixel(float x, float y, float w, float h, Color c) {
            int n = vh.currentVertCount;
            Vector2 p = r.min + Vector2.Scale(new Vector2(x / 32, y / 36), r.size);
            Vector2 s = Vector2.Scale(new Vector2(w / 32, h / 36), r.size);
            c *= color;
            vh.AddVert(p, c, Vector2.zero); vh.AddVert(p + new Vector2(0, s.y), c, Vector2.zero);
            vh.AddVert(p + s, c, Vector2.zero); vh.AddVert(p + new Vector2(s.x, 0), c, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
        Color ink = new(.10f, .08f, .06f), stem = new(.88f, .78f, .53f), shade = new(.5f, .38f, .22f);
        Pixel(5, 0, 23, 3, new Color(0, 0, 0, .32f));
        Pixel(9, 2, 6, 5, ink); Pixel(20, 2, 6, 5, ink);
        Pixel(9, 5, 17, 17, ink); Pixel(11, 7, 13, 14, shade); Pixel(11, 9, 9, 12, stem);
        Pixel(6, 10, 5, 7, ink); Pixel(7, 12, 4, 4, stem);
        Pixel(24, 10, 5, 7, ink); Pixel(24, 12, 4, 4, stem);
        Pixel(12, 16, 3, 3, ink); Pixel(19, 16, 3, 3, ink); Pixel(13, 18, 1, 1, Color.white); Pixel(20, 18, 1, 1, Color.white);
        Pixel(15, 12, 3, 1, ink);
        Pixel(2, 21, 29, 4, ink); Pixel(5, 25, 23, 4, ink); Pixel(9, 29, 15, 4, ink); Pixel(13, 33, 7, 2, ink);
        Pixel(3, 22, 27, 2, Color.Lerp(cap, ink, .45f)); Pixel(6, 24, 21, 4, cap); Pixel(10, 28, 13, 4, cap); Pixel(14, 32, 5, 1, cap);
        Pixel(8, 26, 5, 2, Color.Lerp(cap, Color.white, .4f));
        Pixel(variant % 2 == 0 ? 17 : 13, 29, 4, 2, stem); Pixel(22, 25, 3, 2, stem); Pixel(8, 24, 2, 2, stem);
        // Class equipment silhouette variations remain distinct at small screen sizes.
        if (variant % 3 == 0) { Pixel(2, 4, 7, 11, ink); Pixel(3, 6, 5, 8, cap); Pixel(5, 7, 1, 6, stem); }
        else if (variant % 3 == 1) { Pixel(27, 2, 2, 21, shade); Pixel(25, 22, 6, 5, cap); Pixel(26, 24, 3, 2, stem); }
        else { Pixel(26, 6, 2, 14, ink); Pixel(27, 12, 2, 12, new Color(.7f, .85f, .83f)); Pixel(24, 9, 6, 2, shade); }
        if (enemy) Pixel(15, 8, 4, 2, new Color(.55f, .15f, .1f));
    }
}
