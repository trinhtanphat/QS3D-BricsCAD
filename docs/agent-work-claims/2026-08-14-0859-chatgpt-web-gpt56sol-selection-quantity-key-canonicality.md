# Work claim — Semantic Selection quantity-key canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-selection-quantity-key-canonicality-20260814-0859`
- Registered: `2026-08-14T08:59:00+07:00`
- Baseline main SHA: `3aed2b5af29c33accb0e3df637e2f22e28c4e731`
- Priority: `MTR-05 / P1 Core semantic-integrity hardening` — semantic selection must not surface malformed quantity identities created by bypassing the canonical setter.

## Confirmed source gap

`ProjectElement.SetQuantity(...)` canonicalizes quantity names by requiring a nonblank token and trimming surrounding whitespace before storing it. `SemanticSelectionInspector.InspectQuantities(...)`, however, enumerates the public `Quantities` dictionaries directly and trusts their raw keys. A direct/bypassed write can therefore expose a blank or padded key such as `" LengthM "` as a public `SemanticSelectionQuantityValue.Name`, and two raw keys that should represent one canonical quantity can be projected as distinct semantic quantities. Neighboring revision/persistence/rule boundaries already enforce quantity-name canonicality rather than silently accepting malformed identities.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs` — quantity-key validation inside `InspectQuantities(...)` only.
- `tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorSmoke.cs` — focused regression only.
- this claim file.

## Acceptance

1. Fail closed when a selected element exposes a blank/whitespace-only quantity key through direct dictionary mutation.
2. Fail closed when a selected element exposes a nonblank quantity key with surrounding whitespace.
3. Preserve canonical quantity names, case-insensitive identity semantics, finite/signed-zero value handling, present/mixed counts, deterministic ordering and all property/reference inspection behavior.
4. Inspection remains read-only and does not rewrite corrupted dictionary keys.

## Explicit non-scope

No changes to `ProjectElement.SetQuantity`, quantity values/arithmetic, control-character policy, reports/MAP/revisions, persistence, rules, measurement, cost, IFC, documentation/layout, update/release, LOCAL/native/UI or BricsCAD adapters. No GitHub Actions dispatch and no force-push.

## Evidence / history

- Current `SemanticSelectionInspector.InspectQuantities(...)` at the baseline adds raw dictionary keys to its projection key set with no blank/canonical check.
- `2b2b1479afbd61abed1fd43b0dfc3125a3b73c41` requires canonical nonblank quantity names at the public revision-summary boundary.
- `eaa0116865773848666697be09187c80a1bfd90e` protects persisted quantity identity from ambiguous duplicates.
- Targeted history search found no existing semantic-selection quantity-key canonicality lane; the earlier signed-zero Selection lane is `COMPLETED` and touched value representation only.

## Validation plan

Publish this claim alone to `main`, refresh current HEAD and recheck exact Selection source/test overlap, then apply the smallest fail-closed key validation and extend the existing self-registered inspector smoke. Re-fetch exact source/test diffs and current `main`, close this claim `COMPLETED`, and report managed/native execution as `NOT_RUN` unless actually executed.

## Completion condition

Current `main` rejects blank/padded direct quantity keys at the semantic-selection projection boundary, focused regression coverage is pushed and remotely verified, and this claim is closed `COMPLETED` with exact commit references and truthful validation status.
