# Work claim — Regeneration preview subset bounded targets

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-preview-target-bound`
- Registered: `2026-08-12T00:03:00+07:00`
- Baseline main SHA: `4e6939c675083ca11fd34e05624cfff25d4c239a`
- Priority: P1 — subset preview must not enumerate more targets than could exist in the project.

## Confirmed defect

`RegenerationPreviewService.PreviewSubset(...)` canonicalizes the caller-provided `IEnumerable<string>` before `PreviewInternal(...)` validates `project`, and `CanonicalPreviewTargets(...)` currently consumes the full enumerable without a cardinality bound. A valid unique preview target set cannot exceed `project.Elements.Count`, yet an oversized or non-terminating sequence can currently consume unbounded time/memory before project/target resolution occurs.

This is a distinct read-only preview boundary from the targeted apply engine. The exact project element cardinality is again a natural semantic maximum, not a new product policy limit.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationPreviewService.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationPreviewTargetBoundSmoke.cs` (new auto-registered focused smoke)
- this claim file

## Intended contract

- `PreviewSubset(...)` validates `project` before enumerating target IDs.
- Unique preview target enumeration stops before accepting target `project.Elements.Count + 1`.
- Existing blank/padded/duplicate validation precedence and sorting remain unchanged.
- Exact-cardinality target sets remain accepted and downstream unknown-target/preview behavior remains unchanged inside the bound.
- No preview equivalence, health, revision, apply, regenerator, or native behavior changes.

## Coordination

The just-completed `RegenerationEngine` subset-target bound covers the apply/engine path only. This claim is isolated to `RegenerationPreviewService.cs`. Recent recognition enumerable work and other active claims do not reserve this source.

## Validation plan

- Add a sentinel enumerable proving two-element project preview rejects the third unique target before requesting a fourth.
- Prove a null project fails before target enumeration.
- Preserve duplicate-target diagnostic precedence.
- Re-fetch source before update, SHA-guard all writes, inspect published diffs, then close the claim.
- No GitHub Actions dispatch; no executable .NET or BricsCAD V25 runtime PASS claim from this hosted environment.

## Completion condition

Subset preview input is bounded at the maximum possible valid project cardinality, null-project failure is eager, focused regression is on `main`, and this claim is closed.
