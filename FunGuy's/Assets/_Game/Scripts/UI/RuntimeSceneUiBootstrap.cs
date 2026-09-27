using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RuntimeSceneUiBootstrap
{
    private static Font _cachedFont;
    private static bool _registered;
    private static readonly HashSet<int> _builtSceneHandles = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _registered = false;
        _cachedFont = null;
        _builtSceneHandles.Clear();
    }

    // Subscribe before the first scene finishes loading so every subsequent
    // SceneManager.LoadScene call is handled by the same scene-owned builder.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Register()
    {
        if (_registered) return;
        _registered = true;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // sceneLoaded is the normal path for later scene transitions. The explicit
    // AfterSceneLoad callback is the deterministic path for the first scene.
    // This matters when the initial scene is already loading while static
    // registration is being established.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapFirstScene()
    {
        if (!Application.isPlaying) return;
        var scene = SceneManager.GetActiveScene();
        ProcessScene(scene, LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ProcessScene(scene, mode);
    }

    private static void ProcessScene(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (scene.name != "Boot" && scene.name != "Home" && scene.name != "Summon" &&
            scene.name != "Team" && scene.name != "Battle" && scene.name != "Tutorial" &&
            scene.name != "Options") return;

        // Both the sceneLoaded event and the first-scene callback can legitimately
        // reach this method. Build each scene exactly once per play session.
        if (_builtSceneHandles.Contains(scene.handle)) return;

        try
        {
            EnsureEventSystem(scene);
            if (scene.name == "Boot") return;

            try
            {
                Game.EnsureInitialized();
            }
            catch (Exception ex)
            {
                Debug.LogException(new InvalidOperationException(
                    $"RuntimeSceneUiBootstrap could not initialize Game before building scene '{scene.name}'.", ex));
                return;
            }

            var canvas = EnsureCanvas(scene);
            Canvas.ForceUpdateCanvases();

            try
            {
                switch (scene.name)
                {
                    case "Home": new HomeSceneUiBuilder().Build(scene, canvas); break;
                    case "Summon": new SummonSceneUiBuilder().Build(scene, canvas); break;
                    case "Team": new TeamSceneUiBuilder().Build(scene, canvas); break;
                    case "Battle": new BattleSceneUiBuilder().Build(scene, canvas); break;
                    case "Tutorial":
                        new WelcomeSceneUiBuilder().Build(scene, canvas);
                        if (!Game.Save.tutorialCompleted)
                            new TutorialSceneUiBuilder().Build(scene, canvas);
                        break;
                    case "Options": new OptionsSceneUiBuilder().Build(scene, canvas); break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(new InvalidOperationException(
                    $"RuntimeSceneUiBootstrap failed while building scene '{scene.name}'.", ex));
                return;
            }

            Canvas.ForceUpdateCanvases();

            foreach (var binder in FindInScene<UiPrefabBlueprintBinder>(scene))
            {
                if (binder == null) continue;
                try
                {
                    binder.AutoBindCommonReferences();
                }
                catch (Exception ex)
                {
                    Debug.LogException(new InvalidOperationException(
                        $"RuntimeSceneUiBootstrap failed to bind UI references in scene '{scene.name}'.", ex));
                }
            }

            _builtSceneHandles.Add(scene.handle);
        }
        catch (Exception ex)
        {
            Debug.LogException(new InvalidOperationException(
                $"RuntimeSceneUiBootstrap failed during setup of scene '{scene.name}'.", ex));
        }
    }

    private static Canvas EnsureCanvas(Scene scene)
    {
        var existing = FindInScene<Canvas>(scene).FirstOrDefault();
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            ConfigureLandscapeCanvas(existing);
            return existing;
        }

        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasGo, scene);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.enabled = true;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 1f;

        return canvas;
    }

    private static void ConfigureLandscapeCanvas(Canvas canvas)
    {
        if (canvas == null) return;
        canvas.gameObject.SetActive(true);
        canvas.enabled = true;
        canvas.transform.localScale = Vector3.one;
        canvas.transform.localPosition = Vector3.zero;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 1f;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        var existing = FindInScene<EventSystem>(scene).FirstOrDefault();
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.enabled = true;
            var input = existing.GetComponent<InputSystemUIInputModule>();
            if (input != null) input.enabled = true;
            return;
        }

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(es, scene);
    }

    private static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        var found = new List<T>();
        foreach (var root in scene.GetRootGameObjects())
            found.AddRange(root.GetComponentsInChildren<T>(true));
        return found;
    }
}