# Unity-authored content foundation

This branch adds a Unity Inspector authoring path while retaining the JSON files as a fallback/import source.

## Runtime ownership rule

`UnityContentCatalog.asset` is **authored source data**. Runtime code must never hold references to the asset's nested `CharacterDef`, `StageDef`, `SkillDef`, or `BannerDef` objects.

`UnityContentCatalogLoader` therefore deep-clones all four serialized sections with `JsonUtility`, validates the detached snapshot, normalizes that snapshot, and only then publishes it through `GameData`. This is required because gameplay/tests intentionally mutate in-memory stage/content objects in some scenarios; those mutations must never dirty the ScriptableObject asset.

The regression test `UnityContentCatalogTests.RuntimeCatalog_IsDeepCopiedAndCannotDirtyAuthoredAsset` specifically protects this boundary.

## First-time setup / refresh

1. Open the project with the pinned **Unity 6000.3.2f1** editor.
2. Let compilation/package resolution finish.
3. Select **FunGuy → Content → Create or Refresh Unity Catalog From JSON**.
4. If a catalog already exists, Unity asks before overwriting its Inspector-authored content.
5. Select `Assets/_Game/Resources/GameData/UnityContentCatalog.asset`.
6. Edit characters, skills, stages and banners in the Inspector.
7. Run **FunGuy → Content → Validate Unity Catalog** before Play Mode and before committing content edits.

### Important refresh warning

Refreshing from JSON is intentionally destructive to Inspector-only catalog edits. Use it to seed/rebuild the catalog from the JSON authority, not as a routine validation command.

## Loading behavior

- If `UnityContentCatalog.asset` exists under `Resources/GameData`, it is the runtime content source.
- If the asset is absent, `GameData.LoadAll()` uses the existing JSON path.
- If the asset exists but is invalid, loading fails loudly. It does **not** silently fall back to JSON, because hiding a bad authored asset would make editor/runtime behavior ambiguous.
- `stat_rules.json` and `level_progression.json` remain the shared rules/progression inputs.

## Package/toolchain rule

This feature does not require package upgrades. Keep the repository-pinned package manifest/lockfile and Unity **6000.3.2f1** unless a package/editor migration is intentionally tested on a separate branch.

## Validation workflow for Garrett

After merging the repair PR into `feature/unity-authored-content`:

1. Close Unity and Visual Studio.
2. Pull the branch and run `git lfs pull`.
3. Delete generated local `Library/`, `Temp/`, `Obj/`, and `Logs/` folders (not Assets/Packages/ProjectSettings).
4. Open specifically with Unity 6000.3.2f1.
5. Run **FunGuy → Content → Validate Unity Catalog**.
6. Run the full EditMode suite.
7. Run the full PlayMode suite.
8. Do not commit `.vs/`, test-result XML, TMP dynamic-glyph changes, or package upgrades caused by the local editor.

The previous WIP test XML showed one root content failure repeated across tests: Stage `s_1_1` was serialized with `waves: []` and reward gold `99999`. That was caused by runtime aliasing into the asset and is the defect this repair addresses.
