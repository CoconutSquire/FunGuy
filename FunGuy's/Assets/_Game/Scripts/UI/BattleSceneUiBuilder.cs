using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BattleSceneUiBuilder : RuntimeSceneUiBuilderBase
{
    public void Build(Scene scene, Canvas canvas)
    {
        if (canvas == null) return;
        var asset = Resources.Load<BattleScreenView>("Presentation/BattleScreen");
        if (asset == null)
            throw new InvalidOperationException("Battle presentation prefab is missing. Run the Presentation authoring task.");

        var content = EnsureMenuSafeContent(canvas.transform);
        var screen = UnityEngine.Object.Instantiate(asset, content);
        screen.name = "BattleRoot";
        var rect = screen.GetComponent<RectTransform>();
        if (rect != null) StretchToParent(rect);

        var controller = EnsureSceneComponent<BattleSceneController>(scene, screen.transform);
        controller.Configure(screen);
    }
}