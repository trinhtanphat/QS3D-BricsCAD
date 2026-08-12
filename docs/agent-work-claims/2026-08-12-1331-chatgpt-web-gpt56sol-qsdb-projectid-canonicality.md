# Work claim — QSDB ProjectId canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-projectid-canonicality`
- Registered: `2026-08-12T13:31:00+07:00`
- Baseline main SHA: `719c68e3e2efbfe9a588597b48f200bf4411002a`
- Priority: P1 — serialized project identity must not be silently normalized while other persistence primitives reject non-canonical text.

## Confirmed defect

`QsdbProjectStore.Serialize()` writes `ProjectState.ProjectId` verbatim from the canonical in-memory identity. On load, however, `Required(root, "projectId")` trims the attribute before constructing `ProjectState`. A malformed file containing `projectId=" PROJECT-1 "` is therefore accepted and silently normalized to `PROJECT-1`.

This is asymmetric with the persistence boundary already used for `changeVersion`, dirty flags, numeric values and UTC timestamps, which reject non-canonical serialized representations rather than rewriting them on read. Project identity is especially sensitive because callers use `ProjectId` to bind/rebind cached semantic state.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`, limited to root `projectId` canonicality on load
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Canonical non-empty `projectId` continues to load unchanged.
- A `projectId` with leading/trailing whitespace fails closed with `InvalidDataException`; it is not trimmed into another identity.
- Preserve current behavior for project display name and unrelated required attributes; do not globally change `Required(...)` semantics.
- Preserve migration, backup fallback, XML hardening, schema handling, and save serialization.

## Validation boundary

No GitHub Actions or BricsCAD runtime/build PASS will be claimed unless actually observed.