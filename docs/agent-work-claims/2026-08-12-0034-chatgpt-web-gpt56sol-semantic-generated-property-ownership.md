# Work claim — Semantic property edit generated-state ownership guard

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:34:00+07:00`
- Baseline main SHA: `a635389922783037280012be94a9d5f6b80d541e`
- Priority: evidence-driven remote-safe generated ownership integrity

## Confirmed defect

`SemanticPropertyEditPolicy` blocks `QS3D.Generated...` keys and any key containing `Handle`, but does not block the unnamespaced `Generated...` prefix. `ProjectElement` uses the real native/generated state key `GeneratedCurtainPanelBuildState`, which contains no `Handle` and therefore currently passes the generic semantic edit policy. The interchange portability policy already blocks every `Generated...` prefix as drawing-local/generated state.

A generic semantic edit can therefore overwrite generated build-state metadata such as `GeneratedCurtainPanelBuildState` and falsify the state used to determine whether a curtain-panel output has a valid completed-empty generated signature.

## Reserved scope

Make all `Generated...` prefixed generic semantic property keys non-editable, preserving the existing `QS3D.Generated...`, handle, physical-opening, identity/reference and ordinary semantic-property behavior.

## Expected surfaces

- `src/QS3D.Core/Services/SemanticPropertyEditPolicy.cs`
- `tests/QS3D.Core.SmokeTests/SemanticPropertyGeneratedOwnershipSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SemanticPropertyGeneratedOwnershipRegistration.cs`
- this claim file

## Excluded scope

- No `ProjectElement` generated lifecycle implementation changes.
- No regeneration/native CAD ownership logic changes.
- No interchange policy changes; its existing prefix boundary is consistency evidence only.
- No ordinary semantic property behavior changes.
- No GitHub Actions dispatch.

## Validation plan

- Preserve an ordinary semantic property as editable.
- Reject `GeneratedCurtainPanelBuildState` through public `IsEditablePropertyKey`.
- Reject representative unnamespaced `Generated...` metadata without `Handle`, case-insensitively.
- Preserve blocking of `QS3D.Generated...`, handle, physical-opening and identity/reference keys.
- Use a dedicated module initializer, re-fetch the target blob before product write, inspect exact diffs and verify ancestry.
- No .NET/V25 runtime PASS will be claimed unless actually executed.

## Coordination

The preceding completed lane added only the missing `QS3D.PhysicalOpeningCut...` namespace guard on this file. This new lane is separately scoped to the unnamespaced `Generated...` bypass demonstrated by the existing `GeneratedCurtainPanelBuildState` key. Recent claim search found no other reservation for this exact gap.

## Completion condition

Unnamespaced generated ownership/build state cannot be changed through the generic semantic property editor, focused regression source is on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.