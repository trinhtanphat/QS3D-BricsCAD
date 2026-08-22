# Agent work claim — advanced geometry representable-area overflow

- Status: `RELEASED`
- Agent: `chatgpt-web-gpt56sol-advanced-geometry-representable-area`
- Registered: `2026-08-14T23:42:00+07:00`
- Baseline main SHA: `daf6b9485c72ce315fcaa15d10f4e60ebbee8cef`
- Implementation branch: `agent/chatgpt-web-gpt56sol/advanced-geometry-representable-area-20260814`
- Integration batch: `integration/chatgpt-web-gpt56sol-advanced-geometry-representable-area-20260814`
- Priority: advanced geometry / edge-case numerical correctness

## Reserved scope

Harden finite 2D polygon signed-area and simple-polygon/orientation validation for extreme-scale concave geometry where raw determinant or twice-area intermediates can overflow `double` even though the final signed area is finite and representable. Preserve fail-closed behavior when the final requested numeric result is genuinely non-representable or an input is non-finite.

## Expected surfaces

- `src/QS3D.Core/Geometry/PolylineMetrics.cs` — robust signed-area accumulation/restoration that does not reject a representable final area solely because an intermediate determinant or twice-area exceeds `double.MaxValue`.
- `src/QS3D.Core/Geometry/PolygonScanlineClipper.cs` — align polygon validation/orientation predicates with the robust determinant semantics without weakening finite-input validation.
- one new focused self-registering geometry smoke under `tests/QS3D.Core.SmokeTests/` covering an extreme concave polygon plus reversed winding and an end-to-end public clipping/validation path.
- this claim file for coordination and closeout evidence.

## Excluded scope

- No BricsCAD native `Document` / `Database` / `Transaction` / `Editor` work.
- No LOCAL_ONLY licensed-runtime qualification, private DWG, UI/DPI, packaging, signing, rebar, persistence, interchange, schedule, quantity, or unrelated geometry refactors.
- No change to normal-scale polygon tolerance policy unless required to preserve existing semantics while replacing overflow-prone arithmetic.
- No manual GitHub Actions dispatch/rerun/cancel.

## Validation plan

- Re-fetch current source after this claim is visible on refreshed `main` and re-check overlapping ACTIVE/BLOCKED claims before source writes.
- Add deterministic regression using a finite large concave polygon whose true area is about `9.975e307`, below `double.MaxValue`, while naive fan determinants/twice-area exceed representable range.
- Verify clockwise/counterclockwise winding returns equal magnitude/opposite sign, and exercise a public polygon clipping/validation path so predicate overflow is covered end-to-end.
- Preserve failure for genuinely non-representable final area and existing normal geometry behavior.
- Publish implementation only on the dedicated agent branch, reconcile against fresh `main`, integrate through the named integration branch, then record actual exact-SHA/CI evidence without manufacturing PASS.

## Coordination / release reason

`RELEASED` because this reservation duplicated the earlier canonical ACTIVE claim `docs/agent-work-claims/2026-08-14-2326-chatgpt-web-gpt56sol-advanced-geometry-representable-area-overflow.md`, whose implementation branch already contains the source and focused regression commits. The 23:26 claim remains authoritative; this duplicate must not reserve a second lane or trigger duplicate implementation.

## Completion condition

No implementation is owned by this duplicate reservation. Completion/closeout belongs exclusively to the canonical 23:26 claim after its integration is reviewed and landed under the repository policy.
