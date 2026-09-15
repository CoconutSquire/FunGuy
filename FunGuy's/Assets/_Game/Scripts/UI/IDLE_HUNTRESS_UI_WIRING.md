# Idle Huntress-Style UI Wiring Guide

Milestone 0 update: runtime-generated scenes are a temporary presentation adapter. `RuntimeSceneUiBootstrap` now explicitly initializes `SummonRevealController` and `TutorialOverlay` after creating their controls. A persistent root `TutorialManager` attaches to a fresh overlay in each gameplay scene. If authoring replacement prefabs, preserve those explicit initialization contracts rather than relying on `Awake` to find fields assigned later. Repeated binder calls replace their own button listeners. Debug/QA controller types exist only in Editor/development builds. See the repository-root `BUILD_GUIDE.md` for the current architecture; the layouts below remain reference material for the later prefab milestone.

Step-3 update: the game now uses a landscape safe-area surface. Battle uses the serialized BattleScreen/BattleFighter resource prefabs, while LandscapeMenuLayout adapts generated menus and tutorial overlays. See repository-root BATTLE_PRESENTATION.md for the current authoring workflow. Older menu skin names are implementation history, not a portrait product requirement.

## 1) Home Scene

### Root setup
- `Canvas/HomeRoot`
- `HomeRoot` add:
  - `UiPrefabBlueprintBinder` (optional auto-bind of common fields by object name)
  - `IdleHuntressSkin` (`tone = Home`)
  - `StaggeredEntranceAnimator` (optional for menu card entrances)
  - `HomeMenuController`

### `HomeMenuController` bindings
- `welcomeLabel` -> top hero line text
- `accountStatsLabel` -> currency/account strip text
- Button hooks:
  - Start -> `OnStartPressed`
  - Summon -> `OnSummonPressed`
  - Team -> `OnTeamPressed`
  - Battle -> `OnBattlePressed`
  - Options -> `OnOptionsPressed`

Step 4 binds HomeMenuController.OpenCampaign to CampaignPanelController.Open. Home's existing Btn_Battle is labeled Campaign and opens this picker; explicit picker listeners select only application-unlocked stages and set Game.SelectedStageId before Battle loads. The in-memory cursor survives Team navigation but is not a new progression claim/save field. Do not add generic reward/currency handlers to stage cards. Stage descriptions, boss marker, wave counts and cleared-practice labels come from current content/state.

## 2) Summon Scene

### Root setup
- `Canvas/SummonRoot`
- `SummonRoot` add:
  - `UiPrefabBlueprintBinder` (optional)
  - `IdleHuntressSkin` (`tone = Summon`)
  - `SummonMenuController`

### `SummonMenuController` bindings
- `resultLabel` -> text area below summon buttons
- `sporesLabel` -> currency strip
- `bannerLabel` -> banner title text
- `pullOneButton` -> pull x1 button
- `pullTenButton` -> pull x10 button
- `revealController` -> `SummonRevealController` component on reveal panel

### Reveal panel setup
- `Canvas/SummonRevealPanel` with:
  - `SummonRevealController`
  - `CanvasGroup` (optional)
- `SummonRevealController` bindings:
  - `root` -> reveal panel root game object
  - `titleLabel` -> "New Funguy Acquired"
  - `nameLabel` -> unit name
  - `rarityLabel` -> star row
  - `rarityFrame` -> card frame image
  - `rarityGlow` -> glow/vfx image
  - `nextButton` -> continue/reveal-next button

## 3) Team Scene

### Root setup
- `Canvas/TeamRoot`
- `TeamRoot` add:
  - `UiPrefabBlueprintBinder` (optional)
  - `IdleHuntressSkin` (`tone = Team`)
  - `TeamMenuController`
  - `TeamRosterSlotBinder` (optional quick-select button grid)

### `TeamMenuController` bindings

