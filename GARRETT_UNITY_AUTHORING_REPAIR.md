# Garrett Unity-authored content repair — 2026-09-22

## Scope and safety

Garrett's original WIP commit is preserved in Git history at `669cc4a4`. The repair was built on a separate review branch because the active GitHub integration has read access but no direct write permission to `CoconutSquire/FunGuy`. No changes in this repair are intended for `OneVillage83/main`; the delivery mechanism is a cross-fork PR back to Garrett's `feature/unity-authored-content` branch.

## What the WIP was trying to do

The feature adds a `ContentCatalogAsset` ScriptableObject containing characters, skills, stages and banners; an Editor menu seeds the asset from JSON; and `GameData.LoadAll()` uses the Unity-authored asset when present, falling back to JSON when absent.

That architecture is reasonable for an Inspector-authoring transition, but the first implementation accidentally treated serialized asset objects as live mutable runtime objects.

## Root cause 1 — runtime code aliased the ScriptableObject asset

### Evidence

The committed `UnityContentCatalog.asset` contained:

- Stage `s_1_1` with `waves: []`
- Stage `s_1_1` reward gold changed from `50` to `99999`

Those values exactly match `CampaignSessionTests.FrozenContentAndRewardPayloadSurviveCatalogEditsAndFailedWriteRetry`, which intentionally clears the runtime stage waves and changes reward gold to `99999` after the campaign session snapshots its inputs.

The WIP loader passed `asset.characters`, `asset.skills`, `asset.stages` and `asset.banners` directly into `GameData.LoadValidated`. Therefore `GameData.Stages` held the same `StageDef` objects owned by the ScriptableObject. The test mutated the authored asset rather than an isolated runtime catalog. Unity then serialized that dirtied asset.

The saved EditMode report contained **122 tests: 94 passed, 28 failed**. All 28 failing test cases ultimately reported the same content-load exception:

`Invalid game content: Stage s_1_1: missing waves.`

That concentration is important: the branch did not contain 28 independent gameplay defects; one corrupted catalog entry prevented every content-dependent fixture from initializing.

### Fix

`UnityContentCatalogLoader` now deep-clones each serialized DTO section with `JsonUtility` before validation, normalization, or publication into `GameData`.

The ScriptableObject is now treated as immutable authored source during runtime. The loader's detached snapshot is the only object graph gameplay and tests can mutate.

## Root cause 2 — catalog validation mutated source data

The WIP `ContentCatalogAsset.ValidateSchema()` called `EnsureLists()`, which silently created missing lists and changed a zero stage schema to 1. That makes a damaged authored asset appear repaired during loading.

### Fix

- Runtime `ValidateSchema()` is now non-mutating and fails on unsupported/missing top-level structure.
- `EnsureListsForEditing()` remains as an explicit Editor-authoring convenience only.
- Full content validation still runs through `GameDataValidator`.

## Root cause 3 — production GameData normalization was unintentionally regressed

Garrett's first refactor shortened `GameData.cs` and accidentally removed existing production behavior, including:

- BST fallback calculation
- enemy POT/biome/class/role normalization
- skill energy-cost/cooldown normalization
- banner type/default pity/featured defaults
- fallback/renormalization of banner rates
- legacy element-to-biome mapping

### Fix

The current `main` implementation of those normalizers was restored, while retaining:

- `partial GameData`
- Unity catalog selection in `LoadAll()`
- shared `LoadValidated(...)` finalization

This keeps the new authoring feature additive instead of changing unrelated runtime semantics.

## Root cause 4 — the authored Stage 1 asset was already corrupted

Even after fixing ownership, the committed asset itself would still fail because Stage 1 had already been saved with no waves and 99999 gold.

### Fix

The serialized Stage 1 payload was repaired to match the intended current campaign data:

- Wave 1: R01 / R04
- Wave 2: R10 / R03
- reward gold: 50

## Regression coverage added

