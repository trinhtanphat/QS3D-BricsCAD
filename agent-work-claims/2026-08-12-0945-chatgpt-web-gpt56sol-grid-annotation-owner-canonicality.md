# Work claim — Generated Grid Annotation owner canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-owner-canonicality`
- Registered: `2026-08-12T09:45:00+07:00`
- Completed: `2026-08-12T09:48:00+07:00`
- Baseline main SHA: `2d59c7e11f156387b452e86077a23a6f0f8a8db0`
- Priority: P1 — generated Grid Annotation ownership metadata must match exact writer-owned semantic identities.
- Task Key: `CORE-GRID-ANNOTATION-OWNER-CANONICALITY`

## Confirmed defect

`GridAnnotationBuilder.ReplaceOne(...)` writes exact `GeneratedGridAnnotationOwnerProjectId = project.ProjectId`, `GeneratedGridAnnotationOwnerElementId = element.Id`, and `GeneratedGridAnnotationOwnershipVersion = "1"`. `GeneratedGridAnnotationHealthService.ValidateOwner(...)` previously normalized all three values before compare, allowing padded/case-varied aliases to pass ownership health.

## Implemented

- Claim: `871b5ec6c7b169c111de462ea03839b801876688`
- Branch source: `23ec9e83591a55490e33595eb0db5a94bbdb6427`
- Branch smoke / reviewed PR head: `87ee52b721cf4bc46de6761403551e5716f85840`
- PR: `#709`
- Squash merge on `main`: `f5927ebb349f4ecd85ae9173f6e2a3d51e3e0833`

`GeneratedGridAnnotationHealthService` now preserves raw owner/version strings long enough to emit dedicated canonicality errors for aliases of the correct writer-owned value, while the existing version/project/element mismatch checks continue to use normalized values.

## Regression coverage

`GeneratedGridAnnotationOwnerCanonicalitySmoke` covers padded ownership version, case-varied project owner, padded element owner, padded wrong-project mismatch preservation, and exact canonical controls.

## Validation

- Read back current provider and focused smoke from merged `main`.
- Compared squash merge `f5927ebb349f4ecd85ae9173f6e2a3d51e3e0833` to later `main` `5f426b1fc3a5e3b029269ce98ab1cbc814fda418`: status `ahead`, `ahead_by=1`, `behind_by=0`, merge base exactly the squash commit; later change was unrelated.
- No GitHub Actions workflow was dispatched. No full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote lane.
