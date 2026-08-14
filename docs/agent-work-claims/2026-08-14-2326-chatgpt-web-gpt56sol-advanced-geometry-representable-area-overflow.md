# Agent work claim — Advanced geometry representable-area overflow hardening

- Agent: `chatgpt-web-gpt56sol-advanced-geometry-overflow`
- Date: `2026-08-14`
- Status: `ACTIVE`
- Baseline main SHA: `86c7ac72ad976807a5081977453940d10311aa8b`
- Claim publication SHA: `1d264de37aad4a0540a586a7dd62bbbbbd413299`
- Implementation branch: `agent/chatgpt-web-gpt56sol/advanced-geometry-representable-area-20260814`
- Implementation commits: `7d1bf659ff856bf907db5e6523730221779a03f1`, `7faacd0722aa60e74f33e763408ce13f1260ae63`, `0c5769b73a52caa0f003ed8ec1529de79ff50b13`
- Integration branch: `integration/chatgpt-web-gpt56sol-advanced-geometry-representable-area-20260814`
- Integration PR: `#1381`
- Integration candidate SHA: `d761529fd0af7e803af755442ace46d7049b0b3a`
- Priority: Core advanced geometry / numerical edge cases

## Confirmed defect

A finite, valid simple concave polygon can have a signed area that is representable as `double` while one or more translated cross products, or the accumulated twice-area, exceed `double.MaxValue`. The pre-fix `PolylineMetrics.SignedArea(...)` restored each cross product before applying the final `0.5`, while `PolygonScanlineClipper.NormalizeAndValidate(...)` accumulated raw twice-area and polygon orientation required a fully representable determinant. These paths therefore rejected some valid finite geometry because of intermediate overflow even when the final area or predicate result was well-defined.

## Reserved scope

- `src/QS3D.Core/Geometry/PolylineMetrics.cs` — make signed-area accumulation scale-aware so only a truly unrepresentable final area fails closed.
- `src/QS3D.Core/Geometry/PolygonScanlineClipper.cs` — reuse representable signed-area semantics for validation and make orientation predicates sign-safe when determinant magnitude itself exceeds `double`.
- `tests/QS3D.Core.SmokeTests/AdvancedGeometryRepresentableAreaOverflowSmoke.cs` — clockwise/counter-clockwise signed-area and end-to-end scanline regression for an extreme finite concave polygon.
- this claim for coordination/close-out.

## Explicit exclusions / concurrency protection

- No BricsCAD native `Document`, `Database`, entity, transaction, editor or licensed-runtime changes.
- No changes to region-set ownership, bulge tessellation, rebar, quantity, domain persistence, UI, CI/package, release/signing or unrelated geometry surfaces.
- No `SmokeTestRegistration.cs` change; regression uses the repository's current self-registration pattern.
- No weakening of non-finite input rejection and no acceptance of a final metric that is itself outside the supported `double` range.
- No manual GitHub Actions cancel/rerun and no force-push.

## Implemented result

- `PolylineMetrics.SignedArea(...)` now keeps the normal translated direct path and falls back to globally scaled normalized accumulation when a raw determinant or sum cannot be represented. The `0.5` area factor is applied before restoring coordinate scale, so a representable final area is not rejected merely because twice-area is too large.
- Polygon validation now uses the same robust signed-area result while preserving the previous zero-area threshold semantics (`Epsilon * 0.5`).
- Orientation predicates preserve sign using scaled normalized arithmetic and saturate only predicate magnitude to `±double.MaxValue` when the true determinant magnitude exceeds representable range; non-finite input remains rejected.
- The focused self-registering smoke covers an extreme concave polygon with area about `9.975e307`, reversed winding, public scanline clipping, an ordinary square, and a truly non-representable area that must still throw `OverflowException`.

## Integration / validation evidence

- PR `#1381` targeted the dedicated integration branch, not `main`; GitHub reported it mergeable before integration.
- PR `#1381` merged successfully into the integration branch at exact candidate SHA `d761529fd0af7e803af755442ace46d7049b0b3a`.
- The integration merge has first parent `b2e7aaa08bb8b0b817058005eba4cf53fbf3360b` (fresh main snapshot at integration creation) and second parent `0c5769b73a52caa0f003ed8ec1529de79ff50b13` (implementation head), preserving concurrent main work.
- Exact integration diff is limited to `PolylineMetrics.cs`, `PolygonScanlineClipper.cs`, and `AdvancedGeometryRepresentableAreaOverflowSmoke.cs`.
- No commit status or PR-triggered workflow was observed for implementation head `0c5769b73a52caa0f003ed8ec1529de79ff50b13`; no CI PASS is claimed for that head.
- A local clone/test run could not be executed from the available sandbox because outbound DNS could not resolve `github.com`; therefore no local `dotnet` PASS is manufactured.
- Licensed BricsCAD runtime evidence is not required for this pure Core geometry source lane and remains outside this claim.

## Current landing boundary

The implementation is integrated and review-ready but is **not yet source-landed on `main`**. Repository/owner policy explicitly does not treat `continue`, `continue all`, `fix bug`, or `implement all` as authorization to change `main` source. A final PR may be prepared against `main`, but merging it requires explicit owner authorization for the main landing. Until that landing, exact-main readback, automatic post-integration V25 cloud CI, and final close-out remain pending and this claim stays `ACTIVE`.

## Completion condition

After explicit owner authorization, refresh current `main`, reconcile the integration candidate if needed, perform the one controlled final source landing, verify exact current-main ancestry/tree, observe the automatic exact-SHA CI evidence actually produced, record the result, and then mark this claim `COMPLETED`. Licensed BricsCAD runtime evidence is out of scope for this pure Core geometry lane.
