# Battle sessions, commands and events

Rules version: `battle-actions-v2`. Build-order item 2 follows the user's confirmed contract: 12 hex spaces per side, five fighters, landscape presentation, automatic basics and manual signature skills with Auto. Effect/passive/synergy rules and source resolutions are defined in `COMBAT_EFFECTS.md`.

## Public session contract

`new BattleSession(data, player, enemy, seed, maxActions, auto, encounterId, wave)` creates one wave. Public sessions default to Auto off, clone input fighters/statuses/passives and snapshot skill definitions. Changes to caller-owned units or skills cannot alter an active session. `GetState` and `InitialState` expose immutable detached presentation data. No frame clock or animation timing drives the rules. UI must not mutate combat objects or award currency.

`GetFormationBonuses(side)` returns a detached list of active supported bonus IDs; presentation can refresh it after actions without reproducing eligibility rules. Fighter snapshots also expose basic/signature skill IDs.

`Step()` resolves one actor's action, including start effects, skill choice, effects and status aging. It returns only that action's ordered events. `Events` holds the entire ordered wave history, including commands and terminal events. A terminal session rejects commands and further steps are empty no-ops. Results distinguish Running, Victory, Defeat, Draw (both sides dead), and Timeout. Eliminating the enemy on the final permitted action is Victory, not Timeout; a simultaneous lethal reflection can produce Draw.

The action limit counts actions across both sides, including skipped turns. Gauge time advancement is part of selecting the next actor. Zero allowed actions ends an otherwise live encounter as Timeout; invalid negative limits or invalid sides fail before simulation.

## Signature commands and Auto

- `QueueSignature(instanceId)` accepts only a living player fighter with a distinct, known signature and no existing queue. The request may be made before the skill is ready. It waits through insufficient energy, cooldown and Silence; Stun/Freeze skip the owner's action. Basic attacks continue while the queued signature is ineligible.
- The queued signature is consumed on the next eligible owner action. Energy is spent once, only when cast. It does not create an extra action or interrupt an enemy action. Skill definitions determine targeting; manual target selection is not introduced.
- `CancelSignature(instanceId)` withdraws a pending request without spending energy.
- `SetAuto(true)` allows eligible signatures automatically. Turning Auto off resumes manual signature selection; queued requests remain until used/cancelled. Enemy signatures always use automatic eligibility.
- Death or battle completion clears pending requests with cancellation events. Duplicate, unavailable, enemy, unknown or terminal commands are rejected without changing the accepted-command log or events.

Every accepted command records its sequence, completed-action boundary, kind, fighter ID and Auto setting. Replay applies those commands in order at the same action boundaries, using identical original combat inputs, skill content and seed. The current tests establish reproducibility within the tested Unity/.NET runtime. System.Random and float gauges do not establish cross-runtime determinism; a portable RNG and complete versioned encounter payload remain required before authoritative online battle verification. The public session exposes its seed; campaign batches record their seed and rules version separately.

## Action lifecycle and source resolution

1. Advance living gauges by the minimum whole ticks needed for an action; select by gauge, speed, side and slot.
2. Emit action start and reset the selected gauge to **zero**, as the stat-sheet Spore Gauge section specifies. This corrects the earlier unapproved overflow-retention proposal under RULE-007.
3. Grant 20 start-turn energy (capped at max energy), then reduce positive signature cooldown by one. These happen even when control or subsequent damage prevents acting, matching the existing lifecycle now covered by fixtures.
4. Process owner-turn statuses. Lethal damage prevents skill execution and later regeneration. Otherwise select an eligible requested/automatic signature or a basic.
5. Resolve each skill effect against its target snapshot. A damaging effect that actually removes HP or shield grants the recipient 10 energy and the attacker 5; a killing hit grants 15 instead of that hit's 5. Thus multi-hit/multi-target skills can generate multiple gains. These are explicit current application rules, not a claim that the sheet specified every multi-hit edge case. Status ticks/reflection grant no additional hit energy and reflection cannot recurse.
6. Age statuses that existed at action start through that action, including skipped actions; emit tick/expiry events. New statuses start aging on the next owner action. Emit action complete and evaluate victory/defeat/draw/timeout.

