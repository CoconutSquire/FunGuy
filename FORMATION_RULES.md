# Formation rules and integration

Version: formation-v2, save schema 5. The user's full-board reference supersedes the previous two-front/three-back restriction. See [the supplied reference](docs/references/full-board-reference.png). The image establishes a larger hex battlefield with empty placement options; it does not establish an exact cell count or every targeting rule.

## Board and team size

The approved topology gives each side **12 hex spaces: three depth columns, four staggered lanes**. Team capacity remains **five fighters**, independently of space count. The user explicitly confirmed twelve spaces and five fighters in build step 1. Detailed targeting behavior below remains the documented implementation contract. All owned fighters may occupy any friendly space. At least one is required to battle. The player cannot place a sixth fighter or deploy on the enemy half.

Player formation, facing right toward the enemy:

```text
Rear            Middle            Front -> Enemy
                middle_1
rear_1                            front_1
                middle_2
rear_2                            front_2
                middle_3
rear_3                            front_3
                middle_4
rear_4                            front_4
```

The enemy board mirrors this horizontally. Each side has independent team-local positions. Depth is 0/front, 1/middle, 2/rear; lane is 0-3. Saved slot = depth * 4 + lane. Content IDs are front_1..front_4, middle_1..middle_4 and rear_1..rear_4. Numeric slots and IDs are versioned contracts; changing topology requires migration. `FormationRules.Capacity` is the fighter cap; `SlotCount` is the number of spaces. The simulator and content validator retain the five-fighter cap on each side.

## Placement and persistence

`ITeamService` owns membership and placement as one transaction. Place assigns a bench fighter, moves a selected fighter into an empty space or swaps selected fighters. Assigning a bench fighter to an occupied space replaces that member without deleting ownership. A full team cannot add a bench fighter into an empty hex; the UI explains that an occupied hex must be replaced or a fighter removed. SwapSlots exchanges occupants or moves into a gap. Removing a member does not move anyone else. Invalid positions throw; unavailable/no-op operations do not write; failed storage cannot publish partial changes.

GetFormation returns detached assignments. activeTeam remains the membership compatibility list; formation records exact positions. Auto Fill retains rarity/level/ordinal-ID ranking and is not a tactical optimizer. Its default positions are 0, 2, 8, 9, 11; Add/repair use these first, followed by the remaining spaces in numeric order. Identical Auto Fill membership preserves custom placement. Clear stays empty after onboarding.

**Schema-4 migration:** old slots 0,1,2,3,4 map to 0,2,8,9,11 respectively, retaining front/rear intent and occupied-slot order. Old invalid indices are discarded before new-board repair; an old invalid 7 must not become a valid middle space accidentally. Versions 3 and older first use their membership order as the old five-slot formation and then follow the same mapping. Currency, owned units, progression, tutorial and claims are retained. Schema-5 sparse positions persist without remapping on subsequent loads/saves. PlayerPrefs key names remain unchanged. Future-version saves remain rejected.

Enemy wave entries may specify any current slotId. Omitted positions use the same default placement order around reserved explicit cells. Legacy front_left/front_right/back_left/back_center/back_right remain aliases for their migrated cells, preserving optional content compatibility without increasing the stage schema. Duplicate explicit positions, unknown names and oversized waves fail validation.

## Targeting contract

- EnemyFront / EnemyFrontRow target the nearest occupied depth: front, then middle, then rear. Single targeting chooses its lowest occupied lane; row targeting includes only that depth.
- EnemyBack / EnemyBackRow target the farthest occupied depth: rear, then middle, then front. These are explicit skill selectors, not an automatic class ability.
- AllyRow affects living allies in the actor's exact depth, including the actor. Middle and rear are separate groups.
- AllEnemies / AllAllies use living fighters in numeric slot order; seeded RandomEnemy2 uses that stable order.

Depth is measured relative to each team's facing. Lane does not yet add range, line-of-sight or nearest-lane targeting. A front hex grants no invented defense multiplier. Death leaves cells empty; subsequent skills resolve living targets again. Multi-effect skills retain their target list and do not redirect after a target dies mid-skill. Equal gauge/speed ties use side then slot. Each new wave preserves surviving player positions and assigns a separate enemy board. Battle snapshots never overwrite permanent placements.

## Presentation and integration

Team renders twelve selectable hexes, a five-fighter count and a paged owned roster. Tap a hex and then a fighter to place; tap two hexes to swap/move; tap the same hex twice to cancel. Remove empties the selected occupied hex. Hex hit testing excludes transparent corners. Without a selected hex, roster controls retain membership toggling. Empty teams cannot start a battle.

Battle previews both mirrored twelve-space deployments with the selected stage's first enemy wave. Starting a battle replaces this preview with live ordered event playback, including HP, energy, shields, statuses, movement and defeat. The reusable landscape BattleScreen/BattleFighter prefabs use the same enemy factory and position assignment as campaign combat. The generated kitchen environment is separate from code-rendered hexes and fighter views. Team and surrounding menus also fit the landscape safe area. See BATTLE_PRESENTATION.md for controls and asset replacement contracts; bespoke roster art remains later content work.

## Remaining rules and validation

`COMBAT_EFFECTS.md` defines implemented biome/class bonuses, fourteen representative role pairs, cover, taunt, stealth, empty-cell movement and six-neighbor adjacency. Movement preserves instance identity and never changes saved deployment; step-3 views animate the resulting slot snapshots. Range/pathfinding and roster-specific bomb/spread/execute mechanics remain later content work.

Tests cover all twelve destinations, five-member enforcement with replacement allowed, failed transactions, migration and repeated sparse saves, front/middle/rear fallback, exact-depth ally effects, content aliases and placement-dependent combat outcomes. PlayMode exercises outer and middle hexes, paging, swaps/removal and scene persistence. See PROJECT_LOG.md for actual execution results and APK coverage; the earlier five-slot APK is historical evidence only.
