# Agent Work Claim — BulkEdit property freshness policy parity

- Agent: `chatgpt-gpt56sol-bulk-property-freshness-policy`
- Owner: OpenAI ChatGPT
- Status: `COMPLETED`
- Registered: 2026-08-12 09:00 +07:00
- Completed: 2026-08-12 09:07 +07:00
- Baseline main SHA observed: `ecf86e34009ffd962ed75752db02fa85af8926af`
- Claim commit: `2d3c80f36db9bcf07d5eb6778b67ca4156852a99`
- Source branch commit: `cbae4ef7849423545dfa170a6112be63678e9ccd`
- Regression source commit: `b9acd6b953c6da18613ff53eb453acb304343f8a`
- PR: `#673`
- Squash merge on `main`: `f2b838b66220445508dca99146315f65c644b517`
- Post-merge source blob: `1684108c3b922303040957a459e7b648a7e4ccdc`
- Post-merge smoke blob: `2e515752f731bcf957cdf6dd29f465c0e1e39083`
- Task key: `CORE-BULK-PROPERTY-FRESHNESS-POLICY`

## Defect closed

`BulkEditService.SetProperty(...)` and `MultiplyNumericProperty(...)` bypassed the canonical `ProjectElement.SetProperty(...)` mutation policy by assigning into `Properties` directly and then marking `Properties | Quantity | optional Geometry` dirty. Because `ProjectElement.MarkDirty(...)` treats any Properties dirty bit as generated-output stale, real bulk edits of ordinary properties such as `Scale` incorrectly staled generated output even though `ElementGeometryPolicy.AffectsGeneratedOutput(...)` is false.

## Implemented

- Both committed bulk string and numeric property updates now route through `ProjectElement.SetProperty(...)`.
- Ordinary properties retain Properties/Quantity dirtiness without generated-output staleness.
- Generated-output-only properties such as `Material` stale generated output without Geometry dirty.
- Geometry properties such as `WidthM` retain Geometry dirty plus generated-output staleness.
- Target ownership/enumeration freshness, editable-key validation, numeric parse/non-finite/overflow handling, exact numeric no-op semantics, rollback, changed-element reporting and Family assignment behavior are unchanged.

## Regression source

`tests/QS3D.Core.SmokeTests/BulkEditPropertyFreshnessPolicySmoke.cs` covers:

1. ordinary string property freshness;
2. ordinary numeric property freshness;
3. generated-output-only Material freshness;
4. geometry Width freshness.

The smoke source was committed and read back from `main`, but no executable smoke/build PASS is claimed from this connector session.

## Merge/readback evidence

- Before PR creation, moving-main compare contained exactly the reserved source file plus the new smoke file.
- Current-main `BulkEditService.cs` blob remained `13f4ff2e691545c64634eac99f8d60f826026dc4` through the final overlap check, so concurrent work had not modified the reserved source.
- PR #673 head was `b9acd6b953c6da18613ff53eb453acb304343f8a`; combined status contained no failure statuses.
- Raw GitHub PR state reported `mergeable=null`, `mergeable_state=unknown`, not a confirmed conflict.
- PR #673 was squash-merged using the expected head SHA as `f2b838b66220445508dca99146315f65c644b517`.
- Post-merge readback from `main` confirmed both bulk property commit paths call `SetProperty(...)`, and the focused smoke is present.

No GitHub Actions/build/release was dispatched. No BricsCAD V25/V26 runtime PASS is claimed remotely.
