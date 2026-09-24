# Garrett Debug Log & Testing Checklist

**Purpose:** This is a learning-oriented record of the Unity/Git/content problems we found while integrating Garrett's roster and Unity-authored content work. It records **what was observed before each fix**, the actual cause, what was changed, how to verify the repair, and what engineering lesson to take from it.

This document is intentionally more detailed than a normal changelog. Garrett can use it as a debugging reference and as a template for future work.

---

## 1. Current working baseline

As of **2026-09-22**:

- Shared/upstream production branch: `OneVillage83/FunGuy-s-WorkingName-:main`
- Upstream main commit containing the 71-character roster/campaign conversion: `e18a242f3e05f271c3a50bbcff684208a4d58657`
- Garrett feature branch before the latest battle-target repair: `CoconutSquire/FunGuy:feature/unity-authored-content`
- Garrett feature tip after the first catalog repair: `1c5acbbdd6e5962737bd4f7ae6012584358590c3`
- Latest review repair branch: `OneVillage83/FunGuy-s-WorkingName-:review/garrett-unity-authored-content`
- Latest repair: use the current tip of `review/garrett-unity-authored-content`; the review branch may be squashed during handoff so the branch name is the stable reference.
- Required Unity editor: **6000.3.2f1**
- Game/project version: **v2**
- Unity bundle version: **2.0.0**
- Android version code: **2**

### Important distinction

The game version and Unity editor version are different things:

- `v2 / 2.0.0` = our game/project release label.
- `6000.3.2f1` = the Unity editor/toolchain required to open and build the current project safely.

Never replace `ProjectSettings/ProjectVersion.txt` with `v2`. Unity needs the real editor version there.

---

# 2. Error and repair history

## G-001 — Unity opened in the wrong/newer editor and produced hundreds of errors

### What was observed before the fix

Garrett reported:

- Unity Safe Mode.
- A version number that looked wrong/unavailable locally.
- Opening/upgrading in a newer Unity version produced **999+ red errors/warnings**.
- Dependencies appeared deprecated or removed.
- A force pull did not fully restore the local Unity environment.

### What we investigated

Repository history showed that `ProjectSettings/ProjectVersion.txt` had declared:

```text
6000.3.2f1
revision a9779f353c9b
```

since the original Unity project history.

Codex did **not** introduce a fake Unity version.

The repo also contained historical successful validation using that exact editor, including earlier EditMode, PlayMode and Android build evidence.

### Root cause / important concept

A Git reset/pull restores **tracked source files**. It does not reset Unity's generated local state.

These directories are ignored by Git:

```text
Library/
Temp/
Obj/
Logs/
UserSettings/
```

If a project is opened in another Unity version, those generated caches can be upgraded locally. A later `git pull` does not remove them.

### What was changed

- Kept Unity pinned to **6000.3.2f1**.
- Explicitly documented the editor pin.
- Separated the human-facing project version from the editor version.
- Set the project release to **v2 / 2.0.0**.
- Set Android version code to **2**.
- Added recovery guidance to the build documentation.

### Recovery procedure

Close Unity first, then:

```text
Delete:
Library/
Temp/
Obj/
Logs/

Keep:
Assets/
Packages/
ProjectSettings/
```

Then run:

```bash
git lfs pull
```

and open the project specifically with **Unity 6000.3.2f1**.

### Lesson

When Unity shows hundreds of errors after an editor upgrade, do not start fixing error #437. Start with:

1. correct editor version,
2. clean generated cache,
3. first compiler error only.

Cascading errors often come from one root problem.

---

## G-002 — 71 new roster characters existed, but Battle/Campaign still depended on old placeholders

### What was observed before the fix

Garrett correctly noted that:

- the 71 new characters existed,
- the old placeholder characters/enemies still influenced Battle/Campaign,
- Battle needed placeholder visuals for the new roster,
- the campaign would need updating if old placeholder enemies were removed.

### Root cause

The data model had two separate combat identity sources:

1. current collectible `CharacterDef` roster,
2. old `EnemyDef` placeholder catalog.

Campaign stages still referenced IDs such as:

```text
e_sporeling
e_rot_thrall
e_ashen_spore
e_frost_keeper
e_kitchen_overseer
```

