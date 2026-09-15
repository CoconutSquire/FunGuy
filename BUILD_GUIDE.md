# FunGuy's local build and validation

## Toolchain and project

Use PowerShell 7 and Unity 6000.3.2f1 with Android Build Support, SDK/NDK and OpenJDK installed. The Unity project is the nested `FunGuy's` directory. Close interactive Unity sessions before using the batch runner.

From the repository root:

```powershell
./tools/Invoke-Unity.ps1 -Task EditMode
./tools/Invoke-Unity.ps1 -Task PlayMode
./tools/Invoke-Unity.ps1 -Task PlayMode -CaptureUi
./tools/Invoke-Unity.ps1 -Task Presentation
./tools/Invoke-Unity.ps1 -Task AndroidDevelopment
./tools/Invoke-Unity.ps1 -Task AndroidReleaseCheck
```

The runner accepts `-UnityPath`, `-ProjectPath` and `-TimeoutSeconds` overrides. It starts Unity without a visible window, captures logs and XML in ignored `artifacts/<Task>/`, propagates failures, rejects empty/stale test results and stale build outputs, and restores the exact pre-run bytes of tracked performance-test resources that Unity's test package deletes. Android tasks use repository-local `artifacts/gradle` as `GRADLE_USER_HOME` to avoid interference from user-wide daemon state; the first run may need dependency downloads.

Optional `-CaptureUi` applies only to PlayMode and enables graphics. Tests render onboarding, menus, Team and Battle through a temporary camera at 2400×1080, plus battle at 1920×1080 and 1600×1200, into `artifacts/PlayMode/screenshots`, restoring canvas state afterward. This is editor layout evidence, not Android installation/touch or device-performance coverage. Batch mode uses a render request because it has no presented backbuffer. `-Task Presentation` explicitly rebuilds the battle/fighter resource prefabs from Assets/Editor/PresentationAssets.cs; it overwrites those defaults and is not needed for ordinary imports. See BATTLE_PRESENTATION.md.

Android commands produce `Funguy.apk` and a summary beside the build log. `AndroidReleaseCheck` exercises compilation/stripping without the development define; it is a local release-configuration check, not a signed store release or AAB. No store upload is performed. BuildAutomation validates bundled content and requires the seven enabled gameplay scenes with `Assets/Scenes/Boot.unity` first.

Use a clean checkout with hydrated Git LFS assets and the same pinned editor/packages to establish reproducibility. A successful build using the local Library cache alone does not establish that gate. Actual run results and any limitations are recorded in `PROJECT_LOG.md`.

For uncommitted work, `./tools/New-ValidationSnapshot.ps1 -Name <name>` copies the current tracked and nonignored new files to a fresh ignored directory with a SHA-256 manifest. It excludes local Library/build caches through Git's file inventory. Pass its nested Unity project path to `Invoke-Unity.ps1 -ProjectPath <path>`. This checks clean-project imports of the working source; it does not claim a fresh machine without global Unity/package caches.

## Current application boundaries