The v2 lifecycle additionally runs encounter/owner-turn passives and formation bonuses, distinguishes Burn and action-triggered Bleed, and resolves control, cover, dodge, critical hits and bounded reactions. `COMBAT_EFFECTS.md` specifies their exact trigger order. Permanent formation saves are unchanged.

## Event and identity contract

Events cover battle/action boundaries, gauge, energy and cooldown changes, skill use, damage, healing, shields, status application/refresh/aging/expiry, skipped actions, deaths and command changes. Each event has an increasing sequence, action index, kind, optional source ID, optional immutable target state, amount, detail and outcome. Damage amounts include absorbed shield plus HP removed; the target snapshot distinguishes the remaining HP and shield. Energy amounts are signed changes. Gauge time-advance amounts count ticks; reset events carry the resulting gauge in their snapshot. SkillUsed amount is 1 for a signature, 0 for a basic.

Content IDs are not fighter identity. Within an encounter, players use `encounter/player/initialSlot` and enemies use `encounter/wave/N/enemy/initialSlot`. Repeated enemy content has distinct instances; player identity persists across movement and waves. Campaign begins with a new GUID encounter ID; standalone session callers supply their own encounter scope.

Initial state plus ordered target snapshots lets presentation reconstruct HP, shield, energy, cooldown, positions and statuses without recomputing combat. InitialState is the pre-entry-effect state; consume initial Events to apply HP bonuses/start passives. Event status lists are immutable snapshots and include source instance IDs. DoT source role/multiplier remains in domain status state across waves; ticks can have no actor while retaining that provenance. Additional events include PassiveTriggered, SynergyActivated, Redirected, Moved, Miss, Critical and EffectBlocked.

## Interactive campaign application contract

`ICampaignService.Begin(stageId, seed?, auto=false, maxActionsPerWave=200)` validates unlock/team and captures combat content, all wave fighters and rewards. It returns a `CampaignSession` with its current `Battle`, seed, encounter ID and wave index. `Step()` advances one action. Signature/Auto commands go to the current Battle. `AdvanceWave()` succeeds only after Victory with another wave available, preserving player state and Auto while clearing wave-local signature queues. Each wave retains immutable initial/final states, events and accepted commands.

`Complete()` rejects running/incomplete encounters and cancelled runs. Only all-wave Victory is reward-eligible. It reads the latest save, merges the first-clear reward, then writes once. Concurrent local encounters clearing the same stage award once; an intervening team/summon write is retained. A failed storage write leaves completion retryable; successful repeated Complete calls return the same result without another write. Draw, Defeat and Timeout settle without rewards. `Cancel()` prevents settlement. This is a single-threaded local adapter; a future server must supply transactional concurrency and authority.

`Run(stageId)` remains an automatic synchronous adapter for QA and noninteractive callers. `BattleSim.RunBattle` retains its legacy auto bool/input-mutation contract. The landscape BattleSceneController now uses Begin/Step/AdvanceWave/Complete directly and consumes ordered snapshots for visible manual signatures, Auto and animation. BATTLE_PRESENTATION.md defines controls, pause/focus handling, fast completion and retryable settlement. No wallet writes belong in presentation. Sessions are not saved or resumable across process restarts.

Tests in BattleSessionTests cover queued skills, cooldown/Silence, Auto/cancel, enemy policy, distinct outcomes, terminal no-ops, lethal ticks/reflection, identities, input/content/event isolation, source gauge reset, hit/kill energy and accepted-command replay. Existing campaign first-clear tests also verify ordered wave results and persistent player instance IDs. Actual run results belong in PROJECT_LOG.md.