Selection and placement actions call `Game.Team` (`ITeamService`); the controller must not mutate or persist the saved team itself. Fighter capacity comes from that interface; board space count comes from FormationRules.SlotCount. The runtime board has twelve selectable hexes (three depths/four staggered lanes), five paged roster buttons and Previous/Next. Long names wrap inside each hex; roster ordering uses the same ordinal ID tie-break as autofill. Authored prefabs and richer roster details remain later work.

Call `ConfigureFormation` with twelve buttons/labels in numeric slot order and the Remove selected fighter button. It owns those button listeners. Apply HexBoardVisual to each cell Image and keep labels within its solid center. Call `TeamRosterSlotBinder.ConfigurePaging` with Previous, Next and the page label. A selected board slot plus roster click assigns/replaces; two board clicks swap/move. Re-selecting the same slot cancels. Without board selection, roster clicks retain membership-toggle behavior. Keep listeners on these generated controls owned by their explicit binder to avoid duplicate actions. The battle prefab separately renders both mirrored boards through BattleScreenView.CellPosition and immutable event snapshots.

- `teamStatusLabel` -> active team summary
- `rosterLabel` -> formation targeting guidance (paged buttons display the owned roster)
- Team's persistent `hintLabel` belongs to TeamMenuController; pass null as TutorialSpotlightController's hint for this screen so it cannot clear placement/error instructions after onboarding. Tutorial step text remains in the overlay.
- `hintLabel` -> tactical hint line
- Button hooks:
  - Auto Fill -> `AutoFillTeam`
  - Clear Team -> `OnClearTeamPressed`
  - Start Battle -> `OnStartBattlePressed`
  - Back -> `OnBackPressed`

### Optional unit cards

Step 4 adds `Btn_Upgrades` → `UpgradePanelController.Open`, created and bound explicitly by `UpgradePanelView.Create` inside the existing Team safe-area root. Its modal owns fighter paging/selection, current/next stat labels, the cost button and close callback; closing refreshes Team roster labels. Use IUpgradeService for all previews and purchases. Do not wire an additional generic handler or mutate gold/level in a prefab callback. Team cards now display acquisition tier and owned level; the workshop separately shows evolution stars. See root LEVEL_PROGRESSION.md for transaction and persistence semantics.

- Each card button can call:
  - add: `AddUnitToTeam(string charId)`
  - remove: `RemoveUnitFromTeam(string charId)`

### Optional quick-select roster grid
- Add `TeamRosterSlotBinder` on Team scene.
- Assign:
  - `teamController`
  - `slotButtons` (top N owned units)
  - `slotLabels` (matching text labels)
- Calling `RebindSlots()` refreshes labels and click actions automatically.

## 4) Battle Scene

RuntimeSceneUiBootstrap instantiates `Resources/Presentation/BattleScreen.prefab` as Canvas/BattleRoot and calls BattleSceneController.Configure(BattleScreenView). Do not add UiPrefabBlueprintBinder or separately bind those buttons: Configure owns listeners. BattleScreenView's serialized fields reference all labels, fighter prefab/layer, signature cards, result and inspector panels. BattleFighterView binds portrait, HP/energy bars and state labels; the custom Graphic types require CanvasRenderer.

The controller connects Start, Retry, Next stage, Home/Team, signature queue/cancel, Auto, Pause, Speed, Finish on Auto, inspection and reduced motion. It consumes CampaignSession events rather than running a synchronous simulation or modifying the wallet. Rebuild the default prefabs explicitly with `tools/Invoke-Unity.ps1 -Task Presentation`; edit Assets/Editor/PresentationAssets.cs for durable generator changes. See BATTLE_PRESENTATION.md for playback, lifecycle and replacement boundaries.

## 4.1) Debug / QA Scene (Recommended)

Create a lightweight debug panel scene or embed in `Options`.

- Add `DebugProgressionController` for save/resource utilities:
  - Reset Save -> `OnResetSavePressed`
  - Grant Starter Resources -> `OnGrantStarterResourcesPressed`
  - Seed Starter Roster -> `OnSeedStarterRosterPressed`
  - Skip Tutorial -> `OnSkipTutorialPressed`
  - Open Summon -> `OnOpenSummonPressed`
  - Back Home -> `OnBackHomePressed`
