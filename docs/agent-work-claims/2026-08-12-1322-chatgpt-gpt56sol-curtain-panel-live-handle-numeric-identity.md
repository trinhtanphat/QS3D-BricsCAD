# Work claim — Curtain Panel live-handle numeric identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-curtain-panel-live-handle-numeric-identity-20260812`
- Registered: `2026-08-12T13:22:00+07:00`
- Baseline main SHA: `6067399efbe4a815023fbba07ccc7a46b4224988`
- Task Key: `CORE-CURTAIN-PANEL-LIVE-HANDLE-NUMERIC-IDENTITY`

## Defect

`GeneratedCurtainPanelHealthService.Inspect(...)` validates hexadecimal generated panel handles but keeps their trimmed textual spelling for duplicate counting and live-CAD membership. The shared generated-handle ownership contract canonicalizes numeric handle identity, so persisted `GeneratedCurtainPanelHandles=000A` and a live handle set containing canonical `A` represent the same CAD object but can still emit a false `CURTAIN_PANEL_GENERATED_SOLID_MISSING`; numeric-equivalent duplicate spellings can also be counted as separate handles.

The older curtain-panel handle canonicality lane only covered whitespace padding. The active LOCAL-002/P05 Curtain stale/rebuild claim reserves runtime probe/scripts/docs and explicitly keeps production health/builders read-only, so this source lane does not overlap it.

## Repair scope

- normalize live panel handles once with `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`;
- normalize each valid persisted panel handle for local identity, duplicate counting, ownership lookup, and live membership;
- preserve existing invalid-hex and whitespace-canonicality diagnostics;
- add focused Core regression coverage for numeric-equivalent live handles, numeric-equivalent duplicate spellings, and a truly missing handle.

## Validation boundary

No GitHub Actions/full build/executable smoke/BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
