# Testing Guide

For command-line execution, toolchain requirements, outputs and build variants, see the repository-root `BUILD_GUIDE.md`. Actual dated results belong in `PROJECT_LOG.md`; this guide describes coverage rather than asserting that a current run passed.

## Stabilization coverage

- `StabilizationTests`: all bundled wave objects, explicit enemy growth, invalid content, lethal DoT, Silence timing, independent status durations, Thorns, atomic summons and one-time campaign/tutorial rewards.
- `SaveRecoveryTests`: previous-snapshot backup, schema 2-to-3 migration, and removal of legacy recovery keys. Save keys are preserved/restored around tests.
- `Tests/PlayMode/OnboardingTests`: button-driven fresh onboarding through summon/reveal/team/battle/completion, save persistence, repeat binding, and resume at the summon step without a replacement ticket. Existing save keys are preserved/restored.

Run these from the root with `./tools/Invoke-Unity.ps1 -Task EditMode` or `-Task PlayMode`. A passing PlayMode test does not replace a rendered Android input/layout check.

## Automated Tests (EditMode)

Location:
- `Assets/_Game/Tests/EditMode`

Assembly:
- `Game.EditModeTests.asmdef`

Current coverage:
- `GachaServiceTests`
  - deterministic pull sequence with seed
  - featured guarantee carry behavior on limited banner
- `BattleSimTests`
  - deterministic outcome with seed
  - biome advantage damage multiplier sanity check
- `DataAndSaveTests`
  - stage wave enemy-reference integrity
  - save-model JSON roundtrip for map-like lists

Run in Unity:
1. Open **Window -> General -> Test Runner**.
2. Select **EditMode**.
3. Run all tests or filter by class.

## Runtime Smoke Tests

Runtime smoke and debug controllers are compiled only for Editor or development builds. The runtime scene builder creates and binds their Options UI automatically; release builds do not include these controller types.

Script:
- `Assets/_Game/Scripts/UI/GameplaySmokeTestController.cs`

Recommended setup:
- Add to Debug panel scene (or `Options` scene).
- Bind a button to `OnRunSmokeTestsPressed`.
- Bind `outputLabel` to a multiline text UI element.

Smoke suite checks:
- Data load validity
- Stage wave data sanity
- Deterministic gacha sequence (seeded)
- Deterministic battle result (seeded)
- Save-model serialization roundtrip

## Debug Utilities

Script:
- `Assets/_Game/Scripts/UI/DebugProgressionController.cs`

Useful actions:
- Reset save
- Grant starter resources
- Seed starter roster
- Skip tutorial

Team service coverage adds invalid/unowned/duplicate/over-capacity replacement rollback, ordered selection and no-op writes, deterministic autofill and storage-failure isolation. The PlayMode team-control scenario exercises clear, roster toggle, autofill and persistence through the screen controller. Consult the project log for executed counts; adding tests does not imply a passing run.

Canonical stat coverage (`CanonicalStatRulesTests.cs`) loads the real versioned catalog and checks independently worked Tank/DPS/Support/Assassin values at levels 1/20/100/200, plus Mage/Tactician examples and named Chanterelle/Kitchen/biome-less references. Tests cover evolution caps, flat star SPD, all 36 biome matchups, damage rounding, malformed/aliased input rejection and catalog snapshot isolation. A test-only canonical character verifies the factory and campaign consume owned evolution without importing a fabricated kit into production content. These fixtures implement the resolutions documented in root `COMBAT_RULES.md`; gear, Core, formation synergies and full roster migration are not covered as completed features.

Formation coverage (`FormationTests.cs`, `SaveRecoveryTests.cs`) verifies slot swap/move/replacement, preserved ownership, failed-write rollback, sparse/legacy/damaged-position recovery, schema-5 round trips and schema-4 board migration, front/middle/rear depth targeting and independent board/team capacity, death fallback and explicit enemy content slots. A placement-dependent combat example verifies that a fragile unit dies when moved into the targeted front slot. PlayMode covers roster paging, placement, moving, removal, scene return, persisted gaps and rejecting an empty battle. These are callback-based UI checks; consult PROJECT_LOG for separate visual/device verification.

