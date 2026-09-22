# Step 4 playable content

Working content versions: **garrett-roster-v1** and **garrett-roster-campaign-v2**. Updated 2026-09-21. This is the current prototype content contract, not a claim of final character art, complete bespoke kits, final balance, or a finished commercial game.

## Roster authority and runtime conversion

`Assets/_Game/Resources/GameData/funguy_characters.json` is the authored source supplied for the current roster. It contains **71 characters**: 20 R, 50 SR and 1 UR. Runtime `characters.json` contains those same 71 player-facing identities in the schema consumed by the battle/summon/progression systems. The previous ten test identities are no longer in the runtime roster.

The current conversion preserves stable authored IDs, names, rarity tiers, biomes, classes, roles, synergy tags, HP/ATK/DEF/SPD/POT/BST values and the authored passive/signature text. `class-growth-v1` continues to apply the shared class growth/evolution rules. The imported roster currently uses the temporary executable `imported_character_basic` and `imported_character_signature` skills so every character can enter battle before all 71 bespoke kits are implemented. Authored ability text is retained on each character definition and must not be mistaken for implemented mechanics.

The separate legacy placeholder enemy catalog has been retired from bundled content. `characters.json` intentionally has an empty `enemies` array. The legacy C# `EnemyDef` compatibility type remains temporarily so old fixtures/tools can be migrated without a breaking serialization rewrite, but the shipped campaign no longer consumes it.

## Placeholder battle visuals

All 71 roster characters and roster-backed campaign opponents use the reusable code-native `FungusPortrait` battle placeholder until Garrett's final sprites/rigs are ready. `BattleFighterView` chooses a stable placeholder variation from the content ID/class, colors it by biome, and marks enemy-side instances visually. Cosmic has an explicit placeholder color.

This means adding a roster character does **not** require copying an old character GameObject or maintaining a per-character placeholder sprite asset. Final presentation assets can later replace the placeholder renderer without changing combat IDs, stats, ownership, campaign references or save data.

## Campaign

Stage IDs, unlock order and first-clear reward amounts remain stable so existing save history is preserved. The stage wave field is still named `enemyId` for schema compatibility, but in **garrett-roster-campaign-v2** it references a real `CharacterDef.id` from the 71-character roster.

| Stage | Encounter | First-clear gold | Current opponent focus |
| --- | --- | ---: | --- |
| 1-1 | Forager's Trial | 50 | Early R roster fighters |
| 1-2 | Mushroom Line | 150 | R tank/support/damage mix |
| 1-3 | Spore Cupboard | 200 | Mixed R roles and sustain |
| 1-4 | Frozen Shelf | 250 | Tundra roster fighters |
| 1-5 | Kitchen Muster | 300 | Kitchen tank/support/damage mix |
| 1-6 | Before the Bloom | 400 | Full mixed Kitchen wave |
| 1-7 | Nebula Convergence | 600 | Nebula Cordyceps with Kitchen support |

Campaign construction, battle preview, smoke tests and content validation all resolve these wave IDs through `GameData.Characters`. The five old placeholder enemies and their bespoke enemy skills are no longer bundled or referenced by a stage.

Player state continues across waves exactly as before: HP, energy, cooldowns and statuses are carried forward. First-clear settlement remains owned by `ICampaignService`; replaying a cleared stage remains practice and cannot duplicate the reward.

## Save and compatibility behavior

The seven stage IDs are unchanged, so existing `clearedStages` records remain meaningful. No save schema bump is required for this content replacement. Existing ownership, levels, stars, copies, XP, formation, currencies, tutorial state and banner history continue to use the 71 roster IDs introduced in the Garrett roster integration.

Because old placeholder player IDs and old placeholder enemy IDs are no longer current content, a local development save that was manually built around those removed IDs should be reset or migrated rather than treated as valid production ownership.

## Verification status and next gate

Historical 2026-09-15 evidence (122/122 EditMode, 9/9 PlayMode and the Android emulator chapter route) belongs to the previous ten-character/legacy-enemy content and is **not** a validation claim for this roster-backed campaign.

Repository-level static validation passed for the current source: 71 unique character definitions parse, all character skill references resolve, the legacy enemy array is empty, all 14 waves point only to existing roster characters, no retired enemy IDs/skills remain in bundled runtime data, explicit wave slots are unique, and all seven stage IDs/reward structures remain valid. The next runtime gate is a clean import under the pinned **Unity 6000.3.2f1**, followed by the full EditMode/PlayMode suites and a fresh campaign balance pass.

Until that runtime pass is completed, the new opponent levels and final encounter should be treated as functional provisional content. Do not tune final economy or difficulty from the previous ten-character balance results.

## Remaining content work

1. Run clean Unity 6000.3.2f1 compilation and the full automated suites against the 71-character roster-backed campaign.
2. Rebalance the seven stages using the actual Garrett starter/team flow rather than the retired placeholder enemies.
3. Implement selected characters' real basics, signatures and passives from the authored design, replacing the two shared temporary combat skills incrementally.
4. Replace code-native placeholder portraits with final roster art/animation while preserving stable character IDs and presentation mappings.
5. Perform human Android pacing/discoverability testing and record the evidence in `PROJECT_LOG.md`.
