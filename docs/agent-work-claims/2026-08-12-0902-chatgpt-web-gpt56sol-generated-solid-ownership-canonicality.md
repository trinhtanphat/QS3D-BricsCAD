# Work claim — Generated Solid semantic ownership canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-generated-solid-ownership-canonicality`
- Registered: `2026-08-12T09:02:00+07:00`
- Completed: `2026-08-12T09:04:00+07:00`
- Baseline main SHA: `5954521393578aea948fa3987c470695abfee8eb`
- Priority: P1 — persisted Generated Solid ownership metadata must match the exact writer-owned semantic contract.
- Task Key: `CORE-MODEL-HEALTH-GENERATED-SOLID-OWNERSHIP-CANONICALITY`

## Confirmed defect

`GeneratedGeometryService.CommitReplacement(...)` writes exact `GeneratedSolidOwnershipVersion = "1"`, `GeneratedSolidOwnerProjectId = project.ProjectId` and `GeneratedSolidOwnerElementId = element.Id`. Baseline `ModelHealthService.ValidateGeneratedGeometry(...)` trimmed all three stored values before comparison. Malformed or externally edited persisted values such as `" 1 "`, a padded project id, or a padded element id could therefore pass ownership health even though the canonical writer never emits those spellings.

## Implemented

- Claim: `681ba0b4462ccee0d881282aefeecd3a1b787737`
- Source fix: `bd5a2bd242ddc924fd68c84867492e96d0e96ccd`
- Focused smoke regression: `67878b2278aeb0e82b33490dde8f4e4278e3e52c`

`ModelHealthService.ValidateGeneratedGeometry(...)` now emits dedicated `HealthSeverity.Error` diagnostics for non-canonical surrounding whitespace on ownership version, project owner id and element owner id. Existing version/project/element mismatch checks continue to run against normalized values, so malformed spelling cannot hide an actual ownership mismatch.

## Preserved scope

No edits were made to native XData ownership, `GeneratedGeometryService`, generated handle semantics, category metadata, builders, persistence format or BricsCAD runtime code.

## Validation

- Read back current `ModelHealthService.cs` from merged `main`; the three canonicality diagnostics and normalized mismatch checks are present.
- Read back `ModelHealthGeneratedSolidOwnershipCanonicalitySmoke.cs`; it pins padded version/project/element aliases, a padded wrong-project case that must retain mismatch evidence, and canonical control behavior.
- Compared smoke commit `67878b2278aeb0e82b33490dde8f4e4278e3e52c` to merged `main` `f2b838b66220445508dca99146315f65c644b517`: status `ahead`, `ahead_by=9`, `behind_by=0`, merge base exactly the smoke commit. Later changes do not touch this lane's source or smoke.
- No GitHub Actions workflow was dispatched. No full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote lane.
