# Work claim — Reporting Family/category integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:03:00+07:00`
- Baseline main SHA: `d1c7ef75217717d61723b154e8107411b32e7bb7`
- Priority: P1 — reporting must not inherit Family metadata from a Family belonging to another semantic category.

## Confirmed defect

The reporting reference-existence lane now requires every canonical nonblank `FamilyId` to resolve. However, the shared reporting identity boundary still does not require that the resolved Family category equal the referencing Element category. Report builders resolve that Family and use it for `FamilyName`, inherited `Material`, notes and density, so malformed persisted state can produce a valid-looking report row with metadata inherited from the wrong semantic category.

This mismatch is already fail-closed in other semantic read paths, including Project Browser queries and Quantity Rule variable projection. No reporting-specific Family/category claim or fix was found in current history.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs` — resolved Family/category validation only.
- `tests/QS3D.Core.SmokeTests/ReportingFamilyCategoryIntegritySmoke.cs` — focused auto-registered Core smoke.
- this claim file for close-out.

## Intended contract

- Preserve all existing reporting identity, canonicality and reference-existence checks.
- Blank Family references remain valid.
- A canonical nonblank Family reference must resolve and its `Category` must exactly match the referencing Element `Category`.
- Reject mismatch before report rows, inherited metadata, totals or provenance are produced.
- Preserve case-insensitive Family identity lookup.
- Do not alter Floor/Zone semantics, grouping keys, quantity math, report ordering, Room Finish identity rules, material/density calculation or source-handle provenance.

## Validation plan

- Re-fetch moving `main` and guard after claim.
- Extend the existing Family reference index to retain the resolved Family and compare category.
- Add focused mismatch + matching/blank controls across shared reporting paths.
- Read back exact source/test diffs and verify ancestry.
- No GitHub Actions dispatch; no executable .NET/full build or BricsCAD V25/V26 runtime PASS claim without actual execution.
