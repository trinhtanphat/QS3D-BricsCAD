# Work claim — Generated Solid semantic ownership canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-generated-solid-ownership-canonicality`
- Registered: `2026-08-12T09:02:00+07:00`
- Baseline main SHA: `5954521393578aea948fa3987c470695abfee8eb`
- Priority: P1 — persisted Generated Solid ownership metadata must match the exact writer-owned semantic contract.
- Task Key: `CORE-MODEL-HEALTH-GENERATED-SOLID-OWNERSHIP-CANONICALITY`

## Confirmed defect

`GeneratedGeometryService.CommitReplacement(...)` writes exact `GeneratedSolidOwnershipVersion = "1"`, `GeneratedSolidOwnerProjectId = project.ProjectId` and `GeneratedSolidOwnerElementId = element.Id`. Baseline `ModelHealthService.ValidateGeneratedGeometry(...)` currently trims all three stored values before comparison. Malformed or externally edited persisted values such as `" 1 "`, a padded project id, or a padded element id can therefore pass ownership health even though the canonical writer never emits those spellings.

## Non-overlap check

Recent generated-solid work covers native runtime provider isolation, null-element health and native XData ownership. Existing relation-canonicality work covers Family/Floor/Zone relations only. Recent claim history showed no active or completed lane dedicated to canonical spelling of these three semantic Generated Solid ownership properties.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- one focused Core smoke regression for semantic Generated Solid ownership spelling
- this claim file

Do not modify native XData ownership, `GeneratedGeometryService`, generated handle semantics, category metadata, builders, persistence format or BricsCAD runtime code.

## Intended contract

- Padded/non-writer-canonical ownership version, project id and element id fail visible as dedicated `HealthSeverity.Error` diagnostics.
- Existing version/project/element mismatch checks continue to run against normalized values, so canonicality evidence does not hide an actual ownership mismatch.
- Exact writer-owned values preserve existing health behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Malformed semantic Generated Solid ownership spellings are fail-visible, focused Core smoke coverage pins version/project/element padded cases plus canonical control behavior, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