- `Game.EnsureInitialized` is the composition point for the bundled catalog, local save, campaign and summon services.
- `ICampaignService.Begin` creates a manual/Auto CampaignSession with Step, current Battle commands, AdvanceWave, Cancel and Complete. Inputs are captured at start; first-clear settlement merges the latest save atomically and is retryable after storage failure. BattleSceneController drives this API for live playback; Run remains the synchronous QA adapter. `ISummonService` performs one- or ten-pulls against a detached snapshot and persists the whole successful result once.
- `ITeamService` owns selection capacity, ownership/catalog checks, replacement, add/remove, clear and deterministic autofill. The local implementation preserves ordered IDs, rejects duplicates and invalid replacements before writing, and skips no-op writes. `TeamMenuController` consumes this interface rather than editing the saved team itself.
- Formation operations (`GetFormation`, `Place`, `SwapSlots`) preserve sparse positions and commit placement plus membership atomically. Campaign snapshots those positions; shared enemy conversion accepts optional wave `slotId`. See `FORMATION_RULES.md` for board and targeting behavior.
- `IPlayerSaveStore` requires detached reads and full-snapshot writes. `LocalPlayerSaveStore` adapts PlayerPrefs serialization and publishes committed state to `Game.Save`.
- Controllers invoke these operations; they do not directly calculate battle rewards or partially commit paid ten-pulls.
- `CombatUnitFactory` is the shared conversion path for gameplay and QA. Characters with `statProfileId` use named `stat-sheet-v1` profiles. The ten `class-growth-v1` collectible definitions use explicit draft bases with canonical class growth, owned level and evolution stars. Legacy enemies retain explicit enemy growth. `COMBAT_RULES.md` defines indexing, flat star SPD, caps and rounding. Gear and Core scaling remain unimplemented.
- `BattleSession` advances one action at a time, snapshots its unit/skill inputs, accepts queued signature/cancel/Auto commands and exposes immutable event/state history. `BATTLE_SESSION.md` defines lifecycle/replay limits. The live screen displays events with player controls; legacy automatic adapters use the same engine. Campaign results include per-wave traces, outcomes, seed and rules version.
- These are local prototype adapters. They are not secure against client tampering and do not implement remote request idempotency/concurrency. Production authority belongs to the later server adapter.

## Content and save changes

`stages.json` now requires `schemaVersion: 1`, unique positive `order` values, and wave objects of the form `{ "enemies": [...] }`. The old nested-array format was not loadable by JsonUtility and is rejected explicitly. No shipped remote catalog is known to require runtime support for that old format. All bundled consumers and tests must change together if the schema changes again.

The validator rejects missing/duplicate IDs, invalid stat/growth values, unsupported targets/effects/status names, missing skill/enemy references, malformed waves/rewards and invalid banner rates/pools. Legacy normalization supports old placeholder biome/POT fields but skips explicit profile/model definitions. The ten authored slice kits declare class-growth-v1 and slice-roster-v1; validation checks their source-aligned biome/tier/BST budgets and rejects conflicting stat paths. SLICE_CONTENT.md distinguishes these drafts from named source profiles.

`stat_rules.json` requires schema 1 and rules version `stat-sheet-v1`. It contains six class templates/growth tables, six evolution rows and 14 named stat profiles transcribed from the PDF. Canonical character definitions reference a profile, provide matching name/biome/class/role/acquisition tier and authored skills, and omit local base/growth values. Unknown explicit biomes are rejected. Do not add these references to summon pools until their kits and campaign balance are ready. The damage simulator uses the shared decimal damage/biome rules for all existing units.

Save schema 3 adds account XP, cleared-stage IDs and a tutorial battle reward claim flag while retaining the existing storage key for migration. Earlier balances and account levels are preserved because their historical derivation cannot be reconstructed reliably. Campaign tracking begins empty for legacy saves; existing first-clear rewards may therefore be earned once under the new tracking. Legacy tutorial steps at or beyond rewards count as already claimed, and steps after summoning do not regain a ticket. Account XP accumulates without increasing account level until an approved curve is implemented.

Current save schema **5** migrates the earlier five-slot board into twelve hex spaces per side. Old slots 0/1/2/3/4 map to 0/2/8/9/11; older team-order saves follow the same map. Valid schema-5 sparse positions survive reload without remapping. Invalid/duplicate/stale assignments recover deterministically. Existing balances, owned units, progress and storage keys remain intact. Optional enemy `slotId` accepts current depth/lane names and migrated legacy aliases, remaining compatible with stage schema 1. Waves retain a five-fighter cap despite twelve available spaces.

Until repeat rewards are designed, a stage grants its bundled reward on first clear only; subsequent runs are practice. This is an explicit stabilization policy, not the final campaign economy. The previous valid primary snapshot is retained as backup, and resetting/deleting also clears legacy keys. Comprehensive durable-file storage and account recovery remain later work.

## Temporary presentation and combat limits

