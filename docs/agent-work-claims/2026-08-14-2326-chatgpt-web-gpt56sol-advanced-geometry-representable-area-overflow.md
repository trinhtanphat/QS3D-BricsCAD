# Agent work claim — Advanced geometry representable-area overflow hardening

- Agent: `chatgpt-web-gpt56sol-advanced-geometry-overflow`
- Date: `2026-08-14`
- Status: `ACTIVE`
- Baseline main SHA: `86c7ac72ad976807a5081977453940d10311aa8b`
- Claim publication SHA: `1d264de37aad4a0540a586a7dd62bbbbbd413299`
- Implementation branch: `agent/chatgpt-web-gpt56sol/advanced-geometry-representable-area-20260814`
- Planned integration branch: `integration/chatgpt-web-gpt56sol-advanced-geometry-representable-area-20260814`
- Priority: Core advanced geometry / numerical edge cases

## Confirmed defect

A finite, valid simple concave polygon can have a signed area that is representable as `double` while one or more translated cross products, or the accumulated twice-area, exceed `double.MaxValue`. Current `PolylineMetrics.SignedArea(...)` restores each cross product before applying the final `0.5`, while `PolygonScanlineClipper.NormalizeAndValidate(...)` accumulates raw twice-area and polygon orientation requires a fully representable determinant. These paths therefore reject some valid finite geometry because of intermediate overflow even when the final area or predicate result is well-defined.

## Reserved scope

- `src/QS3D.Core/Geometry/PolylineMetrics.cs` — make signed-area accumulation scale-aware so only a truly unrepresentable final area fails closed.
- `src/QS3D.Core/Geometry/PolygonScanlineClipper.cs` — reuse representable signed-area semantics for validation and make orientation predicates sign-safe when determinant magnitude itself exceeds `double`.
- new self-registering `tests/QS3D.Core.SmokeTests/AdvancedGeometryRepresentableAreaOverflowSmoke.cs` — clockwise/counter-clockwise signed-area and end-to-end scanline regression for an extreme finite concave polygon.
- this claim for coordination/close-out.

## Explicit exclusions / concurrency protection

- No BricsCAD native `Document`, `Database`, entity, transaction, editor or licensed-runtime changes.
- No changes to region-set ownership, bulge tessellation, rebar, quantity, domain persistence, UI, CI/package, release/signing or unrelated geometry surfaces.
- No `SmokeTestRegistration.cs` change; regression must use the repository's current self-registration pattern.
- No weakening of non-finite input rejection and no acceptance of a final metric that is itself outside the supported `double` range.
- No manual GitHub Actions cancel/rerun and no force-push.

## Validation plan

- prove a finite concave polygon with area about `9.975e307` returns finite positive signed area rather than overflowing;
- reverse winding and prove equal-magnitude negative signed area within a tight relative tolerance;
- run the same polygon through public scanline clipping to prove validation/orientation no longer reject it because of intermediate determinant overflow;
- retain existing zero-area/self-intersection/non-finite/true-overflow behavior;
- review exact branch diff, reconcile current `main`, perform integration landing with `force:false`, read back source/test, and report CI only as actually observed.

## Completion condition

Claim-first reservation, focused implementation and regression on the post-claim branch, reconciliation against current `main`, successful relevant remote validation/CI where available, one controlled integration landing, exact-SHA readback/ancestry, and claim close-out are recorded. Licensed BricsCAD runtime evidence is out of scope for this pure Core geometry lane.
