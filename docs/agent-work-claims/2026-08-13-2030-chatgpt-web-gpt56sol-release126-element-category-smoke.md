# Work claim — release #126 ElementVerticalPlacement smoke compile repair

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T20:30:00+07:00`
- Baseline main SHA: `e51d19df145f576b9f3f2e12a68d01fa926076c4`
- Priority: P0 fresh V25 release #126 compile failure

## Confirmed defect

V25 release run #126 (`31704288719`), job `94460816959`, fails Core smoke-test compilation because `tests/QS3D.Core.SmokeTests/ElementVerticalPlacementSignedZeroSmoke.cs` references nonexistent `ElementCategory.Wall` twice. The production enum intentionally exposes concrete wall categories such as `ArchitecturalWall` and does not define a generic `Wall` member. `ElementVerticalPlacementService` does not branch on element category, so category is only fixture identity in this signed-zero regression.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ElementVerticalPlacementSignedZeroSmoke.cs`
- this claim file for closeout

## Read-only reference

- `src/QS3D.Core/Domain/ElementCategory.cs`
- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs`

## Excluded scope

- no production enum/API expansion
- no unrelated signed-zero or RevisionMath changes
- no BricsCAD native/local qualification
- no workflow-trigger, packaging, release-version or updater changes

## Intended fix

Use the existing canonical `ElementCategory.ArchitecturalWall` fixture category at both invalid `ElementCategory.Wall` call sites while preserving all signed-zero assertions unchanged.

## Validation plan

- exact GitHub source readback after the test-only patch
- inspect fresh V25 release CI after the source commit
- if the next fresh run exposes another source failure, handle it in a separate bounded repair lane rather than broadening this claim
