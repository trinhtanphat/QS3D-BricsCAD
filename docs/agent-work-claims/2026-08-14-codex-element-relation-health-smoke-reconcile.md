# Work claim — ProjectElement relation health smoke reconciliation

- Status: `ACTIVE`
- Agent: `codex-element-relation-health-smoke-reconcile-20260814` (`/root/fix_source_reconcile_desync`)
- Registered: `2026-08-14T15:35:00+07:00`
- Baseline main SHA: `d15271a3b1f46aacaf1dcd2ee81dc35f93b8901e`
- Priority: current deterministic Core full-smoke blocker after completed relation persistability work

## Confirmed fixture drift

Completed source `1ea5bc9a0700dc5376cc5ed20097784fff5e4802` routes `ProjectElement.FamilyId`, `FloorId`, and `ZoneId` through `NormalizeOptionalRelationId`. Supported constructor and setter writes now trim padded values, turn whitespace-only input into the empty optional relation, and reject control characters. The four negative cases in `ModelHealthElementRelationCanonicalitySmoke` still expect padded or whitespace-only text to remain stored and produce relation-canonicality diagnostics, so the first module initializer fails before the rest of the full smoke can run.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ModelHealthElementRelationCanonicalitySmoke.cs`
- this claim only

Reconcile the smoke with the completed writer contract: assert padded supported writes persist canonical Family/Floor/Zone values without canonicality errors, and assert whitespace-only Family input persists as the empty optional relation while retaining the reachable `MISSING_FAMILY` diagnostic. Keep the canonical control.

Existing reachable coverage remains unchanged: missing relation diagnostics, ambiguous Family/Floor/Zone diagnostics in `ModelHealthIdentityAmbiguitySmoke`, and padded/case-variant `HOST_REFERENCE_NON_CANONICAL` diagnostics in `ModelHealthHostWallCanonicalitySmoke`.

## Explicit exclusions

- no production source, persistence/schema, health policy or relation-writer changes;
- no LOCAL runner/probe/docs, issue `#1005`, BricsCAD/native/private data, GitHub Actions, release or packaging work;
- no edits to ambiguity or HostWall canonicality fixtures unless the full smoke exposes a separate blocker, which must be reported rather than absorbed.

## Validation

- Core Release build and full deterministic Core smoke;
- focused model-health identity-ambiguity, composite and host-link canonicalization gates;
- existing `ProjectElementRelationPersistabilitySmoke` through the full registered smoke run;
- generic/manual-only source policy gates as appropriate.

If the full smoke advances to another unrelated failure, record that exact first blocker without expanding this claim.
