# Defect provenance and missing-surface triage

This playbook exists to prevent agents from spending long sessions trying to fix a defect whose named test, type, method, workflow output, or implementation surface does not exist in the current product tree.

Use it before creating a source-fix plan whenever a task arrives as a quoted failure, copied regression name, handoff statement, research note, external benchmark, synthesized requirement, or previous-agent conclusion.

## Core rule

A defect description is not implementation truth by itself.

Before editing production source, determine where the defect statement came from and classify it against the current exact baseline.

Do not create a missing type/method/test merely to make a quoted defect name appear real. Do not rename an unrelated implementation to fit an issue. Do not weaken tests or redirect the fix into a nearby persistence/importer class without direct evidence.

Current source and reproducible evidence win over stale handoffs, copied issue text, research notes, model-generated summaries, or internet-derived feature lists.

## Required provenance classes

Every bug lane should be classified as one of the following before source mutation.

### `REPRODUCED`

The failure was executed against the exact current candidate or an explicitly recorded baseline and produced a concrete failing command/test/log.

Minimum evidence:

- exact repository and SHA;
- exact command/workflow/job;
- exact failing test/assertion/error;
- enough stack/file/symbol context to identify the defect surface.

This is the strongest source-fix starting point.

### `SOURCE_PRESENT`

The quoted failure could not yet be reproduced, but the named implementation/test surface exists in the current exact tree and the defect can be demonstrated by deterministic code inspection or a focused test that can be added without inventing product semantics.

Minimum evidence:

- exact repository and SHA;
- exact existing file/symbol/caller;
- concrete invariant violated by the current implementation;
- focused validation plan.

### `ENVIRONMENT_GATED`

The defect surface exists, but reproduction requires a resource unavailable to the current agent, for example licensed BricsCAD, a private DWG, Windows UI interaction, signing material, a proprietary SDK, or hardware-specific behavior.

The remote/source agent may still prepare source-safe tests or guards when the defect surface is known, but must keep unavailable execution evidence `PENDING_LOCAL` / `LOCAL_ONLY` as required by repository policy.

### `EXTERNAL_REFERENCE`

The task comes from an external product, public research source, spreadsheet, PDF, screenshot, benchmark, owner-provided document, or other reference and describes behavior QS3D should implement or emulate.

This is an implementation/specification task, not automatically a regression.

Record:

- the external source/provenance;
- whether the behavior is clean-room product inspiration, an owner requirement, or interoperability requirement;
- the QS3D product surface that should own the new behavior;
- acceptance tests to be created.

Never report an external feature name as a QS3D failing test unless such a test actually exists.

### `SPEC_ONLY / MISSING_SURFACE`

The named failure/test/type/method cannot be found or reproduced in the current repository, relevant refs/history, expected sibling repository, or CI evidence, and no owner-supplied source artifact establishes where it lives.

This is not a safe source-fix lane yet.

Required behavior:

1. stop speculative implementation edits;
2. preserve the issue/reservation as a provenance investigation or reclassify it clearly;
3. state exactly what was searched and what is missing;
4. request or locate the missing artifact/spec only when it can materially resolve the provenance;
5. resume source fixing only after a real implementation/reproducer appears, or explicitly convert the task into a new implementation/spec task.

## Fastest professional triage order

Use the following order. Do not begin with broad symbol hunting when a real failure log may already identify the file and line.

### 1. Lock the exact baseline

Refresh `origin/main` and record the exact SHA. If the report names another SHA/run/branch, record that separately.

Do not use a stale conversation SHA as current truth.

### 2. Read the exact failure producer first

Prefer, in order:

1. exact local failing command output;
2. exact GitHub Actions job log;
3. test runner output / stack trace;
4. compiler diagnostic;
5. static preflight output;
6. only then a copied summary or handoff statement.

Extract the literal test name, project, file, symbol, stack frame, workflow step, and tested SHA.

If the quoted failure does not exist in the log that supposedly produced it, downgrade confidence immediately.

### 3. Search the current tree by literal evidence

Search for the exact assertion, test name, type, method, metadata key, command name, or unique error fragment.

Then search likely aliases only when there is evidence the name may have changed.

A code-search miss is not final proof when the GitHub index is incomplete. Confirm against an exact checkout, recursive tree/content enumeration, or another trustworthy source before declaring a surface absent.

### 4. Reproduce the smallest relevant gate

Run the focused test/project/preflight first. Avoid starting with a large release workflow when a small Core smoke or unit command can isolate the same behavior.

If another earlier failure masks the target, do not fix the unrelated blocker unless the owner expands scope. Use historical logs, direct focused execution, or the smallest test harness to isolate the assigned defect.

### 5. Trace current source, not nearby names

Once the failure is real, trace:

`assertion -> test fixture -> public API -> caller -> mutation helper -> persistence/domain boundary`

Do not choose a file because its name sounds similar. In particular, do not patch persistence stores, importers, coordinators, or serializers merely because they are adjacent to the feature vocabulary.

### 6. Check history and refs only when current source is insufficient

Search commits/refs for:

- the exact test/type/method;
- introduction/removal/rename commits;
- the issue/PR that first described the behavior;
- migration commits that moved the implementation.

History is provenance evidence, not a substitute for current implementation truth.

### 7. Check product-boundary sibling repositories

Only after repository docs or current source indicate a migration/shared-library boundary, inspect the expected sibling repository read-only.

