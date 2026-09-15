# Combat foundation — build step 2

Implemented rule set: `battle-actions-v2`, 2026-09-14. The stat sheet's glossary (page 1), structured Tank rows (pages 3–4), class/role table (page 8) and biome table (page 10) supply the source mechanics. This document records engineering resolutions where those sections omit timing or disagree. These are implemented choices under the user's build request, not additional user approvals or a claim that every number appears in the source.

## Scope and ownership

`BattleSession` owns one wave, commands and immutable events. `CampaignSession` owns the multi-wave encounter and settlement. `BattleSim`/`BattleEffects`, `CombatEffectRules` and `FormationBonuses` own rules. Presentation consumes events; saved formation and wallet values never change in response to animation callbacks. Save schema remains 5; in-flight sessions are memory-only and a process restart abandons the fight without a reward. Server-authoritative verification and persistent encounter resume belong to the online milestone.

The step-2 gate is a complete interactive combat foundation with representative effects/passives/synergies and executable kits. It is not the complete authored roster, Core/equipment implementation or final balance. Those remain the content/progression steps, while landscape, animation and visible controls are step 3.

## Effects and duration

| Rule | Implemented behavior |
| --- | --- |
| Poison | Percentage of victim's current maximum HP at owner-turn start; bypasses DEF. |
| Burn | Flat authored damage at owner-turn start; presence reduces effective DEF by 10%, once regardless of stack count. |
| Bleed | Flat authored damage on an attempted skill action, after control checks, before skill effects. No tick during Stun/Freeze skips. Lethal Bleed prevents effects. |
| Regen | Percentage of maximum HP at owner-turn start. No resurrection; HealBlock blocks recovery. |
| Freeze / Stun | Skip owner's action; start energy/cooldown and duration aging still occur. Freeze applications may coexist with independent durations. Neither ends when hit. |
| Silence / Charm / Confusion | Silence locks signatures. Charm forces basic selection against living teammates other than self. Confusion makes that same change with a seeded 50% roll. A lone charmed fighter has no hostile target. |
| Root | Blocks movement and all dodge checks; does not prevent attacking. |
| Stealth / Intangible | Stealth excludes ordinary single-target hostile selection and ends after an attacking skill. Intangible excludes hostile selection and blocks all incoming effects/damage for its duration, including AoE. |
| Vantage / Cloak | Vantage bypasses Stealth and dodge checks. Cloak consumes one application/stack to dodge one damaging effect. Sure-hit and Root bypass dodge without consuming Cloak. |
| FrostShield / Immunity | FrostShield is a status paired with a positive shield and prevents Freeze while shield remains. It expires when shield is depleted. Immunity blocks debuffs, not damage or Dispel. |
| Immortality | On lethal damage, consumes one application/stack and leaves 1 HP. A later hit can kill. |
| Reflect / Thorns | Reflect returns the configured fraction of the next damaging hit then expires. Thorns persists for its duration. Both use actual shield + HP damage, bypass DEF on return, and never create another reflection or hit-energy loop. |
| Stat changes | ATK/DEF/POT/SPD increases/decreases compose additively against the base stat; SPD has minimum 1, other effective stats minimum 0. Burn's DEF reduction joins this sum. |
| Vulnerability / DamageReduction | Multiply incoming damage after DEF/biome calculation. Reduction floors at zero damage. Brittle adds 25 percentage points to crit chance; Luminescence adds its authored potency. |

DoT/Regen/Freeze/Cloak/Immortality applications have independent duration; DoT potency adds linearly. Twenty concurrent applications of the same stackable status is the safety cap; further applications emit EffectBlocked. Other statuses refresh to the greater potency/duration. Duration `-1` means permanent for the encounter. Finite statuses present at turn start age at action end, including skipped actions; new applications begin aging on the next owner action. A refreshed pre-existing status still ages on that turn. Permanent statuses can be cleansed/dispelled normally. All statuses expire by owner actions, never wall time.

DoT applications snapshot source instance ID, role and DoT multiplier, retaining attribution when the source dies or a wave changes. Tick events themselves have no actor when the source is unavailable; their status snapshot carries provenance. Death never revives through heals or passives.

