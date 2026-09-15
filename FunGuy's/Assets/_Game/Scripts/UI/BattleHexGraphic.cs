using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class BattleHexGraphic : MaskableGraphic
{
    public Color border = new(.78f, .59f, .29f, .8f);
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear(); var r = rectTransform.rect;
        var points = new[] { new Vector2(-.5f, 0), new Vector2(-.25f, .5f), new Vector2(.25f, .5f),
            new Vector2(.5f, 0), new Vector2(.25f, -.5f), new Vector2(-.25f, -.5f) };
        mesh.AddVert(r.center, color, Vector2.zero);
        for (int i = 0; i < 6; i++) mesh.AddVert(r.center + Vector2.Scale(points[i], r.size) * .96f, color, Vector2.zero);
        for (int i = 0; i < 6; i++) mesh.AddTriangle(0, i + 1, (i + 1) % 6 + 1);
        for (int i = 0; i < 6; i++) mesh.AddVert(r.center + Vector2.Scale(points[i], r.size), border, Vector2.zero);
        for (int i = 0; i < 6; i++) { int next = (i + 1) % 6; mesh.AddTriangle(i + 1, i + 7, next + 7); mesh.AddTriangle(i + 1, next + 7, next + 1); }
    }
}
