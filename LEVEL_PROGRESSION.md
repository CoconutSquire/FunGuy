# FunGuy's level upgrade loop

Step 4 increment, 2026-09-14. Economy version **slice-levels-v1**, schema 1. Content: `FunGuy's/Assets/_Game/Resources/GameData/level_progression.json`.

## Player flow

Open **Team → Upgrade fighters**. Choose any owned fighter from the paged list, compare current and next-level HP/ATK/DEF/SPD/POT, and press the button showing the gold price. A successful operation saves immediately. The next battle uses that level; changing or closing the workshop does not change formation. The panel distinguishes acquisition rarity from evolution stars, shows the current purchase cap, and lists basic/signature names and signature energy/cooldown. Team cards show rarity tier and owned level.

Gold comes from the existing first-clear campaign rewards and onboarding. Repeating a cleared stage remains practice and grants no additional currency. Insufficient gold disables the purchase and shows the shortfall. Each accepted purchase adds exactly one level. Closing/reopening or loading the save retains both level and gold.

First-battle onboarding now explains the first-clear policy and takes the player to Team with Upgrade fighters highlighted. Opening the workshop hides the guide; closing restores a continuation prompt to Home → Campaign. A purchase is optional, so players with no gold can still complete onboarding. Existing tutorial IDs 7–9 are reused; ID 9 resumes in Team, completed tutorials remain complete, and no new reward or save field is added.

## Rules and source boundary

The stat sheet supplies class growth, named Tank profiles and evolution caps; it does not provide a character level-up gold-price curve. The following are **authored slice tuning**, not transcribed canonical costs:

- Gold price from current level L to L+1: `25 + 10 * (L - 1)`, recorded as an explicit table.
- Workshop purchase cap: level 20, additionally bounded by the owned evolution cap. The sheet's 100/120/140/160/180/200 caps remain unchanged.
- First upgrade for a level-1 summon costs 25 gold; 1→5 costs 160; 1→20 costs 2,185. Onboarding starters already begin at level 3, so their first upgrade costs 45 gold. A fresh account after first-clear/tutorial rewards has 250 gold, enough to raise all three starters to level 4 for 135 total. The seven-stage chapter supports an earned-gold route with three level-9 starters at the boss; it is not intended to max the entire roster. See SLICE_CONTENT.md for the tested progression policy and remaining human balance review.
- No XP consumption, evolution purchase, gear, Core upgrade, copy consumption or premium-spore cost is introduced. Those saved fields are preserved.

Preview and battle share `CombatUnitFactory`. Named profiles use `stat-sheet-v1`; step-4 `class-growth-v1` definitions use explicitly authored bases with the same class growth/evolution rules, including owned stars. `SLICE_CONTENT.md` records the ten draft kits and their source boundary. The workshop does not rename an owned fighter as a different source character. Core and equipment remain outside both previews and battle stats.

Existing levels above 20 are retained and cannot be purchased further through this version. Invalid canonical levels beyond their evolution cap produce an unavailable preview instead of silently clamping or spending gold. The catalog rejects missing/duplicate/out-of-range levels, nonpositive costs and unsupported versions before startup completes.

## Architecture and persistence

- **Domain:** `LevelProgressionRules` snapshots the versioned table. Existing stat rules own growth/evolution; the purchase cap is separate.
- **Application:** `IUpgradeService.Preview(characterId)` returns a detached immutable read model. `LevelUp(characterId, expectedLevel, rulesVersion)` reads the latest save, validates unique ownership, level, stars, version and gold, calculates the resulting preview, then writes one full snapshot. The client supplies neither price nor stat values.
- **Presentation:** `UpgradePanelView` explicitly builds/binds the landscape menu modal. `UpgradePanelController` owns selection, paging and feedback, and calls the application interface. The existing Team menu adapter owns the entry button; it does not mutate currency or level.
- **Storage:** existing schema-5 `OwnedUnit.level` and `PlayerSave.gold` suffice; no save migration or stat-profile/character-ID remap is needed. Failed local writes leave published Game.Save unchanged. Newer rewards/team changes are retained because the operation reads current state before committing.
- **Future server:** authenticate ownership and calculate costs/stats server-side, use transactional concurrency control and an idempotency ledger. Expected level/version rejects a repeated old request locally; it is not a claim of cross-device synchronization or a substitute for durable server idempotency.

The menu remains compatible with a later prefab view: retain IUpgradeService and the explicit controller binding when replacing the view factory. No new art dependency is required; the existing editable fungus silhouette is reused.

## Verification and remaining work

`UpgradeServiceTests` covers arithmetic/shared stat output, preservation of unrelated fields, canonical profile/evolution, stale/duplicate requests, wallet changes after preview, exact affordability, failed write/retry, level caps, retained older levels and invalid content/ownership. PlayMode covers reward gold → upgrade → reload → battle, plus paging, insufficient gold, cap and empty roster. Validation: **117/117 EditMode and 7/7 PlayMode passed**, with visual review at 2400×1080 and 1600×1200. See the [saved upgrade screen](docs/references/upgrade-workshop-step4.png). These checks do not certify an Android install or a timed full slice; PROJECT_LOG.md records the initial fixture failure and corrected run.

Step 4 includes the draft roster/campaign content described in SLICE_CONTENT.md and guided first-upgrade onboarding. Human balance/pacing review and timed Android slice verification remain. Unique character art and audio remain in the content/polish path. Consult PROJECT_LOG.md for current tests and APK scope; this first increment's evidence does not certify later source.