Record repository + branch/SHA. Do not sweep unrelated repositories without a product-boundary reason.

If the surface is absent across the expected repositories and refs, this is strong `MISSING_SURFACE` evidence.

### 8. Trace docs/spec provenance

Search current and historical Markdown, handoffs, planning documents, issue bodies, spreadsheets, imported research notes, and owner-provided artifacts for the exact phrase or concept.

Determine whether the statement was:

- copied from a real test/log;
- proposed as future work;
- inferred by an agent;
- synthesized from an external product/reference;
- copied from an internet source;
- generated as an acceptance-test name before implementation existed.

A requirement in Markdown can legitimately drive future code, but it must be labeled as a spec/implementation task instead of a reproduced regression.

### 9. Search public web only when provenance could reasonably be external

Use exact-phrase searches first. Then search the concept and named external product/source if applicable.

No public result does not prove a statement was model-generated, but it reduces support for claiming that the literal came from a public implementation/test.

If a public reference is found, capture the source and convert the lane to `EXTERNAL_REFERENCE` unless QS3D already contains a reproducer.

### 10. Decide before editing

Use this decision table:

| Evidence | Classification | Source patch? |
| --- | --- | --- |
| Exact failing command/log + existing source surface | `REPRODUCED` | Yes |
| Existing source surface + deterministic defect proof | `SOURCE_PRESENT` | Yes, with regression coverage |
| Existing surface but proprietary/runtime reproduction required | `ENVIRONMENT_GATED` | Source-safe work only; runtime remains pending |
| External source specifies desired behavior | `EXTERNAL_REFERENCE` | Implement as an explicit feature/spec task |
| No test/type/method/log/source across expected surfaces | `SPEC_ONLY / MISSING_SURFACE` | No speculative patch |

## Ninety-second first pass

For common source bugs, the initial agent pass should try to answer these questions before deep investigation:

1. What exact SHA is being discussed?
2. What exact command/run allegedly failed?
3. Can I see the literal failure in that output?
4. Does the named test/type/method exist in the exact tree?
5. Is code-search known to be incomplete?
6. Is there an obvious migration/sibling-repo boundary?
7. Is the text possibly a spec/handoff/external reference rather than emitted test output?

If questions 3 and 4 are both `no`, switch immediately to provenance triage instead of spending repeated cycles opening nearby source files.

## Issue/reservation requirements

A source-bug issue should distinguish `reported` from `proven` evidence.

Recommended fields:

```text
Provenance state: REPRODUCED | SOURCE_PRESENT | ENVIRONMENT_GATED | EXTERNAL_REFERENCE | SPEC_ONLY / MISSING_SURFACE
Exact baseline: <repo>@<sha>
Failure producer: <command/run/job or NONE>
Literal verified in producer: yes/no
Existing defect surface: <file/symbol or NONE>
Source of report: <runtime/test/CI/owner doc/spreadsheet/handoff/web/agent inference>
Search boundary checked: <current tree/history/refs/sibling repo/docs/web as applicable>
Next evidence needed: <specific artifact or NONE>
```

Do not write acceptance criteria as if a missing test already exists. Say `add regression coverage for ...` rather than inventing an existing test identity.

## CI strategy

Use the earliest reproducible stage.

Preferred order:

```text
focused unit/smoke/preflight
  -> affected project build/test
  -> shared branch/PR CI
  -> combined integration CI when authorized
  -> exact-main release CI after authorized landing when required
```

Do not use a large release workflow as the primary debugging loop when the defect belongs to Core and can be reproduced earlier.

Do not repeatedly rerun CI hoping for a missing failure string to appear.

## Handling masked failures

When a deterministic test harness aborts at the first module initializer or first failing suite, a later quoted failure may not be observable in the current run.

Use one of these approaches without taking over unrelated scope:

- run the focused test directly;
- inspect an older exact run that reached the target stage;
- create an isolated regression test on the task branch if the source contract is known;
- ask the owner/local agent for the actual output artifact when the quoted failure came from another environment.

If none produces the quoted failure and the named source surface is also absent, classify `SPEC_ONLY / MISSING_SURFACE`.

## Example: missing named ProjectDataStore regression

Suppose a handoff states:

```text
ProjectDataStore MergeNonConflicting should merge incoming linked relations
```

but the exact current tree, expected sibling repositories, repository history/refs, and relevant Actions logs contain no `ProjectDataStore`, no `MergeNonConflicting`, and no exact assertion.

Correct response:

- do not patch `QsdbProjectStore` because it is merely a nearby persistence type;
- do not assume Project Interchange field merge is the same abstraction;
- record the search boundary and classify the claim `SPEC_ONLY / MISSING_SURFACE`;
- investigate whether the sentence came from a spec, external reference, generated acceptance-test idea, stale branch/artifact, or owner document;
- resume source implementation only after the real surface appears or the owner explicitly converts the requirement into a new implementation task.

## Completion language

Use precise completion statements:

- `REPRODUCED and fixed on <branch>@<sha>; focused tests passed.`
- `SOURCE_PRESENT; patch ready, runtime qualification remains PENDING_LOCAL.`
- `EXTERNAL_REFERENCE converted into an implementation task; no prior QS3D regression claimed.`
- `SPEC_ONLY / MISSING_SURFACE: no safe production patch made; exact missing evidence is recorded.`

Never say a defect is fixed merely because a nearby source change compiles.

Never claim CI or licensed runtime PASS unless that exact evidence was actually executed against the stated SHA.
