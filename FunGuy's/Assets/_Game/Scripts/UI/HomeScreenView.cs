using UnityEngine;

// Compatibility entry point for older callers. Home navigation is owned by
// HomeSceneUiBuilder; this class must not maintain a second Home hierarchy.
public static class HomeScreenView
{
    public static void Build(Transform homeRoot, HomeMenuController controller)
    {
        if (homeRoot == null || controller == null) return;
        var canvas = homeRoot.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        new HomeSceneUiBuilder().Build(homeRoot.gameObject.scene, canvas);
    }
}
