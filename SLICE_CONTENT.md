# Step 4 playable content

Working content versions: **slice-roster-v1** and **kitchen-slice-v1**. Updated 2026-09-15. This is authored draft balance for the playable slice, not a claim of final art, full progression or a finished commercial game.

## Source and compatibility

The source PDF's structured named rows cover Tanks; its class templates/growth cover six classes, and its older narrative roster conflicts with those rows. The earlier optional source question received no additional material. Continue with draft kits for the existing ten characters rather than silently treating old narrative names as canonical entries. Their names, IDs, acquisition buckets and banner odds/pity remain unchanged.

`class-growth-v1` combines each character's explicit base distribution with the canonical class growth and evolution rules from `stat-sheet-v1`: growth starts after level 1, HP/ATK/DEF/POT use the star multiplier, and SPD is base + 15×stars. The six class templates are source-backed; these characters' class/role assignments, specialized base distributions, abilities and encounter balance are authored drafts. Named `sheet_*` profiles and `combat_examples.json` remain independent references.

R Orbi and Kira are biome-less with 500 BST bases, following the source's Rare identity/budget. Their specialized draft distributions are 90/120/70/110/110 and 100/70/90/100/140 respectively (HP/ATK/DEF/SPD/POT). Elemental SR/UR characters have 600 BST bases; Kitchen Zenith has an explicit 575 BST base with the Kitchen tax already included: 75/155/80/170/95. These are authored distributions, not a runtime rarity multiplier or a second Kitchen tax. Existing numeric buckets 3/4/5/6 still feed the unchanged summon/pity adapter; player-facing character labels use R/SR/UR and show evolution separately. Source review corrected Orbi/Kira's old elemental aliases, so the three guaranteed starters no longer activate the two-Forest HP tier.

There is no character-ID replacement or destructive save migration. Valid existing owned levels, stars, copies, XP, Core/gear placeholders, formation, currencies, tutorial state, pity and first-clear history are retained. Applying the updated content changes derived combat stats and abilities, like a balance patch. Canonical level/star bounds now apply to these definitions; invalid out-of-range debug/corrupt progression is not silently clamped. Existing first-clear IDs 1–5 remain claimed even though their encounter names, layouts and some reward values changed. No retroactive reward difference is granted. New stages 6/7 extend the existing unlock chain.

## Roster

Every row has a distinct basic, signature and passive, with descriptions in the JSON. Full numerical effects and targets are in `characters.json` and `skills.json`; the workshop shows them in a scrollable panel.

| Character | Class / role | Biome | Play pattern |
| --- | --- | --- | --- |
| Puffmage Orbi | Mage / AoE | Biome-less | POT basic, teamwide Poison burst, opening energy |
| Barkrot Thane | Tank / Taunt | Decay | Taunt and self shield; weakens attackers |
| Mosswhisper Luma | Support / Medic | Forest | Basic healing, team heal/Regen, periodic cleanse |
| Shroomblade Rowan | DPS / Duelist | Forest | Two-hit basic, focused burst/ATK buff, periodic self heal |
| Mirelord Gloomrot | Tank / Saboteur | Decay | DEF/POT debuffs and opening shield |
| Frostcap Serin | Tactician / CC | Tundra | Slow, two-target Freeze, opening energy |
| Glacierstalk Bronn | Tank / Cover | Tundra | Adjacent interception and frontmost-row shields |
| Embercap Rugo | DPS / Nuker | Wetlands | Focused 2× ATK signature with Burn and an opening ATK buff |
| Ashmote Kira | Support / Buffer | Biome-less | Ally energy and team ATK/POT buffs |
| Starspore Zenith | Assassin / Stalker | Kitchen | Rear targeting, defense/shield bypass and opening Stealth |

Class/biome/role bonuses remain the existing encounter rules. Status descriptions give **base** chances; POT/resistance and immunity affect actual application. Bronn's Cover requires adjacency and intercepts once per action. Rear targeting still respects Taunt. Enemy wave transitions preserve player HP/energy/cooldowns/statuses; no between-wave full heal has been added. Core/equipment effects are not implied by these kits.

## Campaign

All seven stages use the current kitchen environment. The five combat biomes belong to combatants; a cold cellar does not require a new renderer or change the biome loop.

| Stage | Encounter | First-clear gold | Teaching purpose |
| --- | --- | ---: | --- |
| 1-1 | Pantry Threshold | 50 | Basics and the full board |
| 1-2 | The Spoon Line | 150 | Shields, Taunt and queued signatures |
| 1-3 | Spore Cupboard | 200 | Poison, healing and upgrades |
| 1-4 | Cold Cellar | 250 | Slow and biome choices |
| 1-5 | The Simmering Kitchen | 300 | Team Burn and sustain |
| 1-6 | Before the Boil | 400 | Mixed threats and preparation |
| 1-7 | The Cauldron Keeper | 600 | Boss shield, pressure and Burn |