- Add `GameplaySmokeTestController` for one-click smoke checks:
  - Run Smoke Tests -> `OnRunSmokeTestsPressed`
  - `outputLabel` -> multiline results text

## 5) Notes
- `IdleHuntressSkin` controls panel/button/text colors and accent pulse.
- `StaggeredEntranceAnimator` can be attached to scene root cards for soft panel reveal.
- `TutorialSpotlightController` can pulse-highlight menu buttons by tutorial step.
- `UiPrefabBlueprintBinder` auto-finds and assigns common serialized references by name/path.
- Tutorial progression is already wired from:
  - summon completion (`OnFirstSummonCompleted`)
  - battle completion (`OnFirstBattleCompleted`)

## 5.1) Blueprint Binder Naming

`UiPrefabBlueprintBinder` looks for common aliases. Prefer these names for fastest setup:

- Home:
  - `Lbl_Welcome`
  - `Lbl_AccountStats`
- Summon:
  - `Lbl_Result`
  - `Lbl_Spores`
  - `Lbl_Banner`
  - `Btn_PullOne`
  - `Btn_PullTen`
  - `SummonReveal` (with `SummonRevealController`)
- Team:
  - `Lbl_TeamStatus`
  - `Lbl_Roster`
  - `Lbl_Hint`
- Battle uses serialized BattleScreenView references rather than these naming aliases.
- Tutorial overlay:
  - `TutorialOverlayRoot`
  - `Lbl_TutorialMessage`
  - `Btn_TutorialContinue`

### Auto-button onClick wiring

`UiPrefabBlueprintBinder` now also supports runtime button event wiring by convention.
Enable `autoWireButtons` on the binder.

Default button-name mappings:
- `Btn_Start` -> `OnStartPressed`
- `Btn_Options` -> `OnOptionsPressed`
- `Btn_Summon` -> `OnSummonPressed`
- `Btn_Team` -> `OnTeamPressed`
- `Btn_Battle` -> `OnBattlePressed`
- `Btn_PullOne` -> `OnPullOnePressed`
- `Btn_PullTen` -> `OnPullTenPressed`
- `Btn_AutoFill` -> `AutoFillTeam`
- `Btn_ClearTeam` -> `OnClearTeamPressed`
- `Btn_StartBattle` -> `OnStartBattlePressed`
- `Btn_RunBattle` -> `OnRunBattlePressed`
- `Btn_Retry` -> `OnRetryPressed`
- `Btn_NextStage` -> `OnNextStagePressed`
- `Btn_GoTeam` -> `OnGoTeamPressed`
- `Btn_Back` -> `OnBackPressed`
- `Btn_TutorialContinue` -> `ContinueTutorial`
- `Btn_ResetSave` -> `OnResetSavePressed`
- `Btn_GrantStarterResources` -> `OnGrantStarterResourcesPressed`
- `Btn_SeedStarterRoster` -> `OnSeedStarterRosterPressed`
- `Btn_SkipTutorial` -> `OnSkipTutorialPressed`
- `Btn_RunSmokeTests` -> `OnRunSmokeTestsPressed`

You can add extra `customButtonRules` entries to map any button name to a method and optional target type.

## 6) Tutorial Spotlight Setup

Add `TutorialSpotlightController` to each scene root that should highlight UI.

### Suggested mapping
- Home scene:
  - `ShowSummonPool` -> Summon button
- Team scene:
  - `GoToTeamBuilder` and `PlaceFirstUnit` -> team slot area or autofill button
  - `StartFirstBattle` -> Start Battle button
- Battle scene:
  - `StartFirstBattle` -> Run Battle button

### Binding
- `targets`: add entries with `step`, `target`, and optional `hint` text
- `hintLabel`: optional helper label below top bar for tutorial instruction text
