using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BattleSceneUiBuilder : RuntimeSceneUiBuilderBase
{
public void Build(Scene scene, Canvas canvas)
    {
        var asset = Resources.Load<BattleScreenView>("Presentation/BattleScreen");
        if (asset == null) throw new InvalidOperationException("Battle presentation prefab is missing. Run the Presentation authoring task.");
        var screen = UnityEngine.Object.Instantiate(asset, canvas.transform);
        screen.name = "BattleRoot";
        var controller = EnsureSceneComponent<BattleSceneController>(scene, screen.transform);
        controller.Configure(screen);
    }
}