`UnityContentCatalogTests.cs` adds two focused tests:

1. `AuthoredCatalog_ContainsCurrentRosterAndCompleteStageOne`
   - asset exists
   - 71 characters
   - 7 stages
   - Stage 1 has two waves
   - Stage 1 reward is 50
   - expected Stage 1 opponent IDs are present

2. `RuntimeCatalog_IsDeepCopiedAndCannotDirtyAuthoredAsset`
   - loads runtime data
   - deliberately clears runtime Stage 1 waves
   - deliberately changes runtime reward gold to 99999
   - deliberately renames a runtime character
   - deliberately clears a runtime skill's effects
   - proves the ScriptableObject remains unchanged
   - reloads `GameData` and proves pristine values return

This directly reproduces and guards against the original failure mode.

## Editor workflow hardening

The menu item is now **Create or Refresh Unity Catalog From JSON**.

When a catalog already exists and Unity is interactive, refreshing shows a confirmation because the operation overwrites Inspector-only edits. Validation uses the same detached-snapshot path as runtime.

The JSON files remain a seed/fallback path; they are not silently used when an existing authored asset is invalid.

## Accidental package drift reverted

The WIP had upgraded five packages without the feature requiring them:

- `com.unity.2d.animation` 13.0.2 → 13.0.6
- `com.unity.2d.tooling` 1.0.0 → 1.0.4
- `com.unity.ai.generators` 1.0.0-pre.20 → 1.7.0-pre.1
- `com.unity.ide.visualstudio` 2.0.25 → 2.0.28
- `com.unity.timeline` 1.8.9 → 1.8.13

The repair restores `Packages/manifest.json`, `packages-lock.json`, and `PackageManagerSettings.asset` to the pinned `main` versions. This avoids mixing a content-authoring change with a package migration.

## Generated/editor side effects removed or restored

Removed from the feature diff:

- Visual Studio `.vs/` databases, indexes, `.suo`, and document-layout files
- local `.vsconfig`
- `TestResults_20260921_232029.xml`

Restored from `main`:

- tracked performance-test resource JSON files deleted by the test runner
- TMP fallback font asset dirtied by dynamic glyph caching
- generated solution file removed by the IDE
- pinned package files/settings

Ignore rules now exclude `.vs/` and root `TestResults_*.xml` so these do not recur.

## What was preserved from Garrett's work

The repair keeps the intended feature components:

- `UnityContentCatalog.asset`
- `ContentCatalogAsset.cs`
- `UnityContentCatalogLoader.cs`
- `GameData.UnityCatalog.cs`
- `FunGuyContentAuthoringMenu.cs`
- Unity metadata for those assets/scripts
- Garrett's validation improvements that correctly treat Unity-serialized empty strings as absent optional effect target/stat values and provide clearer invalid-slot errors
- JSON fallback when the authored asset is absent

## Validation performed here

This environment cannot launch Unity, so no new EditMode/PlayMode pass is claimed.

Source-level review verified:

- the saved test report's repeated failure is explained by the corrupted Stage 1 asset
- Stage 1 is repaired in the serialized asset
- runtime loading deep-copies the asset before mutation
- the production `GameData` normalizers are restored
- package files are returned to the pinned main versions
- generated IDE/test artifacts are removed from the feature diff
- focused regression tests are added for the aliasing defect

## Garrett's required local validation

After merging the PR:

1. close Unity/VS
2. pull `feature/unity-authored-content`
3. `git lfs pull`
4. delete generated `Library/`, `Temp/`, `Obj/`, `Logs/`
5. open with **Unity 6000.3.2f1**
6. run **FunGuy → Content → Validate Unity Catalog**
7. run all EditMode tests
8. run all PlayMode tests
9. inspect the first error only if failures remain
10. do not commit generated IDE/test/font/package side effects

If a fresh test report still fails, send the first failing test/stack trace; do not commit the XML.