The keeper starts with a 10% max-HP shield, gains 10% ATK for two owner turns every third turn, and uses Boil Over to damage/burn all opponents. It uses ordinary visible energy/cooldown/status rules; no hidden phase or unsupported boss mechanic is claimed. The final wave includes two supporting enemies.

Home → **Campaign** opens the authored chapter picker. It shows locked/new/cleared stages, encounter tips, wave count and first-clear rewards. Cleared entries explicitly offer practice. Unlock eligibility and reward grants belong to ICampaignService; opening/selecting a stage cannot award anything. Returning from Team keeps the current in-memory selection, and a fresh process offers the first uncleared unlocked stage. Battle's Next stage updates this selection. Save schema 5 remains sufficient because the cursor is navigation state, not progression.

CampaignSession captures the encounter content version for result traces. Changes to the roster or stage kit require a content-version review and corresponding balance fixtures; results remain local evidence, not trusted server battle verification.

## Verification and remaining step 4 work

The content harness uses the three guaranteed starters, their actual level-3 start, first-clear/tutorial gold and the real upgrade/campaign services. Its deterministic progression policy targets level `stage order + 2`, buying upgrades only when affordable. Twenty fixed seeds exercise the whole chapter independently of summons. The final Rare-aligned content passed **122/122 EditMode** and **8/8 PlayMode** tests: 140/140 upgraded stage victories and 20/20 unupgraded boss losses. Results are written to `artifacts/step4-content/campaign-balance.csv`; actual execution and tuning changes are recorded in PROJECT_LOG.md.

Final phone/tablet captures were visually reviewed for campaign opacity, readable kit descriptions and boss settlement. See the [campaign picker](docs/references/campaign-step4.png), [workshop kit panel](docs/references/roster-kits-step4.png) and [boss victory](docs/references/boss-victory-step4.png). Editor captures do not establish native Android touch or performance.

The guided-upgrade increment passed **9/9 PlayMode** tests, including purchase/save and saved-step completion with zero gold. Combat and content rules did not change; the 122/122 EditMode result above belongs to the preceding content run. The fresh AndroidReleaseCheck APK is `artifacts/AndroidReleaseCheck/Funguy.apk`, **59,967,496 bytes**, IL2CPP/ARM64, development disabled, built 2026-09-15. PROJECT_LOG.md records its hash and exact coverage.

That APK passed an agent-operated native acceptance route on isolated emulator-5584: fresh onboarding, guided purchases, sparse placement, a manual signature queue, Auto, all seven stages, boss settlement, practice without duplicate rewards and two force-stop/relaunch checks. Only earned gold and the three guaranteed starters were used for progression, reaching level 9 and finishing with **890 gold / 192 spores**. The summoned Kira remained level 1 and off the team. Phone and tablet-sized views and touch scrolling of the full kit description were reviewed. See [native upgrade](docs/references/android-upgrade-step4.png), [native boss result](docs/references/android-boss-step4.png) and [native cleared campaign](docs/references/android-campaign-step4.png).

Passing scripted balance scenarios or agent-operated Auto/Finish encounters does not establish a 15–30 minute human session, optimal strategy or final balance. First-upgrade guidance is now implemented: the first-battle tutorial explains gold, opens Team, highlights the workshop, hides the banner during purchases and lets the player continue without spending. Saved step 9 resumes in Team; completed tutorials remain complete. Remaining content work includes bespoke portraits/animation/audio and human playtesting. Physical-device performance remains a release gate: the API-36 software emulator used ARM64 translation and showed a System UI ANR before gameplay. No game exception was found in the captured Unity log. The ten previously deferred role-pair mechanics are not introduced unless selected kits need them. Production evolution/equipment/Core/economy and online authority remain later steps.

The native functional route above is complete. Human acceptance should now record the device/build, elapsed play time and any confusion:

1. Complete onboarding, open Campaign and clear Pantry Threshold using the guaranteed starters. Check that the first upgrade is discoverable without external directions.
2. Spend earned gold in Team → Upgrade fighters, return to the selected stage and compare manual signatures with Auto. Exercise sparse placement on the twelve-space board.
3. Continue through the boss, recording losses, upgrade choices and time spent in menus versus battles. Judge the 15–30 minute target from this evidence; do not pad combat to satisfy a timer without reviewing the experience.
4. Relaunch after an upgrade and a first clear. Verify level/gold, formation, unlocks and practice-only repeat rewards. Inspect native touch targets and readable kit scrolling on phone and tablet dimensions.

Functional coverage of these actions is recorded above; their discoverability, pacing and comfort for a human player remain unverified. Record that playtest in PROJECT_LOG.md before closing the step-4 pacing gate.