even after the 71-character player roster was integrated.

### What was changed

- Kept all **71 Garrett characters** as the current roster.
- Removed the bundled legacy enemy definitions.
- Removed orphaned legacy enemy skills.
- Updated all seven campaign stages to use real Garrett roster character IDs as opponents.
- Preserved stage IDs, unlock order and rewards for save compatibility.
- Updated campaign runtime, preview, smoke-test and validation paths to resolve opponents through `CharacterDef`.
- Kept the existing code-native `FungusPortrait` as a universal battle placeholder.
- Added stable placeholder variation by content ID/class.
- Added Cosmic placeholder coloring.

### Static validation result

The source graph was checked for:

- 71 unique roster characters,
- 0 legacy enemies,
- 14 campaign waves,
- 22 distinct current roster opponent IDs,
- no broken stage references,
- resolved skill references.

### Lesson

When replacing a content source, search for **all consumers of the old IDs**, not just the obvious JSON list.

A migration is not complete until runtime code, validation, tests, UI previews and campaign data all resolve through the new source.

---

## G-003 — Unity-authored content branch: 122 tests ran, 28 failed

### What was observed before the fix

Garrett's saved EditMode report showed:

```text
122 total
94 passed
28 failed
```

All 28 failing test cases ultimately reported:

```text
Invalid game content: Stage s_1_1: missing waves.
```

### Critical clue

The committed `UnityContentCatalog.asset` contained:

```yaml
id: s_1_1
waves: []
rewards:
  gold: 99999
```

But the real Stage 1 should have two waves and 50 gold.

### Why those exact values mattered

An existing campaign test intentionally mutates its **runtime copy**:

- clears Stage 1 waves,
- changes reward gold to `99999`.

That test is verifying that an already-started campaign holds a safe snapshot.

The Unity-authored loader passed the ScriptableObject's nested objects directly into runtime `GameData`.

So the test did not mutate a runtime copy.

It mutated the **actual ScriptableObject-owned StageDef**.

Unity then saved that dirtied object back to `UnityContentCatalog.asset`.

### Root cause

This was an **object ownership / aliasing bug**.

Bad ownership model:

```text
UnityContentCatalog.asset
        ↓ same object references
GameData.Stages
        ↓
test/gameplay mutates GameData
        ↓
authored asset also changes
```

### What was changed

`UnityContentCatalogLoader` now creates a **deep-cloned runtime snapshot** before:

- validation,
- normalization,
- runtime dictionary publication.

New ownership model:

```text
UnityContentCatalog.asset
        ↓ deep clone
detached DTO snapshot
        ↓ validate / normalize
GameData runtime state
```

The authored ScriptableObject is no longer live mutable runtime state.

### Additional safety change

Runtime `ValidateSchema()` was made non-mutating.

Previously validation also called list-repair code, which could silently change malformed content while loading it.

Now:

- runtime validation fails loudly,
- `EnsureListsForEditing()` is an explicit Editor-authoring convenience.

### Asset repair

Stage 1 was restored to:

```text
Wave 1: R01, R04
Wave 2: R10, R03
Gold: 50
```

### Regression tests added

`UnityContentCatalogTests` now verifies:

1. current roster/stage structure,
2. runtime mutations cannot modify the ScriptableObject,
3. a second `GameData.LoadAll()` gets pristine data again.

### Lesson

A ScriptableObject under `Resources` is still an authored asset. Do not treat nested serialized objects as disposable mutable runtime state.

When in doubt, define who **owns** an object and who is allowed to mutate it.

---

## G-004 — Garrett's GameData refactor unintentionally removed unrelated production behavior

### What was found during review

While adding the Unity catalog path, `GameData.cs` had been shortened and several existing normalizers disappeared.

This was not the visible Stage 1 error, but it could have caused unrelated regressions later.

Removed behavior included:

- BST fallback calculation,
- POT/growth normalization,
- enemy compatibility normalization,
- skill energy/cooldown defaults,
- banner defaults,
- pity defaults,
- featured defaults,
- banner-rate normalization,
- legacy element-to-biome mapping.

### What was changed

The current production normalization behavior from main was restored while retaining Garrett's intended additions:

