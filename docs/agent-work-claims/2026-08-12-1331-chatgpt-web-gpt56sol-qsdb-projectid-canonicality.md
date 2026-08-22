# Work claim — QSDB ProjectId canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-projectid-canonicality`
- Registered: `2026-08-12T13:31:00+07:00`
- Baseline main SHA: `719c68e3e2efbfe9a588597b48f200bf4411002a`
- Priority: P1 — serialized project identity must not be silently normalized while other persistence primitives reject non-canonical text.

## Confirmed defect

`QsdbProjectStore.Serialize()` writes `ProjectState.ProjectId` verbatim from the canonical in-memory identity. The previous load path used `Required(root, "projectId")`, whose shared behavior trims required attributes before returning them. A malformed file containing `projectId=" PROJECT-1 "` was therefore accepted and silently normalized to `PROJECT-1`.

This was asymmetric with the persistence boundary already used for `changeVersion`, dirty flags, numeric values and UTC timestamps, which reject non-canonical serialized representations rather than rewriting them on read. Project identity is especially sensitive because callers use `ProjectId` to bind/rebind cached semantic state.

## Implemented contract

- Canonical non-empty `projectId` continues to load unchanged.
- A `projectId` with leading/trailing whitespace fails closed with `InvalidDataException`; it is not trimmed into another identity.
- Current behavior for project display name and unrelated required attributes is preserved; shared `Required(...)` semantics were not changed.
- Migration, backup fallback, XML hardening, schema handling, and save serialization are unchanged.

## Commits

- Claim: `d7c9f1d79920b7cf4ba2df4eb18aed0afda68656`
- Source fix: `0781ecbd5fd1ec4cdad3494b0ee50546f27b008a`
- Regression smoke: `1ccd128f538aa2215a9599af6d30d4bd5f3baa3c`

## Validation

GitHub source-commit diff read-back confirmed only the intended root `projectId` loader change and canonical helper were introduced. The focused smoke was read back from `main` and covers a canonical round-trip plus rejection of a padded `projectId`. No GitHub Actions were dispatched and no BricsCAD runtime/build PASS is claimed.