# Unity-authored content foundation

This increment adds a Unity Inspector authoring path without removing the existing JSON fallback.

## First-time setup

1. Open the project with the pinned Unity version.
2. Wait for compilation to finish.
3. Select **FunGuy → Content → Create Unity Catalog From JSON**.
4. Select `Assets/_Game/Resources/GameData/UnityContentCatalog.asset`.
5. Edit characters, skills, stages and banners directly in the Inspector.
6. Use **FunGuy → Content → Validate Unity Catalog** before entering Play Mode.

Once the catalog asset exists under `Resources/GameData`, runtime loading uses it automatically. If it is absent, the original JSON loading path remains active.

## Safety boundary

The catalog contains authored definitions, not live battle or save state. Runtime validation still runs through `GameDataValidator`, and combat, reward, summon and save operations remain owned by the existing C# domain/application services.

The first catalog is intentionally one asset containing editable lists. This makes migration reversible and keeps IDs/references visible while the project moves away from runtime-generated content. Later increments can split large lists into individual Character/Skill/Stage assets and add drag-and-drop references.

## Home screen

The existing Home screen remains runtime-generated in this increment so gameplay behavior is unchanged. The next presentation increment can create `HomeScreen.prefab` and replace only `EnsureHomeScene` after the catalog workflow has been verified.