- `partial GameData`,
- Unity-authored catalog loading,
- shared `LoadValidated(...)` finalization.

### Lesson

During a refactor, a shorter file is not automatically a better file.

Compare old and new behavior, not only whether the new code compiles.

A useful review question is:

> What behavior disappeared from the previous implementation?

---

## G-005 — Unrelated package upgrades were mixed into the feature branch

### What was found before cleanup

The WIP branch had auto-upgraded:

```text
2D Animation:
13.0.2 -> 13.0.6

2D Tooling:
1.0.0 -> 1.0.4

AI Generators:
1.0.0-pre.20 -> 1.7.0-pre.1

Visual Studio Editor:
2.0.25 -> 2.0.28

Timeline:
1.8.9 -> 1.8.13
```

These upgrades were unrelated to the Unity-authored content feature.

### Why this is dangerous

If a content-loader feature and a package migration are committed together, an error can come from either change.

That makes debugging much harder.

### What was changed

Restored the package files/settings to the pinned versions from working `main`:

- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/PackageManagerSettings.asset`

### Lesson

Change one major variable at a time.

A good branch should answer a clear question:

> Did the content-authoring feature work?

It should not simultaneously ask:

> Did five package upgrades work too?

---

## G-006 — Generated local IDE/test files were committed

### What was found

The WIP included local/generated files such as:

```text
.vs/
*.suo
*.vsidx
CodeChunks.db
SemanticSymbols.db
DocumentLayout.json
TestResults_20260921_232029.xml
```

Unity test execution also changed generated TMP/performance-test files.

### What was changed

Removed generated/local files from the feature diff and restored legitimate tracked files from main.

Added ignore rules for:

```text
.vs/
TestResults_*.xml
```

### Lesson

Before every commit:

```bash
git status --short
git diff
```

Ask for every changed file:

> Did I intentionally change this?

If the answer is no, do not commit it.

---

## G-007 — Battle appeared to fail during energy generation

### What Garrett observed

After getting past the earlier catalog problems:

- Home loaded and looked better.
- Battle still stopped.
- The visible error appeared to occur around **energy generation**.

### What the battle engine actually does

At the start of an action:

```text
choose actor
↓
reset action gauge
↓
+20 start-turn energy
↓
tick cooldown
↓
select skill
↓
resolve effect target
↓
apply damage/effect
```

The +20 energy was succeeding.

### Data difference between JSON and Unity-authored content

JSON with an omitted optional field typically produces:

```text
target = null
```

The Unity ScriptableObject serializes the same unset string as:

```text
target = ""
```

The authored asset contained effects like:

```yaml
- type: Damage
  target:
  stat: ATK
```

### The bug

The battle engine used null-only fallback logic:

```csharp
string rule = eff.target ?? skill.target;
```

For JSON:

```text
null -> use EnemyFront
```

For Unity:

```text
"" -> treated as a real target
   -> SelectTargets("")
   -> Unsupported target
