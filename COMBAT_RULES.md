# FunGuy's canonical stat rules

Rules version: **stat-sheet-v1**. Implemented 2026-09-14 following the user's request to build from `Funguy_s Stat sheet.pdf`. Machine-readable source: `FunGuy's/Assets/_Game/Resources/GameData/stat_rules.json`, schema 1. This document covers unmodified character stats, evolution and biome damage. Equipment, Core, battle buffs and formation synergies are separate layers, not silently included in these numbers.

Source PDF SHA-256: `B74D42D0461B31603FCE0B6B95F9B1A2063F19D51162CDB96E990F9D5B5F92AF`. Reconcile a changed sheet against these tables and fixtures before changing a shipped rules version.

## Source precedence and decisions

Use the structured character rows and Basic Class Stats table on PDF page 3, Class Growth and Evolution tables on page 11, the biome loop on page 1, and the revised speed/damage rules on page 13. Page numbers are PDF pages, not spreadsheet tabs. The copied narrative at the right of page 3 uses older names, classes and biomes and is not a second character catalog.

The PDF is internally inconsistent. The following are explicit implementation resolutions under the user's instruction to establish the rules, not claims that each conflict was individually approved:

- **Level one is the listed base.** Earn growth for each completed level after level one: `base + growth * (level - 1)`. The page 11 average examples instead add `growth * level`; these are superseded. This preserves the existing level indexing and avoids granting a level-up before play starts.
- **Revised SPD formula takes precedence.** Page 13 explicitly uses `base SPD + 15 * stars`, including +15 at one star and +90 at six. It omits level growth. In v1, the earlier page 11 SPD growth column is retained as source evidence but not applied. SPD does not receive the multiplicative star bonus. Thus the base table's SPD is the unevolved input, not the one-star combat output.
- **HP, ATK, DEF and POT receive evolution multipliers.** Page 11 applies stars to total stats; page 13 specifically replaces SPD. The shortened page 13 summary lists HP/ATK/DEF without explaining POT. Retain POT under the general rule rather than invent another exception.
- **C = 3000.** The revised formula supersedes page 12's 2000. The prose claiming larger C gives greater mitigation is mathematically wrong: at fixed DEF, larger C increases damage. At DEF 3000 the current curve transmits 50% of raw damage.
- **Biome-less is neutral but distinct from Kitchen.** Page 3 explicitly identifies R units as biome-less. They have no advantage or disadvantage and must not count as Kitchen units when formation synergies are implemented.
- **Round once, nearest integer, ties to even.** This makes half-value behavior explicit and keeps the existing rounding convention. Canonical calculations use decimal arithmetic and checked conversion; overflow is an error, not wraparound. No intermediate stat rounding.

## Character identity and base stats

Class selects growth; role describes the kit and does not select a different growth curve. Acquisition rarity (`R`, `SR`, `UR`) is independent of evolution stars (1-6). No rarity multiplier is invented.

The PDF's structured named table contains these 14 Tank rows. Preserve the individual distribution rather than overwriting it with the average Tank template. There is no complete structured named DPS/Support/Assassin table in this PDF export; tests for those classes use the explicitly labeled class templates. No missing named kits have been invented.

| Profile / source ID | Name | Rarity | Biome | Role | HP | ATK | DEF | SPD | POT | BST |
| --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| sheet_01 / 1 | Golden Chanterelle | SR | Forest | Wall | 190 | 70 | 155 | 70 | 130 | 615 |
| sheet_05 / 5 | Porcelain Guard | SR | Forest | Taunt | 170 | 40 | 180 | 70 | 140 | 600 |
| sheet_14 / 14 | Peat-Moss Shaman | SR | Wetlands | Battery | 160 | 90 | 140 | 80 | 130 | 600 |
| sheet_21 / 21 | Xylaria Bone-Breaker | SR | Decay | Taunt | 155 | 90 | 175 | 80 | 100 | 600 |
| sheet_24 / 24 | Scum-Layer Sentinel | SR | Decay | Saboteur | 170 | 90 | 145 | 90 | 100 | 595 |
| sheet_32 / 32 | Snow-Bank Hermit | SR | Tundra | Cover | 175 | 80 | 170 | 70 | 120 | 615 |
| sheet_34 / 34 | Permafrost Sentinel | SR | Tundra | Wall | 150 | 80 | 140 | 80 | 150 | 600 |
| sheet_38 / 38 | Glassy Bracket | SR | Tundra | Taunt | 160 | 90 | 160 | 60 | 130 | 600 |
| sheet_42 / 42 | Grey Oyster | SR | Kitchen | Taunt | 160 | 60 | 160 | 90 | 105 | 575 |
| sheet_49 / 49 | Turkey Tail Guard | SR | Kitchen | Cover | 145 | 90 | 130 | 100 | 110 | 575 |
| sheet_R09 / R09 | Parasol Runt | R | Biome-less | Cover | 110 | 80 | 130 | 90 | 90 | 500 |
| sheet_R10 / R10 | Portobello Brute | R | Biome-less | Wall | 140 | 70 | 130 | 60 | 100 | 500 |
| sheet_R17 / R17 | Pine Spike | R | Biome-less | Taunt | 150 | 70 | 140 | 50 | 90 | 500 |
| sheet_R19 / R19 | Earth Ball | R | Biome-less | Wall | 150 | 70 | 140 | 60 | 80 | 500 |

BST is the sum of the five unevolved base values. It is descriptive, not another multiplier or combat power formula. Kitchen's neutrality tax is already represented by the named 575-BST rows. Do not subtract a second tax at runtime or force every specialized unit to exactly 600 BST. New named units need an explicit approved distribution; the 600-BST templates alone do not define a new Kitchen character.

## Class templates and growth

| Class | Base HP | ATK | DEF | SPD | POT | HP / level | ATK / level | DEF / level | POT / level | Superseded SPD / level |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Tank | 150 | 90 | 150 | 90 | 120 | 65 | 5 | 12 | 5 | 0.8 |
| DPS | 90 | 180 | 90 | 150 | 90 | 25 | 12 | 5 | 5 | 1.2 |
| Mage | 100 | 150 | 90 | 130 | 130 | 18 | 10 | 2 | 10 | 0.8 |
| Support | 120 | 80 | 120 | 120 | 160 | 30 | 5 | 5 | 12 | 1.0 |
| Tactician | 100 | 100 | 100 | 150 | 150 | 22 | 8 | 4 | 10 | 1.3 |
| Assassin | 80 | 160 | 80 | 180 | 100 | 15 | 12 | 3 | 5 | 1.8 |

| Evolution stars | Multiplier for HP / ATK / DEF / POT | Level cap | Flat SPD bonus |
| ---: | ---: | ---: | ---: |
| 1 | 1.0 | 100 | 15 |
| 2 | 1.2 | 120 | 30 |
| 3 | 1.5 | 140 | 45 |
| 4 | 2.0 | 160 | 60 |
| 5 | 2.5 | 180 | 75 |
| 6 | 3.5 | 200 | 90 |

For `X` in HP, ATK, DEF, POT:

```text
X = RoundToEven((characterBaseX + classGrowthX * (level - 1)) * starMultiplier)
SPD = characterBaseSPD + 15 * stars
```

Reject levels below 1 or above the star cap, stars outside 1-6, unknown profiles/classes, invalid base values, nonfinite/negative growth and unsupported rule versions. Do not silently clamp progression. Zero DEF/POT are legal canonical values. Base HP, ATK and SPD must be positive.

## Worked reference cases

All values are unequipped, without Core, formation or active effects. These are arithmetic fixtures, not a claim of balanced battle duration.

| Reference | Level | Stars | HP | ATK | DEF | SPD | POT |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Golden Chanterelle | 20 | 1 | 1425 | 165 | 383 | 85 | 225 |
| Golden Chanterelle | 200 | 6 | 45938 | 3728 | 8900 | 160 | 3938 |
| Tank template | 100 | 1 | 6585 | 585 | 1338 | 105 | 615 |
| Tank template | 200 | 6 | 45798 | 3798 | 8883 | 180 | 3902 |
| DPS template | 100 | 1 | 2565 | 1368 | 585 | 165 | 585 |
| DPS template | 200 | 6 | 17728 | 8988 | 3798 | 240 | 3798 |
| Support template | 100 | 1 | 3090 | 575 | 615 | 135 | 1348 |
| Support template | 200 | 6 | 21315 | 3762 | 3902 | 210 | 8918 |
| Assassin template | 100 | 1 | 1565 | 1348 | 377 | 195 | 595 |
| Assassin template | 200 | 6 | 10728 | 8918 | 2370 | 270 | 3832 |

For example, Golden Chanterelle at level 20 has `190 + 65 * 19 = 1425 HP`. At level 200/six stars, `(190 + 65 * 199) * 3.5 = 45937.5`, rounded to 45938. Her SPD is `70 + 6 * 15 = 160`; adding Tank SPD growth or multiplying by 3.5 would violate this version.

## Biome damage

Forest beats Wetlands; Wetlands beats Decay; Decay beats Tundra; Tundra beats Forest. Advantage attacks multiply damage by 1.5; reverse attacks multiply by 0.75. Same-biome and opposite pairs are neutral. Kitchen and biome-less deal and receive 1.0 against every biome, including each other.

```text
Damage = max(1, RoundToEven(ATK * skillScale * 3000 / (3000 + max(0, DEF)) * biomeMultiplier))
```

Apply one directed multiplier per attack, not both 1.5 and 0.75. At ATK 3000, DEF 3000, scale 1: neutral damage is 1500, advantage 2250, disadvantage 1125. Crits, penetration, vulnerabilities, passive reductions and defense-ignoring DoTs are not inputs to this baseline function yet. Keep those future operations in the domain layer and define their ordering before adding them. Existing damage skills now use this shared function; explicit unknown biome strings fail instead of becoming silently neutral.

## Integration and migration

`GameData.LoadAll` loads and validates the stat catalog. `CombatUnitFactory` uses a character's optional `statProfileId` and the owned level/stars to calculate canonical stats. It derives identity and biome from the profile, and receives separately authored skill references. Content validation rejects conflicting identity or redundant base/growth values on a profile-backed definition. Local campaign and QA conversion pass the catalog through this boundary. The catalog snapshots its inputs and returns detached character descriptions.

The named rows are stat references, **not newly summonable units**. Step 4 now authors draft kits for the ten existing IDs through explicit `statModel = class-growth-v1`: authored base distributions plus the same canonical class growth/evolution calculator. Their identities are retained; they are not relabeled as named PDF profiles. `SLICE_CONTENT.md` records source-backed templates versus draft distributions/kits, the Kitchen base budget, campaign tuning and save compatibility. `statProfileId` still selects the original named profile path; absence of both profile and model retains explicit legacy conversion for old/reference content. No automatic name matching or biome aliasing is applied to authored content.

Team, workshop and summon character labels now distinguish acquisition tier from evolution; numeric acquisition buckets remain inside the existing gacha adapter for compatibility. Existing gear/Core fields do not imply those layers are implemented. Stat-profile introduction itself required no migration; formation maps remain schema 5. The step-4 balance update preserves valid saved levels/stars/IDs and applies canonical bounds without silent clamping. Workshop purchases still stop at the separate level-20 slice cap.

## Remaining rule layers

The formation board is implemented separately in `FORMATION_RULES.md`, including schema-5 migration and explicit placement. Runtime formation bonuses and effect composition are now specified in `COMBAT_EFFECTS.md`; they do not change permanent base-stat/evolution tables.

- Page 10 biome formation bonuses are implemented at encounter scope: higher stat tiers replace lower ones, five-unit effects retain those stat tiers, and deployed counts remain fixed. Forest HP, Wetlands POT, Decay ATK and Tundra DEF do not alter permanent base stats. Kitchen duration/cooldown and five-unit effects use the action lifecycle. Biome-less remains distinct and supplies no biome bonus.
- Equipment page 11 versus page 12 conflicts remain open: level indexing/endpoints, percentage interpolation and class affinity over gear-only versus all stats. Do not use the contradictory page 12 Golden Chanterelle geared example as a golden test for this unequipped calculator.
- Core has costs and milestone descriptions but no complete flat stat table. Do not manufacture numeric Core bonuses.
- `BATTLE_SESSION.md` defines the action lifecycle, zero-reset gauge, energy timing, signature commands and ordered events. `COMBAT_EFFECTS.md` defines implemented effects/passives/synergies and their content limits; `FORMATION_RULES.md` defines the board.

Validation lives in `CanonicalStatRulesTests.cs`: class and named fixtures, all evolution caps, 36 biome matchups, rounding, malformed catalog rejection, immutable snapshots, and canonical factory/campaign integration. Execution evidence belongs in `PROJECT_LOG.md`. The PDF's balance claims and its suggested speed ratio are not substitutes for simulation and device measurements.
