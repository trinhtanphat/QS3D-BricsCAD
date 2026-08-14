# Work claim — Quantity report FamilyId identity parity

- Status: `COMPLETED`
- Agent: `gpt56sol-rev-familyid-parity-20260814-0804`
- Registered: `2026-08-14T08:04:00+07:00`
- Baseline main SHA: `a82b3c993579d00643bfdad862a4cd6d6610a582`
- Priority: REV/report correctness — prevent false BQ revision changes from identity casing only.

## Confirmed defect

`RevisionService.Compare()` treats `FamilyId` as semantic identity with `StringComparison.OrdinalIgnoreCase`, but `QuantityReportRevisionService.ChangedFields()` routed report-row `FamilyId` through the exact ordinal text comparison used for descriptive fields. A case-only representation change of the same Family identity could therefore create a `Changed` BQ revision row even when the authoritative semantic identity comparison reported no FamilyId change.

## Reserved scope

- `src/QS3D.Core/Revisions/QuantityReportRevisionReview.cs`
- `tests/QS3D.Core.SmokeTests/QuantityReportRevisionReviewSmoke.cs`
- this claim file only

## Implemented

- Regression commit `696c0ca50664a32846bb49a745266498ca394d68` adds a focused case-only Family identity scenario and pins the real different-Family path to `FamilyId`.
- Source commit `87c247827913f5dd252707df237ba6ced47fa7fa` routes only `FamilyId` through case-insensitive identity comparison, matching `RevisionService`.
- Exact ordinal comparison remains unchanged for Floor, Zone, Category, FamilyName, ElementName, Material and Note; quantity tolerance and stable-key behavior are unchanged.

## Validation performed

- Remote commit diff readback: source change is limited to `Add` -> `AddIdentity` for `FamilyId` plus the case-insensitive helper (`+6/-1`).
- Remote regression diff readback confirms casing-only and genuine identity-change coverage.
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` includes `QuantityReportRevisionReviewSmoke.Run()`.
- Final lineage check after source push confirmed current `main` remained ahead of `87c247827913f5dd252707df237ba6ced47fa7fa` with no intervening changes to either reserved revision file.
- GitHub combined status for source SHA reports no attached statuses/checks (`total_count = 0`). No GitHub Actions were dispatched.
- Managed Core smoke/build and licensed BricsCAD native runtime were not executed in this connector-only environment and are not claimed as PASS.

## Excluded scope preserved

- no `ProjectFamilyService` / Family assignment edits
- no SourceHandle / Source Reconcile / LOCAL_ONLY native qualification edits
- no MAP/QSDB persistence, IFC/interchange, Cost, V25/V26 UI or release automation edits
- no normalization of descriptive report text

## Completion

`COMPLETED`: claim-first reservation, focused regression, minimal source fix, remote diff/readback, lineage verification and explicit validation boundary are all recorded on `main`.
