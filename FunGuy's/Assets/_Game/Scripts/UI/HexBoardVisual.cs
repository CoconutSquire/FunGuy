using UnityEngine;
using UnityEngine.UI;

// Presentation-only hex silhouette; board rules never depend on pixels.
public static class HexBoardVisual
{
    private static Sprite sprite;
    public static void Apply(Image image)
    {
        if (sprite == null)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "FormationHex";
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = Mathf.Abs((x + .5f) / size * 2 - 1);
                float ny = Mathf.Abs((y + .5f) / size * 2 - 1);
                pixels[y * size + x] = nx <= 1 - ny * .5f ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels); texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
        }
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.alphaHitTestMinimumThreshold = .1f;
    }
}
