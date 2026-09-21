using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Keeps the authored Home artwork visible when the runtime UI bootstrap rebuilds the menu.
/// The original Home scene contains the sprite reference, while runtime-generated UI adds a
/// second full-screen background panel. This component normalizes the authored rect and makes
/// the generated Home backdrop transparent so the artwork remains the bottom layer.
/// </summary>
public sealed class HomeArtworkBackground : MonoBehaviour
{
    private static HomeArtworkBackground _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (_instance != null) return;

        var host = new GameObject("HomeArtworkBackground");
        DontDestroyOnLoad(host);
        _instance = host.AddComponent<HomeArtworkBackground>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "Home") return;
        StartCoroutine(ApplyAfterRuntimeUi(scene));
    }

    private IEnumerator ApplyAfterRuntimeUi(Scene scene)
    {
        // RuntimeSceneUiBootstrap also responds to sceneLoaded. Wait for it to create HomeRoot.
        yield return null;
        Apply(scene);
        yield return null;
        Apply(scene);
    }

    private static void Apply(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas == null) continue;

            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect != null)
                canvasRect.localScale = Vector3.one;

            var authoredBackground = FindChildImage(canvas.transform, "Background");
            if (authoredBackground != null)
            {
                var rect = authoredBackground.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                authoredBackground.preserveAspect = false;
                authoredBackground.raycastTarget = false;
                authoredBackground.transform.SetAsFirstSibling();
            }

            var generatedBackground = FindChildImage(canvas.transform, "Img_Background");
            if (generatedBackground != null)
            {
                var color = generatedBackground.color;
                color.a = 0f;
                generatedBackground.color = color;
                generatedBackground.raycastTarget = false;
            }
        }
    }

    private static Image FindChildImage(Transform parent, string objectName)
    {
        foreach (var image in parent.GetComponentsInChildren<Image>(true))
        {
            if (image != null && image.name == objectName) return image;
        }

        return null;
    }
}