Damage defaults to ATK and can explicitly use POT. Heal defaults to caster POT; Shield defaults to target maximum HP. Heal/Shield may specify caster `MaxHP` or `TargetMaxHP`. Damage supports authored DEF bypass, shield bypass and sure-hit. HealBlock blocks healing. Cleanse removes debuffs, Dispel removes buffs, in application order; positive integer potency limits the number removed, zero removes all. Energy grants authored points with the normal cap; Cooldown reduces remaining cooldown to a minimum of zero; Gauge adds signed points within 0–2000. None of these grants an immediate extra action.

Hostile status chance retains the existing local formula: clamp(base chance + caster effective POT/1000 − victim effective POT/2000, 0, 1); an authored zero chance remains zero. Friendly application uses its authored chance. Guaranteed synergy applications still respect immunity. This coefficient choice is an explicit local balance rule, not a newly transcribed source formula. Chance is evaluated before application; no hidden extra resistance roll is added.

## Targeting, cover and movement

Single-target order: remove dead/Intangible targets; apply Stealth visibility; visible Taunt takes priority; otherwise use the skill's depth/lowest-current-HP selector. Vantage, or a Nuker with a living Scout against Marked targets, bypasses Stealth. AoE/row/random-two selection bypasses Taunt and Stealth but excludes Intangible. Targets are snapshotted per targeting rule for a skill, so repeated hits do not acquire replacement victims after death. Optional per-effect target overrides let one signature affect self and allies without UI logic.

Cover redirects one single-target damaging effect per guard per action, using the guard's defenses and shield. It never redirects AoE, healing or status application. Lowest-slot eligible guard wins; interception cannot chain. Default Cover protects adjacent cells. Cover potency 1 enables the named Snow-Bank Hermit's first-ally interception anywhere on its side, following the structured character row rather than conflicting generic role prose. A guardian still gets its ordinary hit-energy/passive reaction. RedirectReduction implements Porcelain Guard's 20% reduction when it receives redirected damage.

AdjacentAllies uses six-neighbor odd-depth-offset hex coordinates clipped to the friendly 3 × 4 board. Move selects a specified or seeded random empty friendly cell. Occupied/out-of-range destinations and Root reject movement with an event; no swap or pathfinding is implied. Dead cells are empty. Instance IDs remain unchanged through movement and waves. Combat movement never rewrites saved deployment.

## Passives and trigger order

Passives are validated authored definitions with stable IDs, target selectors and the same effect vocabulary as skills. Supported triggers: BattleStart (once per fighter per encounter), TurnStart (optional every-N owner turns), BasicHit, DamageDealt, DamageTaken and EnergyGranted. `Attacker` means the counterpart of a hit/grant trigger and is rejected where no counterpart exists. EnergyGranted receives actual granted energy; `GrantedEnergy` shield scaling converts its fraction of the recipient's capacity into caster maximum HP. A capped zero grant cannot trigger it.

At encounter entry, all HP bonuses apply before start passives, then Fortress shields apply. On an action: advance gauge; reset actor gauge; grant energy/decrement cooldown; run turn bonuses and passives; process start statuses/control; select and pay for skill; process Bleed; resolve effects. For a damaging effect: cover, dodge, DEF/biome/crit, reductions/shield/HP/Immortality, hit energy, reflection, hit synergies, BasicHit, DamageDealt, DamageTaken. A dead actor stops further effects. Finally age original statuses and evaluate the wave outcome.

Passive effects can deal damage, but nested passive triggers are suppressed. This bounds counter chains without arbitrary frame delays. Every passive, block, miss, critical hit, redirect and move has an event. Critical baseline is 0% chance and 1.5× damage; source bonuses add chance or critical damage. These baseline values are explicit foundation tuning because the provided sections do not fully specify them.

## Formation bonuses

Biome/class membership uses the deployed encounter team, including members who later die. Role pairs require living partners. Bonuses affect allies across the team, following the detailed table over conflicting class-only prose. Two-unit and three-unit biome stat tiers replace each other (5% then 10%); five-unit effects retain the three-unit stat benefit. Class four-unit effects retain two-unit benefits. Different families add their listed stat bonuses; no extra Kitchen neutrality penalty is introduced.

