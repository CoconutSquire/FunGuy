using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HomeSceneUiBuilder : RuntimeSceneUiBuilderBase
{
    public void Build(Scene scene, Canvas canvas)
    {
        if (canvas == null) return;
        EnsureMenuSafeContent(canvas.transform);

        var controller = FindInScene<HomeMenuController>(scene).FirstOrDefault();
        if (controller == null)
        {
            var root = EnsureSceneRoot(scene, canvas.transform, "HomeRoot");
            controller = EnsureSceneComponent<HomeMenuController>(scene, root.transform);
        }
        HomeScreenView.Build(canvas.transform, controller);
    }
}