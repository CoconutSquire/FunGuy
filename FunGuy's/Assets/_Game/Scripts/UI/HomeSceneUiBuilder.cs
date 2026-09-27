using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HomeSceneUiBuilder : RuntimeSceneUiBuilderBase
{
    public void Build(Scene scene, Canvas canvas)
    {
        if (canvas == null) return;
        EnsureMenuSafeContent(canvas.transform);

        var root = EnsureSceneRoot(scene, canvas.transform, "HomeRoot");
        var controller = EnsureSceneComponent<HomeMenuController>(scene, root.transform);
        HomeScreenView.Build(root.transform, controller);
    }
}