| Family | Implemented thresholds |
| --- | --- |
| Forest | 2/3: +5%/+10% max HP once at encounter start; 5: heal 5% max HP at owner-turn start. |
| Wetlands | 2/3: +5%/+10% POT; 5: +10% DoT damage. |
| Decay | 2/3: +5%/+10% ATK; 5: 10% lifesteal from direct damage. |
| Tundra | 2/3: +5%/+10% DEF; 5: separate 10% on-hit Freeze application, one owner turn. |
| Kitchen | 2: applied buff statuses and DoTs last +1 turn, excluding hostile control; 5: signature cooldown −1 with minimum 2 for positive cooldowns. |
| Tank | 2: +10% DEF; 4: each front fighter shields 15% max HP on every third owner turn. |
| DPS | 2: +10% crit chance; 4: +20% damage against targets below 40% HP. |
| Mage | 2: +15% DoT; 4: 10% chance to reset a used signature's cooldown. |
| Support | 2: +15% healing received; 4: positive heals grant the recipient 5 energy. |
| Tactician | 2: +10 flat SPD; 4: ignore 20% of enemy POT resistance. |
| Assassin | 2: +20 percentage points of critical damage; 4: +20% evasion before a fighter has taken its second owner turn. |

The Tank interval and Assassin duration are owner-turn resolutions of the sheet's ambiguous team-turn language. Initial HP bonuses and battle-start shields do not reapply to players at each wave. Biome-less units supply no biome bonus.

Implemented representative role pairs:

- Vanguard (Wall/Medic): +20% healing received and +5% DEF to allies.
- Sniper Nest (Nuker/Scout): Nuker bypasses Stealth/evasion against Marked enemies.
- Shatter (CC/Duelist): Duelist +25% damage against Freeze/Stun/Burn/Taunt/Root.
- Attrition (Taunt/DoT): hitting the Taunt-role fighter applies Poison to the attacker. The sheet omits potency/duration; this version uses 2% max HP for two turns.
- The Battery (Captain/Battery) and Overclock (Buffer/Battery): applied friendly buff statuses grant 5/10 energy respectively.
- Gourmet Line (Captain/Berserker): Captain's buff-status potency is 25% stronger on Berserker.
- Fortress (Wall/Cover): at encounter start, lowest-current-HP ally receives 20% of the lowest-slot Wall's maximum HP as shield.
- Brawling Pair (Brawler/Medic): a positive heal gives Brawler +15% ATK for one owner turn.
- Phalanx (Grunt/Wall): Grunt takes 30% less damage while a living Wall is above 50% HP.
- Purifying Ward (Purifier/Cover): successful cleanse grants living Cover-role allies 10% max HP shield.
- Shadow Step (Stealth/Stalker): an attacking skill begun from Stealth ignores DEF.
- Not Alone (Cover/Survivor): redirected damage is reduced by 20% and grants living Survivors 10 energy.
- Cataclysm (Saboteur/AoE): AoE-role fighter deals +20% damage against a target carrying a Saboteur-origin debuff.

Remaining content-specific pairs (Assassination, Plague Wind, Minefield, Chaos Theory, Vampiric Link, Vengeance, Armor Shred, Time Warp, Coup de Grace and Lifeline) need their corresponding complete character kits and/or unresolved source details. They are not active implicit role powers. Author them with the relevant roster content in step 4, extending the same effect/trigger boundary and adding fixtures. Range/pathfinding, bomb/spread/execute systems, Core modifications and equipment are not claimed as delivered by this foundation.

## Executable stat-sheet examples

`combat_examples.json` is versioned and validated on catalog load. `CombatExampleCatalog` creates a separate five-fighter test encounter from canonical stat profiles: Golden Chanterelle, Porcelain Guard, Peat-Moss Shaman, Snow-Bank Hermit and Grey Oyster. They include signature and passive definitions; examples never enter summon pools or saved ownership. Core abilities remain locked out of this fixture.

Source-specified values are preserved: Golden's 10% shield on damage; Porcelain's 50% self DEF/one turn and two-turn cooldown; Peat's 25 energy/four-turn cooldown and actual-energy shield; Snow's 20% caster-HP front-row shield/four-turn cooldown; Oyster's 15% shield every other turn and 25% self heal/three-turn cooldown. Explicit fixture tuning fills omissions: basics use 1× ATK, all signatures cost 100 energy, Sunlight Barrier shields 20% caster max HP, unspecified Taunt lasts one owner turn, and Peat selects the lowest-current-HP ally. These values require playtest balance in the content milestone.

The legacy Flare Bloom Burn payload changes from a percentage to a flat 6 damage so it remains meaningful under the new Burn semantics. This is prototype tuning, not a named canonical skill import. No saved unit IDs, rarity values, currencies or ownership are migrated.

Actual executed validation and limitations are recorded in PROJECT_LOG.md; presence of the tests alone is not proof of passing.