```

Therefore the visible timeline was:

```text
+20 energy succeeds
↓
next line of battle logic fails
```

That made energy look guilty even though it was the **last successful event**.

### What was changed

Runtime normalization now canonicalizes blank optional effect strings:

```text
target: "" -> null
stat: ""   -> null
status: "" -> null
```

The battle engine was also hardened independently so blank/null behave the same for:

- skill effect target fallback,
- passive effect target fallback,
- damage stat fallback,
- heal/shield/scaled utility stat fallback.

### Regression test added

`BattleSessionTests.UnityBlankOptionalEffectFieldsFallbackAfterStartTurnEnergyGeneration`

The test intentionally sets:

```text
target = ""
stat = ""
```

and verifies:

1. +20 start-turn energy occurs,
2. basic attack still executes,
3. `EnemyFront` is used,
4. damage occurs,
5. +5 attacker hit energy occurs,
6. actor ends at 25 energy,
7. battle stays Running.

### Lesson

The line mentioned in an error report is not always the root cause.

Ask:

> What was the last successful operation, and what operation happens immediately after it?

That often narrows a large system to one failing boundary.

---

# 3. Current energy rules — do not change these while debugging this issue

The blank-target repair does **not** change battle energy design.

Current rules remain:

| Event | Energy |
| --- | ---: |
| Start of actor turn | +20 |
| Actor deals a damaging hit | +5 |
| Actor deals a killing hit | +15 instead of that hit's +5 |
| Unit receives damaging hit | +10 |
| Signature placeholder cost | 100 |
| Max energy | 100 |

Multi-hit/multi-target skills can generate energy per actual damaging effect according to the existing battle contract.

If a future energy result looks wrong, verify the expected rule before changing the engine.

---

# 4. Garrett's pre-test checklist

Use this before testing a feature branch.

## Git

- [ ] I am on my feature/test branch, **not main**.
- [ ] `git status --short` shows only changes I expect.
- [ ] My branch started from the current intended main commit.
- [ ] I ran `git pull` / fetched the correct remote.
- [ ] I ran `git lfs pull`.
- [ ] I know the exact commit I am testing: `git rev-parse HEAD`.

## Unity

- [ ] Unity version is **6000.3.2f1**.
- [ ] I did not allow Unity Hub to silently upgrade the project.
- [ ] If I previously opened another Unity version, I closed Unity and cleared generated caches.
- [ ] I did **not** delete `Assets/`, `Packages/`, or `ProjectSettings/`.

## Packages

- [ ] `Packages/manifest.json` did not change unless package work is intentional.
- [ ] `Packages/packages-lock.json` did not change unless package work is intentional.
- [ ] I am not mixing a package upgrade with an unrelated gameplay/content feature.

## Content

- [ ] Run **FunGuy -> Content -> Validate Unity Catalog**.
- [ ] Stage 1 has two waves.
- [ ] Stage 1 gold is 50.
- [ ] The catalog has 71 characters.
- [ ] No unexpected old placeholder enemy IDs are present.

---

# 5. Runtime test checklist

After content validation succeeds:

## EditMode

- [ ] Run the complete EditMode suite.
- [ ] Record total / passed / failed.
- [ ] If there are failures, inspect the **first failed test** first.
- [ ] Do not start fixing 20 later failures before understanding failure #1.

## PlayMode

- [ ] Run the complete PlayMode suite.
- [ ] Confirm Home loads.
- [ ] Confirm Team loads.
- [ ] Confirm Campaign loads.
- [ ] Confirm Battle scene opens.
- [ ] Confirm battle preview has player and opponent fighters.

## Battle smoke test

For a normal first action:

- [ ] fighter gets +20 start-turn energy,
- [ ] basic skill resolves,
- [ ] a legal target is selected,
- [ ] damage occurs,
- [ ] attacker gets +5 on a nonlethal damaging hit,
- [ ] defender gets +10 when damaged,
- [ ] UI energy bars update,
- [ ] battle continues to the next action,
- [ ] no `Unsupported target:` exception appears.

## Signature test

- [ ] queue a signature below 100 energy,
- [ ] basic attacks continue while waiting,
- [ ] energy reaches 100,
- [ ] signature casts once eligible,
- [ ] 100 energy is spent once,
- [ ] cooldown begins,
- [ ] queue clears.

---

# 6. After-test Git cleanup checklist

Unity tests/editor sessions can modify files that are not real source changes.

Run:

```bash
git status --short
```

Review every line.

Do **not** commit accidental changes to:

- [ ] `.vs/`
- [ ] `Library/`
- [ ] `Temp/`
- [ ] `Obj/`
- [ ] `Logs/`
- [ ] root `TestResults_*.xml`
- [ ] TMP fallback font dynamic glyph-cache changes
- [ ] package files unless package changes were intentional
- [ ] generated build caches

If a tracked file was changed by a test runner, restore only that known generated change rather than doing a broad destructive reset that could delete real work.

---

# 7. What to capture when a new error happens

Do this **before trying random fixes**.

Copy this template into notes or a GitHub issue:

```text
ERROR ID:
DATE/TIME:

BRANCH:
COMMIT SHA:
UNITY VERSION:

WHAT I WAS DOING:
1.
2.
3.

EXPECTED:

ACTUAL:

FIRST CONSOLE ERROR:
[paste exact message]

FIRST STACK TRACE:
[paste exact stack trace]

SCENE:

TEST NAME (if applicable):

