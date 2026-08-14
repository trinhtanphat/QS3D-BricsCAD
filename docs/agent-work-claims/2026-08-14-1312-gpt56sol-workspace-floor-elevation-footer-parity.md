# Work claim — Workspace floor elevation footer parity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-floor-footer`
- Registered: `2026-08-14T13:12:00+07:00`
- Baseline main SHA: `008b668766bc4ea27d7b072dacc6d418f3cb131b`
- Owner request: continue all and complete remaining screenshot/session gaps without speculative native-runtime changes.
- Implementation SHA: `8adfe6655512ca707338c177ef30b787424db8a8`
- Focused guard SHA: `73e2b2e516792eaf64292d151d752e6f72ac7623`
- Plan update SHA: `55f47c271bd6b77081e8930516c134af36dc90fb`

## Concrete screenshot gap completed

The supplied BLT3D reference footer shows the active floor together with its elevation (`Tầng … • Cao độ 0.000 m`). QS3D already showed live Project / Zone / Floor in `WorkspacePanel.FooterContext.cs`; the completed implementation now also reads canonical `FloorDefinition.ElevationM` and renders `CAO ĐỘ` with invariant `0.000 m` precision.

## Completed scope

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FooterContext.cs`
  - resolves the active Floor once;
  - preserves the existing floor-name display;
  - renders `CAO ĐỘ` from `FloorDefinition.ElevationM`;
  - formats finite values with invariant three-decimal meter precision;
  - falls back to `—` for missing/non-finite values;
  - retains the presentation-only exception boundary.
- `scripts/preflight-workspace-footer-context.py`
  - requires active Floor lookup, `ElevationM`, invariant formatting and the `CAO ĐỘ` token;
  - still rejects active Zone/Floor setters, Floor updates, project touch/version changes, active-id assignments and `ElevationM` assignments.
- `docs/BLT-REFERENCE-UI-PARITY-PLAN-2026-08-14.md`
  - records the footer gap, implementation and local/native visual acceptance boundary.

## Validation boundary

Exact `main` readback confirms the source and focused guard agree on the read-only elevation presentation contract. No GitHub Actions run or licensed BricsCAD runtime result is claimed for these commits. Width/clipping, Windows DPI, dark theme and final host rendering remain local/native acceptance under the existing UI/runtime lane; no `LOCAL_PASS` is claimed.

No #1125 Level/Curtain frame-Z production logic, RightPanel active-claim files, startup/Ribbon lifecycle or LOCAL_ONLY runtime surfaces were changed by this claim.