Battle now instantiates reusable serialized BattleScreen/BattleFighter prefabs with TextMeshPro and explicit Configure wiring. Surrounding menus retain the runtime builder with landscape layout adaptation; it initializes reveal controls, owns generated bindings and uses the Input System UI module. Tutorial state belongs to a persistent root manager; each gameplay scene provides a fresh landscape overlay. A missing overlay cannot silently skip tutorial steps. BATTLE_PRESENTATION.md documents art provenance, live controls, safe areas and later bespoke roster artwork.

Team displays a twelve-space hex board with an independent five-fighter limit, explicit assignment/swap/removal and paged roster buttons for all owned units. The generated UI remains a prototype; authored prefabs and drag-and-drop are later work. After onboarding, reopening a cleared team keeps it empty and starting an empty team is rejected. Tutorial numeric step IDs remain compatible with saved progress. `FORMATION_RULES.md` and `RULE_DECISIONS.md` describe implemented board rules; COMBAT_EFFECTS.md defines cover, movement and representative synergy rules.

Debug and smoke controllers are compiled only for Editor/development builds. The non-development Options screen provides a basic audio toggle and Back button; persistent settings/accessibility/account UI are not complete.

`battle-actions-v2` implements distinct Poison/Burn/Bleed timing, validated effects/passives, cover/taunt/stealth, movement, ordered events and representative biome/class/role bonuses. See `COMBAT_EFFECTS.md` for source resolutions, supported behavior and later roster-specific omissions. `combat_examples.json` validates a separate canonical-profile encounter without entering summon pools. Omitted canonical stat objects may deserialize as all-zero nested values in JsonUtility; these count as absent, while nonzero competing stats are rejected. In-flight encounters remain memory-only; persistent/server battle authority follows the online milestone.

## Completion record

Step 4 adds **Team → Upgrade fighters** and **Home → Campaign**. The workshop shows rarity/evolution/level, current and next-level stats, scrollable basic/signature/passive descriptions and the gold price. Level/gold commit through IUpgradeService; the campaign picker consumes ICampaignService unlocks and never awards currency. First-battle onboarding opens Team and highlights the workshop; its banner hides during purchases, returns on close and permits continuing without spending. Saved tutorial step 9 resumes in Team without a schema change. See LEVEL_PROGRESSION.md and SLICE_CONTENT.md for tuning, source boundaries and compatibility. The 2026-09-15 guided-upgrade APK passed the full seven-stage native functional route, practice/relaunch checks and phone/tablet-sized UI review on isolated emulator-5584. Current APK hash, test counts, evidence and emulator limitations are recorded in PROJECT_LOG.md; human pacing and physical-device performance remain unverified.

Do not infer success from the presence of commands or tests in this guide. Record the actual executed test counts, build outputs and device observations in `PROJECT_LOG.md`, including failed attempts and unresolved limitations. Initial historical results remain in `AUDIT_VALIDATION.md`.

## Isolated Android runtime validation

After Unity batch work has exited, run `./tools/Start-ValidationEmulator.ps1`. It launches the existing `Medium_Phone` AVD read-only on `emulator-5580`, with software rendering, two CPU cores and no saved snapshots. It waits for `sys.boot_completed=1`, aborts after five minutes, and kills only its own process tree on failure. The AVD's saved configuration and data are not edited. Logs are under `artifacts/device-5580`.

Use `-Port 5582` (or another unused supported even port) to keep a later verification separate; use its exact serial for every adb command. Step-3 evidence is under artifacts/device-5582. A boot-complete property alone does not mean system services or the game have finished starting; inspect a current frame and logs before input. Recorded emulator startup stalls mean these runs cannot establish physical-device startup/performance quality.

Once boot completes, use the bundled adb to install the current APK on that exact serial. Validate onboarding, navigation and relaunch with real input; save screenshots and relevant Unity logs. Stop the temporary emulator with `adb -s emulator-5580 emu kill` afterward. A successful build or button-event PlayMode test alone does not establish Android touch, rendering or lifecycle correctness.
