# FunGuy's standing project instructions

These instructions apply to the entire repository and all future work on this project.

## Read before working

- Read `PRODUCTION_PATH.md` for the intended architecture, milestone order and completion gates.
- Read the latest entries in `PROJECT_LOG.md` for changes, findings, decisions and unresolved work.
- Treat `Funguy_s Stat sheet.pdf` as the gameplay design authority, subject to subsequent explicit user decisions. Document conflicts rather than silently inventing a resolution.
- The Unity project is `FunGuy's/` inside the repository root. `AUDIT_VALIDATION.md` is the dated initial audit, not a statement of current test results.

## Implement toward the production architecture

- Implement the production architecture progressively during every milestone. Do not treat it as a refactor postponed until the prototype is finished.
- Before implementing a feature, identify its domain rules, application operation, presentation responsibilities, persistence needs and eventual server authority. Put each responsibility in the layer defined by `PRODUCTION_PATH.md`.
- Build shared rules, explicit interfaces, stable identifiers, versioned data and migration paths as the relevant systems are introduced. UI must not become the owner of economy or combat rules.
- Local implementations should satisfy the same application contracts intended for production adapters. For example, a local summon operation should validate and commit one complete transaction so that a future server adapter can replace it without rewriting the UI.
- Consider dependent milestones before changing public interfaces, content schemas or save formats. Preserve working behavior and migrate existing data where needed.
- Defer later features and infrastructure until their milestone, while establishing the boundaries they will use. Do not implement speculative systems merely to anticipate every possible future requirement.
- Avoid disposable shortcuts that create predictable rework. A temporary implementation is acceptable when necessary, but document its reason, limitations, replacement trigger and migration plan.
- Backtracking is allowed when evidence or an explicit requirement makes it necessary. Document the evidence, alternatives, affected systems and revised path before proceeding where practical; update the record promptly for urgent fixes. Routine decisions within the authorized scope do not require an extra approval step.

## Document every change and finding

- Documentation is part of the work, including investigations that change no code. Record every change, material finding, design or architecture decision, test result, issue, blocker and scope adjustment in `PROJECT_LOG.md` as work progresses and before ending the task.
- Group closely related edits into one clear entry; do not create an entry for each keystroke or log the act of updating the log recursively. Each substantive change or finding must still be represented.
- Include date, affected files/systems, what changed or was learned, why it matters, evidence/validation, production-path impact and remaining work. Clearly distinguish implemented behavior, proposals, confirmed defects and unverified risks.
- Update the relevant living documentation in the same change set whenever implementation or a decision changes it. Update `PRODUCTION_PATH.md` for architecture, scope or milestone changes; update rules, setup, build and test guides when those instructions change. A log entry alone does not replace correcting stale guidance.
- Preserve dated audit results and historical entries. Add a correction or superseding entry with a reference rather than making old results appear current.
- Keep records concise, factual and free of secrets or unnecessary personal data. Link to files, tests or artifacts instead of copying large logs.
- A task is not complete until documentation reflects the delivered state, actual validation and any unresolved work. Never report tests as passing unless they were run and passed.
