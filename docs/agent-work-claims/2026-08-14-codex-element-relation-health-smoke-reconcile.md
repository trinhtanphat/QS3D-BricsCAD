# Work claim — ProjectElement relation health smoke reconciliation

- Status: `COMPLETED`
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

## Completion record

- Claim PR `#1217` merged as `089ce72b144078df6fe3e1b1fa938ba4f1808ad6`.
- Test commit `8e0330ad6` merged through PR `#1218` as `f40774eb4b234972e3b8e2f3cda48157d8a011e2`.
- The one reserved smoke now asserts supported padded writes persist canonical Family/Floor/Zone values without canonicality diagnostics. Whitespace-only Family input persists as the empty optional relation and retains `MISSING_FAMILY`. Production and every existing ambiguity/HostWall canonicality fixture remain unchanged.
- Core Release build PASS with `0 warnings / 0 errors`. Focused model-health identity ambiguity, comprehensive model-health and host-link canonicalization gates PASS; generic and manual-only policy gates PASS.
- Full Core smoke advances beyond `ModelHealthElementRelationCanonicalitySmoke` and stops at the next independent fixture drift: `ProjectBrowserQueryReferenceCanonicalitySmoke` still expects a padded FamilyId setter value to survive and be rejected, while the completed relation setter now normalizes it before the query planner reads it. This lane did not edit or absorb that blocker.
- No production, LOCAL, issue `#1005`, BricsCAD/native/private data or GitHub Actions surfaces were changed/run.
