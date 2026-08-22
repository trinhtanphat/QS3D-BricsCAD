# Work claim — Regeneration preview subset bounded targets

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-preview-target-bound`
- Registered: `2026-08-12T00:03:00+07:00`
- Completed: `2026-08-12T00:05:00+07:00`
- Baseline main SHA: `4e6939c675083ca11fd34e05624cfff25d4c239a`
- Reservation commit: `a269ba2e35530daa7b7c03dc472227948b3c626c`
- Priority: P1 — subset preview must not enumerate more targets than could exist in the project.

## Defect fixed

`RegenerationPreviewService.PreviewSubset(...)` previously canonicalized the caller-provided `IEnumerable<string>` before `PreviewInternal(...)` validated `project`, and `CanonicalPreviewTargets(...)` consumed the full enumerable without a cardinality bound. A valid unique preview target set cannot exceed `project.Elements.Count`, yet an oversized or non-terminating sequence could consume unbounded time/memory before project/target resolution occurred.

`PreviewSubset(...)` now rejects a null project before touching the target enumerable and canonicalization uses the current project element count as the exact maximum possible valid unique target count. Blank/padded and duplicate validation remain ahead of the cardinality guard.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationPreviewService.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationPreviewTargetBoundSmoke.cs`
- this claim file

## Published commits

- `8342ec75ae60f0dabdbfb58bb2fb23c05db4acea` — validate project eagerly and bound unique preview targets by `project.Elements.Count` while preserving canonical/duplicate semantics and sorting.
- `150637851d176a7a7ce3b203c689b62bab18790e` — add isolated auto-registered smoke for null-project non-enumeration, sentinel bound, exact-cardinality acceptance, and duplicate precedence.

## Delivered contract

- Null project input fails before caller target enumeration begins.
- Preview subset cannot consume unique target IDs beyond the largest subset that could possibly exist in the current project.
- No valid target subset is rejected by the new bound.
- Existing sorted target identity, duplicate diagnostics, preview equivalence, health/revision comparison, and guarded apply behavior are unchanged.

## Validation notes

- Exact source/test diffs were fetched after publication and are limited to reserved surfaces.
- The sentinel tests are structured so previous behavior would continue target enumeration beyond the maximum possible project subset, while the new behavior fails at the first impossible unique target.
- Dedicated smoke auto-registers via `ModuleInitializer`; shared smoke registration was not edited.
- No force-push and no GitHub Actions dispatch.
- This hosted environment does not provide the repository .NET/BricsCAD V25 qualification toolchain, so executable/native runtime PASS is not claimed.

## Completion condition

Satisfied for the remote-safe source/static contract. Exact executable/native qualification remains separate.
