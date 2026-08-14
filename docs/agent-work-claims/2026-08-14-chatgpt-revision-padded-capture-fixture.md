# Agent work claim — Revision padded capture fixture

- Agent: `chatgpt-20260814-revision-padded-capture-fixture`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `a58e71241f6910d66ceccb8e331d78231bd8f48e`

## Scope

Fresh V25 release #179 (`31800005494`, job `94765535555`) fails `Deterministic Core smoke tests` in `RevisionRegressionSmoke.CaptureRejectsPaddedReferenceIds()`: the fixture expects `RevisionService.Capture()` to throw for padded Family/Floor/Zone IDs, but `ProjectElement` public relation setters now canonicalize those IDs with `Trim()` before Capture runs.

Production `RevisionService.Capture()` still validates optional relation identity fail-closed. The repository already uses reflection in `RevisionCaptureXmlTextIntegritySmoke` to inject persisted/raw invalid relation state when validating the Capture boundary, so this lane aligns the stale regression fixture with that established pattern rather than weakening production validation.

Reserved implementation surface:

- `tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs`

## Validation

- Keep the Capture rejection assertions intact.
- Create canonical elements through public APIs, then inject padded `_familyId`, `_floorId`, `_zoneId` raw fields via a narrow reflection helper before calling `RevisionService.Capture()`.
- Do not modify `RevisionService`, `ProjectElement`, workflow gates, or other tests.
- Land through `agent/*` -> fresh `integration/*` -> `main`; no direct source push or force-push.
- Require fresh V25 exact-SHA/descendant CI before calling this blocker fixed.
