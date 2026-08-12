# Work claim — Reporting Family/category integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:03:00+07:00`
- Completed: `2026-08-12T10:05:00+07:00`
- Baseline main SHA: `d1c7ef75217717d61723b154e8107411b32e7bb7`
- Claim commit: `1ad3b37c638d2a5fe1b294132c3d37de3bf97797`
- Source fix: `80d894ad23111274dfe064ebfa584dc0a5ce8678`
- Regression smoke: `bba09d7b14f5957c73637dabf3cb8bb531843c6b`
- Priority: P1 — reporting must not inherit Family metadata from a Family belonging to another semantic category.

## Confirmed defect

The reporting reference-existence lane requires every canonical nonblank `FamilyId` to resolve. The shared reporting identity boundary still did not require that the resolved Family category equal the referencing Element category. Report builders resolved that Family and used it for `FamilyName`, inherited `Material`, notes and density, so malformed persisted state could produce a valid-looking report row with metadata inherited from the wrong semantic category.

This mismatch was already fail-closed in other semantic read paths, including Project Browser queries and Quantity Rule variable projection. No reporting-specific Family/category claim or fix existed before this reservation.

## Implemented contract

- All existing reporting identity, canonicality and reference-existence checks are preserved.
- Blank Family references remain valid.
- A canonical nonblank Family reference must resolve and its `Category` must exactly match the referencing Element `Category`.
- Family resolution remains case-insensitive using the existing semantic identity convention.
- Mismatch is rejected in the shared reporting guard before report rows, inherited metadata, totals or provenance are produced.
- Floor/Zone semantics, grouping keys, quantity math, report ordering, Room Finish identity rules, material/density calculation and source-handle provenance are unchanged.

## Regression coverage

`ReportingFamilyCategoryIntegritySmoke` is auto-registered with a module initializer and covers:

- a Slab referencing a Beam Family is rejected by Material Usage and Quantity Group/Detail builders before wrong Family material can leak into output;
- a matching Slab Family remains valid and preserves inherited `FamilyName` and `Material` in Quantity and Material Usage reports;
- blank Family reference remains valid and preserves instance-owned material metadata.

## Validation

- Exact source diff readback confirms the existing Family identity set was upgraded to a case-insensitive Family index and only resolved Family/category validation was added.
- Exact regression commit readback confirms mismatch, matching and blank controls across shared reporting paths.
- Compared source fix `80d894ad23111274dfe064ebfa584dc0a5ce8678` to observed current `main` `14ccb751abf6e5893df619b1a81f6b9b09909b96`: `ahead_by=9`, `behind_by=0`, with the source fix as merge base; no concurrent commit in that range modified `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`.
- No GitHub Actions were dispatched. The smoke source was committed/read back but not executed from this connector-only session. No executable .NET/full build PASS and no licensed BricsCAD V25/V26 runtime PASS are claimed.

## Completion

`COMPLETED`: reporting now rejects resolved Family references whose semantic category does not match the referencing Element, preventing wrong-category Family metadata from contaminating report output.
