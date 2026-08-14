# Work claim — Semantic Selection quantity-key canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-selection-quantity-key-canonicality-20260814-0859`
- Registered: `2026-08-14T08:59:00+07:00`
- Baseline main SHA: `3aed2b5af29c33accb0e3df637e2f22e28c4e731`
- Priority: `MTR-05 / P1 Core semantic-integrity hardening` — semantic selection must not surface malformed quantity identities created by bypassing the canonical setter.

## Confirmed source gap

`ProjectElement.SetQuantity(...)` canonicalizes quantity names by requiring a nonblank token and trimming surrounding whitespace before storing it. `SemanticSelectionInspector.InspectQuantities(...)` previously enumerated the public `Quantities` dictionaries directly and trusted their raw keys. A direct/bypassed write could therefore expose a blank or padded key such as `" LengthM "` as a public `SemanticSelectionQuantityValue.Name`, and malformed raw identities could cross the selection projection boundary.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs` — quantity-key validation inside `InspectQuantities(...)` only.
- `tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorSmoke.cs` — focused regression only.
- this claim file.

## Implemented acceptance

1. Selected elements now fail closed when a direct quantity key is blank/whitespace-only.
2. Selected elements now fail closed when a nonblank direct quantity key has surrounding whitespace.
3. Canonical quantity names, case-insensitive identity semantics, finite/signed-zero value handling, present/mixed counts, deterministic ordering and property/reference inspection behavior remain unchanged.
4. Inspection remains read-only; malformed dictionary keys are not rewritten.

## Explicit non-scope

No changes to `ProjectElement.SetQuantity`, quantity values/arithmetic, control-character policy, reports/MAP/revisions, persistence, rules, measurement, cost, IFC, documentation/layout, update/release, LOCAL/native/UI or BricsCAD adapters. No GitHub Actions dispatch and no force-push.

## Evidence / history

- `2b2b1479afbd61abed1fd43b0dfc3125a3b73c41` requires canonical nonblank quantity names at the public revision-summary boundary.
- `eaa0116865773848666697be09187c80a1bfd90e` protects persisted quantity identity from ambiguous duplicates.
- Targeted history search found no pre-existing semantic-selection quantity-key canonicality lane; the earlier signed-zero Selection lane was already `COMPLETED` and touched value representation only.

## Completion record

- Claim-only commit: `6540d757ff2a9b065f604551e92374e8fd129d14`.
- Production fix: `4a9d4068079ba5ac770d721f3072c796b43d526e` (`fix(core): reject noncanonical selection quantity keys`). Exact commit inspection showed the source diff is limited to quantity-key validation around the existing key-collection loop.
- Focused regression: `486f65e65dac943ef53d9502616da3d7d8b24fa1` (`test(core): guard selection quantity key canonicality`). Regression covers whitespace-only keys, padded keys, read-only failure behavior and canonical-key success.
- Remote verification: current `main` was re-fetched at `5c93c9e61994e184fc7a7568d699c1ad1b4a8b90`; live source still contains the fail-closed key guards and live smoke still contains `QuantityKeysMustBeCanonical()`.
- Concurrent reconciliation: unrelated BulkEdit/report/documentation work advanced `main` without touching either reserved Selection file.
- Managed .NET/Core smoke execution: `NOT_RUN` — no executable repository .NET toolchain was available through this connected workflow.
- GitHub Actions: `NOT_DISPATCHED`.
- BricsCAD/native runtime qualification: `NOT_RUN` / not claimed.

## Completion

Satisfied: current `main` rejects blank/padded direct quantity keys at the semantic-selection projection boundary, focused regression coverage is pushed and remotely verified, and the lane is closed `COMPLETED` with exact commit references and truthful validation status.
