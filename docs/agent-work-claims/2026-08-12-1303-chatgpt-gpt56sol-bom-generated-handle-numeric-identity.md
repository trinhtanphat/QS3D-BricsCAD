# Work claim — BOM generated handle numeric identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-bom-generated-handle-numeric-identity-20260812`
- Registered: `2026-08-12T13:03:00+07:00`
- Baseline main SHA: `705682b5833af9a631b97a56de6656d21e483ab2`
- Task Key: `CORE-BOM-GENERATED-HANDLE-NUMERIC-IDENTITY`

## Defect

`BomReleaseGuardService.Inspect(...)` canonicalizes caller-supplied live CAD handles only with `Trim()` and then compares them directly to generated owner handles. The canonical generated-handle ownership contract now uses `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`, so numerically equivalent CAD handles such as `000A`, `0xA`, and `A` can be treated as different by the BOM release guard and emit a false `BOM_GENERATED_HANDLE_MISSING` release blocker.

## Repair scope

- normalize live generated handles with the shared generated-handle identity policy;
- normalize owner handles at the containment boundary for defensive consistency;
- preserve empty-handle filtering and case-insensitive set semantics;
- add focused Core regression coverage for numeric-equivalent live handles and a truly missing handle.

## Validation boundary

No GitHub Actions/full build/executable smoke/BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
