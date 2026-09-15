# Landscape battle presentation

Step 3 implements the confirmed landscape, twelve-spaces-per-side, five-fighter, manual-signature/Auto contract. Gameplay rules remain in BATTLE_SESSION.md and COMBAT_EFFECTS.md. This document describes presentation, not additional combat rules.

## Ownership and playback

RuntimeSceneUiBootstrap instantiates `Resources/Presentation/BattleScreen.prefab` and explicitly configures BattleSceneController. BattleScreenView holds serialized controls and renders detached fighter snapshots. BattleFighterView owns interpolation, hit flashes, action lunges, damage/heal text and defeated styling. It never calculates damage, target eligibility or rewards.

Before Start, the board previews the saved deployment and first enemy wave without starting an encounter. Start calls ICampaignService.Begin with Auto off on a fresh scene entry. The controller consumes the current wave's initial events, requests one domain action only after its existing events have been displayed, and plays the resulting snapshots in order. Surviving state carries into the next wave through CampaignSession.AdvanceWave. A bounded event budget prevents fast completion from processing an unlimited encounter in one frame. Simulation uses no Unity frame time; presentation speed changes only playback and the wall-clock opportunity to submit commands.

After the final event, Complete settles through the application service. The result distinguishes victory, defeat, draw and action limit. First clears show saved rewards; repeat clears show practice. A failed Complete keeps the encounter available through Retry saving. UI errors after successful settlement must not be reported as a failed reward transaction. Next stage is enabled only when unlocked and onboarding is finished. Navigation and disabling the screen cancel unfinished encounters; no battle resume is promised after process termination.

## Player controls

- Basics run automatically. Each living fighter has a signature card showing its name, skill, energy and cooldown/queue state. Tap to queue its next eligible action; tap again to cancel. The command can be queued before full energy and does not grant an extra turn. Queues are wave-local.
- Auto toggles eligible player signatures. Enemy signatures remain automatic. Auto selection carries across waves and retries within this screen; re-entering starts with Auto off.
- Pause freezes event consumption and visual animation. Speed cycles 1×, 1.5×, 2×. Losing application focus or entering the background pauses; Resume is required afterward.
- Tap a fighter for health, shield, energy, signature cost/cooldown and a scrollable list of statuses/durations. Inspection pauses a running battle; closing it preserves a separately requested pause.
- Finish on Auto explicitly enables Auto and drains the same encounter/events quickly. It does not replace the session, reroll the seed or award rewards itself.
- Motion toggles reduced animation and persists the integer preference `FUNGUY_REDUCED_MOTION`. Combat saves and schemas do not change. Color is supplemented by text/numbers for HP, energy, defeat and queue state.

## Layout and prefab authoring

BattleScreen and BattleFighter are reusable uGUI/TextMeshPro prefabs under `Assets/_Game/Resources/Presentation`. Hexes and pixel mushroom silhouettes are code-native Graphics. Their positions are derived from stable domain slots; pixel coordinates never become gameplay positions. Twelve visible spaces on each side remain independent of the five-fighter capacity. Front depths face the central divide, with middle/rear depths outward.

The shared design surface is 1600×900. LandscapeSafeArea fits it inside Screen.safeArea using the smaller width/height ratio. The environment can fill the outer screen; controls stay inside the fitted surface. Both landscape orientations are allowed in PlayerSettings, with portrait disabled. Home, Summon/reveal, Team, Options and tutorial overlays also fit this landscape surface through LandscapeMenuLayout; the welcome scene has a kitchen/title surface. Those surrounding menus retain their existing generated controls and explicit application bindings; converting their visual hierarchy to authored prefabs can happen with their content milestones.

Custom hex/portrait Graphics explicitly require CanvasRenderer. Fighter HP text has a dark backing and sufficient line height. Occupied cell captions hide to avoid competing with fighter names and return when movement vacates the cell. Wave initialization clears transient hit/pop-up state. The [five-versus-five editor capture](docs/references/battle-landscape-step3.png) shows the current readable board; those five enemy placements are a test-only fixture.

Rebuild the default prefabs intentionally with Unity menu **FunGuy's > Rebuild battle presentation prefabs** or:

```powershell
./tools/Invoke-Unity.ps1 -Task Presentation
```

This command overwrites the two generated prefabs with the editor source in `Assets/Editor/PresentationAssets.cs`. It does not run automatically on import. Make durable default-layout edits in that authoring source, or deliberately replace the generator workflow when hand-authoring final assets. Preserve component references and the controller's Configure contract. Generic UiPrefabBlueprintBinder is not attached to this battle screen.

## Art provenance and replacement path

`Assets/_Game/Resources/Presentation/kitchen-battlefield-v1.png` is an original opaque kitchen environment generated on 2026-09-14 with the built-in image generation tool under the imagegen skill. The tool has no explicit model-selection argument; no specific 2.5 model selection is asserted. The supplied historical reference at `docs/references/full-board-reference.png` informed the kitchen/board direction; it is not loaded into the game or used as baked-in controls.

Generation prompt:

> Use case: stylized-concept. Asset type: production 2D game environment backdrop for Funguy's landscape hex formation RPG. Generate a wide 16:9 illustration, charming richly textured pixel-art fantasy mushroom kitchen, warm amber hearth, large simmering copper cauldron against the rear wall, shelves with jars and dried herbs, leafy windows on left, rustic wooden countertops at the edges. Slight elevated three-quarter camera looking down on a broad empty wooden tabletop battlefield occupying the central 75% width and lower 65% height. Keep this playable area visually quiet, medium dark warm brown, open and unobstructed. Rich painterly pixel-art detail concentrated along the top and outer edges. Cozy but adventurous, gold sunlight, subtle green moss, copper and walnut colors. This is ONLY an environment asset. Absolutely no text, letters, numbers, logos, UI, buttons, health bars, characters, mushroom people, board markings or hexagons; engine will render the actual board and fighters separately. Strong readable empty center, no huge foreground objects. Opaque full-frame landscape background, high resolution.

FungusPortrait supplies simple biome-colored mushroom silhouettes with shield/staff/blade variants. These are functional presentation assets, not unique final character illustrations. Replace the portrait graphic within BattleFighter while preserving the view/state contract as the authored roster arrives. This avoids baking units, board markings or controls into environment images. Bespoke character animation, skill art, additional biome environments and audio remain content/polish work.

## Verification

Run EditMode and PlayMode with the repository runner. Optional `-Task PlayMode -CaptureUi` renders real canvases at 2400×1080, plus battle examples at 1920×1080 and 1600×1200. Tests exercise onboarding completion through live playback, pause/inspection/focus, queue/cancel, speed/Auto/reduced motion, final displayed state, reward replay, next-stage selection and cancellation on navigation. Test preferences are restored afterward.

Actual counts, screenshot findings, build/device evidence and outstanding limitations are recorded in PROJECT_LOG.md. Callback tests and editor renders are separate from real device touch, safe-area and performance validation.

Step-3 completion evidence: 109/109 EditMode and 5/5 PlayMode passed; AndroidReleaseCheck produced a 59,932,358-byte non-development IL2CPP/ARM64 APK. Read-only emulator-5582 passed touch onboarding, Rear 4 placement, manual queue/cancel, pause/inspection, Auto/speed/reduced motion, victory rewards and force-stop/relaunch persistence. Captures and logs are in artifacts/device-5582. The emulator experienced Android system-service startup stalls; no physical-device performance, notch or reverse-rotation certification is implied. Surrounding menus retain prototype art and camera-colored gutters, while the battle environment fills the screen. Full authored roster/menu art and audio remain later milestones.
