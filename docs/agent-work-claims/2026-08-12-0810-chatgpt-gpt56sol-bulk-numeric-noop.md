# Work claim — Bulk numeric lexical no-op preservation

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-bulk-numeric-noop`
- Registered: `2026-08-12T08:10:00+07:00`
- Last Updated: `2026-08-12T08:14:00+07:00`
- Baseline main SHA: `68517455c46f688a74f4a1d6632c9b93e8d4bb3a`
- Priority: deterministic Core freshness/no-op mismatch found during owner-requested `continue all`
- Task Key: `CORE-BULK-NUMERIC-NOOP-LEXICAL-PRESERVATION`
- Implementation PR: `#637`
- Main integration commit: `aeae074ae42c0aa0ecc84dd0596e1df0b2f6b023`

## Confirmed defect

`BulkEditService.MultiplyNumericProperty(...)` formatted the multiplied `double` and decided whether the operation was a no-op by comparing that formatted text with the stored property text. A numerically unchanged value such as explicit instance `WidthM="1.0"` multiplied by `1d` therefore became `"1"`, was reported as changed, marked semantic/geometry freshness dirty and advanced `ProjectState.ChangeVersion` even though the numeric value did not change.

The separate selection-oriented `SemanticSelectionBulkEditService` had already established the correct exact-numeric no-op contract in merged PR #633 / `4e08d2c671039ee7509ccd5bc51db8495ef52248`. This lane aligned the distinct Core `BulkEditService` API without modifying the selection implementation.

## Implemented scope

`BulkEditService.MultiplyNumericProperty(...)` now checks exact computed/parsed equality with `next.Equals(current)` immediately after the existing overflow guard and before `ToString("R", ...)` formatting. Exact numeric no-ops therefore preserve the original lexical property text and never enter the mutation executor.

All existing editable-key validation, bounded target materialization, parsing, non-finite/overflow rejection, all-or-nothing mutation execution, dirty-flag policy and real multiplication behavior remain unchanged.

## Regression source

Added `tests/QS3D.Core.SmokeTests/BulkEditNumericNoOpSmoke.cs` with module-initializer coverage for:

- explicit geometry property `WidthM="1.0"` x1: zero changed ids, exact lexical text preserved, project/element freshness unchanged, no generated-solid stale state;
- editable non-geometry property `Scale="1.0"` x1: zero changes with lexical/freshness preservation;
- real `WidthM` x2: changed id returned, round-trip text `"2"`, project revision +1, existing Geometry/Properties/Quantity dirty semantics preserved and generated solid marked stale.

## Coordination / exclusions preserved

- `src/QS3D.Core/Selection/SemanticSelectionBulkEditService.cs` and PR #633 were not modified.
- `BulkEditService.AssignFamily(...)`, Family property canonicality, `SetProperty(...)`, persistence schemas, adapters/WPF/native BricsCAD runtime were not modified.
- No tolerance policy was introduced; only exact parsed/computed `double` equality is treated as a no-op.
- No force-push, GitHub Actions/build/release dispatch, or V25/V26 runtime qualification was performed.

## Validation evidence

- Claim registration was committed to `main` before source edits at `ae225748053366ef67d46705c88bb60b4267e222`.
- A post-claim comparison proved current `main` was ahead of that claim with no intervening `BulkEditService` modification; target source remained blob `00f0f40be82b17b9ccf188c2b5ec6f448dbd1712` immediately before implementation/integration review.
- Branch source fix commit: `6ec2c60bee0df60773f530dc2067341d60b32875`.
- Focused smoke commit/head: `21642b24d09288fa55c62bfe82e9c91a4400b9ef`.
- PR #637 exact diff was reviewed before merge and contained exactly two files, `+121/-1`; the production hunk only moved no-op detection from formatted-text equality to exact numeric equality before formatting.
- Server-side squash merge with exact expected head `21642b24d09288fa55c62bfe82e9c91a4400b9ef` produced `aeae074ae42c0aa0ecc84dd0596e1df0b2f6b023`.
- Post-merge readback on `main` confirms source blob `d8f9d2a5dae99f0a0f73d66d85d60d08009617e2` contains `if (next.Equals(current)) continue;` before formatting, and smoke blob `e7bbbbc7f0bbe1ed1d21bc41f7d8b6b6ca735b4a` is present.
- The smoke executable/build was not run in this connector-only environment; no local .NET or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion

`COMPLETED`: current `main` preserves lexical text, project/element freshness and generated-output state for exact numeric `BulkEditService` no-ops while retaining the established behavior for real numeric mutations.