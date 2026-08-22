# Work claim — Curtain Panel live-handle numeric identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-curtain-panel-live-handle-numeric-identity-20260812`
- Registered: `2026-08-12T13:22:00+07:00`
- Baseline main SHA: `6067399efbe4a815023fbba07ccc7a46b4224988`
- Task Key: `CORE-CURTAIN-PANEL-LIVE-HANDLE-NUMERIC-IDENTITY`
- Integration: `705c79a1cabc074336da52fbb62aaf039217b311` (PR #918)

## Defect

`GeneratedCurtainPanelHealthService.Inspect(...)` validated hexadecimal generated panel handles but kept their trimmed textual spelling for duplicate counting and live-CAD membership. The shared generated-handle ownership contract canonicalizes numeric handle identity, so persisted `GeneratedCurtainPanelHandles=000A` and a live handle set containing canonical `A` represented the same CAD object but could still emit a false `CURTAIN_PANEL_GENERATED_SOLID_MISSING`; numeric-equivalent duplicate spellings could also be counted as separate handles.

The older curtain-panel handle canonicality lane only covered whitespace padding. The active LOCAL-002/P05 Curtain stale/rebuild claim reserves runtime probe/scripts/docs and explicitly keeps production health/builders read-only, so this source lane did not overlap it.

## Repair

- live panel handles are normalized once with `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`;
- each valid persisted panel handle uses the same normalized identity for duplicate counting, ownership lookup, and live membership;
- existing invalid-hex and whitespace-canonicality diagnostics remain unchanged, including lowercase-handle acceptance;
- `GeneratedCurtainPanelNumericHandleIdentitySmoke` covers numeric-equivalent live handles, numeric-equivalent duplicate spellings, a truly missing handle, count stability, and inspection immutability.

## Validation

- exact PR diff reviewed: 2 files, +133/-3;
- source and focused regression read back from `main` after merge;
- `compare_commits(705c79a1cabc074336da52fbb62aaf039217b311, main)` returned `behind_by=0` at verification time (main was one unrelated RebarMath commit ahead);
- no GitHub Actions/full build/executable smoke/BricsCAD V25/V26 runtime PASS claimed.