Run `./tools/Invoke-Unity.ps1 -Task PlayMode -CaptureUi` from the repository root for optional 2400×1080 menu/onboarding/Team/Battle renders, plus 1920×1080 and 1600×1200 battle views. Capture restores canvas/camera state and saves ignored PNGs in `artifacts/PlayMode/screenshots`. The placement scenario also asserts that selected-hex instructions survive after tutorial completion. These editor renders do not certify APK behavior.

`BattleSessionTests` exercises manual queues/cancellation/Auto, cooldown and control statuses, enemy automatic policy, explicit terminal outcomes/no-ops, lethal ticks/reflection, repeated-enemy identities, input/skill/event snapshot isolation, source-aligned gauge reset, hit/kill energy and replay of accepted commands with random targeting/status effects. It also compares the stepped automatic session with the legacy synchronous adapter and verifies healing/shield/status event snapshots. The campaign reward fixture checks event traces across waves and stable player IDs. This suite covers the original command/event boundary; CombatFoundationTests below extend coverage to the v2 effects, passives and representative synergies.

Step-2 completion coverage adds CampaignSessionTests (interactive wave state/Auto/identity carry, frozen content/reward payloads, no early/cancelled/timeout rewards, interleaved-save merge, competing first clears and storage retry) and CombatFoundationTests (DoT timing, control, cover, immunity/cleanse/dispel, movement identity, bounded passives, canonical JSON reference loading, actual-energy shielding, class/biome thresholds and representative role-pair interactions). Public bonus and skill read models support live presentation. The old BattleSessionTests remain regression/replay coverage, not a claim that all eventual roster/Core/gear mechanics exist. Run results and failed attempts are in PROJECT_LOG.md.

Step-3 PlayMode coverage updates onboarding to await live event playback and exercises a five-versus-five fixture, prefab renderer references, manual default, queue/cancel feedback, pause/inspection/focus behavior, speed/Auto/reduced motion, final displayed HP/shield/energy/positions, replay without duplicate rewards, next-stage selection and navigation cancellation. The extra enemies exist only in test memory. Save and motion preferences are restored after tests. Review the generated screenshots as well as test results: callback assertions alone originally missed absent CanvasRenderer components on the custom hex/portrait Graphics.

Step-4 upgrade coverage adds `UpgradeServiceTests`: shared legacy/canonical previews, preserved stars/XP/copies/formation, cost/cap validation, stale requests, intervening wallet/reward changes, failed writes and retry, duplicate/unowned rejection and retained higher-level saves. PlayMode's workshop scenario spends first-clear gold through UI callbacks, reloads save/application state, checks unchanged formation and starts battle with the upgraded HP. Optional captures cover the landscape preview/success and tablet layout. These are editor checks; current Android coverage must be read from PROJECT_LOG.md.

Step-4 content coverage adds SliceContentTests: ten authored kits/six classes/five biomes, independently calculated class-growth/evolution values, execution of every basic/signature/passive, preserved save fields, the 20-seed earned-gold starter campaign and an unupgraded boss comparison. Reports go to repository artifacts/step4-content/campaign-balance.csv. The campaign PlayMode scenario checks locked entries, first-uncleared selection, boss entry/victory and once-only reward/practice labels after navigation. These tests establish a reproducible route, not a human playtime or optimal-balance claim.

Step-4 guidance extends the full onboarding test through Team → workshop → saved purchase → campaign continuation. It verifies the tutorial banner hides while the workshop is open and returns after close. A separate saved-step-9 test resumes into Team with zero gold and completes without purchasing; onboarding must never require an unavailable transaction. Optional captures: step4-upgrade-guidance and step4-guided-upgrade-saved.
