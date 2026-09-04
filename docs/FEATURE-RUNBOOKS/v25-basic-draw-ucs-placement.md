# V25 Basic Draw UCS placement

## Scope

`QS3DDRAWLINE`, `QS3DDRAWRECT`, and `QS3DDRAWCIRCLE` are native drafting helpers. They capture one active UCS at prompt start, accept host prompt coordinates, verify that the active document/project/Family/Floor/Zone/UCS context is still fresh, then publish one operation-owned native entity with `QS3DBASICDRAW` XData.

## Coordinate contract

BricsCAD point prompts are treated as host/world coordinates. Before constructing UCS-local line/rectangle/circle geometry, each prompted point is transformed through `promptUcs.Inverse()`. The resulting UCS-local entity is then transformed exactly once through the already-captured `promptUcs` immediately before native append. This keeps world-UCS behavior unchanged while preventing translated/rotated UCS coordinates from being transformed twice.

Rectangle min/max is evaluated only after both prompt points are normalized into the captured UCS, so its edges remain aligned to that UCS. Circle uses UCS-local `Vector3d.ZAxis`; the publication transform carries that normal into the captured UCS plane.

## Fail-closed freshness

Before native mutation the command must still prove: same active `Document`, ModelSpace, unchanged `CurrentUserCoordinateSystem`, same project identity and `ChangeVersion`, same active Family/category, and same active Floor/Zone. Native append remains inside the existing document lock and transaction, and XData ownership is unchanged.

## Remote validation

Run `python scripts/preflight-v25-basic-draw-ucs-placement.py`, aggregate feature guards, Core smoke, trusted BricsCAD V25 references, and the V25 plugin build. Hosted/source evidence is not licensed native placement evidence.

## LOCAL_ONLY follow-up

When a licensed interactive host is available, exercise world UCS plus at least one rotated and one translated+rotated UCS for all three commands, verify visible placement/orientation against physical picks, cancellation, active-UCS drift refusal, XData ownership, Undo/Redo, save/cold reopen, and zero cross-DWG mutation. Record that separately as exact-SHA runtime evidence; do not label hosted CI as `LOCAL_PASS`.
