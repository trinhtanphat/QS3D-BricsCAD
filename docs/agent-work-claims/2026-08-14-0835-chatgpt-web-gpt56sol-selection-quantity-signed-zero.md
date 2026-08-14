# Work claim — Semantic Selection quantity signed-zero projection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-selection-quantity-signed-zero-20260814-0835`
- Registered: `2026-08-14T08:35:00+07:00`
- Baseline main SHA: `966861a0cc507b55520093f772edaea23e9decb4`
- Priority: `P1 Core semantic-integrity hardening` — public semantic selection quantity projections must not leak IEEE negative-zero representation

## Confirmed source gap

`ProjectElement.SetQuantity()` already canonicalized exact zero to positive `0d`, and its completed setter claim explicitly left direct writes through the public `Quantities` dictionary for downstream defensive boundaries. Quantity Report and MAP projections already canonicalized signed zero. `SemanticSelectionInspector.InspectQuantities(...)` rejected NaN/Infinity but copied a finite zero directly into `SemanticSelectionQuantityValue.Value`; a bypassed/direct `-0d` could therefore leak negative-zero bits through the selection projection. Because numeric equality considers `-0d` and `+0d` equal, a multi-selection could also expose the sign representation of the first sorted element rather than a canonical value.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs` — quantity projection canonicality only
- `tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorSmoke.cs` — focused regression only
- this claim file

## Acceptance

1. Canonicalize finite exact-zero quantity values to positive `0d` while materializing selection quantity inspection.
2. A single directly injected `-0d` quantity projects as numeric zero with positive-zero bits.
3. `-0d` and `+0d` across selected elements remain non-mixed and project canonical positive zero independent of caller selection ordering.
4. Preserve non-finite fail-closed behavior, missing/present counts, ordinary mixed numeric detection, quantity names/order and all property/reference inspection semantics.
5. Do not mutate project quantities while inspecting.

## Evidence / history

- `b0d55331bca2c7bff4d0709407eac8063443bb3d` completed canonical `ProjectElement.SetQuantity()` signed-zero handling and explicitly kept direct dictionary writes available for downstream defensive projections.
- `5426f0a801ad0f51d288a79391467b688e471f8d` canonicalized signed zero at the public Quantity Report projection boundary.
- Targeted commit search found no pre-existing semantic-selection signed-zero claim/fix before registration.

## Explicit non-scope

No changes to `ProjectElement.SetQuantity`, quantity arithmetic, reports/MAP, measurement, persistence, cost, IFC, recognition, properties/references, UI or BricsCAD/native adapters. No GitHub Actions dispatch; no force-push.

## Completion record

- Claim-only commit: `d84da4985147a8868cb24460af5e9d6c6b1aaef1`.
- Production fix: `eb6d5666936f9484f38e195a3b11fad947d58892` (`fix(core): canonicalize selection quantity signed zero`). Exact commit-diff inspection confirmed the full-file write has a net production diff of exactly one added normalization line.
- Focused regression: `f9760b40bdba248e81966c7064585edd422767ea` (`test(core): guard selection quantity signed zero`). The smoke verifies a directly injected IEEE negative zero projects with positive-zero bits, equivalent `-0/+0` values remain non-mixed, caller selection order does not leak representation, and the original quantity dictionary/sign bits plus project change version remain untouched by inspection.
- Remote verification: current `main` was re-fetched at `f9760b40bdba248e81966c7064585edd422767ea`; the live source retained the one-line zero canonicalization and the focused regression diff remained present.
- Concurrent reconciliation: Recognition work advanced independently between this lane's commits and did not touch either reserved Selection file.
- Managed .NET/Core smoke execution: `NOT_RUN` — no executable .NET repository toolchain was available through this connected workflow.
- GitHub Actions: `NOT_DISPATCHED`.
- BricsCAD/native runtime qualification: `NOT_RUN` / not claimed.

## Completion

Satisfied: the semantic selection quantity projection now emits canonical positive zero without mutating source state, focused regression coverage is on current `main`, exact diffs/current source were verified remotely, and the lane is closed `COMPLETED` with truthful validation status.
