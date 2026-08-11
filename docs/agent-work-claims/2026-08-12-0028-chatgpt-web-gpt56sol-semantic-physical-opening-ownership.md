# Work claim — Semantic property edit physical-opening ownership guard

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:28:00+07:00`
- Baseline main SHA: `997eab1c953a5f943074bda103928999cb2379c0`
- Priority: evidence-driven remote-safe ownership integrity

## Confirmed defect

`SemanticPropertyEditPolicy` blocks generic edits for keys beginning with `PhysicalOpeningCut` but does not block the namespaced form `QS3D.PhysicalOpeningCut...`. The interchange portability policy already treats both prefixes as drawing-local/native ownership state. As a result, a generic semantic property edit can currently pass `IsEditablePropertyKey` for namespaced physical-opening ownership metadata and bypass the intended native/generated-state protection.

## Reserved scope

Make `QS3D.PhysicalOpeningCut...` generic semantic property keys non-editable, preserving all existing rules for ordinary semantic properties, CAD-derived keys, identity/reference keys, handles and generated state.

## Expected surfaces

- `src/QS3D.Core/Services/SemanticPropertyEditPolicy.cs`
- `tests/QS3D.Core.SmokeTests/SemanticPropertyPhysicalOpeningOwnershipSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SemanticPropertyPhysicalOpeningOwnershipRegistration.cs`
- this claim file

## Excluded scope

- No physical-opening boolean, target-state codec, host/cutter/native service or command changes.
- No interchange policy changes; its existing two-prefix rule is evidence for consistency only.
- No changes to ordinary user-editable semantic property names.
- No GitHub Actions dispatch.

## Validation plan

- Preserve a representative ordinary semantic property as editable.
- Preserve existing blocking of `PhysicalOpeningCut...`, generated, handle and identity/reference keys.
- Reject representative `QS3D.PhysicalOpeningCut...` fingerprint/target-state keys through the public `IsEditablePropertyKey` API.
- Use a dedicated module initializer to avoid shared smoke registration contention.
- Re-fetch target blob immediately before product write and review exact pushed diffs/ancestry.
- No .NET/V25 runtime PASS will be claimed unless actually executed.

## Coordination

Recent searches found no active/recent claim reserving `SemanticPropertyEditPolicy.cs` or this exact generic-edit namespace gap. Current physical-opening claims target native boolean revision, cut ownership/target state and host enumeration rather than the Core generic property edit policy. The first claim-write attempt received HTTP 409 because `main` moved; no file/product change was published by that failed attempt.

## Completion condition

Namespaced physical-opening ownership state is consistently protected from generic semantic edits, focused regression source is on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.