DOES IT HAPPEN EVERY TIME?
Yes / No / Unknown

LAST KNOWN GOOD COMMIT:

FILES I CHANGED BEFORE IT STARTED:

DID UNITY CHANGE PACKAGES?
Yes / No / Unknown

DID I OPEN THE PROJECT IN ANOTHER UNITY VERSION?
Yes / No

GIT STATUS:
[paste git status --short]

SCREENSHOT / VIDEO:
[reference]
```

### Why exact text matters

A screenshot is useful, but the exact Console error and stack trace are much more searchable.

The first exception often tells us:

- file,
- method,
- line,
- call path,
- whether the fault is compile-time, content validation, domain logic or presentation.

---

# 8. Debugging order

When something breaks, use this order.

## A. Does Unity compile?

If **no**:

1. first compiler error,
2. correct Unity version,
3. package drift,
4. missing/moved script/meta,
5. then later compiler errors.

Do not debug gameplay until compilation succeeds.

## B. Does content validation pass?

If **no**:

1. exact validation message,
2. find the referenced ID/stage/skill,
3. inspect authored asset,
4. compare with expected JSON/source,
5. fix the source—not the validator unless the validator is actually wrong.

Never weaken validation just to make a red error disappear.

## C. Does the scene load?

If **no**:

1. first Console exception,
2. stack trace,
3. missing Resource/prefab/reference,
4. controller initialization,
5. content lookup.

## D. Does Battle start but stop during an action?

Inspect event order:

```text
ActionStarted
GaugeChanged
EnergyChanged
CooldownChanged
SkillUsed
Damage / Heal / Shield / Status...
ActionCompleted
```

Find the **last event that succeeded**.

Then inspect the next operation in code.

That exact approach exposed G-007.

---

# 9. Git workflow Garrett should use

Garrett already has a fork. He does not need another fork.

Recommended pattern:

```text
CoconutSquire/FunGuy
├── main
└── feature/specific-task
```

Example:

```bash
git switch main
git pull
git switch -c feature/my-next-change
```

Work and test there.

Then:

```bash
git add <intentional files>
git commit -m "Describe one coherent change"
git push -u origin feature/my-next-change
```

Do not use `git add .` blindly until after reviewing `git status`.

---

# 10. Current handoff for the latest Battle fix

Garrett's current feature branch is at:

```text
1c5acbbdd6e5962737bd4f7ae6012584358590c3
```

The latest Battle blank-target fix is one clean commit ahead on:

```text
OneVillage83/FunGuy-s-WorkingName-
review/garrett-unity-authored-content

4af159f4fc62c156690f05145201d25e8f4f4672
```

If the remote is named `onevillage`:

```bash
git switch feature/unity-authored-content
git fetch onevillage review/garrett-unity-authored-content
git merge --ff-only onevillage/review/garrett-unity-authored-content
git push origin feature/unity-authored-content
```

If the remote has another name, substitute that remote name.

After pulling the repair:

1. validate catalog,
2. EditMode,
3. PlayMode,
4. Battle smoke test,
5. inspect `git status --short`,
6. report the **first fresh error** if anything remains.

---

# 11. Things not to do

- Do not upgrade Unity just because the current version is not installed.
- Do not weaken validation to hide malformed content.
- Do not commit generated IDE state.
- Do not mix package upgrades into unrelated gameplay/content work.
- Do not assume `git pull` clears Unity's `Library`.
- Do not fix hundreds of cascading errors individually.
- Do not regenerate the Unity catalog from JSON without remembering that it overwrites Inspector-only edits.
- Do not mutate ScriptableObject-authored DTOs directly at runtime.
- Do not merge an experimental branch into main just because it compiles once.

---

# 12. Future error log entries

Add new entries below using this format.

## G-XXX — Short problem title

### Observed before fix

What did the player/developer see?

### Reproduction

Exact steps.

### First error

Exact Console/test error.

### Root cause

What actually failed and why?

### Fix

What files/logic changed?

### Validation

What was actually run and what passed?

### Lesson

What should Garrett remember next time?

### Remaining risk

What is still unverified?

---

The goal is not to avoid every bug. The goal is to make each bug **reproducible, explainable, isolated and testable**.
