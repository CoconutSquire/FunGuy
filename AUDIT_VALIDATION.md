# FunGuy's audit validation

Date: 2026-09-14. Source baseline: `af63cdc`.

## Unity execution

Ran the installed Unity 6000.3.2f1 editor against `C:/FunGuy's/FunGuy's` in batch/nographics mode using `-runTests -testPlatform EditMode`.

Compilation completed. Test runner completed at 19:57:15 UTC and reported exit code 2 because one test failed. Test execution duration was approximately 0.27 seconds, excluding editor startup, package resolution and compilation.

| Test | Result |
| --- | --- |
| `Battle_IsDeterministic_WithSameSeedAndInputs` | Passed |
| `BiomeAdvantage_AppliesExpectedDamageMultiplier` | Passed |
| `SaveModel_RoundTripsJson_WithBannerMaps` | Passed |
| `StageWaves_ReferenceExistingEnemies` | **Failed** |
| `LimitedBanner_UsesFeaturedGuaranteeCarry_AfterOffBannerLoss` | Passed |
| `PullSequence_IsDeterministic_WithSameSeed` | Passed |

Failure message:

```text
Stage s_1_1 missing wave list.
Expected: not null
But was: null
```

The source stores waves as nested lists and loads them with Unity's JSON serializer. This observed failure is consistent with the unsupported nested-container model. Fix the schema and rerun before evaluating campaign gameplay.

Raw test XML and editor log were written to the current user's temporary directory as `funguy-audit-tests.xml` and `funguy-audit-unity.log`. The local test run removed four tracked performance-test resource files; those exact files were restored to their unchanged baseline after the runner exited. No gameplay code, scenes, content or package configuration was intentionally changed during the audit.

## Static checks

All four gameplay JSON files parse successfully using an independent JSON parser. Their current inventory is 10 characters, 3 enemies, 6 skills, 5 stages containing 11 waves, and 2 banners.

Checks passed for unique IDs within each definition collection, character/enemy skill references, stage enemy references, banner rate sums, nonempty rarity pools and featured-character references. These checks establish JSON/reference integrity only; they do not establish Unity deserialization compatibility, design correctness or economy balance.

Reviewed the seven gameplay scene structures, enabled scene order, Android profile, package manifest/lockfile, first-party C# implementation and tests, project notes, PDF text and rendered overview, home artwork, Git state and vendor/generated folder inventory. Initial working tree was clean.

## Limits of this audit

- No new Android APK/AAB was built or installed, and no physical device session was run.
- No PlayMode navigation, tutorial, input or rendered-gameplay test was executed. UI lifecycle findings are source-based risks requiring reproduction, not claimed observed device failures.
- Five passing tests do not prove the combat rules are correct; the existing assertions cover a small fraction of the intended mechanics.
- The stat sheet includes inconsistent formulas and draft material. The production plan marks decisions requiring reconciliation instead of silently treating every example as valid.
- The old local APK establishes an earlier build artifact exists; it does not validate the current checkout.

See `PRODUCTION_PATH.md` for the recommended architecture, milestone gates, source-backed platform requirements and first implementation batch.
