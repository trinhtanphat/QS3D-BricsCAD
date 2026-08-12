# Agent Work Claim — Semantic handle self-collision parity

- Agent: `chatgpt-gpt56sol-semantic-handle-self-collision`
- Owner: OpenAI ChatGPT
- Status: `ACTIVE`
- Registered: 2026-08-12 09:31 +07:00
- Baseline main SHA observed: `4721cc060f242edc67e4d2ec14cb2981ce8e6f60`
- Task key: `CORE-SEMANTIC-HANDLE-SELF-COLLISION`

## Confirmed defect

`SafeGeneratedHandleOwnershipHealthService` treats a CAD handle claimed by the same semantic element through distinct logical ownership channels as `GENERATED_HANDLE_OWNERSHIP_CONFLICT`, unless `GeneratedHandleOwnershipPolicy.AreSameLogicalOwnerSlots(...)` says the slots are aliases of the same logical owner. In contrast, `SemanticHandleOwnershipResolver.Add(...)` currently returns immediately whenever the existing owner is the same `ProjectElement`, ignoring the ownership channel.

Therefore a publicly constructible state such as one element with `SourceHandles = ["A1"]` and `GeneratedSolidHandle = "A1"` is reported as conflicting by Model Health but silently accepted by selection-side semantic ownership resolution. Source/input CAD ownership and generated/output CAD ownership must not collapse merely because they occur on the same semantic element.

## Reserved scope

- `src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs`
- one focused Core smoke source for same-element ownership-channel collisions
- this claim file

## Excluded scope

- `GeneratedHandleOwnershipPolicy.cs` behavior changes
- health-service behavior changes
- generated-solid/category canonicality lanes
- BricsCAD/UI/selection adapter changes
- Actions/build/release/runtime qualification

## Plan

1. Preserve exact selected-handle scoping and existing duplicate-element-ID checks.
2. When the same element is already recorded for a selected handle, compare the previous and current channels with `GeneratedHandleOwnershipPolicy.AreSameLogicalOwnerSlots(...)` instead of accepting all same-element duplicates.
3. Permit true logical aliases such as `GeneratedSolidHandle` / `PhysicalOpeningCutSolidHandle` while rejecting `SourceHandles` ↔ generated-output and distinct generated logical slots.
4. Add focused smoke coverage for self-collision rejection, logical-alias allowance, and existing distinct-element ambiguity behavior.
5. Re-read moving `main`, review exact diff, merge only if the reserved source is still untouched, then close this claim with immutable evidence.

No GitHub Actions/build/release is authorized. Smoke source may be added but will not be claimed as executed. No BricsCAD V25/V26 runtime PASS will be claimed remotely.
